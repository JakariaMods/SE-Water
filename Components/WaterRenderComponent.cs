using Jakaria.SessionComponents;
using Jakaria.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using VRageRender;
using static Jakaria.Components.WaterRenderComponent;

namespace Jakaria.Components
{
    /// <summary>
    /// Component that handles rendering for <see cref="WaterComponent"/>
    /// </summary>
    public class WaterRenderComponent : MyEntityComponentBase
    {
        public const int MIN_DEPTH = 4;

        public Vector3D LocalCameraPosition;
        public double DistanceToHorizon;
        public double CameraAltitude;

        private RenderFace[] _renderFaces = new RenderFace[6];

        private Vector3D _lastLODBuildPositionLocal;
        
        public WaterComponent Water;

        private WaterRenderSessionComponent _renderSessionComponent;
        private WaterSettingsComponent _settingsComponent;

        public override string ComponentTypeDebugString => nameof(WaterRenderComponent);

        public WaterRenderComponent()
        {
        }

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();

            _renderSessionComponent = Session.Instance.Get<WaterRenderSessionComponent>();
            _settingsComponent = Session.Instance.Get<WaterSettingsComponent>();

            Water = Entity.Components.Get<WaterComponent>();

            RebuildLOD();
        }

        public static void GetLeafs(RenderFace.RenderChunk chunk, List<RenderFace.RenderChunk> leafs)
        {
            if(chunk.Children == null)
            {
                leafs.Add(chunk);
                return;
            }

            foreach (var child in chunk.Children)
            {
                GetLeafs(child, leafs);
            }
        }

        /// <summary>
        /// Calculates the maximum possible number of leafs for a given depth in a quadtree
        /// </summary>
        private static int GetMaxPossibleLeafs(int depth)
        {
            int sum = 1;

            for (int i = 0; i < depth; i++)
            {
                sum *= 4;
            }
            
            return sum;
        }

        /// <summary>
        /// Updates the level of detail for the water surface
        /// </summary>
        public void UpdateLOD()
        {
            foreach (var face in _renderFaces)
            {
                face.UpdateLOD();
            }
        }

        /// <summary>
        /// Completely rebuilds render surface from scratch
        /// </summary>
        public void RebuildLOD()
        {
            _lastLODBuildPositionLocal = Vector3D.MaxValue;
            for (int i = 0; i < _renderFaces.Length; i++)
            {
                _renderFaces[i] = new RenderFace(this, Base6Directions.Directions[i]);
            }
        }

        public void Draw()
        {
            if (!Water.InScene)
                return;

            LocalCameraPosition = Vector3D.Transform(_renderSessionComponent.CameraPosition, ref Water.WorldMatrixInv);
            CameraAltitude = Water.GetDepthLocal(ref LocalCameraPosition);
            DistanceToHorizon = CameraAltitude < 0 ? 250 : Math.Max(Math.Sqrt((Math.Pow(Math.Max(CameraAltitude, 25) + Water.Radius, 2)) - (Water.Radius * Water.Radius)), 500);

            if (double.IsPositiveInfinity(CameraAltitude) || double.IsNegativeInfinity(CameraAltitude))
            {
                Vector3 local = (Vector3)LocalCameraPosition;
                CameraAltitude = Vector3D.Distance(Water.Planet.GetClosestSurfacePointLocal(ref local), LocalCameraPosition);
            }
            
            if (Vector3D.DistanceSquared(LocalCameraPosition, _lastLODBuildPositionLocal) >= 15)
            {
                UpdateLOD();
                _lastLODBuildPositionLocal = LocalCameraPosition;
            }

            MyAPIGateway.Parallel.ForEach(_renderFaces, (face) =>
            {
                face.Draw();
            });
        }

        public class RenderFace
        {
            private WaterRenderComponent _renderComponent;
            private Vector3D _direction;
            private Vector3D _perpendicularA;
            private Vector3D _perpendicularB;

            private List<RenderChunk> _maxDepthCache = new List<RenderChunk>();
            private List<RenderChunk> _minNodeCache = new List<RenderChunk>();

            public RenderChunk Chunk;
            public List<MyBillboard> BillboardCache = new List<MyBillboard>();

            public RenderFace(WaterRenderComponent renderComponent, Vector3 direction)
            {
                _renderComponent = renderComponent;
                _direction = direction;
                _perpendicularA = new Vector3D(_direction.Y, _direction.Z, _direction.X);
                _perpendicularB = _direction.Cross(_perpendicularA);

                _minNodeCache.EnsureCapacity(GetMaxPossibleLeafs(MIN_DEPTH));
                Chunk = new RenderChunk(_renderComponent, 0, _renderComponent.Water.Radius, this, _direction * _renderComponent.Water.Radius);

                GetLeafs(Chunk, _minNodeCache);
            }

            public void UpdateLOD()
            {
                _maxDepthCache.Clear();

                foreach (var chunk in _minNodeCache)
                {
                    chunk.UpdateLOD(_maxDepthCache);
                }
            }

            public void Draw()
            {
                BillboardCache.Clear();
                foreach (var chunk in _maxDepthCache)
                {
                    chunk.Draw();
                }

                MyTransparentGeometry.AddBillboards(BillboardCache, false);
            }

            public class RenderChunk
            {
                public RenderChunk[] Children;

                private int _depth;
                private int _textureId;
                private double _radius;
                private Vector3D _localPosition;
                private RenderFace _renderFace;
                private double? _altitude;

                private readonly WaterRenderComponent _renderComponent;

                private MyBillboard _topBillboard;
                private MyBillboard _bottomBillboard;
                private MyBillboard _seaFoamBillboard;

                public RenderChunk(WaterRenderComponent renderComponent, int depth, double radius, RenderFace renderFace, Vector3D localPosition)
                {
                    _renderComponent = renderComponent;
                    _depth = depth;
                    _radius = radius;
                    _renderFace = renderFace;
                    _localPosition = localPosition;

                    if(_depth < MIN_DEPTH)
                    {
                        Split();
                    }
                }

                private void Split()
                {
                    double halfRadius = _radius / 2;
                    int detailPlusOne = _depth + 1;

                    Vector3D axisAPremultiplied = _renderFace._perpendicularA * halfRadius;
                    Vector3D axisBPremultiplied = _renderFace._perpendicularB * halfRadius;

                    Children = new RenderChunk[4]
                    {
                            new RenderChunk(_renderComponent, detailPlusOne, halfRadius, _renderFace, _localPosition + (-axisAPremultiplied + -axisBPremultiplied)),
                            new RenderChunk(_renderComponent, detailPlusOne, halfRadius, _renderFace, _localPosition + (-axisAPremultiplied + axisBPremultiplied)),
                            new RenderChunk(_renderComponent, detailPlusOne, halfRadius, _renderFace, _localPosition + (axisAPremultiplied + -axisBPremultiplied)),
                            new RenderChunk(_renderComponent, detailPlusOne, halfRadius, _renderFace, _localPosition + (axisAPremultiplied + axisBPremultiplied))
                    };
                }

                public void UpdateLOD(List<RenderChunk> chunks)
                {
                    if (_radius > WaterData.MinWaterSplitRadius && ((Vector3D.Normalize(_localPosition) * _renderComponent.Water.Settings.Radius * _renderComponent.Water.Planet.MinimumRadius) - _renderComponent.LocalCameraPosition).AbsMax() < _radius * (3f + (_renderComponent._settingsComponent.Settings.Quality * 2)))
                    {
                        if(Children == null)
                        {
                            Split();
                        }
                    }
                    else
                    {
                        if (Children != null)
                            Children = null;

                        _textureId = Math.Abs(((int)(_localPosition.X + _localPosition.Y + _localPosition.Z) / 20) % 4);
                    }

                    if(Children == null)
                    {
                        chunks.Add(this);
                    }
                    else
                    {
                        foreach (var child in Children)
                        {
                            child.UpdateLOD(chunks);
                        }
                    }
                }

                public void Draw()
                {
                    if (!_renderComponent.Water.InScene)
                        return;

                    if (Children == null)
                    {
                        if (_radius > 8)
                        {
                            Vector3D localPosition = Vector3D.Normalize(_localPosition) * _renderComponent.Water.Radius;

                            BoundingSphereD worldSphere = new BoundingSphereD
                            {
                                Center = Vector3D.Transform(localPosition, _renderComponent.Entity.WorldMatrix),
                                Radius = _radius,
                            };

                            if (Vector3D.DistanceSquared(localPosition, _renderComponent.LocalCameraPosition) > _renderComponent.DistanceToHorizon * _renderComponent.DistanceToHorizon)
                                return;
                        }

                        Vector3D normal1 = Vector3D.Normalize(_localPosition + ((-_renderFace._perpendicularA + -_renderFace._perpendicularB) * _radius));
                        Vector3D normal2 = Vector3D.Normalize(_localPosition + ((_renderFace._perpendicularA + _renderFace._perpendicularB) * _radius));
                        Vector3D normal3 = Vector3D.Normalize(_localPosition + ((-_renderFace._perpendicularA + _renderFace._perpendicularB) * _radius));
                        Vector3D normal4 = Vector3D.Normalize(_localPosition + ((_renderFace._perpendicularA + -_renderFace._perpendicularB) * _radius));

                        double depth1;
                        double depth2;
                        double depth3;
                        double depth4;

                        MyQuadD quad = new MyQuadD()
                        {
                            Point0 = Vector3D.Transform(_renderComponent.Water.GetClosestSurfacePointFromNormalLocal(ref normal1, out depth1), _renderComponent.Water.WorldMatrix),
                            Point1 = Vector3D.Transform(_renderComponent.Water.GetClosestSurfacePointFromNormalLocal(ref normal3, out depth2), ref _renderComponent.Water.WorldMatrix),
                            Point2 = Vector3D.Transform(_renderComponent.Water.GetClosestSurfacePointFromNormalLocal(ref normal2, out depth3), ref _renderComponent.Water.WorldMatrix),
                            Point3 = Vector3D.Transform(_renderComponent.Water.GetClosestSurfacePointFromNormalLocal(ref normal4, out depth4), ref _renderComponent.Water.WorldMatrix)
                        };

                        if (_altitude == null && _renderComponent.Water.Settings.Transparent)
                            _altitude = (float)WaterUtils.GetAltitude(_renderComponent.Water.Planet, quad.Point0);

                        if (_altitude != null)
                        {
                            _altitude = Math.Max(_altitude.Value, 0);
                        }

                        if (_renderComponent.Water.Settings.Volumetric)
                        {
                            int depths = 0;
                            double averageDepth = 0;
                            if (depth1 > 0)
                            {
                                averageDepth += Vector3D.DistanceSquared(_renderComponent.Water.WorldMatrix.Translation, quad.Point0);
                                depths++;
                            }
                            
                            if (depth2 > 0)
                            {
                                averageDepth += Vector3D.DistanceSquared(_renderComponent.Water.WorldMatrix.Translation, quad.Point1);
                                depths++;
                            }
                            if (depth3 > 0)
                            {
                                averageDepth += Vector3D.DistanceSquared(_renderComponent.Water.WorldMatrix.Translation, quad.Point2);
                                depths++;
                            }
                            if (depth4 > 0)
                            {
                                averageDepth += Vector3D.DistanceSquared(_renderComponent.Water.WorldMatrix.Translation, quad.Point3);
                                depths++;
                            }
                            if (depths == 0)
                                return;

                            averageDepth = Math.Sqrt(averageDepth / depths);

                            if (depth1 == 0)
                                quad.Point0 = Vector3D.Transform(normal1 * averageDepth, ref _renderComponent.Water.WorldMatrix);
                            if (depth2 == 0)
                                quad.Point1 = Vector3D.Transform(normal3 * averageDepth, ref _renderComponent.Water.WorldMatrix);
                            if (depth3 == 0)
                                quad.Point2 = Vector3D.Transform(normal2 * averageDepth, ref _renderComponent.Water.WorldMatrix);
                            if (depth4 == 0)
                                quad.Point3 = Vector3D.Transform(normal4 * averageDepth, ref _renderComponent.Water.WorldMatrix);
                        }

                        Vector3 quadNormal;
                        if (_renderComponent.Water.Settings.WaveHeight == 0)
                            quadNormal = (Vector3)normal1;
                        else
                            quadNormal = Vector3.Normalize((Vector3)Vector3D.Cross(quad.Point2 - quad.Point0, quad.Point1 - quad.Point0));

                        double scaleOffset = Math.Min(Math.Pow(_renderComponent.CameraAltitude / 2000, _renderComponent.Water.PlanetConfig.DistantRadiusScaler), _renderComponent.Water.Radius * _renderComponent.Water.PlanetConfig.DistantRadiusOffset);
                        float softnessOffset = (float)Math.Min(Math.Pow(_renderComponent.CameraAltitude / 2000, _renderComponent.Water.PlanetConfig.DistantSoftnessScaler), _renderComponent.Water.Radius * _renderComponent.Water.PlanetConfig.DistantSoftnessOffset) * _renderComponent.Water.PlanetConfig.DistantSoftnessMultiplier;

                        quad.Point0 += normal1 * scaleOffset;
                        quad.Point1 += normal3 * scaleOffset;
                        quad.Point2 += normal2 * scaleOffset;
                        quad.Point3 += normal4 * scaleOffset;
                        
                        Vector3 halfVector = Vector3.Normalize((_renderComponent._renderSessionComponent.SunDirection + Vector3.Normalize(_renderComponent.LocalCameraPosition - ((quad.Point0 + quad.Point1) / 2))) / 2);
                        float sunDot = _renderComponent.Water.Settings.Lit ? Vector3.Dot(quadNormal, _renderComponent._renderSessionComponent.SunDirection) : 1;
                        float colorIntensity = _renderComponent.Water.Settings.Lit ? Math.Max(sunDot, _renderComponent.Water.PlanetConfig.AmbientColorIntensity) * _renderComponent.Water.PlanetConfig.ColorIntensity : _renderComponent.Water.PlanetConfig.ColorIntensity;
                        float specularity = sunDot > 0 ? Math.Max((float)Math.Pow(Vector3.Dot(quadNormal, halfVector), _renderComponent.Water.PlanetConfig.Specularity), 0) * _renderComponent.Water.PlanetConfig.SpecularIntensity : 0;
                        float fresnel = (float)Math.Pow(Math.Max(1f - Math.Abs(Vector3.Dot(quadNormal, Vector3.Normalize(_renderComponent._renderSessionComponent.CameraPosition - quad.Point0))), 0), _renderComponent.Water.Settings.Material.Fresnel);
                        float reflectivity = _renderComponent.Water.Settings.Material.Reflectivity * Math.Max(fresnel, 0.1f);
                        
                        softnessOffset += WaterData.WaterVisibility * fresnel;

                        //Bottom Billboard
                        if (_renderComponent.CameraAltitude > 0 && _renderComponent.CameraAltitude < 5000)
                        {
                            if (_bottomBillboard == null)
                            {
                                _bottomBillboard = new MyBillboard()
                                {
                                    Material = _renderComponent.Water.Settings.Texture,
                                    CustomViewProjection = -1,
                                    UVSize = Vector2.One,
                                    BlendType = WaterData.BlendType,
                                };
                            }

                            Vector3D seperator = normal1 * WaterData.WaterVisibility;

                            _bottomBillboard.Color = WaterData.WhiteColor;
                            
                            _bottomBillboard.ColorIntensity = colorIntensity + specularity;
                            _bottomBillboard.Position0 = quad.Point0 - seperator;
                            _bottomBillboard.Position1 = quad.Point1 - seperator;
                            _bottomBillboard.Position2 = quad.Point2 - seperator;
                            _bottomBillboard.Position3 = quad.Point3 - seperator;
                            _bottomBillboard.SoftParticleDistanceScale = softnessOffset;
                            _bottomBillboard.Reflectivity = reflectivity;

                            _renderFace.BillboardCache.Add(_bottomBillboard);
                        }

                        //Top billboard
                        if (_topBillboard == null)
                        {
                            _topBillboard = new MyBillboard()
                            {
                                Material = _renderComponent.Water.Settings.Texture,
                                CustomViewProjection = -1,
                                UVSize = Vector2.One,
                                BlendType = WaterData.BlendType
                            };
                        }

                        _topBillboard.SoftParticleDistanceScale = 0.5f + softnessOffset;
                        _topBillboard.Reflectivity = reflectivity;
                        if (_renderComponent.CameraAltitude < 0)
                        {
                            _topBillboard.Color = WaterData.WaterUnderwaterColor;
                            _topBillboard.ColorIntensity = colorIntensity;
                        }
                        else
                        {
                            _topBillboard.ColorIntensity = colorIntensity + specularity;

                            if (_renderComponent.Water.Settings.Transparent)
                            {
                                _topBillboard.Color = Vector4.Lerp(WaterData.WaterShallowColor, WaterData.WhiteColor, fresnel);
                            }
                            else
                            {
                                _topBillboard.Color = WaterData.WhiteColor;
                            }
                        }

                        _topBillboard.Position0 = quad.Point0;
                        _topBillboard.Position1 = quad.Point1;
                        _topBillboard.Position2 = quad.Point2;
                        _topBillboard.Position3 = quad.Point3;

                        _renderFace.BillboardCache.Add(_topBillboard);

                        //Sea foam
                        if (_renderComponent.CameraAltitude > 0)
                        {
                            if (_renderComponent.Water.Settings.EnableFoam && _radius < 256 * _renderComponent._settingsComponent.Settings.Quality)
                            {
                                Vector3D noisePosition = (quad.Point0 + _renderComponent.Water.WaveTimer) * _renderComponent.Water.Settings.WaveScale;

                                float intensity = (float)Math.Max(FastNoiseLite.GetNoise(noisePosition.X, noisePosition.Y, noisePosition.Z) / 0.25, 0);

                                if (_radius < 256 * _renderComponent._settingsComponent.Settings.Quality)
                                {
                                    if (_seaFoamBillboard == null)
                                        _seaFoamBillboard = new MyBillboard()
                                        {
                                            Material = WaterData.FoamMaterial,
                                            CustomViewProjection = -1,
                                            UVSize = WaterData.FoamUVSize,
                                            UVOffset = new Vector2(_textureId / 4f, 0),
                                        };

                                    _seaFoamBillboard.Position0 = quad.Point0;
                                    _seaFoamBillboard.Position1 = quad.Point1;
                                    _seaFoamBillboard.Position2 = quad.Point2;
                                    _seaFoamBillboard.Position3 = quad.Point3;
                                    _seaFoamBillboard.ColorIntensity = colorIntensity + specularity;

                                    if (intensity > 0.2f)
                                    {
                                        _seaFoamBillboard.Color = WaterData.WhiteColor * intensity;
                                        _seaFoamBillboard.UVOffset.Y = 0.0f;
                                    }
                                    else
                                    {
                                        _seaFoamBillboard.Color = WaterData.WhiteColor * (1f - intensity);
                                        _seaFoamBillboard.UVOffset.Y = 0.5f;
                                    }

                                    _renderFace.BillboardCache.Add(_seaFoamBillboard);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var child in Children)
                        {
                            child.Draw();
                        }
                    }
                }
            }
        }
    }
}
