using Jakaria.Configs;
using Jakaria.Utils;
using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Jakaria.SessionComponents
{
    /// <summary>
    /// Component used for parsing in-game chat commands
    /// </summary>
    public class WaterCommandComponent : SessionComponentBase
    {
        private WaterModComponent _modComponent;
        private WaterRenderComponent _renderComponent;
        private WaterUIComponent _uiComponent;
        private WaterSettingsComponent _settingsComponent;

        public static WaterCommandComponent Static;

        private Dictionary<string, Command> _commands = new Dictionary<string, Command>();


        public WaterCommandComponent()
        {
            Static = this;
            UpdateOrder = MyUpdateOrder.NoUpdate;
        }

        public override void LoadDependencies()
        {
            _modComponent = WaterModComponent.Static;
            _renderComponent = WaterRenderComponent.Static;
            _uiComponent = WaterUIComponent.Static;
            _settingsComponent = WaterSettingsComponent.Static;
        }

        public override void UnloadDependencies()
        {
            _modComponent = null;
            _renderComponent = null;
            _uiComponent = null;
            _settingsComponent = null;

            Static = null;
        }

        public override void LoadData()
        {
            _commands["whelp"] = new Command(CommandHelp)
            {
                Description = "Opens the steam help guide or provides extended information on a specific command.",
                MinArgs = 1,
                MaxArgs = 2,
            };
            _commands["wcommandlist"] = _commands["wcommands"] = new Command(CommandCommandList)
            {
                Description = "Lists all available commands.",
            };
            _commands["wdiscord"] = new Command(CommandDiscord)
            {
                Description = "Opens the invite link to the Water Mod Discord.",
            };
            _commands["wversion"] = new Command(CommandVersion)
            {
                Description = "Tells the current mod version.",
            };
            _commands["wlanguage"] = new Command(CommandLanguage)
            {
                Description = "Sets the language",
                MinArgs = 2,
                MaxArgs = 2,
            };
            _commands["wquality"] = new Command(CommandQuality)
            {
                Description = "Gets or sets the render quality.",
                MinArgs = 1,
                MaxArgs = 2,

            };
            _commands["wvolume"] = new Command(CommandVolume)
            {
                Description = "Gets or sets the volume of water sounds.",
                MinArgs = 1,
                MaxArgs = 2,
            };
            _commands["wcob"] = new Command(CommandCenterOfBuoyancy)
            {
                Description = "Toggles rendering of a center of buoyancy indicator.",
            };
            _commands["wdebug"] = new Command(CommandDebug)
            {
                Description = "Toggles debug mode.",
            };
            _commands["wdepth"] = new Command(CommandDepth)
            {
                Description = "Toggles depth indicator underwater. Requires Text Hud API.",
            };
            _commands["waltitude"] = new Command(CommandAltitude)
            {
                Description = "Toggles sea-level altitude indicator in cockpits. Requires Text Hud API.",
            };
            _commands["wfog"] = new Command(CommandFog)
            {
                Description = "Toggles fog.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                RequireWater = true,
            };
            _commands["wbird"] = new Command(CommandBird)
            {
                Description = "Toggles birds.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                RequireWater = true,
            };
            _commands["wfish"] = new Command(CommandFish)
            {
                Description = "Toggles fish.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                RequireWater = true,
            };
            _commands["wfoam"] = new Command(CommandFoam)
            {
                Description = "Toggles sea foam.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                RequireWater = true,
            };
            _commands["wbuoyancy"] = new Command(CommandBuoyancy)
            {
                Description = "Gets or sets the buoyancy force multiplier.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                MinArgs = 1,
                MaxArgs = 2,
                RequireWater = true,
            };
            _commands["wrelativeradius"] = _commands["wrradius"] = new Command(CommandRelativeRadius)
            {
                Description = "Calculates a radius relative the position of the camera.",
                RequireWater = true,
            };
            _commands["wradius"] = new Command(CommandRadius)
            {
                Description = "Gets or sets the radius of the closest water.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                MinArgs = 1,
                MaxArgs = 2,
                RequireWater = true,
            };
            _commands["wcurrentspeed"] = new Command(CommandCurrentSpeed)
            {
                Description = "Gets or sets the speed of currents in m/s.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                MinArgs = 1,
                MaxArgs = 2,
                RequireWater = true,
            };
            _commands["wcurrentscale"] = new Command(CommandCurrentScale)
            {
                Description = "Gets or sets the horizontal scale of currents. Smaller numbers are larger.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                MinArgs = 1,
                MaxArgs = 2,
                RequireWater = true,
            };
            _commands["wwaveheight"] = new Command(CommandWaveHeight)
            {
                Description = "Gets or sets the height of waves in meters.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                MinArgs = 1,
                MaxArgs = 2,
                RequireWater = true,
            };
            _commands["wwavespeed"] = new Command(CommandWaveSpeed)
            {
                Description = "Gets or sets the speed waves move.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                MinArgs = 1,
                MaxArgs = 2,
                RequireWater = true,
            };
            _commands["wwavescale"] = new Command(CommandWaveScale)
            {
                Description = "Gets or sets the horizontal scale of waves. Smaller numbers are larger.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                MinArgs = 1,
                MaxArgs = 2,
                RequireWater = true,
            };
            _commands["wtideheight"] = new Command(CommandTideHeight)
            {
                Description = "Gets or sets the height of the tide in meters.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                MinArgs = 1,
                MaxArgs = 2,
                RequireWater = true,
            };
            _commands["wtidespeed"] = new Command(CommandTideSpeed)
            {
                Description = "Gets or sets the speed of the tide.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                MinArgs = 1,
                MaxArgs = 2,
                RequireWater = true,
            };
            _commands["wfogcolor"] = _commands["wcolor"] = new Command(CommandFogColor)
            {
                Description = "Gets or sets fog color in 0-1, RGB format.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                MinArgs = 1,
                MaxArgs = 4,
                RequireWater = true,
            };
            _commands["wreset"] = new Command(CommandReset)
            {
                Description = "Resets all settings of the closest water, only preserving radius.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                RequireWater = true,
            };
            _commands["wcreate"] = new Command(CommandCreate)
            {
                Description = "Creates water at the closest planet with an optional radius parameter.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                MinArgs = 1,
                MaxArgs = 2,
                RequirePlanet = true,
            };
            _commands["wremove"] = new Command(CommandRemove)
            {
                Description = "Removes the closest water.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                RequireWater = true,
            };
            _commands["wpdrag"] = new Command(CommandPlayerDrag)
            {
                Description = "Toggles player drag.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                RequireWater = true,
            };
            _commands["wtransparent"] = new Command(CommandTransparent)
            {
                Description = "Toggles transparency of the water surface.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                RequireWater = true,
            };
            _commands["wlit"] = new Command(CommandLit)
            {
                Description = "Toggles lighting on the water surface.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                RequireWater = true,
            };
            _commands["wtexturelist"] = _commands["wtextures"] = new Command(CommandTextures)
            {
                Description = "Gets a list of all available textures.",
            };
            _commands["wmateriallist"] = _commands["wmaterials"] = new Command(CommandMaterials)
            {
                Description = "Gets a list of all available physics materials.",
            };
            _commands["wtexture"] = new Command(CommandTexture)
            {
                Description = "Gets or sets the surface texture.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                MinArgs = 1,
                MaxArgs = 2,
                RequireWater = true,
            };
            _commands["wmaterial"] = new Command(CommandMaterial)
            {
                Description = "Gets or sets the physics material.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                MinArgs = 1,
                MaxArgs = 2,
                RequireWater = true,
            };
            _commands["wexport"] = new Command(CommandExport)
            {
                Description = "Serializes a copy of the Water Settings to the Clipboard. Used for ModAPI.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                RequireWater = true,
            };
            _commands["wsettings"] = new Command(CommandSettings)
            {
                Description = "Prints all the current settings of the Water.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                RequireWater = true,
            };
            _commands["wcrush"] = new Command(CommandCrush)
            {
                Description = "Gets or sets the crush damage.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                MinArgs = 1,
                MaxArgs = 2,
                RequireWater = true,
            };
            _commands["wrate"] = new Command(CommandRate)
            {
                Description = "Gets or sets the collection rate multiplier.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                MinArgs = 1,
                MaxArgs = 2,
                RequireWater = true,
            };

            MyAPIGateway.Utilities.MessageEntered += Utilities_MessageEntered;
        }

        public override void UnloadData()
        {
            MyAPIGateway.Utilities.MessageEntered -= Utilities_MessageEntered;
        }

        public void Utilities_MessageEntered(string messageText, ref bool sendToOthers)
        {
            if (!messageText.StartsWith("/") || messageText.Length == 0)
                return;

            string[] args = messageText.TrimStart('/').Split(' ');

            if (args.Length == 0)
                return;

            Command command;
            if (_commands.TryGetValue(args[0], out command))
            {
                sendToOthers = false;

                if (MyAPIGateway.Session.PromoteLevel >= command.PromoteLevel)
                {
                    if (command.RequirePlanet && _renderComponent.ClosestPlanet == null)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                        return;
                    }

                    if (command.RequireWater && _renderComponent.ClosestWater == null)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoWater);
                        return;
                    }

                    if (args.Length < command.MinArgs)
                    {
                        WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.GenericMinArgs, command.MinArgs));
                        return;
                    }

                    if (args.Length > command.MaxArgs)
                    {
                        WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.GenericMaxArgs, command.MaxArgs));
                        return;
                    }

                    command.Action.Invoke(args);
                }
                else
                {
                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                }
            }
        }

        private void CommandCommandList(string[] args)
        {
            string commands = "";

            foreach (var command in _commands)
            {
                commands += command.Key + ", ";
            }

            if (commands.Length > 2)
            {
                commands = commands.Substring(0, commands.Length - 2) + "."; //remove end comma and space. add period
                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ListCommands + commands);
            }
        }

        private void CommandHelp(string[] args)
        {
            if (args.Length == 1)
            {
                MyVisualScriptLogicProvider.OpenSteamOverlayLocal(@"https://steamcommunity.com/sharedfiles/filedetails/?id=2574095672");
                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.OpenGuide);
            }

            if (args.Length == 2)
            {
                Command command;
                if (_commands.TryGetValue(args[1], out command))
                {
                    WaterUtils.ShowMessage($"/{args[1]}: {command.Description} Min Promote Level: {command.PromoteLevel}");
                }
            }
        }

        private void CommandDiscord(string[] args)
        {
            MyVisualScriptLogicProvider.OpenSteamOverlayLocal(@"https://steamcommunity.com/sharedfiles/filedetails/?id=2574095672");
            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.OpenGuide);
        }

        private void CommandVersion(string[] args)
        {
            WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.WaterModVersion, WaterData.Version));
        }

        private void CommandLanguage(string[] args)
        {
            Language language;

            if (WaterLocalization.Languages.TryGetValue(WaterUtils.ValidateCommandData(args[1]), out language))
            {
                WaterLocalization.CurrentLanguage = language;

                WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetLanguage, args[1], WaterLocalization.CurrentLanguage.TranslationAuthor));
                _settingsComponent.SaveData();
            }
            else
                WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetLanguageNoParse, args[1]));
        }

        private void CommandQuality(string[] args)
        {
            //Get Quality
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.GetQuality, _settingsComponent.Settings.Quality));
            }

            //Set Quality
            if (args.Length == 2)
            {
                float quality;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out quality))
                {
                    if (_settingsComponent.Settings.ShowDebug)
                        _settingsComponent.Settings.Quality = quality;
                    else
                        _settingsComponent.Settings.Quality = MathHelper.Clamp(quality, 0.4f, 3f);

                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetQuality, _settingsComponent.Settings.Quality));
                    _settingsComponent.SaveData();
                    _renderComponent.RebuildLOD();
                }
                else
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetQualityNoParse, args[1]));
            }
        }

        private void CommandVolume(string[] args)
        {
            //Get Volume
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.GetVolume, _settingsComponent.Settings.Volume));
            }

            //Set Volume
            if (args.Length == 2)
            {
                float volume;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out volume))
                {
                    volume = MathHelper.Clamp(volume, 0f, 1f);
                    _settingsComponent.Settings.Volume = volume;
                    _settingsComponent.SaveData();

                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetVolume, _settingsComponent.Settings.Volume));
                }
                else
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetVolumeNoParse, args[1]));
            }
        }

        private void CommandCenterOfBuoyancy(string[] args)
        {
            _settingsComponent.Settings.ShowCenterOfBuoyancy = !_settingsComponent.Settings.ShowCenterOfBuoyancy;
            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleRenderCOB);
        }

        private void CommandDebug(string[] args)
        {
            _settingsComponent.Settings.ShowDebug = !_settingsComponent.Settings.ShowDebug;
            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleDebug);
        }

        private void CommandDepth(string[] args)
        {
            if (_uiComponent.Heartbeat)
            {
                _settingsComponent.Settings.ShowDepth = !_settingsComponent.Settings.ShowDepth;
                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleShowDepth);
                _settingsComponent.SaveData();
            }
            else
            {
                _settingsComponent.Settings.ShowDepth = false;
                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoTextAPI);
            }
        }

        private void CommandAltitude(string[] args)
        {
            if (_uiComponent.Heartbeat)
            {
                _settingsComponent.Settings.ShowAltitude = !_settingsComponent.Settings.ShowAltitude;
                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleShowAltitude);
                _settingsComponent.SaveData();
            }
            else
            {
                _settingsComponent.Settings.ShowAltitude = false;
                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoTextAPI);
            }
        }

        private void CommandFog(string[] args)
        {
            _settingsComponent.Settings.ShowFog = !_settingsComponent.Settings.ShowFog;
            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleFog);
        }

        private void CommandBird(string[] args)
        {
            _renderComponent.ClosestWater.EnableSeagulls = !_renderComponent.ClosestWater.EnableSeagulls;
            _modComponent.SyncToServer();
            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleBirds);
        }

        private void CommandFish(string[] args)
        {
            _renderComponent.ClosestWater.EnableFish = !_renderComponent.ClosestWater.EnableFish;
            _modComponent.SyncToServer();
            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleFish);
        }

        private void CommandFoam(string[] args)
        {
            _renderComponent.ClosestWater.EnableFoam = !_renderComponent.ClosestWater.EnableFoam;
            _modComponent.SyncToServer();
            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleFoam);
        }

        private void CommandBuoyancy(string[] args)
        {
            //Get Buoyancy
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.GetBuoyancy, _renderComponent.ClosestWater.Buoyancy));
            }

            //Set Buoyancy
            if (args.Length == 2)
            {
                float buoyancy;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out buoyancy))
                {
                    _renderComponent.ClosestWater.Buoyancy = MathHelper.Clamp(buoyancy, 0f, 10f);
                    _modComponent.SyncToServer();
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetBuoyancy, _renderComponent.ClosestWater.Buoyancy));
                }
                else
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetBuoyancyNoParse, args[1]));
            }
        }

        private void CommandRelativeRadius(string[] args)
        {
            double radius = ((Vector3D.Distance(_renderComponent.CameraPosition, _renderComponent.ClosestWater.Position) - WaterSettings.Default.TideHeight) / _renderComponent.ClosestPlanet.MinimumRadius);
            WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.GetRelativeRadius, radius.ToString("0.00000")));
        }

        private void CommandRadius(string[] args)
        {
            //Get Radius
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.GetRadius, (_renderComponent.ClosestWater.Radius / _renderComponent.ClosestPlanet.MinimumRadius)));
            }

            //Set Radius
            if (args.Length == 2)
            {
                float radius;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out radius))
                {
                    radius = MathHelper.Clamp(radius, 0.95f, 1.75f);

                    _renderComponent.ClosestWater.Radius = radius * _renderComponent.ClosestPlanet.MinimumRadius;
                    _modComponent.SyncToServer();
                    _renderComponent.RebuildLOD();
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetRadius, (_renderComponent.ClosestWater.Radius / _renderComponent.ClosestPlanet.MinimumRadius)));
                }
                else
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetRadiusNoParse, args[1]));
            }
        }

        private void CommandCurrentSpeed(string[] args)
        {
            //Get Current Speed
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.GetCurrentSpeed, _renderComponent.ClosestWater.CurrentSpeed));
            }

            //Set Current Speed
            if (args.Length == 2)
            {
                float currentSpeed;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out currentSpeed))
                {
                    _renderComponent.ClosestWater.CurrentSpeed = currentSpeed;
                    _modComponent.SyncToServer();
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetCurrentSpeed, _renderComponent.ClosestWater.CurrentSpeed));
                }
                else
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetCurrentSpeedNoParse, args[1]));
            }
        }
        private void CommandCurrentScale(string[] args)
        {
            //Get Current Scale
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.GetCurrentScale, _renderComponent.ClosestWater.CurrentScale));
            }

            //Set Current Scale
            if (args.Length == 2)
            {
                float currentScale;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out currentScale))
                {
                    _renderComponent.ClosestWater.CurrentScale = currentScale;
                    _modComponent.SyncToServer();
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetCurrentScale, _renderComponent.ClosestWater.CurrentScale));
                }
                else
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetCurrentScaleNoParse, args[1]));
            }
        }

        private void CommandWaveHeight(string[] args)
        {
            //Get Wave Height
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.GetWaveHeight, _renderComponent.ClosestWater.WaveHeight));
            }

            //Set Wave Height
            if (args.Length == 2)
            {
                float waveHeight;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out waveHeight))
                {
                    _renderComponent.ClosestWater.WaveHeight = waveHeight;
                    _modComponent.SyncToServer();
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetWaveHeight, _renderComponent.ClosestWater.WaveHeight));
                }
                else
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetWaveHeightNoParse, args[1]));
            }
        }

        private void CommandWaveSpeed(string[] args)
        {
            //Get Wave Speed
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.GetWaveSpeed, _renderComponent.ClosestWater.WaveSpeed));
            }

            //Set Wave Speed
            if (args.Length == 2)
            {
                float waveSpeed;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out waveSpeed))
                {
                    _renderComponent.ClosestWater.WaveSpeed = MathHelper.Clamp(waveSpeed, 0f, 1f);
                    _modComponent.SyncToServer();
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetWaveSpeed, _renderComponent.ClosestWater.WaveSpeed));
                }
                else
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetWaveSpeedNoParse, args[1]));
            }
        }

        private void CommandWaveScale(string[] args)
        {
            //Get Wave Scale
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.GetWaveScale, _renderComponent.ClosestWater.WaveScale));
            }

            //Set Wave Scale
            if (args.Length == 2)
            {
                float waveScale;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out waveScale))
                {
                    _renderComponent.ClosestWater.WaveScale = waveScale;
                    _modComponent.SyncToServer();
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetWaveScale, _renderComponent.ClosestWater.WaveScale));

                }
                else
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetWaveScaleNoParse, _renderComponent.ClosestWater.WaveScale));
            }
        }

        private void CommandTideHeight(string[] args)
        {
            //Get Tide Height
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.GetTideHeight, _renderComponent.ClosestWater.TideHeight));
            }

            //Set Tide Height
            if (args.Length == 2)
            {
                float tideHeight;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out tideHeight))
                {
                    _renderComponent.ClosestWater.TideHeight = MyMath.Clamp(tideHeight, 0, 10000);
                    _modComponent.SyncToServer();
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetTideHeight, _renderComponent.ClosestWater.TideHeight));
                }
                else
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetTideHeightNoParse, args[1]));
            }
        }

        private void CommandTideSpeed(string[] args)
        {
            //Get Tide Speed
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.GetTideSpeed, _renderComponent.ClosestWater.TideSpeed));
            }

            //Set Tide Speed
            if (args.Length == 2)
            {
                float tideSpeed;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out tideSpeed))
                {
                    _renderComponent.ClosestWater.TideSpeed = MyMath.Clamp(tideSpeed, 0, 1000);
                    _modComponent.SyncToServer();
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetTideSpeed, _renderComponent.ClosestWater.TideSpeed));
                }
                else
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetTideSpeedNoParse, args[1]));
            }
        }

        private void CommandReset(string[] args)
        {
            foreach (var Key in _modComponent.Waters.Keys)
            {
                if (_modComponent.Waters[Key].PlanetID == _renderComponent.ClosestPlanet.EntityId)
                {
                    float radius = _modComponent.Waters[Key].Radius;
                    _modComponent.Waters[Key] = new Water(_renderComponent.ClosestPlanet) { Radius = radius };

                    _modComponent.SyncToServer();
                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.Reset);
                    return;
                }
            }
        }

        private void CommandCreate(string[] args)
        {
            if (WaterUtils.HasWater(_renderComponent.ClosestPlanet))
            {
                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.HasWater);
                return;
            }

            if (args.Length == 1)
            {
                _modComponent.Waters[_renderComponent.ClosestPlanet.EntityId] = new Water(_renderComponent.ClosestPlanet);
                _modComponent.SyncToServer();
                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.CreateWater);
            }

            if (args.Length == 2)
            {
                float radius;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out radius))
                {
                    _modComponent.Waters[_renderComponent.ClosestPlanet.EntityId] = new Water(_renderComponent.ClosestPlanet, radiusMultiplier: MathHelper.Clamp(radius, 0.01f, 2f));
                    _modComponent.SyncToServer();
                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.CreateWater);
                }
                else
                {
                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoParse);
                }
            }
        }

        private void CommandRemove(string[] args)
        {
            if (_modComponent.Waters.Remove(_renderComponent.ClosestPlanet.EntityId))
            {
                _modComponent.SyncToServer();
                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.RemoveWater);
                return;
            }
        }

        private void CommandPlayerDrag(string[] args)
        {
            _renderComponent.ClosestWater.PlayerDrag = !_renderComponent.ClosestWater.PlayerDrag;
            _modComponent.SyncToServer();
            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.TogglePlayerDrag);
        }

        private void CommandTransparent(string[] args)
        {
            _renderComponent.ClosestWater.Transparent = !_renderComponent.ClosestWater.Transparent;
            _modComponent.SyncToServer();
            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleTransparency);
        }
        private void CommandLit(string[] args)
        {
            _renderComponent.ClosestWater.Lit = !_renderComponent.ClosestWater.Lit;
            _modComponent.SyncToServer();
            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleLighting);
        }

        private void CommandTextures(string[] args)
        {
            if (WaterData.WaterTextures.Count > 0)
            {
                string textures = "";

                foreach (var texture in WaterData.WaterTextures)
                {
                    textures += texture + ", ";
                }

                if (textures.Length > 2)
                {
                    textures = textures.Substring(0, textures.Length - 2) + "."; //remove end comma and space. add period
                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ListTextures + textures);
                }
            }
            else
            {
                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoTextures);
            }
        }
        private void CommandMaterials(string[] args)
        {
            if (WaterData.MaterialConfigs.Count > 0)
            {
                string materials = "";

                foreach (var material in WaterData.MaterialConfigs)
                {
                    materials += material.Key + ", ";
                }

                if (materials.Length > 2)
                {
                    materials = materials.Substring(0, materials.Length - 2) + "."; //remove end comma and space. add period
                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ListMaterials + materials);
                }
            }
            else
            {
                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoMaterials);
            }
        }
        private void CommandTexture(string[] args)
        {
            //Get Texture
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.GetTexture, _renderComponent.ClosestWater.Texture));
            }

            //Set Texture
            if (args.Length == 2)
            {
                if (!WaterData.WaterTextures.Contains(args[1]))
                {
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetTextureNoFind, args[1]));
                    return;
                }
                //TODO REDO THIS
                MyStringId waterTexture = MyStringId.GetOrCompute(args[1]);
                if (waterTexture != null)
                {
                    _renderComponent.ClosestWater.TextureID = waterTexture;
                    _renderComponent.ClosestWater.Texture = args[1];

                    _renderComponent.RebuildLOD();
                    _modComponent.SyncToServer();
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetTexture, _renderComponent.ClosestWater.Texture));
                }
                else
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetTextureNoFind, args[1]));
            }
        }

        private void CommandMaterial(string[] args)
        {
            //Get Material
            if (args.Length == 1)
            {
                if (_renderComponent.ClosestPlanet == null)
                {
                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                    return;
                }

                WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.GetMaterial, _renderComponent.ClosestWater.Material.SubtypeId));
            }

            //Set Material
            if (args.Length == 2)
            {
                if (WaterData.MaterialConfigs.ContainsKey(args[1]))
                {
                    _renderComponent.ClosestWater.MaterialId = args[1];

                    _modComponent.SyncToServer();
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetMaterial, _renderComponent.ClosestWater.Material.SubtypeId));
                }
                else
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetMaterialNoFind, args[1]));
            }
        }

        private void CommandExport(string[] args)
        {
            PlanetConfig temp = new PlanetConfig(_renderComponent.ClosestWater.Planet.Generator.Id.SubtypeName, _renderComponent.ClosestWater);
            MyClipboardHelper.SetClipboard(MyAPIGateway.Utilities.SerializeToXML(temp));
            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ExportWater);
        }

        private void CommandSettings(string[] args)
        {
            WaterUtils.ShowMessage(_renderComponent.ClosestWater.ToString());
        }

        private void CommandCrush(string[] args)
        {
            //Get Crush Damage
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.GetCrushDamage, _renderComponent.ClosestWater.CrushDamage));
            }

            //Set Crush Damage
            if (args.Length == 2)
            {
                float crushDepth;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out crushDepth))
                {
                    _renderComponent.ClosestWater.CrushDamage = crushDepth;
                    _modComponent.SyncToServer();
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetCrushDamage, _renderComponent.ClosestWater.CrushDamage));
                }
                else
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetCrushDamageNoParse, args[1]));
            }
        }

        private void CommandRate(string[] args)
        {
            //Get Collection Rate
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.GetCollectRate, _renderComponent.ClosestWater.CollectionRate));
            }

            //Set Collect Rate
            if (args.Length == 2)
            {
                float collectRate;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out collectRate))
                {
                    _renderComponent.ClosestWater.CollectionRate = collectRate;
                    _modComponent.SyncToServer();
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetCollectRate, _renderComponent.ClosestWater.CollectionRate));
                }
                else
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetCollectRateNoParse, args[1]));
            }
        }

        private void CommandFogColor(string[] args)
        {
            //Get Fog Color
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.GetFogColor, _renderComponent.ClosestWater.FogColor));
            }

            if (args.Length == 2 || args.Length == 3)
            {
                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNotEnoughArgs);
                return;
            }

            //Set Fog Color
            if (args.Length == 4)
            {
                float r, g, b;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out r))
                {
                    if (float.TryParse(WaterUtils.ValidateCommandData(args[2]), out g))
                    {
                        if (float.TryParse(WaterUtils.ValidateCommandData(args[3]), out b))
                        {
                            _renderComponent.ClosestWater.FogColor = new Vector3(r, g, b);
                            _modComponent.SyncToServer();
                            WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetFogColor, _renderComponent.ClosestWater.FogColor));
                            return;
                        }
                        else
                            WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetCollectRateNoParse, args[3]));
                    }
                    else
                        WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetCollectRateNoParse, args[2]));
                }
                else
                    WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.SetCollectRateNoParse, args[1]));
            }
        }
    }
}
