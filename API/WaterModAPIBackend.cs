using System;
using System.Collections.Generic;
using VRageMath;
using Jakaria.Utils;
using Sandbox.ModAPI;
using VRage.Serialization;
using VRage.Game;
using VRage;
using System.IO;
using Jakaria.Configs;
using Sandbox.Definitions;
using Sandbox.Game.Entities;

namespace Jakaria.API
{
    //Do not include this file in your project modders
    public class WaterModAPIBackend
    {
        public const int MinVersion = 14;
        public const ushort ModHandlerID = 50271;

        private static readonly Dictionary<string, Delegate> ModAPIMethods = new Dictionary<string, Delegate>()
        {
            ["VerifyVersion"] = new Func<int, string, bool>(VerifyVersion),
            ["IsUnderwater"] = new Func<Vector3D, long?, bool>(IsUnderwater),
            ["GetClosestWater"] = new Func<Vector3D, long?>(GetClosestWater),
            ["SphereIntersectsWater"] = new Func<BoundingSphereD, long?, int>(SphereIntersects),
            ["SphereIntersectsWaterList"] = new Action<List<BoundingSphereD>, ICollection<int>, long?>(SphereIntersects),
            ["GetClosestSurfacePoint"] = new Func<Vector3D, long?, Vector3D>(GetClosestSurfacePoint),
            ["GetClosestSurfacePointList"] = new Action<List<Vector3D>, ICollection<Vector3D>, long?>(GetClosestSurfacePoints),
            ["LineIntersectsWater"] = new Func<LineD, long?, int>(LineIntersectsWater),
            ["LineIntersectsWaterList"] = new Action<List<LineD>, ICollection<int>, long?>(LineIntersectsWaterList),
            ["GetDepth"] = new Func<Vector3D, long?, float?>(GetDepth),
            ["CreateSplash"] = new Action<Vector3D, float, bool>(CreateSplash),
            ["CreatePhysicsSplash"] = new Action<Vector3D, Vector3D, float, int>(CreatePhysicsSplash),
            ["CreateBubble"] = new Action<Vector3D, float>(CreateBubble),
            ["ForceSync"] = new Action(ForceSync),
            ["RunCommand"] = new Action<string>(RunCommand),
            ["GetUpDirection"] = new Func<Vector3D, long?, Vector3D>(GetUpDirection),
            ["HasWater"] = new Func<long, bool>(HasWater),
            ["GetBuoyancyMultiplier"] = new Func<Vector3D, MyCubeSize, long?, float>(GetBuoyancyMultiplier),

            ["GetCrushDepth"] = new Func<long, int>(GetCrushDepth),
            ["GetPhysicalData"] = new Func<long, MyTuple<Vector3D, float, float, float>>(GetPhysicalData),
            ["GetWaveData"] = new Func<long, MyTuple<float, float, float, int>>(GetWaveData),
            ["GetRenderData"] = new Func<long, MyTuple<Vector3D, bool, bool>>(GetRenderData),
            ["GetPhysicsData"] = new Func<long, MyTuple<float, float>>(GetPhysicsData),
            ["GetTideData"] = new Func<long, MyTuple<float, float>>(GetTideData),
            ["GetTideDirection"] = new Func<long, Vector3D>(GetTideDirection),
        };

        public static Vector3D GetTideDirection(long ID)
        {
            Water Water = WaterMod.Static.Waters[ID];
            return Water.tideDirection;
        }

        public static MyTuple<float, float> GetTideData(long ID)
        {
            Water Water = WaterMod.Static.Waters[ID];
            return new MyTuple<float, float>(Water.TideHeight, Water.TideSpeed);
        }

        public static MyTuple<float, float> GetPhysicsData(long ID)
        {
            Water Water = WaterMod.Static.Waters[ID];
            return new MyTuple<float, float>(Water.Material.Density, Water.Buoyancy);
        }

        public static MyTuple<Vector3D, bool, bool> GetRenderData(long ID)
        {
            Water Water = WaterMod.Static.Waters[ID];
            return new MyTuple<Vector3D, bool, bool>(Water.FogColor, Water.Transparent, Water.Lit);
        }

        public static MyTuple<float, float, float, int> GetWaveData(long ID)
        {
            Water Water = WaterMod.Static.Waters[ID];
            return new MyTuple<float, float, float, int>(Water.WaveHeight, Water.WaveSpeed, Water.WaveScale, 0);
        }

        public static MyTuple<Vector3D, float, float, float> GetPhysicalData(long ID)
        {
            Water Water = WaterMod.Static.Waters[ID];
            return new MyTuple<Vector3D, float, float, float>(Water.Position, Water.Radius, Water.Radius - Water.WaveHeight - Water.TideHeight, Water.Radius + Water.WaveHeight + Water.TideHeight);
        }

        public static int GetCrushDepth(long ID)
        {
            return 0;
        }

        public static float GetBuoyancyMultiplier(Vector3D Position, MyCubeSize GridSize, long? ID = 0)
        {
            Water Water = (ID == null) ? WaterMod.Static.GetClosestWater(Position) : WaterMod.Static.Waters[ID.Value];

            if (GridSize == MyCubeSize.Large)
                return (float)(1 + (-Water.GetDepth(ref Position) / 5000f)) / 50f * Water.Buoyancy;
            else
                return (float)(1 + (-Water.GetDepth(ref Position) / 5000f)) / 20f * Water.Buoyancy;
        }

        public static Vector3D GetUpDirection(Vector3D Position, long? ID = 0)
        {
            if (ID == null)
                return WaterMod.Static.GetClosestWater(Position)?.GetUpDirection(ref Position) ?? Vector3D.Up;
            else
                return WaterMod.Static.Waters[ID.Value].GetUpDirection(ref Position);
        }

        public static int SphereIntersects(BoundingSphereD Sphere, long? ID = 0)
        {
            if (ID == null)
                return WaterMod.Static.GetClosestWater(Sphere.Center)?.Intersects(ref Sphere) ?? 0;
            else
                return WaterMod.Static.Waters[ID.Value].Intersects(ref Sphere);
        }

        public static void SphereIntersects(ICollection<BoundingSphereD> Spheres, ICollection<int> Intersections, long? ID = 0)
        {
            if (ID == null)
            {
                foreach (var Sphere in Spheres)
                {
                    BoundingSphereD sphere = Sphere;
                    Intersections.Add(WaterMod.Static.GetClosestWater(Sphere.Center)?.Intersects(ref sphere) ?? 0);
                }
            }
            else
            {
                Water Water = WaterMod.Static.Waters[ID.Value];
                foreach (var Sphere in Spheres)
                {
                    BoundingSphereD sphere = Sphere;
                    Intersections.Add(Water.Intersects(ref sphere));
                }
            }
        }

        public static int LineIntersectsWater(LineD Line, long? ID = 0)
        {
            if (ID == null)
                return WaterMod.Static.GetClosestWater(Line.From)?.Intersects(ref Line) ?? 0;
            else
                return WaterMod.Static.Waters[ID.Value].Intersects(ref Line);
        }

        public static void LineIntersectsWaterList(List<LineD> Lines, ICollection<int> Intersections, long? ID)
        {
            if (ID == null)
            {
                foreach (var Line in Lines)
                {
                    LineD line = Line;
                    Intersections.Add(WaterMod.Static.GetClosestWater(Line.From)?.Intersects(ref line) ?? 0);
                }
            }
            else
            {
                Water Water = WaterMod.Static.Waters[ID.Value];
                foreach (var Line in Lines)
                {
                    LineD line = Line;
                    Intersections.Add(Water?.Intersects(ref line) ?? 0);
                }
            }
        }

        public static void RunCommand(string MessageText)
        {
            bool SendToOthers = false;
            WaterMod.Static.Utilities_MessageEntered(MessageText, ref SendToOthers);
        }

        public static void LoadData()
        {
            MyAPIGateway.Utilities.SendModMessage(ModHandlerID, ModAPIMethods);

            WaterUtils.WriteLog("Beginning load water block configs");

            try
            {
                foreach (var Mod in MyAPIGateway.Session.Mods)
                {
                    if (MyAPIGateway.Utilities.FileExistsInModLocation("Data/WaterConfig.xml", Mod))
                    {

                        TextReader Reader = MyAPIGateway.Utilities.ReadFileInModLocation("Data/WaterConfig.xml", Mod);
                        if (Reader != null)
                        {
                            string xml = Reader.ReadToEnd();

                            if (xml.Length > 0)
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

                                        BlockConfig.Init();
                                        
                                        WaterData.BlockConfigs[BlockConfig.DefinitionId] = BlockConfig;
                                        WaterUtils.WriteLog("Loaded Block Config '" + BlockConfig.DefinitionId + "'");
                                    }

                                if (WaterConfig.PlanetConfigs != null)
                                    foreach (var PlanetConfig in WaterConfig.PlanetConfigs)
                                    {
                                        if (PlanetConfig.TypeId == "" && PlanetConfig.SubtypeId == "")
                                        {
                                            WaterUtils.WriteLog("Empty planet definition, skipping...");
                                            continue;
                                        }

                                        PlanetConfig.Init();

                                        WaterData.PlanetConfigs[PlanetConfig.DefinitionId] = PlanetConfig;
                                        WaterUtils.WriteLog("Loaded Planet Config '" + PlanetConfig.DefinitionId + "'");
                                    }

                                if (WaterConfig.CharacterConfigs != null)
                                    foreach (var CharacterConfig in WaterConfig.CharacterConfigs)
                                    {
                                        CharacterConfig.Init();

                                        WaterData.CharacterConfigs[CharacterConfig.DefinitionId] = CharacterConfig;
                                        WaterUtils.WriteLog("Loaded Character Config '" + CharacterConfig.DefinitionId + "'");
                                    }

                                if (WaterConfig.RespawnPodConfigs != null)
                                    foreach (var RespawnPodConfig in WaterConfig.RespawnPodConfigs)
                                    {
                                        RespawnPodConfig.Init();

                                        WaterData.RespawnPodConfigs[RespawnPodConfig.DefinitionId] = RespawnPodConfig;
                                        WaterUtils.WriteLog("Loaded Respawn Pod Config '" + RespawnPodConfig.DefinitionId + "'");
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
                                        Material.Init();

                                        WaterData.MaterialConfigs[Material.SubtypeId] = Material;
                                        WaterUtils.WriteLog("Loaded Water Material '" + Material.SubtypeId + "'");
                                    }

                                Reader.Dispose();
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                WaterUtils.WriteLog(e.ToString());
            }

            WaterUtils.WriteLog("Finished loading water block configs");
        }

        #region Water
        public static void CreateBubble(Vector3D Position, float Radius)
        {
            WaterMod.Static.CreateBubble(ref Position, Radius);
        }

        public static void CreateSplash(Vector3D Position, float Radius, bool Audible)
        {
            WaterMod.Static.CreateSplash(Position, Radius, Audible);
        }

        public static void CreatePhysicsSplash(Vector3D Position, Vector3D Velocity, float Radius, int Count = 1)
        {
            WaterMod.Static.CreatePhysicsSplash(Position, Velocity, Radius, Count);
        }

        public static float? GetDepth(Vector3D Position, long? ID = null)
        {
            if (ID == null)
                return (float?)WaterMod.Static.GetClosestWater(Position)?.GetDepth(ref Position);
            else
                return (float?)WaterMod.Static.Waters[ID.Value].GetDepth(ref Position);
        }

        public static Vector3D GetClosestSurfacePoint(Vector3D Position, long? ID = null)
        {
            if (ID == null)
                return WaterMod.Static.GetClosestWater(Position)?.GetClosestSurfacePoint(Position) ?? Position;
            else
                return WaterMod.Static.Waters[ID.Value].GetClosestSurfacePoint(Position);
        }

        public static void GetClosestSurfacePoints(List<Vector3D> Positions, ICollection<Vector3D> Points, long? ID = null)
        {
            if (ID == null)
            {
                foreach (var Position in Positions)
                {
                    Points.Add(WaterMod.Static.GetClosestWater(Position)?.GetClosestSurfacePoint(Position) ?? Position);
                }
            }
            else
            {
                Water Water = WaterMod.Static.Waters[ID.Value];

                foreach (var Position in Positions)
                {
                    Points.Add(Water.GetClosestSurfacePoint(Position));
                }
            }
        }

        public static void ForceSync()
        {
            if (MyAPIGateway.Session.IsServer)
                MyAPIGateway.Multiplayer.SendMessageToOthers(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(WaterMod.Static.Waters)));
        }

        public static bool VerifyVersion(int ModAPIVersion, string ModName)
        {
            if (ModAPIVersion < MinVersion)
            {
                WaterUtils.ShowMessage("The Mod '" + ModName + "' is using an oudated Water Mod API (" + ModAPIVersion + "), tell the author to update!");
                return false;
            }

            return true;
        }

        public static long? GetClosestWater(Vector3D Position)
        {
            return WaterMod.Static.GetClosestWater(Position)?.PlanetID;
        }

        public static bool IsUnderwater(Vector3D Position, long? ID = null)
        {
            if (ID == null)
                return WaterMod.Static.GetClosestWater(Position)?.IsUnderwater(ref Position) ?? false;
            else
                return WaterMod.Static.Waters[ID.Value]?.IsUnderwater(ref Position) ?? false;
        }

        public static bool HasWater(long ID)
        {
            return WaterMod.Static.Waters.ContainsKey(ID);
        }
        #endregion
    }
}