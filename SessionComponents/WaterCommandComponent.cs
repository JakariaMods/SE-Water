using Jakaria.Components;
using Jakaria.Configs;
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
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.Utils;
using VRageMath;

namespace Jakaria.SessionComponents
{
    /// <summary>
    /// Component used for parsing in-game chat commands
    /// </summary>
    public class WaterCommandComponent : SessionComponentBase
    {
        public const char COMMAND_PREFIX = '/';

        private WaterSyncComponent _syncComponent;
        private WaterModComponent _modComponent;

        private Dictionary<string, Command> _commands = new Dictionary<string, Command>();

        public override void LoadData()
        {
            _syncComponent = Session.Instance.Get<WaterSyncComponent>();
            _modComponent = Session.Instance.Get<WaterModComponent>();

            _commands["whelp"] = new Command(CommandHelp)
            {
                Description = "Opens the steam help guide or provides extended information on a specific command.",
                MinArgs = 1,
                MaxArgs = 2,
                Console = true,
                Client = true,
            };
            _commands["wcommandlist"] = _commands["wcommands"] = new Command(CommandCommandList)
            {
                Description = "Lists all available commands.",
                Console = true,
                Client = true,
            };
            _commands["wdiscord"] = new Command(CommandDiscord)
            {
                Description = "Opens the invite link to the Water Mod Discord.",
                Client = true,
            };
            _commands["wversion"] = new Command(CommandVersion)
            {
                Description = "Tells the current mod version.",
                Console = true,
                Client = true,
            };
            _commands["wquality"] = new Command(CommandQuality)
            {
                Description = "Gets or sets the render quality.",
                MinArgs = 1,
                MaxArgs = 2,
                Client = true,
            };
            _commands["wvolume"] = new Command(CommandVolume)
            {
                Description = "Gets or sets the volume of water sounds.",
                MinArgs = 1,
                MaxArgs = 2,
                Client = true,
            };
            _commands["wcob"] = new Command(CommandCenterOfBuoyancy)
            {
                Description = "Toggles rendering of a center of buoyancy indicator.",
                Client = true,
            };
            _commands["wdebug"] = new Command(CommandDebug)
            {
                Description = "Toggles debug mode.",
                Client = true,
            };
            _commands["wdepth"] = new Command(CommandDepth)
            {
                Description = "Toggles depth indicator underwater. Requires Text Hud API.",
                Client = true,
            };
            _commands["waltitude"] = new Command(CommandAltitude)
            {
                Description = "Toggles sea-level altitude indicator in cockpits. Requires Text Hud API.",
                Client = true,
            };
            _commands["wfog"] = new Command(CommandFog)
            {
                Description = "Toggles fog.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                RequireWater = true,
                Client = true,
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
                Console = true,
            };
            _commands["wrelativeradius"] = _commands["wrradius"] = new Command(CommandRelativeRadius)
            {
                Description = "Calculates a radius relative to the player's position.",
                Console = true,
                Client = true,
            };
            _commands["wradius"] = new Command(CommandRadius)
            {
                Description = "Gets or sets the radius of the closest water.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                MinArgs = 1,
                MaxArgs = 2,
                RequireWater = true,
                Console = true,
            };
            _commands["wcurrentspeed"] = new Command(CommandCurrentSpeed)
            {
                Description = "Gets or sets the speed of currents in m/s.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                MinArgs = 1,
                MaxArgs = 2,
                RequireWater = true,
                Console = true,
            };
            _commands["wcurrentscale"] = new Command(CommandCurrentScale)
            {
                Description = "Gets or sets the horizontal scale of currents. Smaller numbers are larger.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                MinArgs = 1,
                MaxArgs = 2,
                RequireWater = true,
                Console = true,
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
                Console = true,
            };
            _commands["wtidespeed"] = new Command(CommandTideSpeed)
            {
                Description = "Gets or sets the speed of the tide.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                MinArgs = 1,
                MaxArgs = 2,
                RequireWater = true,
                Console = true,
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
                Console = true,
            };
            _commands["wcreate"] = new Command(CommandCreate)
            {
                Description = "Creates water at the closest planet with an optional radius parameter.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                MinArgs = 1,
                MaxArgs = 2,
                RequirePlanet = true,
                Console = true,
            };
            _commands["wremove"] = new Command(CommandRemove)
            {
                Description = "Removes the closest water.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                RequireWater = true,
                Console = true,
            };
            _commands["wpdrag"] = new Command(CommandPlayerDrag)
            {
                Description = "Toggles player drag.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                RequireWater = true,
                Console = true,
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
                Client = true,
            };
            _commands["wmateriallist"] = _commands["wmaterials"] = new Command(CommandMaterials)
            {
                Description = "Gets a list of all available physics materials.",
                Console = true,
                Client = true,
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
                Console = true,
            };
            _commands["wexport"] = new Command(CommandExport)
            {
                Description = "Serializes a copy of the Water Settings to the Clipboard. Used for ModAPI.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                RequireWater = true,
                Client = true,
            };
            _commands["wsettings"] = new Command(CommandSettings)
            {
                Description = "Prints all the current settings of the Water.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                RequireWater = true,
                Console = true,
                Client = true,
            };
            _commands["wcrush"] = new Command(CommandCrush)
            {
                Description = "Gets or sets the crush damage.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                MinArgs = 1,
                MaxArgs = 2,
                RequireWater = true,
                Console = true,
            };
            _commands["wrate"] = new Command(CommandRate)
            {
                Description = "Gets or sets the collection rate multiplier.",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                MinArgs = 1,
                MaxArgs = 2,
                RequireWater = true,
                Console = true,
            };
            /*_commands["wvolumetric"] = new Command(CommandVolumetric)
            {
                Description = "Toggles water volumetrics (fluid flow).",
                PromoteLevel = MyPromoteLevel.SpaceMaster,
                RequireWater = true,
                RequireDebug = true
            };*/

            MyAPIGateway.Utilities.MessageEntered += Utilities_MessageEntered;
        }

        public override void UnloadData()
        {
            MyAPIGateway.Utilities.MessageEntered -= Utilities_MessageEntered;
        }

        private static bool ValidateCommand(string messageText, out string[] args)
        {
            args = null;

            if (messageText.Length == 0 || messageText[0] != COMMAND_PREFIX)
                return false;

            args = messageText.TrimStart(COMMAND_PREFIX).Split(new char[1] {' '}, StringSplitOptions.RemoveEmptyEntries);
            
            if (args.Length == 0)
                return false;

            return true;
        }

        public void SendCommand(string messageText, ulong sender = 0)
        {
            string[] args;
            if (!ValidateCommand(messageText, out args))
                return;

            if (MyAPIGateway.Session.IsServer)
            {
                List<IMyPlayer> players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players);
                foreach (var player in players)
                {
                    if(player != null && player.Identity != null)
                    {
                        if(player.SteamUserId == sender)
                        {
                            Vector3D position = player.GetPosition();
                            RunCommand(messageText, new CommandArgs
                            {
                                Planet = MyGamePruningStructure.GetClosestPlanet(position),
                                Position = position,
                                User = player.IdentityId,
                                Sender = player.SteamUserId,
                                PromoteLevel = player.PromoteLevel,
                                Water = _modComponent.GetClosestWater(position),
                            });

                            break;
                        }
                    }
                }
            }
            else
            {
                _syncComponent.SendSignalToServer(new CommandPacket
                {
                    Message = messageText
                });
            }
        }

        private void Utilities_MessageEntered(string messageText, ref bool sendToOthers)
        {
            string[] args;
            if (!ValidateCommand(messageText, out args))
                return;

            Command command;
            if (_commands.TryGetValue(args[0], out command))
            {
                WaterSettingsComponent settingsComponent = Session.Instance.TryGet<WaterSettingsComponent>();

                if (command.RequireDebug && (settingsComponent == null || !settingsComponent.Settings.ShowDebug))
                    return;

                sendToOthers = false;

                if (command.Client)
                {
                    Vector3D position = MyAPIGateway.Session.Camera.Position;
                    RunCommand(messageText, new CommandArgs
                    {
                        Planet = MyGamePruningStructure.GetClosestPlanet(position),
                        Position = position,
                        User = MyAPIGateway.Session.Player.IdentityId,
                        Sender = MyAPIGateway.Session.Player.SteamUserId,
                        PromoteLevel = MyAPIGateway.Session.Player.PromoteLevel,
                        Water = _modComponent.GetClosestWater(position),
                    });
                }
                else
                {
                    SendCommand(messageText, MyAPIGateway.Session.Player.SteamUserId);
                }
            }
        }

        private void RunCommand(string messageText, CommandArgs commandArgs)
        {
            string[] args;
            if (!ValidateCommand(messageText, out args))
                return;

            if (args == null || args.Length == 0)
                return;

            Command command;
            if (_commands.TryGetValue(args[0], out command))
            {
                if (commandArgs.PromoteLevel >= command.PromoteLevel)
                {
                    if(!command.Console && Session.CONSOLE_MODE)
                    {
                        WaterUtils.SendMessage(WaterTexts.NoConsoleSupport, commandArgs.User);
                        return;
                    }

                    if (!command.Client && Session.Instance.IsClient && !MyAPIGateway.Session.IsServer)
                    {
                        WaterUtils.SendMessage(WaterTexts.NoClientSupport, commandArgs.User);
                        return;
                    }

                    if (command.RequirePlanet && commandArgs.Planet == null)
                    {
                        WaterUtils.SendMessage(WaterTexts.NoPlanet, commandArgs.User);
                        return;
                    }

                    if (command.RequireWater && commandArgs.Water == null)
                    {
                        WaterUtils.SendMessage(WaterTexts.NoWater, commandArgs.User);
                        return;
                    }

                    if (args.Length < command.MinArgs)
                    {
                        WaterUtils.SendMessage(string.Format(WaterTexts.GenericMinArgs, command.MinArgs - 1), commandArgs.User);
                        return;
                    }

                    if (args.Length > command.MaxArgs)
                    {
                        WaterUtils.SendMessage(string.Format(WaterTexts.GenericMaxArgs, command.MaxArgs - 1), commandArgs.User);
                        return;
                    }

                    command.Action.Invoke(args, commandArgs);
                }
                else
                {
                    WaterUtils.SendMessage(WaterTexts.GenericNoPermissions, commandArgs.User);
                }
            }
        }

        private void CommandCommandList(string[] args, CommandArgs commandArgs)
        {
            WaterSettingsComponent settingsComponent = Session.Instance.TryGet<WaterSettingsComponent>();

            string commands = "";

            foreach (var command in _commands)
            {
                if (Session.CONSOLE_MODE && !command.Value.Console)
                    continue;

                if (command.Value.RequireDebug && (settingsComponent == null || !settingsComponent.Settings.ShowDebug))
                    continue;

                commands += command.Key + ", ";
            }

            if (commands.Length > 2)
            {
                commands = commands.Substring(0, commands.Length - 2) + "."; //remove end comma and space. add period
                WaterUtils.SendMessage(WaterTexts.ListCommands + commands, commandArgs.User);
            }
        }

        private void CommandHelp(string[] args, CommandArgs commandArgs)
        {
            if (args.Length == 1)
            {
                MyVisualScriptLogicProvider.OpenSteamOverlayLocal(@"https://steamcommunity.com/sharedfiles/filedetails/?id=2574095672");
                WaterUtils.SendMessage(WaterTexts.OpenGuide, commandArgs.User);
            }

            if (args.Length == 2)
            {
                args[1].TrimStart(COMMAND_PREFIX);

                Command command;
                if (_commands.TryGetValue(args[1], out command))
                {
                    WaterUtils.SendMessage($"{COMMAND_PREFIX}{args[1]}: {command.Description} Min Promote Level: {command.PromoteLevel}", commandArgs.User);
                }
            }
        }

        private void CommandDiscord(string[] args, CommandArgs commandArgs)
        {
            MyVisualScriptLogicProvider.OpenSteamOverlayLocal(@"https://steamcommunity.com/sharedfiles/filedetails/?id=2574095672");
            WaterUtils.SendMessage(WaterTexts.OpenGuide, commandArgs.User);
        }

        private void CommandVersion(string[] args, CommandArgs commandArgs)
        {
            WaterUtils.SendMessage(string.Format(WaterTexts.WaterModVersion, WaterData.Version), commandArgs.User);
        }

        private void CommandQuality(string[] args, CommandArgs commandArgs)
        {
            WaterSettingsComponent settingsComponent = Session.Instance.TryGet<WaterSettingsComponent>();

            //Get Quality
            if (args.Length == 1)
            {
                WaterUtils.SendMessage(string.Format(WaterTexts.GetQuality, settingsComponent.Settings.Quality), commandArgs.User);
            }

            //Set Quality
            if (args.Length == 2)
            {
                float quality;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out quality))
                {
                    if (settingsComponent.Settings.ShowDebug)
                        settingsComponent.Settings.Quality = quality;
                    else
                        settingsComponent.Settings.Quality = MathHelper.Clamp(quality, 0.4f, 3f);

                    WaterUtils.SendMessage(string.Format(WaterTexts.SetQuality, settingsComponent.Settings.Quality), commandArgs.User);
                    settingsComponent.SaveData();
                }
                else
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetQualityNoParse, args[1]), commandArgs.User);
            }
        }

        private void CommandVolume(string[] args, CommandArgs commandArgs)
        {
            WaterSettingsComponent settingsComponent = Session.Instance.TryGet<WaterSettingsComponent>();

            //Get Volume
            if (args.Length == 1)
            {
                WaterUtils.SendMessage(string.Format(WaterTexts.GetVolume, settingsComponent.Settings.Volume), commandArgs.User);
            }

            //Set Volume
            if (args.Length == 2)
            {
                float volume;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out volume))
                {
                    volume = MathHelper.Clamp(volume, 0f, 1f);
                    settingsComponent.Settings.Volume = volume;
                    settingsComponent.SaveData();

                    WaterUtils.SendMessage(string.Format(WaterTexts.SetVolume, settingsComponent.Settings.Volume), commandArgs.User);
                }
                else
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetVolumeNoParse, args[1]), commandArgs.User);
            }
        }

        private void CommandCenterOfBuoyancy(string[] args, CommandArgs commandArgs)
        {
            WaterSettingsComponent settingsComponent = Session.Instance.TryGet<WaterSettingsComponent>();

            settingsComponent.Settings.ShowCenterOfBuoyancy = !settingsComponent.Settings.ShowCenterOfBuoyancy;
            settingsComponent.SaveData();

            WaterUtils.SendMessage(WaterTexts.ToggleRenderCOB, commandArgs.User);
        }

        private void CommandDebug(string[] args, CommandArgs commandArgs)
        {
            WaterSettingsComponent settingsComponent = Session.Instance.TryGet<WaterSettingsComponent>();

            settingsComponent.Settings.ShowDebug = !settingsComponent.Settings.ShowDebug;
            settingsComponent.SaveData();

            WaterUtils.SendMessage(WaterTexts.ToggleDebug, commandArgs.User);
        }

        private void CommandDepth(string[] args, CommandArgs commandArgs)
        {
            WaterSettingsComponent settingsComponent = Session.Instance.TryGet<WaterSettingsComponent>();
            WaterUIComponent uiComponent = Session.Instance.TryGet<WaterUIComponent>();

            if (uiComponent != null && uiComponent.Heartbeat)
            {
                settingsComponent.Settings.ShowDepth = !settingsComponent.Settings.ShowDepth;
                WaterUtils.SendMessage(WaterTexts.ToggleShowDepth, commandArgs.User);

                settingsComponent.SaveData();
            }
            else
            {
                WaterUtils.SendMessage(WaterTexts.NoTextAPI, commandArgs.User);
            }
        }

        private void CommandAltitude(string[] args, CommandArgs commandArgs)
        {
            WaterSettingsComponent settingsComponent = Session.Instance.TryGet<WaterSettingsComponent>();
            WaterUIComponent uiComponent = Session.Instance.TryGet<WaterUIComponent>();

            if (uiComponent != null && uiComponent.Heartbeat)
            {
                settingsComponent.Settings.ShowAltitude = !settingsComponent.Settings.ShowAltitude;
                WaterUtils.SendMessage(WaterTexts.ToggleShowAltitude, commandArgs.User);

                settingsComponent.SaveData();
            }
            else
            {
                settingsComponent.Settings.ShowAltitude = false;
                WaterUtils.SendMessage(WaterTexts.NoTextAPI, commandArgs.User);
            }
        }

        private void CommandFog(string[] args, CommandArgs commandArgs)
        {
            WaterSettingsComponent settingsComponent = Session.Instance.TryGet<WaterSettingsComponent>();

            settingsComponent.Settings.ShowFog = !settingsComponent.Settings.ShowFog;
            settingsComponent.SaveData();
            WaterUtils.SendMessage(WaterTexts.ToggleFog, commandArgs.User);
        }

        private void CommandBird(string[] args, CommandArgs commandArgs)
        {
            commandArgs.Water.Settings.EnableSeagulls = !commandArgs.Water.Settings.EnableSeagulls;
            _syncComponent.SyncClients(commandArgs.Water);
            WaterUtils.SendMessage(WaterTexts.ToggleBirds, commandArgs.User);
        }

        private void CommandFish(string[] args, CommandArgs commandArgs)
        {
            commandArgs.Water.Settings.EnableFish = !commandArgs.Water.Settings.EnableFish;
            _syncComponent.SyncClients(commandArgs.Water);
            WaterUtils.SendMessage(WaterTexts.ToggleFish, commandArgs.User);
        }

        private void CommandFoam(string[] args, CommandArgs commandArgs)
        {
            commandArgs.Water.Settings.EnableFoam = !commandArgs.Water.Settings.EnableFoam;
            _syncComponent.SyncClients(commandArgs.Water);
            WaterUtils.SendMessage(WaterTexts.ToggleFoam, commandArgs.User);
        }

        private void CommandBuoyancy(string[] args, CommandArgs commandArgs)
        {
            //Get Buoyancy
            if (args.Length == 1)
            {
                WaterUtils.SendMessage(string.Format(WaterTexts.GetBuoyancy, commandArgs.Water.Settings.Buoyancy), commandArgs.User);
            }

            //Set Buoyancy
            if (args.Length == 2)
            {
                float buoyancy;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out buoyancy))
                {
                    commandArgs.Water.Settings.Buoyancy = MathHelper.Clamp(buoyancy, 0f, 10f);
                    _syncComponent.SyncClients(commandArgs.Water);
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetBuoyancy, commandArgs.Water.Settings.Buoyancy), commandArgs.User);
                }
                else
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetBuoyancyNoParse, args[1]), commandArgs.User);
            }
        }

        private void CommandRelativeRadius(string[] args, CommandArgs commandArgs)
        {
            double radius = ((Vector3D.Distance(commandArgs.Position, commandArgs.Planet.PositionComp.GetPosition()) - WaterSettings.Default.TideHeight) / commandArgs.Planet.MinimumRadius);
            WaterUtils.SendMessage(string.Format(WaterTexts.GetRelativeRadius, radius.ToString("0.00000")), commandArgs.User);
        }

        private void CommandRadius(string[] args, CommandArgs commandArgs)
        {
            //Get Radius
            if (args.Length == 1)
            {
                WaterUtils.SendMessage(string.Format(WaterTexts.GetRadius, commandArgs.Water.Settings.Radius), commandArgs.User);
            }

            //Set Radius
            if (args.Length == 2)
            {
                float radius;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out radius))
                {
                    radius = MathHelper.Clamp(radius, 0.1f, 1.75f);

                    commandArgs.Water.Settings.Radius = radius;
                    _syncComponent.SyncClients(commandArgs.Water);
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetRadius, commandArgs.Water.Settings.Radius), commandArgs.User);
                }
                else
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetRadiusNoParse, args[1]), commandArgs.User);
            }
        }

        private void CommandCurrentSpeed(string[] args, CommandArgs commandArgs)
        {
            //Get Current Speed
            if (args.Length == 1)
            {
                WaterUtils.SendMessage(string.Format(WaterTexts.GetCurrentSpeed, commandArgs.Water.Settings.CurrentSpeed), commandArgs.User);
            }

            //Set Current Speed
            if (args.Length == 2)
            {
                float currentSpeed;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out currentSpeed))
                {
                    commandArgs.Water.Settings.CurrentSpeed = currentSpeed;
                    _syncComponent.SyncClients(commandArgs.Water);
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetCurrentSpeed, commandArgs.Water.Settings.CurrentSpeed), commandArgs.User);
                }
                else
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetCurrentSpeedNoParse, args[1]), commandArgs.User);
            }
        }
        private void CommandCurrentScale(string[] args, CommandArgs commandArgs)
        {
            //Get Current Scale
            if (args.Length == 1)
            {
                WaterUtils.SendMessage(string.Format(WaterTexts.GetCurrentScale, commandArgs.Water.Settings.CurrentScale), commandArgs.User);
            }

            //Set Current Scale
            if (args.Length == 2)
            {
                float currentScale;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out currentScale))
                {
                    commandArgs.Water.Settings.CurrentScale = currentScale;
                    _syncComponent.SyncClients(commandArgs.Water);
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetCurrentScale, commandArgs.Water.Settings.CurrentScale), commandArgs.User);
                }
                else
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetCurrentScaleNoParse, args[1]), commandArgs.User);
            }
        }

        private void CommandWaveHeight(string[] args, CommandArgs commandArgs)
        {
            //Get Wave Height
            if (args.Length == 1)
            {
                WaterUtils.SendMessage(string.Format(WaterTexts.GetWaveHeight, commandArgs.Water.Settings.WaveHeight), commandArgs.User);
            }

            //Set Wave Height
            if (args.Length == 2)
            {
                float waveHeight;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out waveHeight))
                {
                    commandArgs.Water.Settings.WaveHeight = Math.Abs(waveHeight);
                    _syncComponent.SyncClients(commandArgs.Water);
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetWaveHeight, commandArgs.Water.Settings.WaveHeight), commandArgs.User);
                }
                else
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetWaveHeightNoParse, args[1]), commandArgs.User);
            }
        }

        private void CommandWaveSpeed(string[] args, CommandArgs commandArgs)
        {
            //Get Wave Speed
            if (args.Length == 1)
            {
                WaterUtils.SendMessage(string.Format(WaterTexts.GetWaveSpeed, commandArgs.Water.Settings.WaveSpeed), commandArgs.User);
            }

            //Set Wave Speed
            if (args.Length == 2)
            {
                float waveSpeed;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out waveSpeed))
                {
                    commandArgs.Water.Settings.WaveSpeed = MathHelper.Clamp(waveSpeed, 0f, 1f);
                    _syncComponent.SyncClients(commandArgs.Water);
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetWaveSpeed, commandArgs.Water.Settings.WaveSpeed), commandArgs.User);
                }
                else
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetWaveSpeedNoParse, args[1]), commandArgs.User);
            }
        }

        private void CommandWaveScale(string[] args, CommandArgs commandArgs)
        {
            //Get Wave Scale
            if (args.Length == 1)
            {
                WaterUtils.SendMessage(string.Format(WaterTexts.GetWaveScale, commandArgs.Water.Settings.WaveScale), commandArgs.User);
            }

            //Set Wave Scale
            if (args.Length == 2)
            {
                float waveScale;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out waveScale))
                {
                    commandArgs.Water.Settings.WaveScale = waveScale;
                    _syncComponent.SyncClients(commandArgs.Water);
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetWaveScale, commandArgs.Water.Settings.WaveScale), commandArgs.User);

                }
                else
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetWaveScaleNoParse, commandArgs.Water.Settings.WaveScale), commandArgs.User);
            }
        }

        private void CommandTideHeight(string[] args, CommandArgs commandArgs)
        {
            //Get Tide Height
            if (args.Length == 1)
            {
                WaterUtils.SendMessage(string.Format(WaterTexts.GetTideHeight, commandArgs.Water.Settings.TideHeight), commandArgs.User);
            }

            //Set Tide Height
            if (args.Length == 2)
            {
                float tideHeight;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out tideHeight))
                {
                    commandArgs.Water.Settings.TideHeight = Math.Abs(tideHeight);
                    _syncComponent.SyncClients(commandArgs.Water);

                    WaterUtils.SendMessage(string.Format(WaterTexts.SetTideHeight, commandArgs.Water.Settings.TideHeight), commandArgs.User);
                }
                else
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetTideHeightNoParse, args[1]), commandArgs.User);
            }
        }

        private void CommandTideSpeed(string[] args, CommandArgs commandArgs)
        {
            //Get Tide Speed
            if (args.Length == 1)
            {
                WaterUtils.SendMessage(string.Format(WaterTexts.GetTideSpeed, commandArgs.Water.Settings.TideSpeed), commandArgs.User);
            }

            //Set Tide Speed
            if (args.Length == 2)
            {
                float tideSpeed;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out tideSpeed))
                {
                    commandArgs.Water.Settings.TideSpeed = MyMath.Clamp(tideSpeed, 0, 1000);
                    _syncComponent.SyncClients(commandArgs.Water);
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetTideSpeed, commandArgs.Water.Settings.TideSpeed), commandArgs.User);
                }
                else
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetTideSpeedNoParse, args[1]), commandArgs.User);
            }
        }

        private void CommandReset(string[] args, CommandArgs commandArgs)
        {
            float radius = commandArgs.Water.Settings.Radius;

            commandArgs.Water.Settings = new WaterSettings();
            commandArgs.Water.Settings.Radius = radius;

            _syncComponent.SyncClients(commandArgs.Water);

            WaterUtils.SendMessage(WaterTexts.Reset, commandArgs.User);
        }

        private void CommandCreate(string[] args, CommandArgs commandArgs)
        {
            if (commandArgs.Planet.Components.Has<WaterComponent>())
            {
                WaterUtils.SendMessage(WaterTexts.HasWater, commandArgs.User);
                return;
            }
            
            if (args.Length == 1)
            {
                _modComponent.AddWater(commandArgs.Planet);
                
                WaterUtils.SendMessage(WaterTexts.CreateWater, commandArgs.User);
            }

            if (args.Length == 2)
            {
                float radius;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out radius))
                {
                    _modComponent.AddWater(commandArgs.Planet, new WaterSettings
                    {
                        Radius = radius
                    });
                    _syncComponent.SyncClients();

                    WaterUtils.SendMessage(WaterTexts.CreateWater, commandArgs.User);
                }
                else
                {
                    WaterUtils.SendMessage(WaterTexts.GenericNoParse, commandArgs.User);
                }
            }
        }

        private void CommandRemove(string[] args, CommandArgs commandArgs)
        {
            if(commandArgs.Planet.Components.Has<WaterComponent>())
            {
                WaterUtils.SendMessage(WaterTexts.RemoveWater, commandArgs.User);
                _modComponent.RemoveWater(commandArgs.Planet);
            }
        }

        private void CommandPlayerDrag(string[] args, CommandArgs commandArgs)
        {
            commandArgs.Water.Settings.PlayerDrag = !commandArgs.Water.Settings.PlayerDrag;
            _syncComponent.SyncClients(commandArgs.Water);
            WaterUtils.SendMessage(WaterTexts.TogglePlayerDrag, commandArgs.User);
        }

        private void CommandTransparent(string[] args, CommandArgs commandArgs)
        {
            commandArgs.Water.Settings.Transparent = !commandArgs.Water.Settings.Transparent;
            _syncComponent.SyncClients(commandArgs.Water);
            WaterUtils.SendMessage(WaterTexts.ToggleTransparency, commandArgs.User);
        }
        private void CommandLit(string[] args, CommandArgs commandArgs)
        {
            commandArgs.Water.Settings.Lit = !commandArgs.Water.Settings.Lit;
            _syncComponent.SyncClients(commandArgs.Water);
            WaterUtils.SendMessage(WaterTexts.ToggleLighting, commandArgs.User);
        }

        private void CommandTextures(string[] args, CommandArgs commandArgs)
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
                    WaterUtils.SendMessage(WaterTexts.ListTextures + textures, commandArgs.User);
                }
            }
            else
            {
                WaterUtils.SendMessage(WaterTexts.NoTextures, commandArgs.User);
            }
        }
        private void CommandMaterials(string[] args, CommandArgs commandArgs)
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
                    WaterUtils.SendMessage(WaterTexts.ListMaterials + materials, commandArgs.User);
                }
            }
            else
            {
                WaterUtils.SendMessage(WaterTexts.NoMaterials, commandArgs.User);
            }
        }
        private void CommandTexture(string[] args, CommandArgs commandArgs)
        {
            //Get Texture
            if (args.Length == 1)
            {
                WaterUtils.SendMessage(string.Format(WaterTexts.GetTexture, commandArgs.Water.Settings.Texture), commandArgs.User);
            }

            //Set Texture
            if (args.Length == 2)
            {
                if (WaterData.WaterTextures.Contains(args[1]))
                {
                    commandArgs.Water.Settings.Texture = SerializableStringId.Create(MyStringId.GetOrCompute(args[1]));

                    _syncComponent.SyncClients(commandArgs.Water);
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetTexture, commandArgs.Water.Settings.Texture), commandArgs.User);
                }
                else
                {
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetTextureNoFind, args[1]), commandArgs.User);
                }
            }
        }

        private void CommandMaterial(string[] args, CommandArgs commandArgs)
        {
            //Get Material
            if (args.Length == 1)
            {
                if (commandArgs.Planet == null)
                {
                    WaterUtils.SendMessage(WaterTexts.NoPlanet, commandArgs.User);
                    return;
                }

                WaterUtils.SendMessage(string.Format(WaterTexts.GetMaterial, commandArgs.Water.Settings.Material.SubtypeId), commandArgs.User);
            }

            //Set Material
            if (args.Length == 2)
            {
                if (WaterData.MaterialConfigs.ContainsKey(args[1]))
                {
                    commandArgs.Water.Settings.MaterialId = args[1];
                    _syncComponent.SyncClients(commandArgs.Water);
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetMaterial, commandArgs.Water.Settings.Material.SubtypeId), commandArgs.User);
                }
                else
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetMaterialNoFind, args[1]), commandArgs.User);
            }
        }

        private void CommandExport(string[] args, CommandArgs commandArgs)
        {
            PlanetConfig config = new PlanetConfig(commandArgs.Water.Planet.Generator.Id);
            config.WaterSettings = commandArgs.Water.Settings;

            MyClipboardHelper.SetClipboard(MyAPIGateway.Utilities.SerializeToXML(config));
            WaterUtils.SendMessage(WaterTexts.ExportWater, commandArgs.User);
        }

        private void CommandSettings(string[] args, CommandArgs commandArgs)
        {
            WaterUtils.SendMessage(string.Format(WaterTexts.GetWaterSettings, commandArgs.Water.Settings), commandArgs.User);
        }

        private void CommandCrush(string[] args, CommandArgs commandArgs)
        {
            //Get Crush Damage
            if (args.Length == 1)
            {
                WaterUtils.SendMessage(string.Format(WaterTexts.GetCrushDamage, commandArgs.Water.Settings.CrushDamage), commandArgs.User);
            }

            //Set Crush Damage
            if (args.Length == 2)
            {
                float crushDepth;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out crushDepth))
                {
                    commandArgs.Water.Settings.CrushDamage = crushDepth;
                    _syncComponent.SyncClients(commandArgs.Water);
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetCrushDamage, commandArgs.Water.Settings.CrushDamage), commandArgs.User);
                }
                else
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetCrushDamageNoParse, args[1]), commandArgs.User);
            }
        }

        private void CommandRate(string[] args, CommandArgs commandArgs)
        {
            //Get Collection Rate
            if (args.Length == 1)
            {
                WaterUtils.SendMessage(string.Format(WaterTexts.GetCollectRate, commandArgs.Water.Settings.CollectionRate), commandArgs.User);
            }

            //Set Collect Rate
            if (args.Length == 2)
            {
                float collectRate;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out collectRate))
                {
                    commandArgs.Water.Settings.CollectionRate = collectRate;
                    _syncComponent.SyncClients(commandArgs.Water);
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetCollectRate, commandArgs.Water.Settings.CollectionRate), commandArgs.User);
                }
                else
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetCollectRateNoParse, args[1]), commandArgs.User);
            }
        }

        private void CommandFogColor(string[] args, CommandArgs commandArgs)
        {
            //Get Fog Color
            if (args.Length == 1)
            {
                WaterUtils.SendMessage(string.Format(WaterTexts.GetFogColor, commandArgs.Water.Settings.FogColor), commandArgs.User);
            }

            if (args.Length == 2 || args.Length == 3)
            {
                WaterUtils.SendMessage(WaterTexts.GenericNotEnoughArgs, commandArgs.User);
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
                            commandArgs.Water.Settings.FogColor = new Vector3(r, g, b);
                            _syncComponent.SyncClients(commandArgs.Water);
                            WaterUtils.SendMessage(string.Format(WaterTexts.SetFogColor, commandArgs.Water.Settings.FogColor), commandArgs.User);
                            return;
                        }
                        else
                            WaterUtils.SendMessage(string.Format(WaterTexts.SetCollectRateNoParse, args[3]), commandArgs.User);
                    }
                    else
                        WaterUtils.SendMessage(string.Format(WaterTexts.SetCollectRateNoParse, args[2]), commandArgs.User);
                }
                else
                    WaterUtils.SendMessage(string.Format(WaterTexts.SetCollectRateNoParse, args[1]), commandArgs.User);
            }
        }

        private void CommandVolumetric(string[] args, CommandArgs commandArgs)
        {
            commandArgs.Water.Settings.Volumetric = !commandArgs.Water.Settings.Volumetric;
            _syncComponent.SyncClients(commandArgs.Water);
            WaterUtils.SendMessage(WaterTexts.ToggleVolumetrics, commandArgs.User);
        }
    }

    /// <summary>
    /// Arguments and information about the command sender
    /// </summary>
    public struct CommandArgs
    {
        /// <summary>
        /// The IdentityID of the command sender
        /// </summary>
        public long User;

        /// <summary>
        /// The SteamUserID of the command sender
        /// </summary>
        public ulong Sender;

        /// <summary>
        /// The promotion level of the command sender
        /// </summary>
        public MyPromoteLevel PromoteLevel;

        /// <summary>
        /// The position of the command sender, may be of player or spectator
        /// </summary>
        public Vector3D Position;

        /// <summary>
        /// The water closest to the command sender's position, may be null
        /// </summary>
        public WaterComponent Water;

        /// <summary>
        /// The planet closest to the command sender's position, may be null
        /// </summary>
        public MyPlanet Planet;
    }

    /// <summary>
    /// <see cref="WaterCommandComponent"/>
    /// </summary>
    public struct Command
    {
        /// <summary>
        /// The action that will be invoked when the command is run
        /// </summary>
        public readonly Action<string[], CommandArgs> Action;

        /// <summary>
        /// The /help descriptor of the command
        /// </summary>
        public string Description;
        
        /// <summary>
        /// The minimum promotion level for the command to be run
        /// </summary>
        public MyPromoteLevel PromoteLevel;

        /// <summary>
        /// The minimum number of args
        /// </summary>
        public int MinArgs;

        /// <summary>
        /// The maximum number of args
        /// </summary>
        public int MaxArgs;

        /// <summary>
        /// If true, the user must be near a planet for the command to run
        /// </summary>
        public bool RequirePlanet;

        /// <summary>
        /// If true, the user must be near water for the command to run
        /// </summary>
        public bool RequireWater;

        /// <summary>
        /// Whether the command should be allowed to run on Console edition 
        /// </summary>
        public bool Console;

        /// <summary>
        /// Whether the command should be run client-side
        /// </summary>
        public bool Client;

        /// <summary>
        /// The command will only be enabled when debug mode is enabled
        /// </summary>
        public bool RequireDebug;

        public Command(Action<string[], CommandArgs> action)
        {
            Action = action;

            Description = "No descriptor provided";
            PromoteLevel = MyPromoteLevel.None;
            MinArgs = 1;
            MaxArgs = 1;
            RequirePlanet = false;
            RequireWater = false;
            Console = false;
            Client = false;
            RequireDebug = false;
        }
    }
}
