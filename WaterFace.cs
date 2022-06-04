using Jakaria.Components;
using Jakaria.Utils;
using Sandbox.ModAPI;
using System;
using VRage.Game;
using VRageMath;
using VRageRender;
using Jakaria.SessionComponents;

namespace Jakaria
{
    public class WaterFace
    {
        public Vector3D AxisA;
        public Vector3D AxisB;
        public double Radius;
        
        public Water Water;
        public WaterRenderComponent RenderComponent;
        public WaterSettingsComponent SettingsComponent;

        private Chunk _tree;
        private Vector3D _centerPosition;
        

        public WaterFace(Water water, Vector3D upDirection)
        {
            this.Water = water;
            this.Radius = water.Radius;
            this.AxisA = new Vector3D(upDirection.Y, upDirection.Z, upDirection.X);
            this.AxisB = upDirection.Cross(AxisA);

            _centerPosition = (water.Radius * upDirection);
            RenderComponent = WaterRenderComponent.Static;
            SettingsComponent = WaterSettingsComponent.Static;
        }

        public void ConstructTree(int depth)
        {
            if(_tree == null)
            {
                _tree = new Chunk(_centerPosition, Radius, 0, this, -1);
            }

            _tree.GenerateChildren(depth);
        }

        public void Draw(bool closestToCamera)
        {
            if (Water.Planet != null)
            {
                if (_tree == null)
                {
                    ConstructTree(0);
                }
                else
                {
                    _tree.Draw(closestToCamera);
                }
            }
        }
    }

    public class Chunk
    {
        private WaterFace _face;
        private Chunk[] _children;
        private Vector3D _position;
        private double _radius;
        private int _detailLevel;
        private int _textureId; //The UV index for sea foam
        private BoundingSphereD _sphere;
        private float _altitude;
        private int _parentIndex; //The index/corner of the subdivision this chunk is relative to the previous(parent) chunk

        private MyBillboard _topBillboard;
        private MyBillboard _bottomBillboard;
        private MyBillboard _seaFoamBillboard;

        public Chunk(Vector3D position, double radius, int detailLevel, WaterFace face, int parentIndex)
        {
            _position = position;
            _radius = radius;
            _face = face;
            _detailLevel = detailLevel;
            _parentIndex = parentIndex;
        }

        public void GenerateChildren(int startingDepth)
        {
            if(_detailLevel == startingDepth)
            {
                _children = null;
            }
            
            if (_detailLevel < WaterData.MinWaterSplitDepth || (_radius > WaterData.MinWaterSplitRadius && (_face.Water.Position + (Vector3D.Normalize(_position + (-_face.AxisA + -_face.AxisB) * _radius) * _face.Water.Radius) - _face.RenderComponent.CameraPosition).AbsMax() < _radius * (3f + (_face.SettingsComponent.Settings.Quality * 2))))
            {
                double halfRadius = _radius / 2.0;
                int detailPlusOne = _detailLevel + 1;

                Vector3D axisAPremultiplied = _face.AxisA * (0.5 * _radius);
                Vector3D axisBPremultiplied = _face.AxisB * (0.5 * _radius);
                _children = new Chunk[4];
                _children[0] = new Chunk(this._position + (-axisAPremultiplied + -axisBPremultiplied), halfRadius, detailPlusOne, _face, 0);
                _children[1] = new Chunk(this._position + (-axisAPremultiplied + axisBPremultiplied), halfRadius, detailPlusOne, _face, 1);
                _children[2] = new Chunk(this._position + (axisAPremultiplied + -axisBPremultiplied), halfRadius, detailPlusOne, _face, 2);
                _children[3] = new Chunk(this._position + (axisAPremultiplied + axisBPremultiplied), halfRadius, detailPlusOne, _face, 3);

                foreach (var child in _children)
                {
                    child.GenerateChildren(startingDepth);
                }
            }

            if (_children == null)
            {
                //Maximum depth
                _textureId = Math.Abs(((int)(_position.X + _position.Y + _position.Z) / 10) % 4);
                _sphere = new BoundingSphereD(_face.Water.Position + (Vector3D.Normalize(_position + ((-_face.AxisA + -_face.AxisB) * _radius)) * _face.Water.Radius), _radius * 3);

                _altitude = _face.Water.Transparent ? (float)Math.Max(WaterUtils.GetAltitude(_face.Water.Planet, _sphere.Center), 0) : float.MaxValue;
            }
        }

        public void Draw(bool closestToCamera)
        {
            if (_children != null)
            {
                foreach (var child in _children)
                {
                    child.Draw(closestToCamera);
                }
            }
            else if (_radius < 8 || (WaterRenderComponent.Static.CameraAltitude > 10000 && MyAPIGateway.Session.Camera.WorldToScreen(ref _sphere.Center).AbsMax() > 1) || (closestToCamera && MyAPIGateway.Session.Camera.IsInFrustum(ref _sphere))) //Camera Frustum only works around 20km
            {
                Vector3D normal1 = Vector3D.Normalize(_position + ((-_face.AxisA + -_face.AxisB) * _radius));

                MyQuadD quad = new MyQuadD()
                {
                    Point0 = _face.Water.GetClosestSurfacePointFromNormal(ref normal1),
                };

                if (closestToCamera && Vector3D.DistanceSquared(quad.Point0, _face.RenderComponent.CameraPosition) > WaterRenderComponent.Static.DistanceToHorizon * WaterRenderComponent.Static.DistanceToHorizon)
                    return;

                Vector3D normal2 = Vector3D.Normalize(_position + ((_face.AxisA + _face.AxisB) * _radius));
                Vector3D normal3 = Vector3D.Normalize(_position + ((-_face.AxisA + _face.AxisB) * _radius));
                Vector3D normal4 = Vector3D.Normalize(_position + ((_face.AxisA + -_face.AxisB) * _radius));

                quad.Point1 = _face.Water.GetClosestSurfacePointFromNormal(ref normal3);
                quad.Point2 = _face.Water.GetClosestSurfacePointFromNormal(ref normal2);
                quad.Point3 = _face.Water.GetClosestSurfacePointFromNormal(ref normal4);

                Vector3 quadNormal = Vector3.Normalize(Vector3.Cross(quad.Point2 - quad.Point0, quad.Point1 - quad.Point0));

                if (_face.SettingsComponent.Settings.ShowDebug)
                {
                    float length = (float)_radius;
                    float radius = length * 0.01f;
                    //Currents
                    MyTransparentGeometry.AddLineBillboard(WaterData.BlankMaterial, WaterData.BlueColor, quad.Point0, _face.Water.GetCurrentVelocity(normal1), length, radius);

                    //Waves
                    MyTransparentGeometry.AddLineBillboard(WaterData.BlankMaterial, WaterData.GreenColor, quad.Point0, _face.Water.GetWaveVelocity(normal1), 1, 0.01f);

                    //Normals
                    MyTransparentGeometry.AddLineBillboard(WaterData.BlankMaterial, WaterData.RedColor, quad.Point0, quadNormal, length, radius);
                }

                double ScaleOffset = Math.Min(Math.Pow(WaterRenderComponent.Static.CameraDepth / 2000, _face.Water.PlanetConfig.DistantRadiusScaler), _face.Water.Radius * _face.Water.PlanetConfig.DistantRadiusOffset);
                float SoftnessOffset = Math.Min((float)Math.Pow(WaterRenderComponent.Static.CameraDepth / 2000, _face.Water.PlanetConfig.DistantSoftnessScaler), _face.Water.Radius * _face.Water.PlanetConfig.DistantSoftnessOffset) * _face.Water.PlanetConfig.DistantSoftnessMultiplier;

                quad.Point0 += normal1 * ScaleOffset;
                quad.Point1 += normal3 * ScaleOffset;
                quad.Point2 += normal2 * ScaleOffset;
                quad.Point3 += normal4 * ScaleOffset;

                Vector3 halfVector = Vector3.Normalize((_face.RenderComponent.SunDirection + Vector3.Normalize(_face.RenderComponent.CameraPosition - ((quad.Point0 + quad.Point1) / 2))) / 2);
                float dot = _face.Water.Lit ? Vector3.Dot(quadNormal, _face.RenderComponent.SunDirection) : 1;
                float ColorIntensity = _face.Water.Lit ? Math.Max(dot, _face.Water.PlanetConfig.AmbientColorIntensity) * _face.Water.PlanetConfig.ColorIntensity : _face.Water.PlanetConfig.ColorIntensity;
                float Specularity = dot > 0 ? Math.Max((float)Math.Pow(Vector3.Dot(quadNormal, halfVector), _face.Water.PlanetConfig.Specularity), 0) * _face.Water.PlanetConfig.SpecularIntensity : 0;

                if (_altitude < -_radius * 2) //I'm already calculating this so might as well use it
                    return;

                //Bottom Billboard
                if (!_face.RenderComponent.CameraUnderwater && closestToCamera)
                {
                    if (_bottomBillboard == null)
                    {
                        _bottomBillboard = new MyBillboard()
                        {
                            Material = _face.Water.TextureID,
                            CustomViewProjection = -1,
                            UVOffset = _radius <= 4 ? WaterData.SurfaceUVOffsets[_parentIndex] : Vector2.Zero,
                            UVSize = _radius <= 4 ? new Vector2(0.5f, 0.5f) : Vector2.One,
                            Reflectivity = _face.Water.Material.Reflectivity,
                            BlendType = WaterData.BlendType
                        };
                    }

                    Vector3D Seperator = normal1 * WaterData.WaterVisibility;
                    
                    _bottomBillboard.Color = WaterData.WhiteColor;

                    _bottomBillboard.ColorIntensity = ColorIntensity + Specularity;
                    _bottomBillboard.Position0 = quad.Point0 - Seperator;
                    _bottomBillboard.Position1 = quad.Point1 - Seperator;
                    _bottomBillboard.Position2 = quad.Point2 - Seperator;
                    _bottomBillboard.Position3 = quad.Point3 - Seperator;
                    _bottomBillboard.SoftParticleDistanceScale = SoftnessOffset;

                    _face.Water.BillboardCache.Add(_bottomBillboard);
                }

                //Top billboard
                if (_topBillboard == null)
                {
                    _topBillboard = new MyBillboard()
                    {
                        Material = _face.Water.TextureID,
                        CustomViewProjection = -1,
                        UVOffset = _radius <= 4 ? WaterData.SurfaceUVOffsets[_parentIndex] : Vector2.Zero,
                        UVSize = _radius <= 4 ? new Vector2(0.5f, 0.5f) : Vector2.One,
                        BlendType = WaterData.BlendType
                    };
                }

                _topBillboard.SoftParticleDistanceScale = 0.5f + SoftnessOffset;
                if (_face.RenderComponent.CameraUnderwater)
                {
                    _topBillboard.Color = WaterData.WaterUnderwaterColor;
                    _topBillboard.Reflectivity = _face.Water.Material.UnderwaterReflectivity;
                    _topBillboard.ColorIntensity = ColorIntensity;
                }
                else
                {
                    _topBillboard.Reflectivity = _face.Water.Material.Reflectivity;
                    _topBillboard.ColorIntensity = ColorIntensity + Specularity;

                    if (_face.Water.Transparent)
                    {
                        if (_radius < 32)
                            _topBillboard.SoftParticleDistanceScale = (float)MathHelper.Lerp(2f, WaterData.WaterVisibility, Math.Min(8f / _radius, 1));

                        _topBillboard.Color = Vector4.Lerp(WaterData.WaterShallowColor, WaterData.WhiteColor, (float)(Math.Min((_altitude / WaterData.WaterVisibility) * Math.Min(_radius / 32f, 1), 1.0)));
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

                _face.Water.BillboardCache.Add(_topBillboard);

                //Sea foam
                if (!_face.RenderComponent.CameraUnderwater)
                {
                    if (_face.Water.EnableFoam && _radius < 256 * _face.SettingsComponent.Settings.Quality)
                    {
                        Vector3D noisePosition = (quad.Point0 + _face.Water.WaveTimer) * _face.Water.WaveScale;

                        float intensity = (float)Math.Max(FastNoiseLite.GetNoise(noisePosition.X, noisePosition.Y, noisePosition.Z) / 0.25f, 0);

                        if (_radius < 256 * _face.SettingsComponent.Settings.Quality)
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
                            _seaFoamBillboard.ColorIntensity = ColorIntensity + Specularity;

                            if (intensity > 0.2f)
                            {
                                _seaFoamBillboard.Color = WaterData.WhiteColor * intensity;
                                _seaFoamBillboard.UVOffset.Y = 0.0f;
                                _face.Water.BillboardCache.Add(_seaFoamBillboard);
                            }
                            else
                            {
                                _seaFoamBillboard.Color = WaterData.WhiteColor * (1f - intensity);
                                _seaFoamBillboard.UVOffset.Y = 0.5f;
                                _face.Water.BillboardCache.Add(_seaFoamBillboard);
                            }
                        }
                    }
                }
            }
        }
    }
}
