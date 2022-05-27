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
using Jakaria.Components;
using Jakaria.SessionComponents;

namespace Jakaria.API
{
    //Do not include this file in your project modders
    public class WaterModAPIBackend
    {
        public const int MIN_API_VERSION = 14;
        public const ushort MOD_SYNC_ID = 50271;

        private static readonly Dictionary<string, Delegate> _modAPIMethods = new Dictionary<string, Delegate>()
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
            Water Water = WaterModComponent.Static.Waters[ID];
            return Water.TideDirection;
        }

        public static MyTuple<float, float> GetTideData(long ID)
        {
            Water Water = WaterModComponent.Static.Waters[ID];
            return new MyTuple<float, float>(Water.TideHeight, Water.TideSpeed);
        }

        public static MyTuple<float, float> GetPhysicsData(long ID)
        {
            Water Water = WaterModComponent.Static.Waters[ID];
            return new MyTuple<float, float>(Water.Material.Density, Water.Buoyancy);
        }

        public static MyTuple<Vector3D, bool, bool> GetRenderData(long ID)
        {
            Water Water = WaterModComponent.Static.Waters[ID];
            return new MyTuple<Vector3D, bool, bool>(Water.FogColor, Water.Transparent, Water.Lit);
        }

        public static MyTuple<float, float, float, int> GetWaveData(long ID)
        {
            Water Water = WaterModComponent.Static.Waters[ID];
            return new MyTuple<float, float, float, int>(Water.WaveHeight, Water.WaveSpeed, Water.WaveScale, 0);
        }

        public static MyTuple<Vector3D, float, float, float> GetPhysicalData(long ID)
        {
            Water Water = WaterModComponent.Static.Waters[ID];
            return new MyTuple<Vector3D, float, float, float>(Water.Position, Water.Radius, Water.Radius - Water.WaveHeight - Water.TideHeight, Water.Radius + Water.WaveHeight + Water.TideHeight);
        }

        public static int GetCrushDepth(long ID)
        {
            return 0;
        }

        public static float GetBuoyancyMultiplier(Vector3D Position, MyCubeSize GridSize, long? ID = 0)
        {
            Water Water = (ID == null) ? WaterModComponent.Static.GetClosestWater(Position) : WaterModComponent.Static.Waters[ID.Value];

            if (GridSize == MyCubeSize.Large)
                return (float)(1 + (-Water.GetDepth(ref Position) / 5000f)) / 50f * Water.Buoyancy;
            else
                return (float)(1 + (-Water.GetDepth(ref Position) / 5000f)) / 20f * Water.Buoyancy;
        }

        public static Vector3D GetUpDirection(Vector3D Position, long? ID = 0)
        {
            if (ID == null)
                return WaterModComponent.Static.GetClosestWater(Position)?.GetUpDirection(ref Position) ?? Vector3D.Up;
            else
                return WaterModComponent.Static.Waters[ID.Value].GetUpDirection(ref Position);
        }

        public static int SphereIntersects(BoundingSphereD Sphere, long? ID = 0)
        {
            if (ID == null)
                return WaterModComponent.Static.GetClosestWater(Sphere.Center)?.Intersects(ref Sphere) ?? 0;
            else
                return WaterModComponent.Static.Waters[ID.Value].Intersects(ref Sphere);
        }

        public static void SphereIntersects(ICollection<BoundingSphereD> Spheres, ICollection<int> Intersections, long? ID = 0)
        {
            if (ID == null)
            {
                foreach (var Sphere in Spheres)
                {
                    BoundingSphereD sphere = Sphere;
                    Intersections.Add(WaterModComponent.Static.GetClosestWater(Sphere.Center)?.Intersects(ref sphere) ?? 0);
                }
            }
            else
            {
                Water Water = WaterModComponent.Static.Waters[ID.Value];
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
                return WaterModComponent.Static.GetClosestWater(Line.From)?.Intersects(ref Line) ?? 0;
            else
                return WaterModComponent.Static.Waters[ID.Value].Intersects(ref Line);
        }

        public static void LineIntersectsWaterList(List<LineD> Lines, ICollection<int> Intersections, long? ID)
        {
            if (ID == null)
            {
                foreach (var Line in Lines)
                {
                    LineD line = Line;
                    Intersections.Add(WaterModComponent.Static.GetClosestWater(Line.From)?.Intersects(ref line) ?? 0);
                }
            }
            else
            {
                Water Water = WaterModComponent.Static.Waters[ID.Value];
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
            WaterCommandComponent.Static.Utilities_MessageEntered(MessageText, ref SendToOthers);
        }

        public static void BeforeStart()
        {
            MyAPIGateway.Utilities.SendModMessage(MOD_SYNC_ID, _modAPIMethods);
        }

        #region Water
        public static void CreateBubble(Vector3D Position, float Radius)
        {
            WaterEffectsComponent.Static.CreateBubble(ref Position, Radius);
        }

        public static void CreateSplash(Vector3D Position, float Radius, bool Audible)
        {
            WaterEffectsComponent.Static.CreateSplash(Position, Radius, Audible);
        }

        public static void CreatePhysicsSplash(Vector3D Position, Vector3D Velocity, float Radius, int Count = 1)
        {
            WaterEffectsComponent.Static.CreatePhysicsSplash(Position, Velocity, Radius, Count);
        }

        public static float? GetDepth(Vector3D Position, long? ID = null)
        {
            if (ID == null)
                return (float?)WaterModComponent.Static.GetClosestWater(Position)?.GetDepth(ref Position);
            else
                return (float?)WaterModComponent.Static.Waters[ID.Value].GetDepth(ref Position);
        }

        public static Vector3D GetClosestSurfacePoint(Vector3D Position, long? ID = null)
        {
            if (ID == null)
                return WaterModComponent.Static.GetClosestWater(Position)?.GetClosestSurfacePointGlobal(Position) ?? Position;
            else
                return WaterModComponent.Static.Waters[ID.Value].GetClosestSurfacePointGlobal(Position);
        }

        public static void GetClosestSurfacePoints(List<Vector3D> Positions, ICollection<Vector3D> Points, long? ID = null)
        {
            if (ID == null)
            {
                foreach (var Position in Positions)
                {
                    Points.Add(WaterModComponent.Static.GetClosestWater(Position)?.GetClosestSurfacePointGlobal(Position) ?? Position);
                }
            }
            else
            {
                Water Water = WaterModComponent.Static.Waters[ID.Value];

                foreach (var Position in Positions)
                {
                    Points.Add(Water.GetClosestSurfacePointGlobal(Position));
                }
            }
        }

        public static void ForceSync()
        {
            if (MyAPIGateway.Session.IsServer)
                MyAPIGateway.Multiplayer.SendMessageToOthers(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(WaterModComponent.Static.Waters)));
        }

        public static bool VerifyVersion(int ModAPIVersion, string ModName)
        {
            if (ModAPIVersion < MIN_API_VERSION)
            {
                WaterUtils.ShowMessage("The Mod '" + ModName + "' is using an oudated Water Mod API (" + ModAPIVersion + "), tell the author to update!");
                return false;
            }

            return true;
        }

        public static long? GetClosestWater(Vector3D Position)
        {
            return WaterModComponent.Static.GetClosestWater(Position)?.PlanetID;
        }

        public static bool IsUnderwater(Vector3D Position, long? ID = null)
        {
            if (ID == null)
                return WaterModComponent.Static.GetClosestWater(Position)?.IsUnderwater(ref Position) ?? false;
            else
                return WaterModComponent.Static.Waters[ID.Value]?.IsUnderwater(ref Position) ?? false;
        }

        public static bool HasWater(long ID)
        {
            return WaterModComponent.Static.Waters.ContainsKey(ID);
        }
        #endregion
    }
}