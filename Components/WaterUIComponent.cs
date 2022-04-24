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
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);

            TextAPI = new HudAPIv2(OnRegisteredAction);
        }

        public override void UpdateAfterSimulation()
        {
            if (!MyAPIGateway.Utilities.IsDedicated && Heartbeat)
            {
                depthMeter.Visible = WaterModComponent.Settings.ShowDepth && (WaterModComponent.Session.CameraDepth < 0 || (WaterModComponent.Settings.ShowAltitude && ((MyAPIGateway.Session.Player?.Controller?.ControlledEntity is IMyCharacter) == false && WaterModComponent.Session.CameraDepth < 100)));

                if (WaterModComponent.Session.CameraDepth > 128)
                {
                    depthMeter.Visible = false;
                }

                if (WaterModComponent.Session.ClosestWater != null && WaterModComponent.Session.ClosestPlanet != null && MyAPIGateway.Session.Player?.Character != null)
                {
                    if (TextAPI.Heartbeat && depthMeter.Visible)
                    {
                        double crushDepth = WaterUtils.GetCrushDepth(WaterModComponent.Session.ClosestWater, MyAPIGateway.Session.Player.Character);
                        string message;
                        if (WaterModComponent.Session.CameraDepth < -WaterModComponent.Session.ClosestWater.WaveHeight * 2)
                            message = WaterLocalization.CurrentLanguage.Depth.Replace("{0}", Math.Round(-WaterModComponent.Session.ClosestWater.GetDepth(ref WaterModComponent.Session.CameraPosition)).ToString());
                        else if (WaterModComponent.Session.CameraDepth < 0)
                            message = WaterLocalization.CurrentLanguage.Depth.Replace("{0}", Math.Round(-WaterModComponent.Session.CameraDepth).ToString());
                        else
                            message = WaterLocalization.CurrentLanguage.Altitude.Replace("{0}", Math.Round(WaterModComponent.Session.CameraDepth).ToString());

                        if (WaterModComponent.Session.CameraDepth < -crushDepth)
                            message = "- " + message + " -";

                        depthMeter.Message = new StringBuilder("\n\n\n" + message);

                        if (!WaterModComponent.Session.CameraAirtight)
                            depthMeter.InitialColor = Color.Lerp(Color.Lerp(Color.White, Color.Yellow, (float)MathHelper.Clamp(-WaterModComponent.Session.CameraDepth / crushDepth, 0f, 1f)), Color.Red, (float)-(WaterModComponent.Session.CameraDepth + (crushDepth - 100)) / 100);
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
            qualitySlider = new HudAPIv2.MenuSliderInput(String.Format(WaterLocalization.CurrentLanguage.UIQuality, WaterModComponent.Settings.Quality), clientMenuRoot, 0.5f, "Adjust slider to modify value", OnSetQualitySlider, QualitySliderToValue);
            volumeSlider = new HudAPIv2.MenuSliderInput(String.Format(WaterLocalization.CurrentLanguage.UIVolume, WaterModComponent.Settings.Volume), clientMenuRoot, 1f, "Adjust slider to modify value", OnSetVolumeSlider);
            showCenterOfBuoyancy = new HudAPIv2.MenuItem(String.Format(WaterLocalization.CurrentLanguage.UIShowCenterOfBuoyancy, WaterModComponent.Settings.ShowCenterOfBuoyancy), clientMenuRoot, ToggleShowCenterOfBuoyancy);
            showDepth = new HudAPIv2.MenuItem(String.Format(WaterLocalization.CurrentLanguage.UIShowDepth, WaterModComponent.Settings.ShowDepth), clientMenuRoot, ToggleShowDepth);
            showAltitude = new HudAPIv2.MenuItem(String.Format(WaterLocalization.CurrentLanguage.UIShowAltitude, WaterModComponent.Settings.ShowAltitude), clientMenuRoot, ToggleShowAltitude);
            showDebug = new HudAPIv2.MenuItem(String.Format(WaterLocalization.CurrentLanguage.UIShowDebug, WaterModComponent.Settings.ShowDebug), clientMenuRoot, ToggleShowDebug);
        }

        #region Server/Admin Settings

        private void ToggleShowFog()
        {
            if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.SpaceMaster)
                return;

            WaterModComponent.Settings.ShowFog = !WaterModComponent.Settings.ShowFog;
            RefreshAdminValues();
        }

        private void RefreshClientValues()
        {
            qualitySlider.Text = String.Format(WaterLocalization.CurrentLanguage.UIQuality, WaterModComponent.Settings.Quality);
            volumeSlider.Text = String.Format(WaterLocalization.CurrentLanguage.UIVolume, WaterModComponent.Settings.Volume);
            showDebug.Text = String.Format(WaterLocalization.CurrentLanguage.UIShowDebug, WaterModComponent.Settings.ShowDebug);
            showDepth.Text = String.Format(WaterLocalization.CurrentLanguage.UIShowDepth, WaterModComponent.Settings.ShowDepth);
            showCenterOfBuoyancy.Text = String.Format(WaterLocalization.CurrentLanguage.UIShowCenterOfBuoyancy, WaterModComponent.Settings.ShowCenterOfBuoyancy);
            showAltitude.Text = String.Format(WaterLocalization.CurrentLanguage.UIShowAltitude, WaterModComponent.Settings.ShowAltitude);
        }

        private void RefreshAdminValues()
        {
            showFog.Text = String.Format(WaterLocalization.CurrentLanguage.UIShowFog, WaterModComponent.Settings.ShowFog);
            waveHeight.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterWaveHeight, WaterModComponent.Session.ClosestWater?.WaveHeight.ToString() ?? NO_DATA);
            waveScale.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterWaveScale, WaterModComponent.Session.ClosestWater?.WaveScale.ToString() ?? NO_DATA);
            waveSpeed.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterWaveSpeed, WaterModComponent.Session.ClosestWater?.WaveSpeed.ToString() ?? NO_DATA);
            currentScale.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterCurrentScale, WaterModComponent.Session.ClosestWater?.CurrentScale.ToString() ?? NO_DATA);
            currentSpeed.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterCurrentSpeed, WaterModComponent.Session.ClosestWater?.CurrentSpeed.ToString() ?? NO_DATA);
            tideHeight.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterTideHeight, WaterModComponent.Session.ClosestWater?.TideHeight.ToString() ?? NO_DATA);
            tideSpeed.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterTideSpeed, WaterModComponent.Session.ClosestWater?.TideSpeed.ToString() ?? NO_DATA);
            enableFish.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterEnableFish, WaterModComponent.Session.ClosestWater?.EnableFish.ToString() ?? NO_DATA);
            enableFoam.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterEnableFoam, WaterModComponent.Session.ClosestWater?.EnableFoam.ToString() ?? NO_DATA);
            enableSeagulls.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterEnableSeagulls, WaterModComponent.Session.ClosestWater?.EnableSeagulls.ToString() ?? NO_DATA);
            lit.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterLit, WaterModComponent.Session.ClosestWater?.Lit.ToString() ?? NO_DATA);
            transparent.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterTransparent, WaterModComponent.Session.ClosestWater?.Transparent.ToString() ?? NO_DATA);
            textureInput.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterTexture, WaterModComponent.Session.ClosestWater?.Texture.ToString() ?? NO_DATA);
            buoyancySlider.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterBuoyancy, WaterModComponent.Session.ClosestWater?.Buoyancy.ToString() ?? NO_DATA);
            CollectionRateSlider.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterCollectionRate, WaterModComponent.Session.ClosestWater?.CollectionRate.ToString() ?? NO_DATA);
            CrushDamageSlider.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterCrushDamage, WaterModComponent.Session.ClosestWater?.CrushDamage.ToString() ?? NO_DATA);
            materialIdInput.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterMaterialId, WaterModComponent.Session.ClosestWater?.MaterialId.ToString() ?? NO_DATA);
            playerDrag.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterPlayerDrag, WaterModComponent.Session.ClosestWater?.PlayerDrag.ToString() ?? NO_DATA);

            radiusSlider.Text = String.Format(WaterLocalization.CurrentLanguage.UIWaterRadius, (WaterModComponent.Session.ClosestWater?.Radius / WaterModComponent.Session.ClosestPlanet.MinimumRadius).ToString() ?? NO_DATA);

            if (WaterModComponent.Session.ClosestWater != null)
            {
                waveHeight.InputDialogTitle = String.Format(String.Format(WaterLocalization.CurrentLanguage.UIInputNumber, WaterModComponent.Session.ClosestWater.WaveHeight), WaterModComponent.Session.ClosestWater.WaveHeight);
                waveScale.InputDialogTitle = String.Format(String.Format(WaterLocalization.CurrentLanguage.UIInputNumber, WaterModComponent.Session.ClosestWater.WaveScale), WaterModComponent.Session.ClosestWater.WaveScale);
                waveSpeed.InputDialogTitle = String.Format(String.Format(WaterLocalization.CurrentLanguage.UIInputNumber, WaterModComponent.Session.ClosestWater.WaveSpeed), WaterModComponent.Session.ClosestWater.WaveSpeed);

                fogColorSelector.InitialColor = WaterModComponent.Session.ClosestWater.FogColor;
                radiusSlider.InitialPercent = RadiusValueToSlider(WaterModComponent.Session.ClosestWater.Radius / WaterModComponent.Session.ClosestPlanet.MinimumRadius);
            }
        }

        private void OnSubmitTexture(string obj)
        {
            if (WaterModComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                if (WaterData.WaterTextures.Contains(obj))
                {
                    WaterModComponent.Session.ClosestWater.Texture = obj;
                    WaterModComponent.Static.SyncToServer(true);
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
            if (WaterModComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                WaterModComponent.Session.ClosestWater.Radius = (float)RadiusSliderToValue(percentage) * WaterModComponent.Session.ClosestPlanet.MinimumRadius;
                MyAPIGateway.Utilities.ShowMessage("set radsius", "seram");
                WaterModComponent.Static.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitPlayerDrag()
        {
            if (WaterModComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                WaterModComponent.Session.ClosestWater.PlayerDrag = !WaterModComponent.Session.ClosestWater.PlayerDrag;
                WaterModComponent.Static.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitMaterial(string obj)
        {
            if (WaterModComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                if (WaterData.MaterialConfigs.ContainsKey(obj))
                {
                    WaterModComponent.Session.ClosestWater.MaterialId = obj;
                    WaterModComponent.Static.SyncToServer(false);
                    RefreshAdminValues();
                }
            }
        }

        private void OnSubmitCrushDamage(string obj)
        {
            float value;
            if (WaterModComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                WaterModComponent.Session.ClosestWater.CrushDamage = value;
                WaterModComponent.Static.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitCollectorRate(string obj)
        {
            float value;
            if (WaterModComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                WaterModComponent.Session.ClosestWater.CollectionRate = value;
                WaterModComponent.Static.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitBuoyancy(string obj)
        {
            float value;
            if (WaterModComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                WaterModComponent.Session.ClosestWater.Buoyancy = value;
                WaterModComponent.Static.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitEnableTransparency()
        {
            if (WaterModComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                WaterModComponent.Session.ClosestWater.Transparent = !WaterModComponent.Session.ClosestWater.Transparent;
                WaterModComponent.Static.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitEnableLighting()
        {
            if (WaterModComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                WaterModComponent.Session.ClosestWater.Lit = !WaterModComponent.Session.ClosestWater.Lit;
                WaterModComponent.Static.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitFogColor(Color color)
        {
            if (WaterModComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                WaterModComponent.Session.ClosestWater.FogColor = color;
                WaterModComponent.Static.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitEnableSeagulls()
        {
            if (WaterModComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                WaterModComponent.Session.ClosestWater.EnableSeagulls = !WaterModComponent.Session.ClosestWater.EnableSeagulls;
                WaterModComponent.Static.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitEnableFoam()
        {
            if (WaterModComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                WaterModComponent.Session.ClosestWater.EnableFoam = !WaterModComponent.Session.ClosestWater.EnableFoam;
                WaterModComponent.Static.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitEnableFish()
        {
            if (WaterModComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                WaterModComponent.Session.ClosestWater.EnableSeagulls = !WaterModComponent.Session.ClosestWater.EnableFish;
                WaterModComponent.Static.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitTideSpeed(string obj)
        {
            float value;
            if (WaterModComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                WaterModComponent.Session.ClosestWater.TideSpeed = value;
                WaterModComponent.Static.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmtTideHeight(string obj)
        {
            float value;
            if (WaterModComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                WaterModComponent.Session.ClosestWater.TideHeight = value;
                WaterModComponent.Static.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitCurrentSpeed(string obj)
        {
            float value;
            if (WaterModComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                WaterModComponent.Session.ClosestWater.CurrentSpeed = value;
                WaterModComponent.Static.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitCurrentScale(string obj)
        {
            float value;
            if (WaterModComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                WaterModComponent.Session.ClosestWater.CurrentScale = value;
                WaterModComponent.Static.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitWaveSpeed(string obj)
        {
            float value;
            if (WaterModComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                WaterModComponent.Session.ClosestWater.WaveSpeed = value;
                WaterModComponent.Static.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitWaveScale(string obj)
        {
            float value;
            if (WaterModComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                WaterModComponent.Session.ClosestWater.WaveScale = value;
                WaterModComponent.Static.SyncToServer(false);
                RefreshAdminValues();
            }
        }

        private void OnSubmitWaveHeight(string obj)
        {
            float value;
            if (WaterModComponent.Session.ClosestWater != null && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && float.TryParse(obj, out value))
            {
                WaterModComponent.Session.ClosestWater.WaveHeight = value;
                WaterModComponent.Static.SyncToServer(false);
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
            WaterModComponent.Settings.Quality = (float)QualitySliderToValue(percentage);
            WaterModComponent.Static.SaveSettings();
            RefreshClientValues();
        }

        private void OnSetVolumeSlider(float value)
        {
            WaterModComponent.Settings.Volume = value;
            WaterModComponent.Static.SaveSettings();
            RefreshClientValues();
        }

        private void ToggleShowCenterOfBuoyancy()
        {
            WaterModComponent.Settings.ShowCenterOfBuoyancy = !WaterModComponent.Settings.ShowCenterOfBuoyancy;
            RefreshClientValues();
        }

        private void ToggleShowDepth()
        {
            WaterModComponent.Settings.ShowDepth = !WaterModComponent.Settings.ShowDepth;
            WaterModComponent.Static.SaveSettings();
            RefreshClientValues();
        }

        private void ToggleShowAltitude()
        {
            WaterModComponent.Settings.ShowAltitude = !WaterModComponent.Settings.ShowAltitude;
            WaterModComponent.Static.SaveSettings();
            RefreshClientValues();
        }

        private void ToggleShowDebug()
        {
            WaterModComponent.Settings.ShowDebug = !WaterModComponent.Settings.ShowDebug;
            RefreshClientValues();
        }

        #endregion Client Settings
    }
}
