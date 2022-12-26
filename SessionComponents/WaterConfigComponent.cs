using Jakaria.Configs;
using Jakaria.Utils;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace Jakaria.SessionComponents
{
    /// <summary>
    /// Component in charge of loading configs/definitions
    /// </summary>
    public class WaterConfigComponent : SessionComponentBase
    {
        public override void LoadData()
        {
            LoadConfigFiles();

            LoadBlockConfigs();
        }
        
        private void LoadConfigFiles()
        {
            WaterUtils.WriteLog("Beginning load water configs");
            
            TextReader reader = null;

            int configsLoaded = 0;
            foreach (var mod in MyAPIGateway.Session.Mods)
            {
                try
                {
                    IMyModContext modContext = mod.GetModContext();
                    string relativePath = "Data\\WaterConfig.xml";

                    if (MyAPIGateway.Utilities.FileExistsInModLocation(relativePath, mod))
                    {
                        WaterUtils.WriteLog($"Found Water Config for mod '{mod.FriendlyName}'");

                        reader = MyAPIGateway.Utilities.ReadFileInModLocation(relativePath, mod);

                        if (reader != null)
                        {
                            string xml = reader.ReadToEnd();

                            if (!string.IsNullOrEmpty(xml))
                            {
                                configsLoaded++;

                                LoadConfig(xml, modContext);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    WaterUtils.WriteLog($"Exception loading water config for mod '{mod.FriendlyName}'");
                    WaterUtils.WriteLog(e.ToString());
                }
                finally
                {
                    reader?.Dispose();
                }
            }

            if (configsLoaded == 0)
            {
                WaterUtils.WriteLog("Unable to locate any water config files. This is likely due to the -path parameter in your server's setup. If you are using a service provider, this is likely not fixable until Keen fixes the issue. A backup config file has been loaded instead.");
                LoadConfig(WaterData.BackupConfig, null);
            }

            WaterUtils.WriteLog("Finished loading water configs");
        }

        private void LoadConfig(string xml, IMyModContext modContext)
        {
            WaterConfigAPI WaterConfig = MyAPIGateway.Utilities.SerializeFromXML<WaterConfigAPI>(xml);

            if (WaterConfig.BlockConfigs != null)
                foreach (var BlockConfig in WaterConfig.BlockConfigs)
                {
                    if (BlockConfig.TypeId == "" && BlockConfig.SubtypeId == "")
                    {
                        WaterUtils.WriteLog("Empty block definition, skipping...");
                        continue;
                    }

                    BlockConfig.Init(modContext);

                    if (!BlockConfig.DefinitionId.TypeId.IsNull)
                    {
                        WaterData.BlockConfigs[BlockConfig.DefinitionId] = BlockConfig;
                        WaterUtils.WriteLog("Loaded Block Config '" + BlockConfig.DefinitionId + "'");
                    }
                }

            if (WaterConfig.PlanetConfigs != null)
                foreach (var PlanetConfig in WaterConfig.PlanetConfigs)
                {
                    if (PlanetConfig.TypeId == "" && PlanetConfig.SubtypeId == "")
                    {
                        WaterUtils.WriteLog("Empty planet definition, skipping...");
                        continue;
                    }

                    PlanetConfig.Init(modContext);

                    if (!PlanetConfig.DefinitionId.TypeId.IsNull)
                    {
                        WaterData.PlanetConfigs[PlanetConfig.DefinitionId] = PlanetConfig;
                        WaterUtils.WriteLog("Loaded Planet Config '" + PlanetConfig.DefinitionId + "'");
                    }
                }

            if (WaterConfig.CharacterConfigs != null)
                foreach (var CharacterConfig in WaterConfig.CharacterConfigs)
                {
                    CharacterConfig.Init(modContext);

                    if (!CharacterConfig.DefinitionId.TypeId.IsNull)
                    {
                        WaterData.CharacterConfigs[CharacterConfig.DefinitionId] = CharacterConfig;
                        WaterUtils.WriteLog("Loaded Character Config '" + CharacterConfig.DefinitionId + "'");
                    }
                }

            if (WaterConfig.RespawnPodConfigs != null)
                foreach (var RespawnPodConfig in WaterConfig.RespawnPodConfigs)
                {
                    RespawnPodConfig.Init(modContext);

                    if (!RespawnPodConfig.DefinitionId.TypeId.IsNull)
                    {
                        WaterData.RespawnPodConfigs[RespawnPodConfig.DefinitionId] = RespawnPodConfig;
                        WaterUtils.WriteLog("Loaded Respawn Pod Config '" + RespawnPodConfig.DefinitionId + "'");
                    }
                }

            if (WaterConfig.WaterTextures != null)
                foreach (var Texture in WaterConfig.WaterTextures)
                {
                    WaterData.WaterTextures.Add(Texture);
                    WaterUtils.WriteLog("Loaded Water Texture '" + Texture + "'");
                }

            if (WaterConfig.MaterialConfigs != null)
                foreach (var Material in WaterConfig.MaterialConfigs)
                {
                    Material.Init(modContext);

                    WaterData.MaterialConfigs[Material.SubtypeId] = Material;
                    WaterUtils.WriteLog("Loaded Water Material '" + Material.SubtypeId + "'");
                }

            if (WaterConfig.FishConfigs != null)
                foreach (var Fish in WaterConfig.FishConfigs)
                {
                    Fish.Init(modContext);

                    WaterData.FishConfigs[Fish.SubtypeId] = Fish;
                    WaterUtils.WriteLog("Loaded Fish Config '" + Fish.SubtypeId + "'");
                }
        }

        private void LoadBlockConfigs()
        {
            //Calculate volume of every block in m^3
            foreach (var definition in MyDefinitionManager.Static.GetAllDefinitions())
            {
                if (definition is MyCharacterDefinition && !WaterData.CharacterConfigs.ContainsKey(definition.Id))
                {
                    MyCharacterDefinition characterDefinition = (MyCharacterDefinition)definition;
                    WaterData.CharacterConfigs[characterDefinition.Id] = new CharacterConfig()
                    {
                        Volume = characterDefinition.CharacterLength * characterDefinition.CharacterWidth * characterDefinition.CharacterHeight,
                    };
                }

                if(definition is MyPlanetGeneratorDefinition && !WaterData.PlanetConfigs.ContainsKey(definition.Id))
                {
                    MyPlanetGeneratorDefinition planetGeneratorDefinition = (MyPlanetGeneratorDefinition)definition;
                    WaterData.PlanetConfigs[planetGeneratorDefinition.Id] = new PlanetConfig(planetGeneratorDefinition.Id)
                    {
                        
                    };
                }

                if (definition is MyCubeBlockDefinition)
                {
                    //Light Armor Block is 2.5x2.5x2.5 and weighs 500kg
                    //Density = Mass / Volume
                    //Density = 500kg / (2.5*2.5*2.5)
                    //Density = 32kg/m^3
                    //Volume = Mass / Density
                    //Volume = Block Mass / 32kg

                    //Heavy Armor Block is 2.5x2.5x2.5 and weighs 3300kg
                    //Density = Mass / Volume
                    //Density = 3300kg / (2.5*2.5*2.5)
                    //Density = 211.2kg/m^3
                    //Volume = Mass / Density
                    //Volume = Block Mass / 211.2kg

                    //Catwalk is 193kg and has 3.125m^3 Density of 61.76
                    //Window is 435kg and has 3.125m^3 Density of 139.2
                    //Passage is 574kg and has 7.03125m^3 Density of 81.6
                    //Conveyor is 394kg and has 4.375m^3 Density of 90.1
                    //Piston top is 400kg and has 3.5m^3 Density of 114.3
                    //Desk is 330kg and has 1.1m^3 Density of 300
                    //Solar Panel is 516kg and has 25m^3 Density of 20.64
                    //Text Panel is 222kg and has 3.125m^3 Density of 71
                    //Cover Block is 160kg and has 1.25m^3 Density of 128
                    //Neon tube is 58kg and has 0.3125m^3 Density of 185.6
                    //Beam Block is 500kg and has 8.125m^3 Density of 61.5
                    //Sliding Door is 1065kg and has 8.125m^3 Density of 131.1

                    MyCubeBlockDefinition blockDefinition = (MyCubeBlockDefinition)definition;

                    if (blockDefinition.Enabled && blockDefinition.Id != null)
                    {
                        float blockSize = ((blockDefinition.CubeSize == MyCubeSize.Large) ? 2.5f : 0.5f);
                        Vector3 blockDimensions = blockDefinition.Size * blockSize;

                        BlockConfig Config;

                        if (!WaterData.BlockConfigs.TryGetValue(definition.Id, out Config) || Config.Volume < 0) //If it could not find the definition or the volume isn't set
                        {
                            if (Config == null)
                                Config = WaterData.BlockConfigs[blockDefinition.Id] = new BlockConfig()
                                {
                                    DefinitionId = blockDefinition.Id
                                };

                            if (!(blockDefinition is MyFunctionalBlockDefinition))
                                Config.MaximumPressure = 4014.08f;

                            if (blockDefinition.IsAirTight == true)
                                Config.Volume = blockDimensions.Volume; //IsAirtight implies that every side is a solid wall, meaning that 100% of water in the block is displaced
                            else if (!blockDefinition.HasPhysics)
                                Config.Volume = 0; //Block is negligibly small (Control panel, Light, etc...)
                            else if (blockDefinition.MountPoints == null)
                                Config.Volume = blockDimensions.Volume; //No mount points means you can place it on any side, meaning that 100% of water is displaced
                            else if (blockDefinition.DescriptionEnum?.String == "Description_LightArmor")
                            {
                                Config.Volume = Math.Min(blockDefinition.Mass / 32f, (blockDimensions.Volume)); //Light Armor has a uniform density
                                Config.MaximumPressure = 4014.08f;
                            }
                            else if (blockDefinition.DescriptionEnum?.String == "Description_HeavyArmor")
                            {
                                Config.Volume = Math.Min(blockDefinition.Mass / 211.2f, (blockDimensions.Volume)); //Heavy Armor can assume uniform density
                                Config.MaximumPressure = 6021.12f;
                            }
                            else if (blockDefinition.DescriptionEnum?.String == "Description_DeadEngineer")
                                Config.Volume = 0; //Don't bother with dead engineer
                            else if (blockDefinition.DescriptionEnum?.String == "Description_ButtonPanel")
                                Config.Volume = 0; //Don't bother with button panels
                            else if (blockDefinition.DescriptionEnum?.String == "Description_Wheel")
                                Config.Volume = (float)(Math.PI * Math.Pow(blockDimensions.Max() / 2f, 2f) * blockDimensions.Min()); //Wheels are cylinders and only the diameter ever changes. v = π(r^2)h
                            else if (blockDefinition.DescriptionEnum?.String == "Description_SteelCatwalk")
                                Config.Volume = Math.Min(blockDefinition.Mass / 61.76f, (blockDimensions.Volume)); //Catwalk can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_GratedCatwalk")
                                Config.Volume = Math.Min(blockDefinition.Mass / 61.76f, (blockDimensions.Volume)); //Catwalk can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_GratedCatwalkCorner")
                                Config.Volume = Math.Min(blockDefinition.Mass / 61.76f, (blockDimensions.Volume)); //Catwalk can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_GratedCatwalkStraight")
                                Config.Volume = Math.Min(blockDefinition.Mass / 61.76f, (blockDimensions.Volume)); //Catwalk can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_GratedCatwalkWall")
                                Config.Volume = Math.Min(blockDefinition.Mass / 61.76f, (blockDimensions.Volume)); //Catwalk can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_Ramp")
                                Config.Volume = (blockDimensions.X * blockDimensions.Y * blockDimensions.Z) / 2; //Ramps are literally just half the block space it takes up
                            else if (blockDefinition is MyGasTankDefinition)
                            {
                                Config.Volume = (float)(Math.PI * Math.Pow(blockDimensions.Min() / 2f, 2f) * (blockDimensions.Max())); //Tanks are generally cylinders. v = πr2h
                                Config.IsPressurized = true;
                            }
                            else if (blockDefinition is MyThrustDefinition)
                                Config.Volume = (float)(Math.PI * Math.Pow(blockDimensions.Min() / 2f, 2f) * (blockDimensions.Max())); //Thrusters are generally cylinders. v = πr2h
                            else if (blockDefinition is MyPistonBaseDefinition)
                                Config.Volume = (float)(Math.PI * Math.Pow(blockDimensions.Min() / 2f, 2f) * (blockDimensions.Max())); //Pistons are generally cylinders. v = πr2h
                            else if (blockDefinition.DescriptionEnum?.String == "Description_VerticalWindow")
                                Config.Volume = Math.Min(blockDefinition.Mass / 139.2f, (blockDimensions.Volume)); //Window can assume uniform density (glass)
                            else if (blockDefinition.DescriptionEnum?.String == "Description_DiagonalWindow")
                                Config.Volume = Math.Min(blockDefinition.Mass / 139.2f, (blockDimensions.Volume)); //Window can assume uniform density (glass)
                            else if (blockDefinition.DescriptionEnum?.String == "Description_Window")
                                Config.Volume = Math.Min(blockDefinition.Mass / 139.2f, (blockDimensions.Volume)); //Window can assume uniform density (glass)
                            else if (blockDefinition.DescriptionEnum?.String == "Description_Letters")
                                Config.Volume = 0; //Letter blocks are really small, don't bother
                            else if (blockDefinition.DescriptionEnum?.String == "Description_Symbols")
                                Config.Volume = 0; //Symbol blocks are really small, don't bother
                            else if (blockDefinition is MyGravityGeneratorBaseDefinition)
                                Config.Volume = (float)((4f / 3f) * Math.PI * (blockDimensions.X / 2) * (blockDimensions.Y / 2) * (blockDimensions.Z / 2)); //Gravity Generators are pretty much spheres v = (4/3)πr3
                            else if (blockDefinition is MyReactorDefinition)
                                Config.Volume = (float)((4f / 3f) * Math.PI * (blockDimensions.X / 2) * (blockDimensions.Y / 2) * (blockDimensions.Z / 2)); //Reactors are pretty much spheres v = (4/3)πr3
                            else if (blockDefinition is MyDecoyDefinition)
                                Config.Volume = (float)((4f / 3f) * Math.PI * (blockDimensions.X / 2) * (blockDimensions.Y / 2) * (blockDimensions.Z / 2)); //Decoys are pretty much spheres v = (4/3)πr3
                            else if (blockDefinition is MyWarheadDefinition)
                                Config.Volume = (float)((4f / 3f) * Math.PI * (blockDimensions.X / 2) * (blockDimensions.Y / 2) * (blockDimensions.Z / 2)); //Warheads are pretty much spheres v = (4/3)πr3
                            else if (blockDefinition is MyGyroDefinition)
                                Config.Volume = (float)((4f / 3f) * Math.PI * (blockDimensions.X / 2) * (blockDimensions.Y / 2) * (blockDimensions.Z / 2)); //Gyroscopes are pretty much spheres v = (4/3)πr3
                            else if (blockDefinition is MyConveyorSorterDefinition)
                                Config.Volume = Math.Min(blockDefinition.Mass / 90.1f, (blockDimensions.Volume)); //Conveyors can assume uniform density
                            else if (blockDefinition is MySolarPanelDefinition)
                                Config.Volume = Math.Min(blockDefinition.Mass / 20.64f, (blockDimensions.Volume)); //Solar Panels can assume uniform density
                            else if (blockDefinition is MyTextPanelDefinition)
                                Config.Volume = Math.Min(blockDefinition.Mass / 71f, (blockDimensions.Volume)); //Text Panels can assume uniform density
                            else if (blockDefinition is MyShipToolDefinition)
                                Config.Volume = blockDimensions.Volume / 2; //Ship tools are generally one full block and some extra, I'll just account for one block
                            else if (blockDefinition.DescriptionEnum?.String == "Description_Passage")
                                Config.Volume = Math.Min(blockDefinition.Mass / 81.6f, (blockDimensions.Volume)); //Passage can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_Conveyor")
                                Config.Volume = Math.Min(blockDefinition.Mass / 90.1f, (blockDimensions.Volume)); //Conveyors can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_PistonTop")
                                Config.Volume = Math.Min(blockDefinition.Mass / 114.3f, (blockDimensions.Volume)); //Piston top can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_Desk")
                                Config.Volume = Math.Min(blockDefinition.Mass / 300f, (blockDimensions.Volume)); //Desks can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_FullCoverWall")
                                Config.Volume = Math.Min(blockDefinition.Mass / 128f, (blockDimensions.Volume)); //Cover Walls can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_FullCoverWall")
                                Config.Volume = Math.Min(blockDefinition.Mass / 128f, (blockDimensions.Volume)); //Cover Walls can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_RailingStraight")
                                Config.Volume = Math.Min(blockDefinition.Mass / 128f, (blockDimensions.Volume)); //Cover Walls can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_RailingDouble")
                                Config.Volume = Math.Min(blockDefinition.Mass / 128f, (blockDimensions.Volume)); //Cover Walls can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_RailingCorner")
                                Config.Volume = Math.Min(blockDefinition.Mass / 128f, (blockDimensions.Volume)); //Cover Walls can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_RailingDiagonal")
                                Config.Volume = Math.Min(blockDefinition.Mass / 128f, (blockDimensions.Volume)); //Cover Walls can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_NeonTubes")
                                Config.Volume = Math.Min(blockDefinition.Mass / 185.6f, (blockDimensions.Volume)); //Neon Tubes can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_BeamBlock")
                                Config.Volume = Math.Min(blockDefinition.Mass / 61.5f, (blockDimensions.Volume)); //Neon Tubes can assume uniform density
                            else if (blockDefinition is MyCargoContainerDefinition)
                                Config.Volume = blockDimensions.Volume; //Cargo Containers are cubes
                            else if (blockDefinition is MyAssemblerDefinition)
                                Config.Volume = blockDimensions.Volume; //Assemblers are cubes
                            else if (blockDefinition is MyJumpDriveDefinition)
                                Config.Volume = blockDimensions.Volume; //Jump Drives are cubes
                            else if (blockDefinition is MyBatteryBlockDefinition)
                                Config.Volume = blockDimensions.Volume; //Batteries are cubes
                            else if (blockDefinition is MyAirtightHangarDoorDefinition)
                                Config.Volume = (blockDimensions.X * blockDimensions.Y * blockDimensions.Z) / 2; //Hangar Doors are somewhat slopes
                            else if (blockDefinition is MyAirtightSlideDoorDefinition)
                                Config.Volume = Math.Min(blockDefinition.Mass / 131.1f, blockDimensions.Volume); //Sliding Doors assume uniform density
                            else if (blockDefinition is MyAirtightDoorGenericDefinition)
                                Config.Volume = Math.Min(blockDefinition.Mass / 131.1f, blockDimensions.Volume); //Doors assume uniform density
                            else if (blockDefinition is MyAdvancedDoorDefinition)
                                Config.Volume = Math.Min(blockDefinition.Mass / 131.1f, blockDimensions.Volume); //Doors assume uniform density
                            else if (blockDefinition is MyDoorDefinition)
                                Config.Volume = Math.Min(blockDefinition.Mass / 131.1f, blockDimensions.Volume); //Doors assume uniform density
                            else if (blockDefinition is MyRefineryDefinition)
                                Config.Volume = (float)(Math.PI * Math.Pow(blockDimensions.Min() / 2f, 2f) * (blockDimensions.Max())); //Refinerys are cylinders
                            else if (blockDefinition is MyCockpitDefinition)
                            {
                                Config.Volume = (blockDimensions.X * blockDimensions.Y * blockDimensions.Z) / 2; //Assume cockpits are similar to ramp shape
                                Config.IsPressurized = (blockDefinition as MyCockpitDefinition).IsPressurized;
                            }
                            else if (blockDefinition.DescriptionEnum?.String == "Description_ViewPort")
                                Config.Volume = (blockDimensions.X * blockDimensions.Y * blockDimensions.Z) / 2; //Viewports are half blocks
                            else if (blockDefinition.DescriptionEnum?.String == "Description_StorageShelf")
                                Config.Volume = (blockDimensions.X * blockDimensions.Y * blockDimensions.Z) * (2 / 5); //Shelfs are weird so I'll just approximate
                            else if (blockDefinition.DescriptionEnum?.String == "Description_WeaponRack")
                                Config.Volume = (blockDimensions.X * blockDimensions.Y * blockDimensions.Z) * (2 / 5); //Weapon Racks are weird so I'll just approximate
                            else
                            {
                                Config.Volume = Math.Min(blockDefinition.Mass / 32f, (blockDimensions.Volume)); //No more information, treat it like a steel block
                            }

                            Config.Init(null);
                        }

                        Config.Volume = Math.Max(Math.Min(Config.Volume, blockDimensions.Volume), 0); //Clamp volume to never be greater than the possible volume
                    }
                }
            }
        }
    }
}
