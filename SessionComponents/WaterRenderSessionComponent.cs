using Jakaria.Components;
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
    public class WaterRenderSessionComponent : SessionComponentBase
    {
        private const float MIN_SUN_INTENSITY = 0.0000001f;

        public Vector3D CameraClosestSurfacePosition;
        public double CameraDepth;
        public Vector3D CameraGravityDirection;
        public Vector3D CameraClosestWaterPosition;
        public Vector3D GravityAxisA;
        public Vector3D GravityAxisB;
        public float DistanceToHorizon;

        public Vector4 WhiteColor;

        public bool CameraAirtight;
        public Vector3D CameraPosition;
        public Vector3D CameraDirection;
        public bool CameraUnderwater;
        public double CameraAltitude;

        public MyPlanet ClosestPlanet;
        public WaterComponent ClosestWater;

        public Vector3 SunDirection;
        public float AmbientColorIntensity;

        private MyEntity _sunOccluder;
        private MyEntity _particleOccluder;

        public event Action<WaterComponent> OnWaterChanged;
        public event Action OnEnteredWater;
        public event Action OnExitedWater;

        private bool? _previousUnderwaterState;
        private float _nightValue;
        private bool _init;

        private event Action _drawAction;

        private WaterModComponent _modComponent;
        private WaterSyncComponent _syncComponent;
        private IMyWeatherEffects _weatherComponent;
        private WaterEffectsComponent _effectsComponent;
        private WaterSettingsComponent _settingsComponent;

        public override void LoadData()
        {
            _modComponent = Session.Instance.Get<WaterModComponent>();
            _syncComponent = Session.Instance.Get<WaterSyncComponent>();
            _effectsComponent = Session.Instance.Get<WaterEffectsComponent>();
            _settingsComponent = Session.Instance.Get<WaterSettingsComponent>();

            _modComponent.OnWaterAdded += OnWaterAdded;
            _modComponent.OnWaterRemoved += OnWaterRemoved;
            _syncComponent.OnWaterUpdated += OnWaterUpdated;
            OnEnteredWater += OnCameraEnteredWater;
            OnExitedWater += OnCameraExitedWater;
        }

        private void OnWaterUpdated(WaterComponent water, WaterSettings oldSettings)
        {
            WaterRenderComponent renderComponent;
            if (water.Entity.Components.TryGet<WaterRenderComponent>(out renderComponent))
            {
                renderComponent.RebuildLOD();
            }
        }

        private void OnWaterRemoved(MyEntity entity)
        {
            if (entity.Components.Has<WaterRenderComponent>())
            {
                WaterRenderComponent renderComponent = entity.Components.Get<WaterRenderComponent>();

                _drawAction -= renderComponent.Draw;

                entity.Components.Remove<WaterRenderComponent>();
            }
        }

        private void OnWaterAdded(MyEntity entity)
        {
            if (!entity.Components.Has<WaterRenderComponent>())
            {
                WaterRenderComponent renderComponent = new WaterRenderComponent();

                _drawAction += renderComponent.Draw;

                entity.Components.Add(renderComponent);
            }
        }

        public override void UnloadData()
        {
            _modComponent.OnWaterAdded -= OnWaterAdded;
            _modComponent.OnWaterRemoved -= OnWaterRemoved;
            _syncComponent.OnWaterUpdated -= OnWaterUpdated;
            OnEnteredWater -= OnCameraEnteredWater;
            OnExitedWater -= OnCameraExitedWater;
        }

        public override void BeforeStart()
        {
            _sunOccluder = CreateOccluderEntity();
            _particleOccluder = CreateOccluderEntity();

            _weatherComponent = MyAPIGateway.Session.WeatherEffects;
        }

        public override void Init()
        {
            foreach (var water in _modComponent.Waters)
            {
                WaterRenderComponent renderComponent = water.Planet.Components.Get<WaterRenderComponent>();

                renderComponent.UpdateLOD();
            }

            OnExitedWater?.Invoke();
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
            ent.InScene = true;
            ent.Render.UpdateRenderObject(true, false);

            return ent;
        }

        public override void UpdateAfterSimulation()
        {
            ClosestPlanet = MyGamePruningStructure.GetClosestPlanet(CameraPosition);
            
            WaterComponent water = _modComponent.GetClosestWater(CameraPosition);
            WaveModifier modifier = WaterUtils.GetWaveModifier(CameraPosition);

            if (water != ClosestWater)
            {
                ClosestWater = water;
                OnWaterChanged?.Invoke(ClosestWater);
            }

            CameraAirtight = WaterUtils.IsPositionAirtight(ref CameraPosition);
            SunDirection = MyVisualScriptLogicProvider.GetSunDirection();

            if (ClosestPlanet == null)
            {
                CameraClosestSurfacePosition = Vector3D.MaxValue;
                CameraAltitude = double.MaxValue;

                _nightValue = 1;
            }
            else
            {
                CameraClosestSurfacePosition = ClosestPlanet.GetClosestSurfacePointGlobal(CameraPosition);
                CameraAltitude = WaterUtils.GetAltitude(ClosestPlanet, CameraPosition);

                _nightValue = MyMath.Clamp(Vector3.Dot(-CameraGravityDirection, SunDirection) + 0.22f, 0.22f, 1f);
            }

            if (ClosestWater == null)
            {
                CameraDepth = double.MaxValue;
                CameraUnderwater = false;
                CameraClosestWaterPosition = Vector3D.MaxValue;
            }
            else
            {
                CameraDepth = ClosestWater.GetDepthGlobal(ref CameraPosition, ref modifier);
                CameraUnderwater = CameraDepth <= WaterComponent.DEPTH_EPSILON;
                CameraClosestWaterPosition = ClosestWater.GetClosestSurfacePointGlobal(ref CameraPosition, ref modifier);
            }

            WhiteColor = new Vector4(_nightValue * (1f - (float)Math.Min(-Math.Min(CameraDepth + 200, 0) / 400, 1)));
            WhiteColor.W = 1;
            
            //Effects

            if (!CameraUnderwater && ClosestWater != null)
            {
                float size;
                Vector3D axisA;
                Vector3D axisB;

                //TODO REENABLE
                /*lock (_effectsComponent.SurfaceSplashes)
                {
                    foreach (var splash in _effectsComponent.SurfaceSplashes)
                    {
                        if (splash == null)
                            continue;

                        float lifeRatio = (float)splash.Life / splash.MaxLife;
                        size = splash.Radius * lifeRatio;
                        axisA = GravityAxisA * size;
                        axisB = GravityAxisB * size;
                        splash.Billboard.Position0 = ClosestWater.GetClosestSurfacePointGlobal(splash.Position - axisA - axisB);
                        splash.Billboard.Position2 = ClosestWater.GetClosestSurfacePointGlobal(splash.Position + axisA + axisB);
                        splash.Billboard.Position1 = ClosestWater.GetClosestSurfacePointGlobal(splash.Position - axisA + axisB);
                        splash.Billboard.Position3 = ClosestWater.GetClosestSurfacePointGlobal(splash.Position + axisA - axisB);
                        splash.Billboard.Color = WhiteColor * (1f - lifeRatio) * ClosestWater.PlanetConfig.ColorIntensity;
                    }
                }*/
            }
        }

        public override void Draw()
        {
            if (MyAPIGateway.Session?.Camera?.WorldMatrix != null)
            {
                CameraPosition = MyAPIGateway.Session.Camera.WorldMatrix.Translation;
                CameraDirection = MyAPIGateway.Session.Camera.WorldMatrix.Forward;

                if (CameraGravityDirection != Vector3D.Zero)
                {
                    GravityAxisA = Vector3D.CalculatePerpendicularVector(CameraGravityDirection);
                    GravityAxisB = GravityAxisA.Cross(CameraGravityDirection);
                }

                WaveModifier modifier = WaterUtils.GetWaveModifier(CameraPosition);

                _drawAction?.Invoke();

                if (ClosestWater != null)
                {
                    //float dot = _face.Water.Lit ? Vector3.Dot(quadNormal,SunDirection) : 1;
                    //float ColorIntensity = _face.Water.Lit ? Math.Max(dot, _face.Water.PlanetConfig.AmbientColorIntensity) * _face.Water.PlanetConfig.ColorIntensity : _face.Water.PlanetConfig.ColorIntensity;
                    if (ClosestWater.PlanetConfig != null)
                        AmbientColorIntensity = Math.Max(Vector3.Dot(-CameraGravityDirection, SunDirection), ClosestWater.PlanetConfig.AmbientColorIntensity) * ClosestWater.PlanetConfig.ColorIntensity;
                    else
                        AmbientColorIntensity = 1f;

                    if (_sunOccluder != null)
                    {
                        _sunOccluder.Render.Visible = CameraUnderwater || (ClosestWater.IntersectsGlobal(CameraPosition, CameraPosition + (SunDirection * 100), ref modifier) != 0);

                        if (_sunOccluder.Render.Visible)
                            _sunOccluder.WorldMatrix = MatrixD.CreateWorld(CameraPosition + (SunDirection * 1000), -Vector3.CalculatePerpendicularVector(SunDirection), -SunDirection);
                    }

                    if (_particleOccluder != null)
                    {
                        if (CameraUnderwater)
                            _particleOccluder.WorldMatrix = MatrixD.CreateWorld(CameraPosition + (CameraDirection * 250), -Vector3D.CalculatePerpendicularVector(CameraDirection), -CameraDirection);
                        else
                            _particleOccluder.WorldMatrix = MatrixD.CreateWorld(ClosestWater.GetClosestSurfacePointGlobal(CameraPosition, ref modifier) + (CameraGravityDirection * 20), GravityAxisA, -CameraGravityDirection);
                    }

                    bool previousUnderwater = CameraUnderwater;

                    if (previousUnderwater != CameraUnderwater)
                    {
                        if (CameraUnderwater)
                        {
                            OnEnteredWater?.Invoke();
                        }
                        else
                        {
                            OnExitedWater?.Invoke();
                        }
                    }
                }
                else
                {
                    DistanceToHorizon = float.PositiveInfinity;
                }
                
                if (CameraUnderwater)
                {
                    if (_settingsComponent.Settings.ShowFog || !MyAPIGateway.Session.CreativeMode)
                        _weatherComponent.FogDensityOverride = (float)(ClosestWater.Settings.Material.MinFogDensity + (ClosestWater.Settings.Material.FogDensityDepth * (1.0 + (-CameraDepth / ClosestWater.Settings.Material.FogDensityDepthScalar))));
                    else
                        _weatherComponent.FogDensityOverride = 0.001f;

                    //TODO
                    /*if (CameraAirtight)
                        weatherEffects.SunSpecularColorOverride = null;
                    else
                        weatherEffects.SunSpecularColorOverride = WaterMod.Session.ClosestWater.FogColor;*/

                    WhiteColor *= Math.Min(Math.Max((400 - (float)CameraDepth) / 400, 0), 1);
                    WhiteColor.Z = 1;

                    _weatherComponent.FogColorOverride = Vector3.Lerp(ClosestWater.Settings.FogColor, Vector3.Zero, (float)Math.Min(-Math.Min(CameraDepth + 200, 0) / 400, 1)) * Math.Min(_nightValue + 0.3f, 1f);
                    _weatherComponent.SunIntensityOverride = Math.Max(MathHelper.Lerp(ClosestWater.Settings.Material.InitialSunIntensity, MIN_SUN_INTENSITY, (float)Math.Min(-CameraDepth / (400 / (ClosestWater.Settings.Material.Density / 1000)), 1)) * Math.Min(_nightValue + 0.3f, 1f), MIN_SUN_INTENSITY);
                }

                //Only Change fog once above water and all the time underwater
                if (_previousUnderwaterState != CameraUnderwater || !_init)
                {
                    _previousUnderwaterState = CameraUnderwater;
                    _init = true;

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
            }
        }

        private void OnCameraEnteredWater()
        {
            lock (_effectsComponent.AmbientBubbles)
            {
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
            }
            lock (_effectsComponent.SimulatedSplashes)
            {
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

        private void OnCameraExitedWater()
        {
            lock (_effectsComponent.SimulatedSplashes)
            {
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
            }

            lock (_effectsComponent.AmbientBubbles)
            {
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
