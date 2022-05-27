using Jakaria.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Jakaria.SessionComponents
{
    /// <summary>
    /// Component that handles environmental sounds
    /// </summary>
    public class WaterSoundComponent : SessionComponentBase
    {
        public float VolumeMultiplier { get; private set; }

        private MyEntity3DSoundEmitter _environmentUnderwaterSoundEmitter = new MyEntity3DSoundEmitter(null);
        private MyEntity3DSoundEmitter _environmentOceanSoundEmitter = new MyEntity3DSoundEmitter(null);
        private MyEntity3DSoundEmitter _environmentBeachSoundEmitter = new MyEntity3DSoundEmitter(null);

        private MyEntity3DSoundEmitter _ambientSoundEmitter = new MyEntity3DSoundEmitter(null);
        private MyEntity3DSoundEmitter _ambientBoatSoundEmitter = new MyEntity3DSoundEmitter(null);

        private float _ambientTimer = 0;
        private float _ambientBoatTimer = 0;

        private int _insideGrid;
        private int _insideVoxel;

        private bool _cameraAirtight;

        private WaterModComponent _modComponent;
        private WaterRenderComponent _renderComponent;
        private WaterEffectsComponent _effectsComponent;
        private WaterSettingsComponent _settingsComponent;

        public static WaterSoundComponent Static;

        public WaterSoundComponent()
        {
            Static = this;
            UpdateOrder = MyUpdateOrder.AfterSimulation;
        }

        public override void LoadDependencies()
        {
            _modComponent = WaterModComponent.Static;
            _renderComponent = WaterRenderComponent.Static;
            _effectsComponent = WaterEffectsComponent.Static;
            _settingsComponent = WaterSettingsComponent.Static;
        }

        public override void UnloadDependencies()
        {
            _modComponent = null;
            _renderComponent = null;
            _effectsComponent = null;
            _settingsComponent = null;

            Static = null;
        }

        public override void UpdateAfterSimulation()
        {
            if (_renderComponent.ClosestWater == null || _renderComponent.ClosestPlanet == null)
            {
                StopAmbientSounds();
                return;
            }

            if (_renderComponent.ClosestPlanet.HasAtmosphere)
            {
                if (_renderComponent.CameraAirtight)
                    _insideGrid = 25;
                else
                    _insideGrid = MyAPIGateway.Session?.Player?.Character?.Components?.Get<MyEntityReverbDetectorComponent>() != null ? MyAPIGateway.Session.Player.Character.Components.Get<MyEntityReverbDetectorComponent>().Grids : 0;

                _insideVoxel = MyAPIGateway.Session?.Player?.Character?.Components?.Get<MyEntityReverbDetectorComponent>() != null ? MyAPIGateway.Session.Player.Character.Components.Get<MyEntityReverbDetectorComponent>().Voxels : 0;

                VolumeMultiplier = ((25f - Math.Max(_insideGrid - 15f, 0f)) / 25f) * ((25f - Math.Max(_insideVoxel - 15f, 0f)) / 25f) * _settingsComponent.Settings.Volume;

                if (_renderComponent.CameraUnderwater)
                {
                    _ambientTimer--;
                    if (_ambientTimer <= 0)
                    {
                        _ambientTimer = MyUtils.GetRandomInt(1000, 1900); //Divide by 60 to get in seconds

                        if (!_ambientSoundEmitter.IsPlaying)
                        {
                            _ambientSoundEmitter.PlaySound(WaterData.AmbientSound);
                            _ambientSoundEmitter.SetPosition(_renderComponent.CameraPosition + (MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(0, 75)));
                            _ambientSoundEmitter.VolumeMultiplier = VolumeMultiplier;
                        }
                    }

                    if (!_environmentUnderwaterSoundEmitter.IsPlaying)
                        _environmentUnderwaterSoundEmitter.PlaySound(WaterData.EnvironmentUnderwaterSound, force2D: true);

                    if (_environmentBeachSoundEmitter.IsPlaying)
                        _environmentBeachSoundEmitter.StopSound(true, false);

                    if (_environmentOceanSoundEmitter.IsPlaying)
                        _environmentOceanSoundEmitter.StopSound(true, false);

                    _ambientBoatTimer--;

                    if (_ambientBoatTimer <= 0)
                    {
                        _ambientBoatTimer = MyUtils.GetRandomInt(1000, 1500); //Divide by 60 to get in seconds

                        //TODO CRUSH DAMAGE STUF
                        if (MyAPIGateway.Session.Player?.Character != null)
                            if (_renderComponent.CameraUnderwater && !_ambientBoatSoundEmitter.IsPlaying && _insideGrid > 10 && _renderComponent.CameraDepth < -_renderComponent.ClosestWater.CrushDamage)
                            {
                                _ambientBoatSoundEmitter.PlaySound(WaterData.GroanSound);
                                _ambientBoatSoundEmitter.VolumeMultiplier = VolumeMultiplier;
                                _ambientBoatSoundEmitter.SetPosition(_renderComponent.CameraPosition + (MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(0, 75)));
                            }
                    }
                }
                else
                {
                    if (!_environmentOceanSoundEmitter.IsPlaying)
                        _environmentOceanSoundEmitter.PlaySound(WaterData.EnvironmentOceanSound);

                    if (!_environmentBeachSoundEmitter.IsPlaying)
                        _environmentBeachSoundEmitter.PlaySound(WaterData.EnvironmentBeachSound, force2D: true);

                    if (_renderComponent.ClosestWater.EnableSeagulls)
                        foreach (var seagull in _effectsComponent.Seagulls)
                        {
                            if (seagull == null)
                                continue;

                            if (MyUtils.GetRandomInt(0, 300) < 1)
                            {
                                seagull.Caw();
                            }
                        }

                    if (_environmentUnderwaterSoundEmitter.IsPlaying)
                        _environmentUnderwaterSoundEmitter.StopSound(true, false);
                }

                _environmentUnderwaterSoundEmitter.VolumeMultiplier = VolumeMultiplier;
                _environmentOceanSoundEmitter.VolumeMultiplier = VolumeMultiplier * MathHelper.Clamp((500f - (float)_renderComponent.CameraDepth) / 500.0f, 0, 1.0f);
                _environmentBeachSoundEmitter.VolumeMultiplier = VolumeMultiplier * MathHelper.Clamp((100f - (float)_renderComponent.CameraDepth) / 100.0f, 0, 1.0f);
            }
            else
            {
                StopAmbientSounds();
            }
        }

        private void StopAmbientSounds()
        {
            if (_environmentBeachSoundEmitter.IsPlaying)
                _environmentBeachSoundEmitter.StopSound(true);

            if (_environmentOceanSoundEmitter.IsPlaying)
                _environmentOceanSoundEmitter.StopSound(true);

            if (_environmentUnderwaterSoundEmitter.IsPlaying)
                _environmentUnderwaterSoundEmitter.StopSound(true);
        }

        public override void LoadData()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
            {
                UpdateOrder = MyUpdateOrder.NoUpdate;
            }
        }

        public override void UnloadData()
        {
            _environmentUnderwaterSoundEmitter.Cleanup();
            _environmentOceanSoundEmitter.Cleanup();
            _environmentBeachSoundEmitter.Cleanup();

            _ambientSoundEmitter.Cleanup();
            _ambientBoatSoundEmitter.Cleanup();
        }
    }
}
