using Jakaria.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Jakaria.SessionComponents
{
    /// <summary>
    /// Component that handles rendering of everything
    /// </summary>
    public class WaterRenderComponent : SessionComponentBase
    {
        public Vector3D CameraClosestPosition;
        public double CameraDepth;
        public Vector3D CameraGravityDirection;
        public Vector3D CameraClosestWaterPosition;
        public Vector3D GravityAxisA;
        public Vector3D GravityAxisB;
        public float DistanceToHorizon;

        public bool CameraAirtight;
        public Vector3D CameraPosition;
        public Vector3D CameraDirection;
        public bool CameraUnderwater;
        public double CameraAltitude;

        public MyPlanet ClosestPlanet = null;
        public Water ClosestWater = null;

        public Vector3 SunDirection;
        public float AmbientColorIntensity;

        private MyEntity _sunOccluder;
        private MyEntity _particleOccluder;

        private bool? _previousUnderwaterState;
        private Vector3D _lastLODBuildPosition;
        private float _nightValue;

        private WaterModComponent _modComponent;
        private IMyWeatherEffects _weatherComponent;
        private WaterEffectsComponent _effectsComponent;
        private WaterSettingsComponent _settingsComponent;

        public static WaterRenderComponent Static;

        public WaterRenderComponent()
        {
            Static = this;
            UpdateOrder = MyUpdateOrder.AfterSimulation;
        }

        public override void LoadDependencies()
        {
            _modComponent = WaterModComponent.Static;
            _effectsComponent = WaterEffectsComponent.Static;
            _settingsComponent = WaterSettingsComponent.Static;
        }

        public override void UnloadDependencies()
        {
            _modComponent = null;
            _effectsComponent = null;
            _settingsComponent = null;

            Static = null;
        }

        public override void BeforeStart()
        {
            _sunOccluder = CreateOccluderEntity();
            _particleOccluder = CreateOccluderEntity();

            _weatherComponent = MyAPIGateway.Session.WeatherEffects;
        }

        /// <summary>
        /// Creates mesh that occludes the sun bloom
        /// </summary>
        private MyEntity CreateOccluderEntity()
        {
            MyEntity ent = new MyEntity();
            ent.Init(null, ModContext.ModPath + @"\Models\Water2.mwm", null, 250, null);
            ent.Render.CastShadows = false;
            ent.Render.DrawOutsideViewDistance = true;
            ent.IsPreview = true;
            ent.Save = false;
            ent.SyncFlag = false;
            ent.NeedsWorldMatrix = false;
            ent.Flags |= EntityFlags.IsNotGamePrunningStructureObject;
            MyEntities.Add(ent, false);
            ent.Render.Transparency = 100f;
            ent.PositionComp.SetPosition(Vector3D.MaxValue);
            ent.Render.DrawInAllCascades = false;
            //ent.Render.UpdateTransparency();
            ent.InScene = true;
            ent.Render.UpdateRenderObject(true, false);

            return ent;
        }

        public void RebuildLOD()
        {
            _lastLODBuildPosition = CameraPosition;

            if (_modComponent.Waters != null)
            {
                foreach (var water in _modComponent.Waters.Values)
                {
                    if (water.WaterFaces == null)
                    {
                        water.WaterFaces = new WaterFace[Base6Directions.Directions.Length];
                    }

                    for (int i = 0; i < water.WaterFaces.Length; i++)
                    {
                        if (water.WaterFaces[i] == null)
                            water.WaterFaces[i] = new WaterFace(water, Base6Directions.Directions[i]);

                        water.WaterFaces[i].ConstructTree(WaterData.MinWaterSplitDepth);
                    }
                }
            }
        }

        public override void UpdateAfterSimulation()
        {
            CameraAirtight = WaterUtils.IsPositionAirtight(ref CameraPosition);
            SunDirection = MyVisualScriptLogicProvider.GetSunDirection();

            if (ClosestPlanet == null)
                _nightValue = 1;
            else
                _nightValue = MyMath.Clamp(Vector3.Dot(-CameraGravityDirection, SunDirection) + 0.22f, 0.22f, 1f);

            if (ClosestWater == null)            
                CameraUnderwater = false;
            else
                CameraUnderwater = ClosestWater.IsUnderwater(ref CameraPosition);

            Vector4 WhiteColor = new Vector4(_nightValue, _nightValue, _nightValue, 1);

            if (_modComponent.Waters != null)
            {
                MyAPIGateway.Parallel.ForEach(_modComponent.Waters.Values, water =>
                {
                    if (water != null)
                    {
                        water.BillboardCache?.Clear();

                        if (water.Material == null)
                            water.UpdateMaterial();

                        if (water.WaterFaces == null)
                            water.WaterFaces = new WaterFace[Base6Directions.Directions.Length];

                        for (int i = 0; i < water.WaterFaces.Length; i++)
                        {
                            WaterFace face = water.WaterFaces[i];
                            if (face == null)
                            {
                                water.WaterFaces[i] = face = new WaterFace(water, Base6Directions.Directions[i]);
                                face.ConstructTree(WaterData.MinWaterSplitDepth);
                            }

                            if (face.Water == null)
                                break;

                            face.Draw(ClosestWater?.PlanetID == water.PlanetID);
                        }
                    }
                });
            }

            //Effects
            lock (_effectsComponent.EffectLock)
            {
                float lifeRatio = 0;

                if (!CameraUnderwater)
                {
                    float size;
                    MyQuadD Quad;
                    Vector3D axisA;
                    Vector3D axisB;

                    foreach (var splash in _effectsComponent.SurfaceSplashes)
                    {
                        if (splash == null)
                            continue;

                        lifeRatio = (float)splash.Life / splash.MaxLife;
                        size = splash.Radius * lifeRatio;
                        axisA = GravityAxisA * size;
                        axisB = GravityAxisB * size;
                        splash.Billboard.Position0 = ClosestWater.GetClosestSurfacePointGlobal(splash.Position - axisA - axisB);
                        splash.Billboard.Position2 = ClosestWater.GetClosestSurfacePointGlobal(splash.Position + axisA + axisB);
                        splash.Billboard.Position1 = ClosestWater.GetClosestSurfacePointGlobal(splash.Position - axisA + axisB);
                        splash.Billboard.Position3 = ClosestWater.GetClosestSurfacePointGlobal(splash.Position + axisA - axisB);
                        splash.Billboard.Color = WhiteColor * (1f - lifeRatio) * ClosestWater.PlanetConfig.ColorIntensity;
                    }
                }
            }
        }

        public override void Draw()
        {
            if (MyAPIGateway.Session.Camera?.WorldMatrix != null)
            {
                CameraPosition = MyAPIGateway.Session.Camera.Position;
                CameraDirection = MyAPIGateway.Session.Camera.WorldMatrix.Forward;
                CameraClosestWaterPosition = ClosestWater != null ? ClosestWater.GetClosestSurfacePointGlobal(CameraPosition) : Vector3D.Zero;
                CameraClosestPosition = ClosestPlanet?.GetClosestSurfacePointGlobal(CameraPosition) ?? Vector3D.Zero;
                CameraAltitude = ClosestPlanet != null ? WaterUtils.GetAltitude(ClosestPlanet, CameraPosition) : double.MaxValue;
            }

            if (CameraGravityDirection != Vector3D.Zero)
            {
                GravityAxisA = Vector3D.CalculatePerpendicularVector(CameraGravityDirection);
                GravityAxisB = GravityAxisA.Cross(CameraGravityDirection);
            }

            if (ClosestWater != null)
            {
                DistanceToHorizon = CameraUnderwater ? 250 : (float)Math.Max(Math.Sqrt((Math.Pow(Math.Max(CameraDepth, 50) + ClosestWater.Radius, 2)) - (ClosestWater.Radius * ClosestWater.Radius)), 500);

                //float dot = _face.Water.Lit ? Vector3.Dot(quadNormal,SunDirection) : 1;
                //float ColorIntensity = _face.Water.Lit ? Math.Max(dot, _face.Water.PlanetConfig.AmbientColorIntensity) * _face.Water.PlanetConfig.ColorIntensity : _face.Water.PlanetConfig.ColorIntensity;
                if (ClosestWater.PlanetConfig != null)
                    AmbientColorIntensity = Math.Max(Vector3.Dot(-CameraGravityDirection, SunDirection), ClosestWater.PlanetConfig.AmbientColorIntensity) * ClosestWater.PlanetConfig.ColorIntensity;
                else
                    AmbientColorIntensity = 1f;

                if (_sunOccluder != null)
                {
                    _sunOccluder.Render.Visible = CameraUnderwater || (ClosestWater.Intersects(CameraPosition, CameraPosition + (SunDirection * 100)) != 0);

                    if (_sunOccluder.Render.Visible)
                        _sunOccluder.WorldMatrix = MatrixD.CreateWorld(CameraPosition + (SunDirection * 1000), -Vector3.CalculatePerpendicularVector(SunDirection), -SunDirection);
                }

                if (_particleOccluder != null)
                {
                    if (CameraUnderwater)
                        _particleOccluder.WorldMatrix = MatrixD.CreateWorld(CameraPosition + (CameraDirection * 250), -Vector3D.CalculatePerpendicularVector(CameraDirection), -CameraDirection);
                    else
                        _particleOccluder.WorldMatrix = MatrixD.CreateWorld(ClosestWater.GetClosestSurfacePointGlobal(CameraPosition) + (CameraGravityDirection * 20), GravityAxisA, -CameraGravityDirection);
                }

                bool previousUnderwater = CameraUnderwater;

                CameraDepth = ClosestWater.GetDepth(ref CameraPosition);
                CameraUnderwater = CameraDepth <= 0;

                if (previousUnderwater != CameraUnderwater)
                {
                    if (CameraUnderwater)
                    {
                        //Camera enters water
                        lock (_effectsComponent.EffectLock)
                        {
                            foreach (var fish in _effectsComponent.Fishes)
                            {
                                if (fish == null || fish.Billboard == null)
                                    continue;

                                if (!fish.InScene)
                                {
                                    MyTransparentGeometry.AddBillboard(fish.Billboard, true);
                                    fish.InScene = true;
                                }
                            }

                            foreach (var bird in _effectsComponent.Seagulls)
                            {
                                if (bird == null || bird.Billboard == null)
                                    continue;

                                if (bird.InScene)
                                {
                                    MyTransparentGeometry.RemovePersistentBillboard(bird.Billboard);
                                    bird.InScene = false;
                                }
                            }

                            foreach (var bubble in _effectsComponent.AmbientBubbles)
                            {
                                if (bubble == null || bubble.Billboard == null)
                                    continue;

                                if (!bubble.InScene)
                                {
                                    MyTransparentGeometry.AddBillboard(bubble.Billboard, true);
                                    bubble.InScene = true;
                                }
                            }

                            foreach (var splash in _effectsComponent.SimulatedSplashes)
                            {
                                if (splash == null || splash.Billboard == null)
                                    continue;

                                if (splash.InScene)
                                {
                                    MyTransparentGeometry.RemovePersistentBillboard(splash.Billboard);
                                    splash.InScene = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        //Camera exits water
                        lock (_effectsComponent.EffectLock)
                        {
                            foreach (var fish in _effectsComponent.Fishes)
                            {
                                if (fish == null || fish.Billboard == null)
                                    continue;

                                if (fish.InScene)
                                {
                                    MyTransparentGeometry.RemovePersistentBillboard(fish.Billboard);
                                    fish.InScene = false;
                                }
                            }

                            foreach (var splash in _effectsComponent.SimulatedSplashes)
                            {
                                if (splash == null || splash.Billboard == null)
                                    continue;

                                if (!splash.InScene)
                                {
                                    MyTransparentGeometry.AddBillboard(splash.Billboard, true);
                                    splash.InScene = true;
                                }
                            }

                            foreach (var bird in _effectsComponent.Seagulls)
                            {
                                if (bird == null || bird.Billboard == null)
                                    continue;

                                if (!bird.InScene)
                                {
                                    MyTransparentGeometry.AddBillboard(bird.Billboard, true);
                                    bird.InScene = true;
                                }
                            }

                            foreach (var bubble in _effectsComponent.AmbientBubbles)
                            {
                                if (bubble == null || bubble.Billboard == null)
                                    continue;

                                if (bubble.InScene)
                                {
                                    MyTransparentGeometry.RemovePersistentBillboard(bubble.Billboard);
                                    bubble.InScene = false;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                DistanceToHorizon = float.PositiveInfinity;
                CameraDepth = 0;
                CameraUnderwater = false;
            }

            if (CameraUnderwater)
            {
                if (_settingsComponent.Settings.ShowFog || !MyAPIGateway.Session.CreativeMode)
                    _weatherComponent.FogDensityOverride = (float)(0.06 + (0.00125 * (1.0 + (-CameraDepth / 100))));
                else
                    _weatherComponent.FogDensityOverride = 0.001f;

                //TODO
                /*if (CameraAirtight)
                    weatherEffects.SunSpecularColorOverride = null;
                else
                    weatherEffects.SunSpecularColorOverride = WaterMod.Session.ClosestWater.FogColor;*/

                _weatherComponent.FogColorOverride = Vector3.Lerp(ClosestWater.FogColor, Vector3.Zero, (float)Math.Min(-Math.Min(CameraDepth + 200, 0) / 800, 1)) * Math.Min(_nightValue + 0.3f, 1f);
                _weatherComponent.SunIntensityOverride = Math.Max(MathHelper.Lerp(100, 0.00001f, (float)Math.Min(-CameraDepth / (800 / (ClosestWater.Material.Density / 1000)), 1)) * Math.Min(_nightValue + 0.3f, 1f), 0);
            }

            //Only Change fog once above water and all the time underwater
            if (_previousUnderwaterState != CameraUnderwater)
            {
                _previousUnderwaterState = CameraUnderwater;

                if (CameraUnderwater)
                {
                    _weatherComponent.ParticleVelocityOverride = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                    //TODO weatherEffects.SunColorOverride = Vector3.Lerp(Vector3.One, WaterMod.Session.ClosestWater.FogColor, 0.5f);
                    _weatherComponent.FogMultiplierOverride = 1;
                    _weatherComponent.FogAtmoOverride = 1;
                    _weatherComponent.FogSkyboxOverride = 1;
                }
                else
                {
                    _weatherComponent.FogDensityOverride = null;
                    _weatherComponent.FogMultiplierOverride = null;
                    _weatherComponent.FogColorOverride = null;
                    _weatherComponent.FogSkyboxOverride = null;
                    _weatherComponent.FogAtmoOverride = null;
                    _weatherComponent.SunIntensityOverride = null;
                    _weatherComponent.ParticleVelocityOverride = null;
                    /*weatherEffects.SunColorOverride = null; TODO
                    weatherEffects.SunSpecularColorOverride = null;*/
                }
            }

            if (_modComponent.Waters != null)
                foreach (var Water in _modComponent.Waters)
                {
                    MyTransparentGeometry.AddBillboards(Water.Value.BillboardCache, false);
                }

            if (CameraDepth > -100)
            {
                if (Vector3D.RectangularDistance(_lastLODBuildPosition, CameraPosition) > 15)
                {
                    RebuildLOD();
                }
            }
        }
    }
}
