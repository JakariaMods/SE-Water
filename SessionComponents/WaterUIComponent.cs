using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using Draygo.API;
using Jakaria;
using Jakaria.Utils;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Serialization;
using VRageMath;
using VRage.Utils;

namespace Jakaria.SessionComponents
{
    /// <summary>
    /// Component for in-game UI using TextHudAPI
    /// </summary>
    class WaterUIComponent : SessionComponentBase
    {
        public const string NO_DATA = "Nil";

        private bool _registered;
        private bool _initialized;

        private HudAPIv2 _textAPI;

        private HudAPIv2.HUDMessage _depthMeter;

        private HudAPIv2.MenuRootCategory _adminMenuRoot;
        private HudAPIv2.MenuItem _showFog;
        private HudAPIv2.MenuItem _refreshAdmin;
        private HudAPIv2.MenuSubCategory _waterSubcategory;
        private HudAPIv2.MenuSubCategory _waterWaveSubcategory;
        private HudAPIv2.MenuSubCategory _waterTideSubcategory;
        private HudAPIv2.MenuSubCategory _waterVisualsSubcategory;
        private HudAPIv2.MenuSubCategory _waterGameplaySubcategory;
        private HudAPIv2.MenuSubCategory _waterCurrentsSubcategory;

        private HudAPIv2.MenuTextInput _buoyancySlider;
        private HudAPIv2.MenuTextInput _collectionRateSlider;
        private HudAPIv2.MenuTextInput _crushDamageSlider;
        private HudAPIv2.MenuItem _enableFish;
        private HudAPIv2.MenuItem _enableFoam;
        private HudAPIv2.MenuItem _enableSeagulls;
        private HudAPIv2.MenuColorPickerInput _fogColorSelector;
        private HudAPIv2.MenuItem _lit;
        private HudAPIv2.MenuTextInput _materialIdInput;
        private HudAPIv2.MenuItem _playerDrag;
        private HudAPIv2.MenuSliderInput _radiusSlider;
        private HudAPIv2.MenuTextInput _textureInput;
        private HudAPIv2.MenuTextInput _tideHeight;
        private HudAPIv2.MenuTextInput _tideSpeed;
        private HudAPIv2.MenuItem _transparent;
        private HudAPIv2.MenuTextInput _waveHeight;
        private HudAPIv2.MenuTextInput _waveScale;
        private HudAPIv2.MenuTextInput _waveSpeed;
        private HudAPIv2.MenuTextInput _currentSpeed;
        private HudAPIv2.MenuTextInput _currentScale;

        private HudAPIv2.MenuRootCategory _clientMenuRoot;
        private HudAPIv2.MenuSliderInput _qualitySlider;
        private HudAPIv2.MenuSliderInput _volumeSlider;
        private HudAPIv2.MenuItem _showCenterOfBuoyancy;
        private HudAPIv2.MenuItem _showDepth;
        private HudAPIv2.MenuItem _showAltitude;
        private HudAPIv2.MenuItem _showDebug;

        private WaterSyncComponent _syncComponent;
        private WaterRenderSessionComponent _renderComponent;
        private WaterSettingsComponent _settingsComponent;

        public bool Heartbeat => _textAPI.Heartbeat && _registered;

        public override void BeforeStart()
        {
            string version = WaterUtils.GetVersionString();
            string text = string.Format(WaterTexts.WaterModVersion, version);

            WaterUtils.ShowMessage(text);
            WaterUtils.WriteLog(text);

            if (!string.IsNullOrEmpty(WaterData.StartMessage))
            {
                MyAPIGateway.Utilities.ShowMissionScreen(WaterTexts.ModChatName, version, null, WaterData.StartMessage);
            }
        }

        public override void LoadData()
        {
            _syncComponent = Session.Instance.Get<WaterSyncComponent>();
            _renderComponent = Session.Instance.Get<WaterRenderSessionComponent>();
            _settingsComponent = Session.Instance.Get<WaterSettingsComponent>();

            _textAPI = new HudAPIv2(OnRegisteredAction);
        }

        public override void UpdateAfterSimulation()
        {
            if (Heartbeat)
            {
                if (!_initialized)
                {
                    _initialized = true;

                    _depthMeter = new HudAPIv2.HUDMessage(new StringBuilder(""), Vector2D.Zero);

                    //Admin
                    _adminMenuRoot = new HudAPIv2.MenuRootCategory(WaterTexts.ModChatName + " " + WaterData.Version, HudAPIv2.MenuRootCategory.MenuFlag.AdminMenu, "Water Mod Admin Settings");

                    _showFog = new HudAPIv2.MenuItem("Not yet implemented", _adminMenuRoot);
                    /*showFog = new HudAPIv2.MenuItem(string.Format(WaterTexts.UIShowFog, WaterMod.Settings.ShowFog), adminMenuRoot, ToggleShowFog);
                    refreshAdmin = new HudAPIv2.MenuItem(WaterTexts.UIRefreshData, adminMenuRoot, RefreshAdminValues);
                    waterSubcategory = new HudAPIv2.MenuSubCategory(WaterTexts.UIWaterSettings, adminMenuRoot, "Water Settings");

                    waterWaveSubcategory = new HudAPIv2.MenuSubCategory(WaterTexts.UIWaterWaveSettings, waterSubcategory, WaterTexts.UIWaterWaveSettings);
                    waveHeight = new HudAPIv2.MenuTextInput(string.Format(WaterTexts.UIWaterWaveHeight, WaterMod.Session.ClosestWater?.WaveHeight.ToString() ?? NO_DATA), waterWaveSubcategory, string.Format(WaterTexts.UIInputNumber, WaterMod.Session.ClosestWater?.WaveHeight.ToString() ?? NO_DATA), OnSubmitWaveHeight);
                    waveScale = new HudAPIv2.MenuTextInput(string.Format(WaterTexts.UIWaterWaveScale, WaterMod.Session.ClosestWater?.WaveScale.ToString() ?? NO_DATA), waterWaveSubcategory, string.Format(WaterTexts.UIInputNumber, WaterMod.Session.ClosestWater?.WaveScale.ToString() ?? NO_DATA), OnSubmitWaveScale);
                    waveSpeed = new HudAPIv2.MenuTextInput(string.Format(WaterTexts.UIWaterWaveSpeed, WaterMod.Session.ClosestWater?.WaveSpeed.ToString() ?? NO_DATA), waterWaveSubcategory, string.Format(WaterTexts.UIInputNumber, WaterMod.Session.ClosestWater?.WaveSpeed.ToString() ?? NO_DATA), OnSubmitWaveSpeed);

                    waterCurrentsSubcategory = new HudAPIv2.MenuSubCategory(WaterTexts.UIWaterCurrentSettings, waterSubcategory, WaterTexts.UIWaterCurrentSettings);
                    currentScale = new HudAPIv2.MenuTextInput(string.Format(WaterTexts.UIWaterCurrentScale, WaterMod.Session.ClosestWater?.CurrentScale.ToString() ?? NO_DATA), waterCurrentsSubcategory, string.Format(WaterTexts.UIInputNumber, WaterMod.Session.ClosestWater?.CurrentScale.ToString() ?? NO_DATA), OnSubmitCurrentScale);
                    currentSpeed = new HudAPIv2.MenuTextInput(string.Format(WaterTexts.UIWaterCurrentSpeed, WaterMod.Session.ClosestWater?.CurrentSpeed.ToString() ?? NO_DATA), waterCurrentsSubcategory, string.Format(WaterTexts.UIInputNumber, WaterMod.Session.ClosestWater?.CurrentSpeed.ToString() ?? NO_DATA), OnSubmitCurrentSpeed);

                    waterTideSubcategory = new HudAPIv2.MenuSubCategory(WaterTexts.UIWaterTideSettings, waterSubcategory, WaterTexts.UIWaterTideSettings);
                    tideHeight = new HudAPIv2.MenuTextInput(string.Format(WaterTexts.UIWaterTideHeight, WaterMod.Session.ClosestWater?.TideHeight.ToString() ?? NO_DATA), waterTideSubcategory, string.Format(WaterTexts.UIInputNumber, WaterMod.Session.ClosestWater?.TideHeight.ToString() ?? NO_DATA), OnSubmtTideHeight);
                    tideSpeed = new HudAPIv2.MenuTextInput(string.Format(WaterTexts.UIWaterTideSpeed, WaterMod.Session.ClosestWater?.TideSpeed.ToString() ?? NO_DATA), waterTideSubcategory, string.Format(WaterTexts.UIInputNumber, WaterMod.Session.ClosestWater?.TideSpeed.ToString() ?? NO_DATA), OnSubmitTideSpeed);

                    waterVisualsSubcategory = new HudAPIv2.MenuSubCategory(WaterTexts.UIWaterVisualSettings, waterSubcategory, WaterTexts.UIWaterVisualSettings);
                    enableFish = new HudAPIv2.MenuItem(string.Format(WaterTexts.UIWaterEnableFish, WaterMod.Session.ClosestWater?.EnableFish.ToString() ?? NO_DATA), waterVisualsSubcategory, OnSubmitEnableFish);
                    enableFoam = new HudAPIv2.MenuItem(string.Format(WaterTexts.UIWaterEnableFoam, WaterMod.Session.ClosestWater?.EnableFoam.ToString() ?? NO_DATA), waterVisualsSubcategory, OnSubmitEnableFoam);
                    enableSeagulls = new HudAPIv2.MenuItem(string.Format(WaterTexts.UIWaterEnableSeagulls, WaterMod.Session.ClosestWater?.EnableSeagulls.ToString() ?? NO_DATA), waterVisualsSubcategory, OnSubmitEnableSeagulls);
                    fogColorSelector = new HudAPIv2.MenuColorPickerInput(WaterTexts.UIWaterFogColor, waterVisualsSubcategory, WaterSettings.Default.FogColor, "Select fog color", OnSubmitFogColor);
                    lit = new HudAPIv2.MenuItem(string.Format(WaterTexts.UIWaterLit, WaterMod.Session.ClosestWater?.Lit.ToString() ?? NO_DATA), waterVisualsSubcategory, OnSubmitEnableLighting);
                    transparent = new HudAPIv2.MenuItem(string.Format(WaterTexts.UIWaterTransparent, WaterMod.Session.ClosestWater?.Transparent.ToString() ?? NO_DATA), waterVisualsSubcategory, OnSubmitEnableTransparency);
                    textureInput = new HudAPIv2.MenuTextInput(string.Format(WaterTexts.UIWaterTexture, WaterMod.Session.ClosestWater?.Texture.ToString() ?? NO_DATA), waterVisualsSubcategory, string.Format(WaterTexts.UIInputNumber, WaterMod.Session.ClosestWater?.Texture ?? NO_DATA), OnSubmitTexture);

                    waterGameplaySubcategory = new HudAPIv2.MenuSubCategory(WaterTexts.UIWaterGameplaySettings, waterSubcategory, WaterTexts.UIWaterGameplaySettings);
                    buoyancySlider = new HudAPIv2.MenuTextInput(string.Format(WaterTexts.UIWaterBuoyancy, WaterMod.Session.ClosestWater?.Buoyancy.ToString() ?? NO_DATA), waterGameplaySubcategory, onSubmit: OnSubmitBuoyancy);
                    CollectionRateSlider = new HudAPIv2.MenuTextInput(string.Format(WaterTexts.UIWaterCollectionRate, WaterMod.Session.ClosestWater?.CollectionRate.ToString() ?? NO_DATA), waterGameplaySubcategory, onSubmit: OnSubmitCollectorRate);
                    CrushDamageSlider = new HudAPIv2.MenuTextInput(string.Format(WaterTexts.UIWaterCrushDamage, WaterMod.Session.ClosestWater?.CrushDamage.ToString() ?? NO_DATA), waterGameplaySubcategory, onSubmit: OnSubmitCrushDamage);
                    materialIdInput = new HudAPIv2.MenuTextInput(string.Format(WaterTexts.UIWaterMaterialId, WaterMod.Session.ClosestWater?.MaterialId.ToString() ?? NO_DATA), waterGameplaySubcategory, string.Format(WaterTexts.UIInputNumber, WaterMod.Session.ClosestWater?.MaterialId ?? NO_DATA), OnSubmitMaterial);
                    playerDrag = new HudAPIv2.MenuItem(string.Format(WaterTexts.UIWaterPlayerDrag, WaterMod.Session.ClosestWater?.PlayerDrag.ToString() ?? NO_DATA), waterGameplaySubcategory, OnSubmitPlayerDrag);
                    radiusSlider = new HudAPIv2.MenuSliderInput(string.Format(WaterTexts.UIWaterRadius, WaterMod.Session.ClosestWater?.Radius.ToString() ?? NO_DATA), waterGameplaySubcategory, RadiusValueToSlider(WaterSettings.Default.Radius), OnSubmitAction: OnSubmitRadius, SliderPercentToValue: RadiusSliderToValue);*/

                    //Client
                    _clientMenuRoot = new HudAPIv2.MenuRootCategory(WaterTexts.ModChatName + " " + WaterData.Version, HudAPIv2.MenuRootCategory.MenuFlag.PlayerMenu, "Water Mod Client Settings");

                    _qualitySlider = new HudAPIv2.MenuSliderInput(string.Format(WaterTexts.UIQuality, _settingsComponent.Settings.Quality), _clientMenuRoot, 0.5f, "Adjust slider to modify value", OnSetQualitySlider, QualitySliderToValue);
                    _volumeSlider = new HudAPIv2.MenuSliderInput(string.Format(WaterTexts.UIVolume, _settingsComponent.Settings.Volume), _clientMenuRoot, 1f, "Adjust slider to modify value", OnSetVolumeSlider);
                    _showCenterOfBuoyancy = new HudAPIv2.MenuItem(string.Format(WaterTexts.UIShowCenterOfBuoyancy, _settingsComponent.Settings.ShowCenterOfBuoyancy), _clientMenuRoot, ToggleShowCenterOfBuoyancy);
                    _showDepth = new HudAPIv2.MenuItem(string.Format(WaterTexts.UIShowDepth, _settingsComponent.Settings.ShowDepth), _clientMenuRoot, ToggleShowDepth);
                    _showAltitude = new HudAPIv2.MenuItem(string.Format(WaterTexts.UIShowAltitude, _settingsComponent.Settings.ShowAltitude), _clientMenuRoot, ToggleShowAltitude);
                    _showDebug = new HudAPIv2.MenuItem(string.Format(WaterTexts.UIShowDebug, _settingsComponent.Settings.ShowDebug), _clientMenuRoot, ToggleShowDebug);
                }

                _depthMeter.Visible = _settingsComponent.Settings.ShowDepth && (_renderComponent.CameraDepth < 0 || (_settingsComponent.Settings.ShowAltitude && ((MyAPIGateway.Session.Player?.Controller?.ControlledEntity is IMyCharacter) == false && _renderComponent.CameraDepth < 500)));

                if (_renderComponent.ClosestWater != null && _renderComponent.ClosestPlanet != null && MyAPIGateway.Session.Player?.Character != null)
                {
                    if (_depthMeter.Visible)
                    {
                        double crushDepth = WaterUtils.GetCrushDepth(_renderComponent.ClosestWater, MyAPIGateway.Session.Player.Character);
                        string message;
                        if (_renderComponent.CameraDepth < 0)
                            message = string.Format(WaterTexts.Depth, Math.Round(-_renderComponent.CameraDepth));
                        else
                            message = string.Format(WaterTexts.Altitude, Math.Round(_renderComponent.CameraDepth));

                        if (_renderComponent.CameraDepth < -crushDepth)
                            message = "- " + message + " -";

                        _depthMeter.Message = new StringBuilder("\n\n\n" + message);

                        if (!_renderComponent.CameraAirtight)
                            _depthMeter.InitialColor = Color.Lerp(Color.White, Color.Red, (float)MathHelper.Clamp(-_renderComponent.CameraDepth / crushDepth, 0f, 1f));
                        else
                            _depthMeter.InitialColor = Color.Lerp(_depthMeter.InitialColor, Color.White, 0.25f);

                        _depthMeter.Offset = new Vector2D(-_depthMeter.GetTextLength().X / 2, 0);
                    }
                }
            }
        }

        private void OnRegisteredAction()
        {
            _registered = true;
        }

        #region Server/Admin Settings

        private void ToggleShowFog()
        {
            if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.SpaceMaster)
                return;

            _settingsComponent.Settings.ShowFog = !_settingsComponent.Settings.ShowFog;
            RefreshAdminValues();
        }

        private void RefreshClientValues()
        {
            _qualitySlider.Text = string.Format(WaterTexts.UIQuality, _settingsComponent.Settings.Quality);
            _volumeSlider.Text = string.Format(WaterTexts.UIVolume, _settingsComponent.Settings.Volume);
            _showDebug.Text = string.Format(WaterTexts.UIShowDebug, _settingsComponent.Settings.ShowDebug);
            _showDepth.Text = string.Format(WaterTexts.UIShowDepth, _settingsComponent.Settings.ShowDepth);
            _showCenterOfBuoyancy.Text = string.Format(WaterTexts.UIShowCenterOfBuoyancy, _settingsComponent.Settings.ShowCenterOfBuoyancy);
            _showAltitude.Text = string.Format(WaterTexts.UIShowAltitude, _settingsComponent.Settings.ShowAltitude);
        }

        private void RefreshAdminValues()
        {
            _showFog.Text = string.Format(WaterTexts.UIShowFog, _settingsComponent.Settings.ShowFog);
            _waveHeight.Text = string.Format(WaterTexts.UIWaterWaveHeight, _renderComponent.ClosestWater.Settings?.WaveHeight.ToString() ?? NO_DATA);
            _waveScale.Text = string.Format(WaterTexts.UIWaterWaveScale, _renderComponent.ClosestWater.Settings?.WaveScale.ToString() ?? NO_DATA);
            _waveSpeed.Text = string.Format(WaterTexts.UIWaterWaveSpeed, _renderComponent.ClosestWater.Settings?.WaveSpeed.ToString() ?? NO_DATA);
            _currentScale.Text = string.Format(WaterTexts.UIWaterCurrentScale, _renderComponent.ClosestWater.Settings?.CurrentScale.ToString() ?? NO_DATA);
            _currentSpeed.Text = string.Format(WaterTexts.UIWaterCurrentSpeed, _renderComponent.ClosestWater.Settings?.CurrentSpeed.ToString() ?? NO_DATA);
            _tideHeight.Text = string.Format(WaterTexts.UIWaterTideHeight, _renderComponent.ClosestWater.Settings?.TideHeight.ToString() ?? NO_DATA);
            _tideSpeed.Text = string.Format(WaterTexts.UIWaterTideSpeed, _renderComponent.ClosestWater.Settings?.TideSpeed.ToString() ?? NO_DATA);
            _enableFish.Text = string.Format(WaterTexts.UIWaterEnableFish, _renderComponent.ClosestWater.Settings?.EnableFish.ToString() ?? NO_DATA);
            _enableFoam.Text = string.Format(WaterTexts.UIWaterEnableFoam, _renderComponent.ClosestWater.Settings?.EnableFoam.ToString() ?? NO_DATA);
            _enableSeagulls.Text = string.Format(WaterTexts.UIWaterEnableSeagulls, _renderComponent.ClosestWater.Settings?.EnableSeagulls.ToString() ?? NO_DATA);
            _lit.Text = string.Format(WaterTexts.UIWaterLit, _renderComponent.ClosestWater.Settings?.Lit.ToString() ?? NO_DATA);
            _transparent.Text = string.Format(WaterTexts.UIWaterTransparent, _renderComponent.ClosestWater.Settings?.Transparent.ToString() ?? NO_DATA);
            _textureInput.Text = string.Format(WaterTexts.UIWaterTexture, _renderComponent.ClosestWater.Settings?.Texture.ToString() ?? NO_DATA);
            _buoyancySlider.Text = string.Format(WaterTexts.UIWaterBuoyancy, _renderComponent.ClosestWater.Settings?.Buoyancy.ToString() ?? NO_DATA);
            _collectionRateSlider.Text = string.Format(WaterTexts.UIWaterCollectionRate, _renderComponent.ClosestWater.Settings?.CollectionRate.ToString() ?? NO_DATA);
            _crushDamageSlider.Text = string.Format(WaterTexts.UIWaterCrushDamage, _renderComponent.ClosestWater.Settings?.CrushDamage.ToString() ?? NO_DATA);
            _materialIdInput.Text = string.Format(WaterTexts.UIWaterMaterialId, _renderComponent.ClosestWater.Settings?.MaterialId.ToString() ?? NO_DATA);
            _playerDrag.Text = string.Format(WaterTexts.UIWaterPlayerDrag, _renderComponent.ClosestWater.Settings?.PlayerDrag.ToString() ?? NO_DATA);

            _radiusSlider.Text = string.Format(WaterTexts.UIWaterRadius, (_renderComponent.ClosestWater.Settings?.Radius / _renderComponent.ClosestPlanet?.MinimumRadius).ToString() ?? NO_DATA);

            if (_renderComponent.ClosestWater != null)
            {
                _waveHeight.InputDialogTitle = string.Format(string.Format(WaterTexts.UIInputNumber, _renderComponent.ClosestWater.Settings.WaveHeight), _renderComponent.ClosestWater.Settings.WaveHeight);
                _waveScale.InputDialogTitle = string.Format(string.Format(WaterTexts.UIInputNumber, _renderComponent.ClosestWater.Settings.WaveScale), _renderComponent.ClosestWater.Settings.WaveScale);
                _waveSpeed.InputDialogTitle = string.Format(string.Format(WaterTexts.UIInputNumber, _renderComponent.ClosestWater.Settings.WaveSpeed), _renderComponent.ClosestWater.Settings.WaveSpeed);

                _fogColorSelector.InitialColor = _renderComponent.ClosestWater.Settings.FogColor;
                _radiusSlider.InitialPercent = RadiusValueToSlider(_renderComponent.ClosestWater.Settings.Radius / _renderComponent.ClosestPlanet.MinimumRadius);
            }
        }

        private void OnSubmitTexture(string obj)
        {
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                if (WaterData.WaterTextures.Contains(obj))
                {
                    _renderComponent.ClosestWater.Settings.Texture = SerializableStringId.Create(MyStringId.GetOrCompute(obj));
                    _syncComponent.SendSignalToServer(new WaterUpdateAddPacket
                    {
                        EntityId = _renderComponent.ClosestWater.Planet.EntityId,
                        Settings = _renderComponent.ClosestWater.Settings
                    });
                    RefreshAdminValues();
                }
            }
        }

        private object RadiusSliderToValue(float percentage)
        {
            return Math.Round(MathHelper.Lerp(0.95, 1.75, percentage), 5);
        }

        private float RadiusValueToSlider(float value)
        {
            return WaterUtils.InvLerp(0.95f, 1.75f, value);
        }

        private void OnSubmitRadius(float percentage)
        {
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                _renderComponent.ClosestWater.Settings.Radius = (float)RadiusSliderToValue(percentage) * _renderComponent.ClosestPlanet.MinimumRadius;
                _syncComponent.SendSignalToServer(new WaterUpdateAddPacket
                {
                    EntityId = _renderComponent.ClosestWater.Planet.EntityId,
                    Settings = _renderComponent.ClosestWater.Settings
                });
                RefreshAdminValues();
            }
        }

        private void OnSubmitPlayerDrag()
        {
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                _renderComponent.ClosestWater.Settings.PlayerDrag = !_renderComponent.ClosestWater.Settings.PlayerDrag;
                _syncComponent.SendSignalToServer(new WaterUpdateAddPacket
                {
                    EntityId = _renderComponent.ClosestWater.Planet.EntityId,
                    Settings = _renderComponent.ClosestWater.Settings
                });
                RefreshAdminValues();
            }
        }

        private void OnSubmitMaterial(string obj)
        {
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                if (WaterData.MaterialConfigs.ContainsKey(obj))
                {
                    _renderComponent.ClosestWater.Settings.MaterialId = obj;
                    _syncComponent.SendSignalToServer(new WaterUpdateAddPacket
                    {
                        EntityId = _renderComponent.ClosestWater.Planet.EntityId,
                        Settings = _renderComponent.ClosestWater.Settings
                    });
                    RefreshAdminValues();
                }
            }
        }

        private void OnSubmitCrushDamage(string obj)
        {
            float value;
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _renderComponent.ClosestWater.Settings.CrushDamage = value;
                _syncComponent.SendSignalToServer(new WaterUpdateAddPacket
                {
                    EntityId = _renderComponent.ClosestWater.Planet.EntityId,
                    Settings = _renderComponent.ClosestWater.Settings
                });
                RefreshAdminValues();
            }
        }

        private void OnSubmitCollectorRate(string obj)
        {
            float value;
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _renderComponent.ClosestWater.Settings.CollectionRate = value;
                _syncComponent.SendSignalToServer(new WaterUpdateAddPacket
                {
                    EntityId = _renderComponent.ClosestWater.Planet.EntityId,
                    Settings = _renderComponent.ClosestWater.Settings
                });
                RefreshAdminValues();
            }
        }

        private void OnSubmitBuoyancy(string obj)
        {
            float value;
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _renderComponent.ClosestWater.Settings.Buoyancy = value;
                _syncComponent.SendSignalToServer(new WaterUpdateAddPacket
                {
                    EntityId = _renderComponent.ClosestWater.Planet.EntityId,
                    Settings = _renderComponent.ClosestWater.Settings
                });
                RefreshAdminValues();
            }
        }

        private void OnSubmitEnableTransparency()
        {
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                _renderComponent.ClosestWater.Settings.Transparent = !_renderComponent.ClosestWater.Settings.Transparent;
                _syncComponent.SendSignalToServer(new WaterUpdateAddPacket
                {
                    EntityId = _renderComponent.ClosestWater.Planet.EntityId,
                    Settings = _renderComponent.ClosestWater.Settings
                });
                RefreshAdminValues();
            }
        }

        private void OnSubmitEnableLighting()
        {
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                _renderComponent.ClosestWater.Settings.Lit = !_renderComponent.ClosestWater.Settings.Lit;
                _syncComponent.SendSignalToServer(new WaterUpdateAddPacket
                {
                    EntityId = _renderComponent.ClosestWater.Planet.EntityId,
                    Settings = _renderComponent.ClosestWater.Settings
                });
                RefreshAdminValues();
            }
        }

        private void OnSubmitFogColor(Color color)
        {
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                _renderComponent.ClosestWater.Settings.FogColor = color;
                _syncComponent.SendSignalToServer(new WaterUpdateAddPacket
                {
                    EntityId = _renderComponent.ClosestWater.Planet.EntityId,
                    Settings = _renderComponent.ClosestWater.Settings
                });
                RefreshAdminValues();
            }
        }

        private void OnSubmitEnableSeagulls()
        {
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                _renderComponent.ClosestWater.Settings.EnableSeagulls = !_renderComponent.ClosestWater.Settings.EnableSeagulls;
                _syncComponent.SendSignalToServer(new WaterUpdateAddPacket
                {
                    EntityId = _renderComponent.ClosestWater.Planet.EntityId,
                    Settings = _renderComponent.ClosestWater.Settings
                });
                RefreshAdminValues();
            }
        }

        private void OnSubmitEnableFoam()
        {
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                _renderComponent.ClosestWater.Settings.EnableFoam = !_renderComponent.ClosestWater.Settings.EnableFoam;
                _syncComponent.SendSignalToServer(new WaterUpdateAddPacket
                {
                    EntityId = _renderComponent.ClosestWater.Planet.EntityId,
                    Settings = _renderComponent.ClosestWater.Settings
                });
                RefreshAdminValues();
            }
        }

        private void OnSubmitEnableFish()
        {
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                _renderComponent.ClosestWater.Settings.EnableSeagulls = !_renderComponent.ClosestWater.Settings.EnableFish;
                _syncComponent.SendSignalToServer(new WaterUpdateAddPacket
                {
                    EntityId = _renderComponent.ClosestWater.Planet.EntityId,
                    Settings = _renderComponent.ClosestWater.Settings
                });
                RefreshAdminValues();
            }
        }

        private void OnSubmitTideSpeed(string obj)
        {
            float value;
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _renderComponent.ClosestWater.Settings.TideSpeed = value;
                _syncComponent.SendSignalToServer(new WaterUpdateAddPacket
                {
                    EntityId = _renderComponent.ClosestWater.Planet.EntityId,
                    Settings = _renderComponent.ClosestWater.Settings
                });
                RefreshAdminValues();
            }
        }

        private void OnSubmtTideHeight(string obj)
        {
            float value;
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _renderComponent.ClosestWater.Settings.TideHeight = value;
                _syncComponent.SendSignalToServer(new WaterUpdateAddPacket
                {
                    EntityId = _renderComponent.ClosestWater.Planet.EntityId,
                    Settings = _renderComponent.ClosestWater.Settings
                });
                RefreshAdminValues();
            }
        }

        private void OnSubmitCurrentSpeed(string obj)
        {
            float value;
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _renderComponent.ClosestWater.Settings.CurrentSpeed = value;
                _syncComponent.SendSignalToServer(new WaterUpdateAddPacket
                {
                    EntityId = _renderComponent.ClosestWater.Planet.EntityId,
                    Settings = _renderComponent.ClosestWater.Settings
                });
                RefreshAdminValues();
            }
        }

        private void OnSubmitCurrentScale(string obj)
        {
            float value;
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _renderComponent.ClosestWater.Settings.CurrentScale = value;
                _syncComponent.SendSignalToServer(new WaterUpdateAddPacket
                {
                    EntityId = _renderComponent.ClosestWater.Planet.EntityId,
                    Settings = _renderComponent.ClosestWater.Settings
                });
                RefreshAdminValues();
            }
        }

        private void OnSubmitWaveSpeed(string obj)
        {
            float value;
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _renderComponent.ClosestWater.Settings.WaveSpeed = value;
                _syncComponent.SendSignalToServer(new WaterUpdateAddPacket
                {
                    EntityId = _renderComponent.ClosestWater.Planet.EntityId,
                    Settings = _renderComponent.ClosestWater.Settings
                });
                RefreshAdminValues();
            }
        }

        private void OnSubmitWaveScale(string obj)
        {
            float value;
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _renderComponent.ClosestWater.Settings.WaveScale = value;
                _syncComponent.SendSignalToServer(new WaterUpdateAddPacket
                {
                    EntityId = _renderComponent.ClosestWater.Planet.EntityId,
                    Settings = _renderComponent.ClosestWater.Settings
                });
                RefreshAdminValues();
            }
        }

        private void OnSubmitWaveHeight(string obj)
        {
            float value;
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _renderComponent.ClosestWater.Settings.WaveHeight = value;
                _syncComponent.SendSignalToServer(new WaterUpdateAddPacket
                {
                    EntityId = _renderComponent.ClosestWater.Planet.EntityId,
                    Settings = _renderComponent.ClosestWater.Settings
                });
                RefreshAdminValues();
            }
        }

        #endregion Server/Admin Settings

        #region Client Settings

        private object QualitySliderToValue(float percentage)
        {
            return (float)Math.Round(0.4f + (percentage * 2.6f), 1);
        }

        private void OnSetQualitySlider(float percentage)
        {
            _settingsComponent.Settings.Quality = (float)QualitySliderToValue(percentage);
            _settingsComponent.SaveData();
            
            RefreshClientValues();
        }

        private void OnSetVolumeSlider(float value)
        {
            _settingsComponent.Settings.Volume = value;
            _settingsComponent.SaveData();
            RefreshClientValues();
        }

        private void ToggleShowCenterOfBuoyancy()
        {
            _settingsComponent.Settings.ShowCenterOfBuoyancy = !_settingsComponent.Settings.ShowCenterOfBuoyancy;
            RefreshClientValues();
        }

        private void ToggleShowDepth()
        {
            _settingsComponent.Settings.ShowDepth = !_settingsComponent.Settings.ShowDepth;
            _settingsComponent.SaveData();
            RefreshClientValues();
        }

        private void ToggleShowAltitude()
        {
            _settingsComponent.Settings.ShowAltitude = !_settingsComponent.Settings.ShowAltitude;
            _settingsComponent.SaveData();
            RefreshClientValues();
        }

        private void ToggleShowDebug()
        {
            _settingsComponent.Settings.ShowDebug = !_settingsComponent.Settings.ShowDebug;
            RefreshClientValues();
        }

        #endregion Client Settings
    }
}
