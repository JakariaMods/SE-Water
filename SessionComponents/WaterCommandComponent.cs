using Jakaria.Components;
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
        private WaterRenderSessionComponent _renderComponent;
        private WaterUIComponent _uiComponent;
        private WaterSettingsComponent _settingsComponent;
        private WaterSyncComponent _syncComponent;

        private Dictionary<string, Command> _commands = new Dictionary<string, Command>();

        public override void LoadData()
        {
            _syncComponent = Session.Instance.Get<WaterSyncComponent>();
            _renderComponent = Session.Instance.Get<WaterRenderSessionComponent>();
            _uiComponent = Session.Instance.TryGet<WaterUIComponent>();
            _settingsComponent = Session.Instance.Get<WaterSettingsComponent>();

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

        private void SendWaterToServer(WaterComponent water)
        {
            _syncComponent.SendSignalToServer(new WaterUpdateAddPacket
            {
                EntityId = water.Planet.EntityId,
                Settings = water.Settings,
                Timer = water.WaveTimer,
            });
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
                        WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.GenericMinArgs, command.MinArgs));
                        return;
                    }

                    if (args.Length > command.MaxArgs)
                    {
                        WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.GenericMaxArgs, command.MaxArgs));
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
            WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.WaterModVersion, WaterData.Version));
        }

        private void CommandLanguage(string[] args)
        {
            Language language;

            if (WaterLocalization.Languages.TryGetValue(WaterUtils.ValidateCommandData(args[1]), out language))
            {
                WaterLocalization.CurrentLanguage = language;

                WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetLanguage, args[1], WaterLocalization.CurrentLanguage.TranslationAuthor));
                _settingsComponent.SaveData();
            }
            else
                WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetLanguageNoParse, args[1]));
        }

        private void CommandQuality(string[] args)
        {
            //Get Quality
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.GetQuality, _settingsComponent.Settings.Quality));
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

                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetQuality, _settingsComponent.Settings.Quality));
                    _settingsComponent.SaveData();
                }
                else
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetQualityNoParse, args[1]));
            }
        }

        private void CommandVolume(string[] args)
        {
            //Get Volume
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.GetVolume, _settingsComponent.Settings.Volume));
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

                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetVolume, _settingsComponent.Settings.Volume));
                }
                else
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetVolumeNoParse, args[1]));
            }
        }

        private void CommandCenterOfBuoyancy(string[] args)
        {
            _settingsComponent.Settings.ShowCenterOfBuoyancy = !_settingsComponent.Settings.ShowCenterOfBuoyancy;
            _settingsComponent.SaveData();

            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleRenderCOB);
        }

        private void CommandDebug(string[] args)
        {
            _settingsComponent.Settings.ShowDebug = !_settingsComponent.Settings.ShowDebug;
            _settingsComponent.SaveData();

            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleDebug);
        }

        private void CommandDepth(string[] args)
        {
            if (_uiComponent != null && _uiComponent.Heartbeat)
            {
                _settingsComponent.Settings.ShowDepth = !_settingsComponent.Settings.ShowDepth;
                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleShowDepth);

                _settingsComponent.SaveData();
            }
            else
            {
                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoTextAPI);
            }
        }

        private void CommandAltitude(string[] args)
        {
            if (_uiComponent != null && _uiComponent.Heartbeat)
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
            _renderComponent.ClosestWater.Settings.EnableSeagulls = !_renderComponent.ClosestWater.Settings.EnableSeagulls;
            SendWaterToServer(_renderComponent.ClosestWater);
            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleBirds);
        }

        private void CommandFish(string[] args)
        {
            _renderComponent.ClosestWater.Settings.EnableFish = !_renderComponent.ClosestWater.Settings.EnableFish;
            SendWaterToServer(_renderComponent.ClosestWater);
            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleFish);
        }

        private void CommandFoam(string[] args)
        {
            _renderComponent.ClosestWater.Settings.EnableFoam = !_renderComponent.ClosestWater.Settings.EnableFoam;
            SendWaterToServer(_renderComponent.ClosestWater);
            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleFoam);
        }

        private void CommandBuoyancy(string[] args)
        {
            //Get Buoyancy
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.GetBuoyancy, _renderComponent.ClosestWater.Settings.Buoyancy));
            }

            //Set Buoyancy
            if (args.Length == 2)
            {
                float buoyancy;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out buoyancy))
                {
                    _renderComponent.ClosestWater.Settings.Buoyancy = MathHelper.Clamp(buoyancy, 0f, 10f);
                    SendWaterToServer(_renderComponent.ClosestWater);
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetBuoyancy, _renderComponent.ClosestWater.Settings.Buoyancy));
                }
                else
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetBuoyancyNoParse, args[1]));
            }
        }

        private void CommandRelativeRadius(string[] args)
        {
            double radius = ((Vector3D.Distance(_renderComponent.CameraPosition, _renderComponent.ClosestWater.Planet.PositionComp.GetPosition()) - WaterSettings.Default.TideHeight) / _renderComponent.ClosestPlanet.MinimumRadius);
            WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.GetRelativeRadius, radius.ToString("0.00000")));
        }

        private void CommandRadius(string[] args)
        {
            //Get Radius
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.GetRadius, _renderComponent.ClosestWater.Settings.Radius));
            }

            //Set Radius
            if (args.Length == 2)
            {
                float radius;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out radius))
                {
                    radius = MathHelper.Clamp(radius, 0.95f, 1.75f);

                    _renderComponent.ClosestWater.Settings.Radius = radius;
                    SendWaterToServer(_renderComponent.ClosestWater);
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetRadius, _renderComponent.ClosestWater.Settings.Radius));
                }
                else
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetRadiusNoParse, args[1]));
            }
        }

        private void CommandCurrentSpeed(string[] args)
        {
            //Get Current Speed
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.GetCurrentSpeed, _renderComponent.ClosestWater.Settings.CurrentSpeed));
            }

            //Set Current Speed
            if (args.Length == 2)
            {
                float currentSpeed;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out currentSpeed))
                {
                    _renderComponent.ClosestWater.Settings.CurrentSpeed = currentSpeed;
                    SendWaterToServer(_renderComponent.ClosestWater);
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetCurrentSpeed, _renderComponent.ClosestWater.Settings.CurrentSpeed));
                }
                else
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetCurrentSpeedNoParse, args[1]));
            }
        }
        private void CommandCurrentScale(string[] args)
        {
            //Get Current Scale
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.GetCurrentScale, _renderComponent.ClosestWater.Settings.CurrentScale));
            }

            //Set Current Scale
            if (args.Length == 2)
            {
                float currentScale;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out currentScale))
                {
                    _renderComponent.ClosestWater.Settings.CurrentScale = currentScale;
                    SendWaterToServer(_renderComponent.ClosestWater);
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetCurrentScale, _renderComponent.ClosestWater.Settings.CurrentScale));
                }
                else
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetCurrentScaleNoParse, args[1]));
            }
        }

        private void CommandWaveHeight(string[] args)
        {
            //Get Wave Height
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.GetWaveHeight, _renderComponent.ClosestWater.Settings.WaveHeight));
            }

            //Set Wave Height
            if (args.Length == 2)
            {
                float waveHeight;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out waveHeight))
                {
                    _renderComponent.ClosestWater.Settings.WaveHeight = Math.Abs(waveHeight);
                    SendWaterToServer(_renderComponent.ClosestWater);
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetWaveHeight, _renderComponent.ClosestWater.Settings.WaveHeight));
                }
                else
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetWaveHeightNoParse, args[1]));
            }
        }

        private void CommandWaveSpeed(string[] args)
        {
            //Get Wave Speed
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.GetWaveSpeed, _renderComponent.ClosestWater.Settings.WaveSpeed));
            }

            //Set Wave Speed
            if (args.Length == 2)
            {
                float waveSpeed;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out waveSpeed))
                {
                    _renderComponent.ClosestWater.Settings.WaveSpeed = MathHelper.Clamp(waveSpeed, 0f, 1f);
                    SendWaterToServer(_renderComponent.ClosestWater);
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetWaveSpeed, _renderComponent.ClosestWater.Settings.WaveSpeed));
                }
                else
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetWaveSpeedNoParse, args[1]));
            }
        }

        private void CommandWaveScale(string[] args)
        {
            //Get Wave Scale
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.GetWaveScale, _renderComponent.ClosestWater.Settings.WaveScale));
            }

            //Set Wave Scale
            if (args.Length == 2)
            {
                float waveScale;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out waveScale))
                {
                    _renderComponent.ClosestWater.Settings.WaveScale = waveScale;
                    SendWaterToServer(_renderComponent.ClosestWater);
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetWaveScale, _renderComponent.ClosestWater.Settings.WaveScale));

                }
                else
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetWaveScaleNoParse, _renderComponent.ClosestWater.Settings.WaveScale));
            }
        }

        private void CommandTideHeight(string[] args)
        {
            //Get Tide Height
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.GetTideHeight, _renderComponent.ClosestWater.Settings.TideHeight));
            }

            //Set Tide Height
            if (args.Length == 2)
            {
                float tideHeight;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out tideHeight))
                {
                    _renderComponent.ClosestWater.Settings.TideHeight = Math.Abs(tideHeight);
                    _syncComponent.SendSignalToServer(new WaterUpdateAddPacket
                    {
                        EntityId = _renderComponent.ClosestWater.Planet.EntityId,
                        Settings = _renderComponent.ClosestWater.Settings
                    });
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetTideHeight, _renderComponent.ClosestWater.Settings.TideHeight));
                }
                else
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetTideHeightNoParse, args[1]));
            }
        }

        private void CommandTideSpeed(string[] args)
        {
            //Get Tide Speed
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.GetTideSpeed, _renderComponent.ClosestWater.Settings.TideSpeed));
            }

            //Set Tide Speed
            if (args.Length == 2)
            {
                float tideSpeed;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out tideSpeed))
                {
                    _renderComponent.ClosestWater.Settings.TideSpeed = MyMath.Clamp(tideSpeed, 0, 1000);
                    SendWaterToServer(_renderComponent.ClosestWater);
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetTideSpeed, _renderComponent.ClosestWater.Settings.TideSpeed));
                }
                else
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetTideSpeedNoParse, args[1]));
            }
        }

        private void CommandReset(string[] args)
        {
            float radius = _renderComponent.ClosestWater.Settings.Radius;

            _renderComponent.ClosestWater.Settings = new WaterSettings();
            _renderComponent.ClosestWater.Settings.Radius = radius;

            SendWaterToServer(_renderComponent.ClosestWater);

            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.Reset);
        }

        private void CommandCreate(string[] args)
        {
            if (_renderComponent.ClosestPlanet.Components.Has<WaterComponent>())
            {
                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.HasWater);
                return;
            }
            
            if (args.Length == 1)
            {
                _syncComponent.SendSignalToServer(new WaterUpdateAddPacket
                {
                    EntityId = _renderComponent.ClosestPlanet.EntityId,
                    Settings = new WaterSettings()
                });

                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.CreateWater);
            }

            if (args.Length == 2)
            {
                float radius;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out radius))
                {
                    _syncComponent.SendSignalToServer(new WaterUpdateAddPacket
                    {
                        EntityId = _renderComponent.ClosestPlanet.EntityId,
                        Settings = new WaterSettings
                        {
                            Radius = radius,
                        }
                    });

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
            if(_renderComponent.ClosestPlanet.Components.Has<WaterComponent>())
            {
                _syncComponent.SendSignalToServer(new WaterRemovePacket { EntityId = _renderComponent.ClosestPlanet.EntityId });
            }
        }

        private void CommandPlayerDrag(string[] args)
        {
            _renderComponent.ClosestWater.Settings.PlayerDrag = !_renderComponent.ClosestWater.Settings.PlayerDrag;
            SendWaterToServer(_renderComponent.ClosestWater);
            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.TogglePlayerDrag);
        }

        private void CommandTransparent(string[] args)
        {
            _renderComponent.ClosestWater.Settings.Transparent = !_renderComponent.ClosestWater.Settings.Transparent;
            SendWaterToServer(_renderComponent.ClosestWater);
            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleTransparency);
        }
        private void CommandLit(string[] args)
        {
            _renderComponent.ClosestWater.Settings.Lit = !_renderComponent.ClosestWater.Settings.Lit;
            SendWaterToServer(_renderComponent.ClosestWater);
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
                WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.GetTexture, _renderComponent.ClosestWater.Settings.Texture));
            }

            //Set Texture
            if (args.Length == 2)
            {
                if (WaterData.WaterTextures.Contains(args[1]))
                {
                    _renderComponent.ClosestWater.Settings.Texture = SerializableStringId.Create(MyStringId.GetOrCompute(args[1]));

                    SendWaterToServer(_renderComponent.ClosestWater);
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetTexture, _renderComponent.ClosestWater.Settings.Texture));
                }
                else
                {
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetTextureNoFind, args[1]));
                }
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

                WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.GetMaterial, _renderComponent.ClosestWater.Settings.Material.SubtypeId));
            }

            //Set Material
            if (args.Length == 2)
            {
                if (WaterData.MaterialConfigs.ContainsKey(args[1]))
                {
                    _renderComponent.ClosestWater.Settings.MaterialId = args[1];
                    WaterUtils.ShowMessage($"{args[1]}");
                    SendWaterToServer(_renderComponent.ClosestWater);
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetMaterial, _renderComponent.ClosestWater.Settings.Material.SubtypeId));
                }
                else
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetMaterialNoFind, args[1]));
            }
        }

        private void CommandExport(string[] args)
        {
            PlanetConfig config = new PlanetConfig(_renderComponent.ClosestWater.Planet.Generator.Id);
            config.WaterSettings = _renderComponent.ClosestWater.Settings;

            MyClipboardHelper.SetClipboard(MyAPIGateway.Utilities.SerializeToXML(config));
            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ExportWater);
        }

        private void CommandSettings(string[] args)
        {
            WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.GetWaterSettings, _renderComponent.ClosestWater.Settings.ToString()));
        }

        private void CommandCrush(string[] args)
        {
            //Get Crush Damage
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.GetCrushDamage, _renderComponent.ClosestWater.Settings.CrushDamage));
            }

            //Set Crush Damage
            if (args.Length == 2)
            {
                float crushDepth;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out crushDepth))
                {
                    _renderComponent.ClosestWater.Settings.CrushDamage = crushDepth;
                    SendWaterToServer(_renderComponent.ClosestWater);
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetCrushDamage, _renderComponent.ClosestWater.Settings.CrushDamage));
                }
                else
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetCrushDamageNoParse, args[1]));
            }
        }

        private void CommandRate(string[] args)
        {
            //Get Collection Rate
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.GetCollectRate, _renderComponent.ClosestWater.Settings.CollectionRate));
            }

            //Set Collect Rate
            if (args.Length == 2)
            {
                float collectRate;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out collectRate))
                {
                    _renderComponent.ClosestWater.Settings.CollectionRate = collectRate;
                    SendWaterToServer(_renderComponent.ClosestWater);
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetCollectRate, _renderComponent.ClosestWater.Settings.CollectionRate));
                }
                else
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetCollectRateNoParse, args[1]));
            }
        }

        private void CommandFogColor(string[] args)
        {
            //Get Fog Color
            if (args.Length == 1)
            {
                WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.GetFogColor, _renderComponent.ClosestWater.Settings.FogColor));
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
                            _renderComponent.ClosestWater.Settings.FogColor = new Vector3(r, g, b);
                            SendWaterToServer(_renderComponent.ClosestWater);
                            WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetFogColor, _renderComponent.ClosestWater.Settings.FogColor));
                            return;
                        }
                        else
                            WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetCollectRateNoParse, args[3]));
                    }
                    else
                        WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetCollectRateNoParse, args[2]));
                }
                else
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetCollectRateNoParse, args[1]));
            }
        }
    }

    /// <summary>
    /// <see cref="WaterCommandComponent"/>
    /// </summary>
    public struct Command
    {
        public readonly Action<string[]> Action;
        public string Description;
        public MyPromoteLevel PromoteLevel;

        public int MinArgs;
        public int MaxArgs;

        public bool RequirePlanet;
        public bool RequireWater;

        public bool SyncWater;

        public Command(Action<string[]> action)
        {
            Action = action;

            Description = "Empty Description";
            PromoteLevel = MyPromoteLevel.None;
            MinArgs = 1;
            MaxArgs = 1;
            RequirePlanet = false;
            RequireWater = false;
            SyncWater = false;
        }
    }
}
