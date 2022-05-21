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

namespace Jakaria.Components
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    class WaterUIComponent : MySessionComponentBase
    {
        private const string NO_DATA = "Nil";
        
        private HudAPIv2 TextAPI;

        private HudAPIv2.HUDMessage depthMeter;

        private HudAPIv2.MenuRootCategory adminMenuRoot;
        private HudAPIv2.MenuItem showFog;
        private HudAPIv2.MenuItem refreshAdmin;
        private HudAPIv2.MenuSubCategory waterSubcategory;
        private HudAPIv2.MenuSubCategory waterWaveSubcategory;
        private HudAPIv2.MenuSubCategory waterTideSubcategory;
        private HudAPIv2.MenuSubCategory waterVisualsSubcategory;
        private HudAPIv2.MenuSubCategory waterGameplaySubcategory;
        private HudAPIv2.MenuSubCategory waterCurrentsSubcategory;

        private HudAPIv2.MenuTextInput buoyancySlider;
        private HudAPIv2.MenuTextInput CollectionRateSlider;
        private HudAPIv2.MenuTextInput CrushDamageSlider;
        private HudAPIv2.MenuItem enableFish;
        private HudAPIv2.MenuItem enableFoam;
        private HudAPIv2.MenuItem enableSeagulls;
        private HudAPIv2.MenuColorPickerInput fogColorSelector;
        private HudAPIv2.MenuItem lit;
        private HudAPIv2.MenuTextInput materialIdInput;
        private HudAPIv2.MenuItem playerDrag;
        private HudAPIv2.MenuSliderInput radiusSlider;
        private HudAPIv2.MenuTextInput textureInput;
        private HudAPIv2.MenuTextInput tideHeight;
        private HudAPIv2.MenuTextInput tideSpeed;
        private HudAPIv2.MenuItem transparent;
        private HudAPIv2.MenuTextInput waveHeight;
        private HudAPIv2.MenuTextInput waveScale;
        private HudAPIv2.MenuTextInput waveSpeed;
        private HudAPIv2.MenuTextInput currentSpeed;
        private HudAPIv2.MenuTextInput currentScale;
        
        private HudAPIv2.MenuRootCategory clientMenuRoot;
        private HudAPIv2.MenuSliderInput qualitySlider;
        private HudAPIv2.MenuSliderInput volumeSlider;
        private HudAPIv2.MenuItem showCenterOfBuoyancy;
        private HudAPIv2.MenuItem showDepth;
        private HudAPIv2.MenuItem showAltitude;
        private HudAPIv2.MenuItem showDebug;
        //private HudAPIv2.MenuSliderInput languageSelector;

        private WaterModComponent _modComponent;

        public static WaterUIComponent Static;

        public bool Heartbeat
        {
            get
            {
                return TextAPI.Heartbeat;
            }
        }

        public WaterUIComponent()
        {
            if (Static == null)
                Static = this;
            else
                WaterUtils.WriteLog($"There should not be two {nameof(WaterUIComponent)}s");
        }

        public override void LoadData()
        {
            _modComponent = WaterModComponent.Static;

            TextAPI = new HudAPIv2(OnRegisteredAction);
        }
        
        public override void UpdateAfterSimulation()
        {
            if (!MyAPIGateway.Utilities.IsDedicated && Heartbeat)
            {
                depthMeter.Visible = _modComponent.Settings.ShowDepth && (_modComponent.Session.CameraDepth < 0 || (_modComponent.Settings.ShowAltitude && ((MyAPIGateway.Session.Player?.Controller?.ControlledEntity is IMyCharacter) == false && _modComponent.Session.CameraDepth < 100)));

                if (_modComponent.Session.CameraDepth > 128)
                {
                    depthMeter.Visible = false;
                }

                if (_modComponent.Session.ClosestWater != null && _modComponent.Session.ClosestPlanet != null && MyAPIGateway.Session.Player?.Character != null)
                {
                    if (TextAPI.Heartbeat && depthMeter.Visible)
                    {
                        double crushDepth = WaterUtils.GetCrushDepth(_modComponent.Session.ClosestWater, MyAPIGateway.Session.Player.Character);
                        string message;
                        if (_modComponent.Session.CameraDepth < -_modComponent.Session.ClosestWater.WaveHeight * 2)
                            message = WaterLocalization.CurrentLanguage.Depth.Replace("{0}", Math.Round(-_modComponent.Session.ClosestWater.GetDepth(ref _modComponent.Session.CameraPosition)).ToString());
                        else if (_modComponent.Session.CameraDepth < 0)
                            message = WaterLocalization.CurrentLanguage.Depth.Replace("{0}", Math.Round(-_modComponent.Session.CameraDepth).ToString());
                        else
                            message = WaterLocalization.CurrentLanguage.Altitude.Replace("{0}", Math.Round(_modComponent.Session.CameraDepth).ToString());

                        if (_modComponent.Session.CameraDepth < -crushDepth)
                            message = "- " + message + " -";

                        depthMeter.Message = new StringBuilder("\n\n\n" + message);

                        if (!_modComponent.Session.CameraAirtight)
                            depthMeter.InitialColor = Color.Lerp(Color.Lerp(Color.White, Color.Yellow, (float)MathHelper.Clamp(-_modComponent.Session.CameraDepth / crushDepth, 0f, 1f)), Color.Red, (float)-(_modComponent.Session.CameraDepth + (crushDepth - 100)) / 100);
                        else
                            depthMeter.InitialColor = Color.Lerp(depthMeter.InitialColor, Color.White, 0.25f);

                        depthMeter.Offset = new Vector2D(-depthMeter.GetTextLength().X / 2, 0);
                    }
                }
            }
        }

        private void OnRegisteredAction()
        {
            depthMeter = new HudAPIv2.HUDMessage(new StringBuilder(""), Vector2D.Zero);

            //Admin
            adminMenuRoot = new HudAPIv2.MenuRootCategory(WaterLocalization.ModChatName + " " + WaterData.Version, HudAPIv2.MenuRootCategory.MenuFlag.AdminMenu, "Water Mod Admin Settings");
            
            showFog = new HudAPIv2.MenuItem("Not yet implemented", adminMenuRoot);
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
            clientMenuRoot = new HudAPIv2.MenuRootCategory(WaterLocalization.ModChatName + " " + WaterData.Version, HudAPIv2.MenuRootCategory.MenuFlag.PlayerMenu, "Water Mod Client Settings");

            //languageSelector = new HudAPIv2.MenuSliderInput(String.Format(WaterLocalization.CurrentLanguage.UILanguage, WaterLocalization.CurrentLanguage.NativeName), clientMenuRoot, 0, "Adjust slider to modify value", OnLanguageSelectorSubmit, LanguageSelectorToValue);
            qualitySlider = new HudAPIv2.MenuSliderInput(String.Format(WaterLocalization.CurrentLanguage.UIQuality, _modComponent.Settings.Quality), clientMenuRoot, 0.5f, "Adjust slider to modify value", OnSetQualitySlider, QualitySliderToValue);
            volumeSlider = new HudAPIv2.MenuSliderInput(String.Format(WaterLocalization.CurrentLanguage.UIVolume, _modComponent.Settings.Volume), clientMenuRoot, 1f, "Adjust slider to modify value", OnSetVolumeSlider);
            showCenterOfBuoyancy = new HudAPIv2.MenuItem(String.Format(WaterLocalization.CurrentLanguage.UIShowCenterOfBuoyancy, _modComponent.Settings.ShowCenterOfBuoyancy), clientMenuRoot, ToggleShowCenterOfBuoyancy);
            showDepth = new HudAPIv2.MenuItem(String.Format(WaterLocalization.CurrentLanguage.UIShowDepth, _modComponent.Settings.ShowDepth), clientMenuRoot, ToggleShowDepth);
            showAltitude = new HudAPIv2.MenuItem(String.Format(WaterLocalization.CurrentLanguage.UIShowAltitude, _modComponent.Settings.ShowAltitude), clientMenuRoot, ToggleShowAltitude);
            showDebug = new HudAPIv2.MenuItem(String.Format(WaterLocalization.CurrentLanguage.UIShowDebug, _modComponent.Settings.ShowDebug), clientMenuRoot, ToggleShowDebug);
        }

        #region Server/Admin Settings

        private void ToggleShowFog()
        {
            if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.SpaceMaster)
                return;

            _modComponent.Settings.ShowFog = !_modComponent.Settings.ShowFog;
            RefreshAdminValues();
        }

        private void RefreshClientValues()
        {
            qualitySlider.Text = String.Format(WaterLocalization.CurrentLanguage.UIQuality, _modComponent.Settings.Quality);
            volumeSlider.Text = String.Format(WaterLocalization.CurrentLanguage.UIVolume, _modComponent.Settings.Volume);
            showDebug.Text = String.Format(WaterLocalization.CurrentLanguage.UIShowDebug, _modComponent.Settings.ShowDebug);
            showDepth.Text = String.Format(WaterLocalization.CurrentLanguage.UIShowDepth, _modComponent.Settings.ShowDepth);
            showCenterOfBuoyancy.Text = String.Format(WaterLocalization.CurrentLanguage.UIShowCenterOfBuoyancy, _modComponent.Settings.ShowCenterOfBuoyancy);
            showAltitude.Text = String.Format(WaterLocalization.CurrentLanguage.UIShowAltitude, _modComponent.Settings.ShowAltitude);
        }

        private void RefreshAdminValues()
        {
            showFog.Text = String.Format(WaterLocalization.CurrentLanguage.UIShowFog, _modComponent.Settings.ShowFog);
            waveHeight.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterWaveHeight, _modComponent.Session.ClosestWater?.WaveHeight.ToString() ?? NO_DATA);
            waveScale.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterWaveScale, _modComponent.Session.ClosestWater?.WaveScale.ToString() ?? NO_DATA);
            waveSpeed.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterWaveSpeed, _modComponent.Session.ClosestWater?.WaveSpeed.ToString() ?? NO_DATA);
            currentScale.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterCurrentScale, _modComponent.Session.ClosestWater?.CurrentScale.ToString() ?? NO_DATA);
            currentSpeed.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterCurrentSpeed, _modComponent.Session.ClosestWater?.CurrentSpeed.ToString() ?? NO_DATA);
            tideHeight.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterTideHeight, _modComponent.Session.ClosestWater?.TideHeight.ToString() ?? NO_DATA);
            tideSpeed.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterTideSpeed, _modComponent.Session.ClosestWater?.TideSpeed.ToString() ?? NO_DATA);
            enableFish.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterEnableFish, _modComponent.Session.ClosestWater?.EnableFish.ToString() ?? NO_DATA);
            enableFoam.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterEnableFoam, _modComponent.Session.ClosestWater?.EnableFoam.ToString() ?? NO_DATA);
            enableSeagulls.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterEnableSeagulls, _modComponent.Session.ClosestWater?.EnableSeagulls.ToString() ?? NO_DATA);
            lit.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterLit, _modComponent.Session.ClosestWater?.Lit.ToString() ?? NO_DATA);
            transparent.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterTransparent, _modComponent.Session.ClosestWater?.Transparent.ToString() ?? NO_DATA);
            textureInput.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterTexture, _modComponent.Session.ClosestWater?.Texture.ToString() ?? NO_DATA);
            buoyancySlider.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterBuoyancy, _modComponent.Session.ClosestWater?.Buoyancy.ToString() ?? NO_DATA);
            CollectionRateSlider.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterCollectionRate, _modComponent.Session.ClosestWater?.CollectionRate.ToString() ?? NO_DATA);
            CrushDamageSlider.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterCrushDamage, _modComponent.Session.ClosestWater?.CrushDamage.ToString() ?? NO_DATA);
            materialIdInput.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterMaterialId, _modComponent.Session.ClosestWater?.MaterialId.ToString() ?? NO_DATA);
            playerDrag.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterPlayerDrag, _modComponent.Session.ClosestWater?.PlayerDrag.ToString() ?? NO_DATA);

            radiusSlider.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterRadius, (_modComponent.Session.ClosestWater?.Radius / _modComponent.Session.ClosestPlanet.MinimumRadius).ToString() ?? NO_DATA);

            if (_modComponent.Session.ClosestWater != null)
            {
                waveHeight.InputDialogTitle = String.Format(String.Format(WaterLocalization.CurrentLanguage.UIInputNumber, _modComponent.Session.ClosestWater.WaveHeight), _modComponent.Session.ClosestWater.WaveHeight);
                waveScale.InputDialogTitle = String.Format(String.Format(WaterLocalization.CurrentLanguage.UIInputNumber, _modComponent.Session.ClosestWater.WaveScale), _modComponent.Session.ClosestWater.WaveScale);
                waveSpeed.InputDialogTitle = String.Format(String.Format(WaterLocalization.CurrentLanguage.UIInputNumber, _modComponent.Session.ClosestWater.WaveSpeed), _modComponent.Session.ClosestWater.WaveSpeed);

                fogColorSelector.InitialColor = _modComponent.Session.ClosestWater.FogColor;
                radiusSlider.InitialPercent = RadiusValueToSlider(_modComponent.Session.ClosestWater.Radius / _modComponent.Session.ClosestPlanet.MinimumRadius);
            }
        }

        private void OnSubmitTexture(string obj)
        {
            if (_modComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                if (WaterData.WaterTextures.Contains(obj))
                {
                    _modComponent.Session.ClosestWater.Texture = obj;
                    _modComponent.SyncToServer(true);
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
            if (_modComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                _modComponent.Session.ClosestWater.Radius = (float)RadiusSliderToValue(percentage) * _modComponent.Session.ClosestPlanet.MinimumRadius;
                MyAPIGateway.Utilities.ShowMessage("set radsius", "seram");
                _modComponent.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitPlayerDrag()
        {
            if (_modComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                _modComponent.Session.ClosestWater.PlayerDrag = !_modComponent.Session.ClosestWater.PlayerDrag;
                _modComponent.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitMaterial(string obj)
        {
            if (_modComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                if (WaterData.MaterialConfigs.ContainsKey(obj))
                {
                    _modComponent.Session.ClosestWater.MaterialId = obj;
                    _modComponent.SyncToServer(false);
                    RefreshAdminValues();
                }
            }
        }

        private void OnSubmitCrushDamage(string obj)
        {
            float value;
            if (_modComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _modComponent.Session.ClosestWater.CrushDamage = value;
                _modComponent.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitCollectorRate(string obj)
        {
            float value;
            if (_modComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _modComponent.Session.ClosestWater.CollectionRate = value;
                _modComponent.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitBuoyancy(string obj)
        {
            float value;
            if (_modComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _modComponent.Session.ClosestWater.Buoyancy = value;
                _modComponent.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitEnableTransparency()
        {
            if (_modComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                _modComponent.Session.ClosestWater.Transparent = !_modComponent.Session.ClosestWater.Transparent;
                _modComponent.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitEnableLighting()
        {
            if (_modComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                _modComponent.Session.ClosestWater.Lit = !_modComponent.Session.ClosestWater.Lit;
                _modComponent.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitFogColor(Color color)
        {
            if (_modComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                _modComponent.Session.ClosestWater.FogColor = color;
                _modComponent.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitEnableSeagulls()
        {
            if (_modComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                _modComponent.Session.ClosestWater.EnableSeagulls = !_modComponent.Session.ClosestWater.EnableSeagulls;
                _modComponent.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitEnableFoam()
        {
            if (_modComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                _modComponent.Session.ClosestWater.EnableFoam = !_modComponent.Session.ClosestWater.EnableFoam;
                _modComponent.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitEnableFish()
        {
            if (_modComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                _modComponent.Session.ClosestWater.EnableSeagulls = !_modComponent.Session.ClosestWater.EnableFish;
                _modComponent.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitTideSpeed(string obj)
        {
            float value;
            if (_modComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _modComponent.Session.ClosestWater.TideSpeed = value;
                _modComponent.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmtTideHeight(string obj)
        {
            float value;
            if (_modComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _modComponent.Session.ClosestWater.TideHeight = value;
                _modComponent.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitCurrentSpeed(string obj)
        {
            float value;
            if (_modComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _modComponent.Session.ClosestWater.CurrentSpeed = value;
                _modComponent.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitCurrentScale(string obj)
        {
            float value;
            if (_modComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _modComponent.Session.ClosestWater.CurrentScale = value;
                _modComponent.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitWaveSpeed(string obj)
        {
            float value;
            if (_modComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _modComponent.Session.ClosestWater.WaveSpeed = value;
                _modComponent.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitWaveScale(string obj)
        {
            float value;
            if (_modComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _modComponent.Session.ClosestWater.WaveScale = value;
                _modComponent.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitWaveHeight(string obj)
        {
            float value;
            if (_modComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                _modComponent.Session.ClosestWater.WaveHeight = value;
                _modComponent.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        #endregion Server/Admin Settings

        #region Client Settings
        /*private object LanguageSelectorToValue(float percentage)
        {
            return WaterLocalization.Languages[WaterLocalization.Languages.Keys.ToArray()[Math.Min((int)Math.Round(WaterLocalization.Languages.Keys.Count * percentage), WaterLocalization.Languages.Keys.Count - 1)]].NativeName;
        }

        private void OnLanguageSelectorSubmit(float percentage)
        {
            WaterLocalization.CurrentLanguage = WaterLocalization.Languages[WaterLocalization.Languages.Keys.ToArray()[Math.Min((int)Math.Round(WaterLocalization.Languages.Keys.Count * percentage), WaterLocalization.Languages.Keys.Count - 1)]];
            WaterMod.Static.SaveSettings();
            RefreshClientValues();
        }*/

        private object QualitySliderToValue(float percentage)
        {
            return Math.Round(0.4f + (percentage * 2.6f), 1);
        }

        private void OnSetQualitySlider(float percentage)
        {
            _modComponent.Settings.Quality = (float)QualitySliderToValue(percentage);
            _modComponent.SaveSettings();
            RefreshClientValues();
        }

        private void OnSetVolumeSlider(float value)
        {
            _modComponent.Settings.Volume = value;
            _modComponent.SaveSettings();
            RefreshClientValues();
        }

        private void ToggleShowCenterOfBuoyancy()
        {
            _modComponent.Settings.ShowCenterOfBuoyancy = !_modComponent.Settings.ShowCenterOfBuoyancy;
            RefreshClientValues();
        }

        private void ToggleShowDepth()
        {
            _modComponent.Settings.ShowDepth = !_modComponent.Settings.ShowDepth;
            _modComponent.SaveSettings();
            RefreshClientValues();
        }

        private void ToggleShowAltitude()
        {
            _modComponent.Settings.ShowAltitude = !_modComponent.Settings.ShowAltitude;
            _modComponent.SaveSettings();
            RefreshClientValues();
        }

        private void ToggleShowDebug()
        {
            _modComponent.Settings.ShowDebug = !_modComponent.Settings.ShowDebug;
            RefreshClientValues();
        }

        #endregion Client Settings
    }
}
