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

        private WaterModComponent _modComponent;
        private WaterRenderComponent _renderComponent;
        private WaterSettingsComponent _settingsComponent;

        public static WaterUIComponent Static;

        public bool Heartbeat
        {
            get
            {
                return _textAPI.Heartbeat;
            }
        }

        public WaterUIComponent()
        {
            Static = this;
            UpdateOrder = MyUpdateOrder.AfterSimulation;
        }

        public override void LoadDependencies()
        {
            _modComponent = WaterModComponent.Static;
            _renderComponent = WaterRenderComponent.Static;
            _settingsComponent = WaterSettingsComponent.Static;
        }

        public override void UnloadDependencies()
        {
            _modComponent = null;
            _renderComponent = null;
            _settingsComponent = null;

            Static = null;
        }

        public override void LoadData()
        {
            _textAPI = new HudAPIv2(OnRegisteredAction);

            if (MyAPIGateway.Utilities.IsDedicated)
            {
                UpdateOrder = MyUpdateOrder.NoUpdate;
            }
        }

        public override void UpdateAfterSimulation()
        {
            if (Heartbeat)
            {
                _depthMeter.Visible = _settingsComponent.Settings.ShowDepth && (_renderComponent.CameraDepth < 0 || (_settingsComponent.Settings.ShowAltitude && ((MyAPIGateway.Session.Player?.Controller?.ControlledEntity is IMyCharacter) == false && _renderComponent.CameraDepth < 100)));

                if (_renderComponent.CameraDepth > 128)
                {
                    _depthMeter.Visible = false;
                }

                if (_renderComponent.ClosestWater != null && _renderComponent.ClosestPlanet != null && MyAPIGateway.Session.Player?.Character != null)
                {
                    if (_textAPI.Heartbeat && _depthMeter.Visible)
                    {
                        double crushDepth = WaterUtils.GetCrushDepth(_renderComponent.ClosestWater, MyAPIGateway.Session.Player.Character);
                        string message;
                        if (_renderComponent.CameraDepth < -_renderComponent.ClosestWater.WaveHeight * 2)
                            message = String.Format(WaterLocalization.CurrentLanguage.Depth, Math.Round(-_renderComponent.ClosestWater.GetDepth(ref _renderComponent.CameraPosition)));
                        else if (_renderComponent.CameraDepth < 0)
                            message = String.Format(WaterLocalization.CurrentLanguage.Depth, Math.Round(-_renderComponent.CameraDepth));
                        else
                            message = String.Format(WaterLocalization.CurrentLanguage.Altitude, Math.Round(_renderComponent.CameraDepth));

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
            _depthMeter = new HudAPIv2.HUDMessage(new StringBuilder(""), Vector2D.Zero);

            //Admin
            _adminMenuRoot = new HudAPIv2.MenuRootCategory(WaterLocalization.ModChatName + " " + WaterData.Version, HudAPIv2.MenuRootCategory.MenuFlag.AdminMenu, "Water Mod Admin Settings");

            _showFog = new HudAPIv2.MenuItem("Not yet implemented", _adminMenuRoot);
            /*showFog = new HudAPIv2.MenuItem(String.Format(WaterLocalization.CurrentLanguage.UIShowFog, WaterMod.Settings.ShowFog), adminMenuRoot, ToggleShowFog);
            refreshAdmin = new HudAPIv2.MenuItem(WaterLocalization.CurrentLanguage.UIRefreshData, adminMenuRoot, RefreshAdminValues);
            waterSubcategory = new HudAPIv2.MenuSubCategory(WaterLocalization.CurrentLanguage.UIWaterSettings, adminMenuRoot, "Water Settings");

            waterWaveSubcategory = new HudAPIv2.MenuSubCategory(WaterLocalization.CurrentLanguage.UIWaterWaveSettings, waterSubcategory, WaterLocalization.CurrentLanguage.UIWaterWaveSettings);
            waveHeight = new HudAPIv2.MenuTextInput(String.Format(WaterLocalization.CurrentLanguage.UIWaterWaveHeight, WaterMod.Session.ClosestWater?.WaveHeight.ToString() ?? NO_DATA), waterWaveSubcategory, String.Format(WaterLocalization.CurrentLanguage.UIInputNumber, WaterMod.Session.ClosestWater?.WaveHeight.ToString() ?? NO_DATA), OnSubmitWaveHeight);
            waveScale = new HudAPIv2.MenuTextInput(String.Format(WaterLocalization.CurrentLanguage.UIWaterWaveScale, WaterMod.Session.ClosestWater?.WaveScale.ToString() ?? NO_DATA), waterWaveSubcategory, String.Format(WaterLocalization.CurrentLanguage.UIInputNumber, WaterMod.Session.ClosestWater?.WaveScale.ToString() ?? NO_DATA), OnSubmitWaveScale);
            waveSpeed = new HudAPIv2.MenuTextInput(String.Format(WaterLocalization.CurrentLanguage.UIWaterWaveSpeed, WaterMod.Session.ClosestWater?.WaveSpeed.ToString() ?? NO_DATA), waterWaveSubcategory, String.Format(WaterLocalization.CurrentLanguage.UIInputNumber, WaterMod.Session.ClosestWater?.WaveSpeed.ToString() ?? NO_DATA), OnSubmitWaveSpeed);

            waterCurrentsSubcategory = new HudAPIv2.MenuSubCategory(WaterLocalization.CurrentLanguage.UIWaterCurrentSettings, waterSubcategory, WaterLocalization.CurrentLanguage.UIWaterCurrentSettings);
            currentScale = new HudAPIv2.MenuTextInput(String.Format(WaterLocalization.CurrentLanguage.UIWaterCurrentScale, WaterMod.Session.ClosestWater?.CurrentScale.ToString() ?? NO_DATA), waterCurrentsSubcategory, String.Format(WaterLocalization.CurrentLanguage.UIInputNumber, WaterMod.Session.ClosestWater?.CurrentScale.ToString() ?? NO_DATA), OnSubmitCurrentScale);
            currentSpeed = new HudAPIv2.MenuTextInput(String.Format(WaterLocalization.CurrentLanguage.UIWaterCurrentSpeed, WaterMod.Session.ClosestWater?.CurrentSpeed.ToString() ?? NO_DATA), waterCurrentsSubcategory, String.Format(WaterLocalization.CurrentLanguage.UIInputNumber, WaterMod.Session.ClosestWater?.CurrentSpeed.ToString() ?? NO_DATA), OnSubmitCurrentSpeed);

            waterTideSubcategory = new HudAPIv2.MenuSubCategory(WaterLocalization.CurrentLanguage.UIWaterTideSettings, waterSubcategory, WaterLocalization.CurrentLanguage.UIWaterTideSettings);
            tideHeight = new HudAPIv2.MenuTextInput(String.Format(WaterLocalization.CurrentLanguage.UIWaterTideHeight, WaterMod.Session.ClosestWater?.TideHeight.ToString() ?? NO_DATA), waterTideSubcategory, String.Format(WaterLocalization.CurrentLanguage.UIInputNumber, WaterMod.Session.ClosestWater?.TideHeight.ToString() ?? NO_DATA), OnSubmtTideHeight);
            tideSpeed = new HudAPIv2.MenuTextInput(String.Format(WaterLocalization.CurrentLanguage.UIWaterTideSpeed, WaterMod.Session.ClosestWater?.TideSpeed.ToString() ?? NO_DATA), waterTideSubcategory, String.Format(WaterLocalization.CurrentLanguage.UIInputNumber, WaterMod.Session.ClosestWater?.TideSpeed.ToString() ?? NO_DATA), OnSubmitTideSpeed);

            waterVisualsSubcategory = new HudAPIv2.MenuSubCategory(WaterLocalization.CurrentLanguage.UIWaterVisualSettings, waterSubcategory, WaterLocalization.CurrentLanguage.UIWaterVisualSettings);
            enableFish = new HudAPIv2.MenuItem(String.Format(WaterLocalization.CurrentLanguage.UIWaterEnableFish, WaterMod.Session.ClosestWater?.EnableFish.ToString() ?? NO_DATA), waterVisualsSubcategory, OnSubmitEnableFish);
            enableFoam = new HudAPIv2.MenuItem(String.Format(WaterLocalization.CurrentLanguage.UIWaterEnableFoam, WaterMod.Session.ClosestWater?.EnableFoam.ToString() ?? NO_DATA), waterVisualsSubcategory, OnSubmitEnableFoam);
            enableSeagulls = new HudAPIv2.MenuItem(String.Format(WaterLocalization.CurrentLanguage.UIWaterEnableSeagulls, WaterMod.Session.ClosestWater?.EnableSeagulls.ToString() ?? NO_DATA), waterVisualsSubcategory, OnSubmitEnableSeagulls);
            fogColorSelector = new HudAPIv2.MenuColorPickerInput(WaterLocalization.CurrentLanguage.UIWaterFogColor, waterVisualsSubcategory, WaterSettings.Default.FogColor, "Select fog color", OnSubmitFogColor);
            lit = new HudAPIv2.MenuItem(String.Format(WaterLocalization.CurrentLanguage.UIWaterLit, WaterMod.Session.ClosestWater?.Lit.ToString() ?? NO_DATA), waterVisualsSubcategory, OnSubmitEnableLighting);
            transparent = new HudAPIv2.MenuItem(String.Format(WaterLocalization.CurrentLanguage.UIWaterTransparent, WaterMod.Session.ClosestWater?.Transparent.ToString() ?? NO_DATA), waterVisualsSubcategory, OnSubmitEnableTransparency);
            textureInput = new HudAPIv2.MenuTextInput(String.Format(WaterLocalization.CurrentLanguage.UIWaterTexture, WaterMod.Session.ClosestWater?.Texture.ToString() ?? NO_DATA), waterVisualsSubcategory, String.Format(WaterLocalization.CurrentLanguage.UIInputNumber, WaterMod.Session.ClosestWater?.Texture ?? NO_DATA), OnSubmitTexture);

            waterGameplaySubcategory = new HudAPIv2.MenuSubCategory(WaterLocalization.CurrentLanguage.UIWaterGameplaySettings, waterSubcategory, WaterLocalization.CurrentLanguage.UIWaterGameplaySettings);
            buoyancySlider = new HudAPIv2.MenuTextInput(String.Format(WaterLocalization.CurrentLanguage.UIWaterBuoyancy, WaterMod.Session.ClosestWater?.Buoyancy.ToString() ?? NO_DATA), waterGameplaySubcategory, onSubmit: OnSubmitBuoyancy);
            CollectionRateSlider = new HudAPIv2.MenuTextInput(String.Format(WaterLocalization.CurrentLanguage.UIWaterCollectionRate, WaterMod.Session.ClosestWater?.CollectionRate.ToString() ?? NO_DATA), waterGameplaySubcategory, onSubmit: OnSubmitCollectorRate);
            CrushDamageSlider = new HudAPIv2.MenuTextInput(String.Format(WaterLocalization.CurrentLanguage.UIWaterCrushDamage, WaterMod.Session.ClosestWater?.CrushDamage.ToString() ?? NO_DATA), waterGameplaySubcategory, onSubmit: OnSubmitCrushDamage);
            materialIdInput = new HudAPIv2.MenuTextInput(String.Format(WaterLocalization.CurrentLanguage.UIWaterMaterialId, WaterMod.Session.ClosestWater?.MaterialId.ToString() ?? NO_DATA), waterGameplaySubcategory, String.Format(WaterLocalization.CurrentLanguage.UIInputNumber, WaterMod.Session.ClosestWater?.MaterialId ?? NO_DATA), OnSubmitMaterial);
            playerDrag = new HudAPIv2.MenuItem(String.Format(WaterLocalization.CurrentLanguage.UIWaterPlayerDrag, WaterMod.Session.ClosestWater?.PlayerDrag.ToString() ?? NO_DATA), waterGameplaySubcategory, OnSubmitPlayerDrag);
            radiusSlider = new HudAPIv2.MenuSliderInput(String.Format(WaterLocalization.CurrentLanguage.UIWaterRadius, WaterMod.Session.ClosestWater?.Radius.ToString() ?? NO_DATA), waterGameplaySubcategory, RadiusValueToSlider(WaterSettings.Default.Radius), OnSubmitAction: OnSubmitRadius, SliderPercentToValue: RadiusSliderToValue);*/

            //Client
            _clientMenuRoot = new HudAPIv2.MenuRootCategory(WaterLocalization.ModChatName + " " + WaterData.Version, HudAPIv2.MenuRootCategory.MenuFlag.PlayerMenu, "Water Mod Client Settings");

            _qualitySlider = new HudAPIv2.MenuSliderInput(String.Format(WaterLocalization.CurrentLanguage.UIQuality, _settingsComponent.Settings.Quality), _clientMenuRoot, 0.5f, "Adjust slider to modify value", OnSetQualitySlider, QualitySliderToValue);
            _volumeSlider = new HudAPIv2.MenuSliderInput(String.Format(WaterLocalization.CurrentLanguage.UIVolume, _settingsComponent.Settings.Volume), _clientMenuRoot, 1f, "Adjust slider to modify value", OnSetVolumeSlider);
            _showCenterOfBuoyancy = new HudAPIv2.MenuItem(String.Format(WaterLocalization.CurrentLanguage.UIShowCenterOfBuoyancy, _settingsComponent.Settings.ShowCenterOfBuoyancy), _clientMenuRoot, ToggleShowCenterOfBuoyancy);
            _showDepth = new HudAPIv2.MenuItem(String.Format(WaterLocalization.CurrentLanguage.UIShowDepth, _settingsComponent.Settings.ShowDepth), _clientMenuRoot, ToggleShowDepth);
            _showAltitude = new HudAPIv2.MenuItem(String.Format(WaterLocalization.CurrentLanguage.UIShowAltitude, _settingsComponent.Settings.ShowAltitude), _clientMenuRoot, ToggleShowAltitude);
            _showDebug = new HudAPIv2.MenuItem(String.Format(WaterLocalization.CurrentLanguage.UIShowDebug, _settingsComponent.Settings.ShowDebug), _clientMenuRoot, ToggleShowDebug);
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
            _qualitySlider.Text = String.Format(WaterLocalization.CurrentLanguage.UIQuality, _settingsComponent.Settings.Quality);
            _volumeSlider.Text = String.Format(WaterLocalization.CurrentLanguage.UIVolume, _settingsComponent.Settings.Volume);
            _showDebug.Text = String.Format(WaterLocalization.CurrentLanguage.UIShowDebug, _settingsComponent.Settings.ShowDebug);
            _showDepth.Text = String.Format(WaterLocalization.CurrentLanguage.UIShowDepth, _settingsComponent.Settings.ShowDepth);
            _showCenterOfBuoyancy.Text = String.Format(WaterLocalization.CurrentLanguage.UIShowCenterOfBuoyancy, _settingsComponent.Settings.ShowCenterOfBuoyancy);
            _showAltitude.Text = String.Format(WaterLocalization.CurrentLanguage.UIShowAltitude, _settingsComponent.Settings.ShowAltitude);
        }

        private void RefreshAdminValues()
        {
            _showFog.Text = String.Format(WaterLocalization.CurrentLanguage.UIShowFog, _settingsComponent.Settings.ShowFog);
            _waveHeight.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterWaveHeight, _renderComponent.ClosestWater?.WaveHeight.ToString() ?? NO_DATA);
            _waveScale.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterWaveScale, _renderComponent.ClosestWater?.WaveScale.ToString() ?? NO_DATA);
            _waveSpeed.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterWaveSpeed, _renderComponent.ClosestWater?.WaveSpeed.ToString() ?? NO_DATA);
            _currentScale.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterCurrentScale, _renderComponent.ClosestWater?.CurrentScale.ToString() ?? NO_DATA);
            _currentSpeed.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterCurrentSpeed, _renderComponent.ClosestWater?.CurrentSpeed.ToString() ?? NO_DATA);
            _tideHeight.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterTideHeight, _renderComponent.ClosestWater?.TideHeight.ToString() ?? NO_DATA);
            _tideSpeed.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterTideSpeed, _renderComponent.ClosestWater?.TideSpeed.ToString() ?? NO_DATA);
            _enableFish.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterEnableFish, _renderComponent.ClosestWater?.EnableFish.ToString() ?? NO_DATA);
            _enableFoam.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterEnableFoam, _renderComponent.ClosestWater?.EnableFoam.ToString() ?? NO_DATA);
            _enableSeagulls.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterEnableSeagulls, _renderComponent.ClosestWater?.EnableSeagulls.ToString() ?? NO_DATA);
            _lit.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterLit, _renderComponent.ClosestWater?.Lit.ToString() ?? NO_DATA);
            _transparent.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterTransparent, _renderComponent.ClosestWater?.Transparent.ToString() ?? NO_DATA);
            _textureInput.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterTexture, _renderComponent.ClosestWater?.Texture.ToString() ?? NO_DATA);
            _buoyancySlider.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterBuoyancy, _renderComponent.ClosestWater?.Buoyancy.ToString() ?? NO_DATA);
            _collectionRateSlider.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterCollectionRate, _renderComponent.ClosestWater?.CollectionRate.ToString() ?? NO_DATA);
            _crushDamageSlider.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterCrushDamage, _renderComponent.ClosestWater?.CrushDamage.ToString() ?? NO_DATA);
            _materialIdInput.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterMaterialId, _renderComponent.ClosestWater?.MaterialId.ToString() ?? NO_DATA);
            _playerDrag.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterPlayerDrag, _renderComponent.ClosestWater?.PlayerDrag.ToString() ?? NO_DATA);

            _radiusSlider.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterRadius, (_renderComponent.ClosestWater?.Radius / _renderComponent.ClosestPlanet.MinimumRadius).ToString() ?? NO_DATA);

            if (_renderComponent.ClosestWater != null)
            {
                _waveHeight.InputDialogTitle = String.Format(String.Format(WaterLocalization.CurrentLanguage.UIInputNumber, _renderComponent.ClosestWater.WaveHeight), _renderComponent.ClosestWater.WaveHeight);
                _waveScale.InputDialogTitle = String.Format(String.Format(WaterLocalization.CurrentLanguage.UIInputNumber, _renderComponent.ClosestWater.WaveScale), _renderComponent.ClosestWater.WaveScale);
                _waveSpeed.InputDialogTitle = String.Format(String.Format(WaterLocalization.CurrentLanguage.UIInputNumber, _renderComponent.ClosestWater.WaveSpeed), _renderComponent.ClosestWater.WaveSpeed);

                _fogColorSelector.InitialColor = _renderComponent.ClosestWater.FogColor;
                _radiusSlider.InitialPercent = RadiusValueToSlider(_renderComponent.ClosestWater.Radius / _renderComponent.ClosestPlanet.MinimumRadius);
            }
        }

        private void OnSubmitTexture(string obj)
        {
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                if (WaterData.WaterTextures.Contains(obj))
                {
                    _renderComponent.ClosestWater.Texture = obj;
                    _modComponent.SyncToServer();
                    _renderComponent.RebuildLOD();
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
                _renderComponent.ClosestWater.Radius = (float)RadiusSliderToValue(percentage) * _renderComponent.ClosestPlanet.MinimumRadius;
                _modComponent.SyncToServer();
                _renderComponent.RebuildLOD();
                RefreshAdminValues();
            }
        }

        private void OnSubmitPlayerDrag()
        {
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                _renderComponent.ClosestWater.PlayerDrag = !_renderComponent.ClosestWater.PlayerDrag;
                _modComponent.SyncToServer();
                RefreshAdminValues();
            }
        }

        private void OnSubmitMaterial(string obj)
        {
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                if (WaterData.MaterialConfigs.ContainsKey(obj))
                {
                    _renderComponent.ClosestWater.MaterialId = obj;
                    _modComponent.SyncToServer();
                    RefreshAdminValues();
                }
            }
        }

        private void OnSubmitCrushDamage(string obj)
        {
            float value;
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _renderComponent.ClosestWater.CrushDamage = value;
                _modComponent.SyncToServer();
                RefreshAdminValues();
            }
        }

        private void OnSubmitCollectorRate(string obj)
        {
            float value;
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _renderComponent.ClosestWater.CollectionRate = value;
                _modComponent.SyncToServer();
                RefreshAdminValues();
            }
        }

        private void OnSubmitBuoyancy(string obj)
        {
            float value;
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _renderComponent.ClosestWater.Buoyancy = value;
                _modComponent.SyncToServer();
                RefreshAdminValues();
            }
        }

        private void OnSubmitEnableTransparency()
        {
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                _renderComponent.ClosestWater.Transparent = !_renderComponent.ClosestWater.Transparent;
                _modComponent.SyncToServer();
                RefreshAdminValues();
            }
        }

        private void OnSubmitEnableLighting()
        {
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                _renderComponent.ClosestWater.Lit = !_renderComponent.ClosestWater.Lit;
                _modComponent.SyncToServer();
                RefreshAdminValues();
            }
        }

        private void OnSubmitFogColor(Color color)
        {
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                _renderComponent.ClosestWater.FogColor = color;
                _modComponent.SyncToServer();
                RefreshAdminValues();
            }
        }

        private void OnSubmitEnableSeagulls()
        {
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                _renderComponent.ClosestWater.EnableSeagulls = !_renderComponent.ClosestWater.EnableSeagulls;
                _modComponent.SyncToServer();
                RefreshAdminValues();
            }
        }

        private void OnSubmitEnableFoam()
        {
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                _renderComponent.ClosestWater.EnableFoam = !_renderComponent.ClosestWater.EnableFoam;
                _modComponent.SyncToServer();
                RefreshAdminValues();
            }
        }

        private void OnSubmitEnableFish()
        {
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                _renderComponent.ClosestWater.EnableSeagulls = !_renderComponent.ClosestWater.EnableFish;
                _modComponent.SyncToServer();
                RefreshAdminValues();
            }
        }

        private void OnSubmitTideSpeed(string obj)
        {
            float value;
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _renderComponent.ClosestWater.TideSpeed = value;
                _modComponent.SyncToServer();
                RefreshAdminValues();
            }
        }

        private void OnSubmtTideHeight(string obj)
        {
            float value;
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _renderComponent.ClosestWater.TideHeight = value;
                _modComponent.SyncToServer();
                RefreshAdminValues();
            }
        }

        private void OnSubmitCurrentSpeed(string obj)
        {
            float value;
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _renderComponent.ClosestWater.CurrentSpeed = value;
                _modComponent.SyncToServer();
                RefreshAdminValues();
            }
        }

        private void OnSubmitCurrentScale(string obj)
        {
            float value;
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _renderComponent.ClosestWater.CurrentScale = value;
                _modComponent.SyncToServer();
                RefreshAdminValues();
            }
        }

        private void OnSubmitWaveSpeed(string obj)
        {
            float value;
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _renderComponent.ClosestWater.WaveSpeed = value;
                _modComponent.SyncToServer();
                RefreshAdminValues();
            }
        }

        private void OnSubmitWaveScale(string obj)
        {
            float value;
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _renderComponent.ClosestWater.WaveScale = value;
                _modComponent.SyncToServer();
                RefreshAdminValues();
            }
        }

        private void OnSubmitWaveHeight(string obj)
        {
            float value;
            if (_renderComponent.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _renderComponent.ClosestWater.WaveHeight = value;
                _modComponent.SyncToServer();
                RefreshAdminValues();
            }
        }

        #endregion Server/Admin Settings

        #region Client Settings

        private object QualitySliderToValue(float percentage)
        {
            return Math.Round(0.4f + (percentage * 2.6f), 1);
        }

        private void OnSetQualitySlider(float percentage)
        {
            _settingsComponent.Settings.Quality = (float)QualitySliderToValue(percentage);
            _settingsComponent.SaveData();
            _renderComponent.RebuildLOD();
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
