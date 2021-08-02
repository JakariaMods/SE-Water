using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;
using Jakaria.Utils;
using Sandbox.ModAPI;
using VRage.Serialization;
using VRage.Game;
using VRage;

namespace Jakaria.API
{
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
            ["GetClosestSurfacePointList"] = new Action<List<Vector3D>, ICollection<Vector3D>, long?>(GetClosestSurfacePoint),
            ["LineIntersectsWater"] = new Func<LineD, long?, int>(LineIntersectsWater),
            ["LineIntersectsWaterList"] = new Action<List<LineD>, ICollection<int>, long?>(LineIntersectsWaterList),
            ["GetDepth"] = new Func<Vector3D, long?, float?>(GetDepth),
            ["CreateSplash"] = new Action<Vector3D, float, bool>(CreateSplash),
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

        private static Vector3D GetTideDirection(long ID)
        {
            Water Water = WaterMod.Static.Waters[ID];
            return Water.tideDirection;
        }

        private static MyTuple<float, float> GetTideData(long ID)
        {
            Water Water = WaterMod.Static.Waters[ID];
            return new MyTuple<float, float>(Water.tideHeight, Water.tideSpeed);
        }

        private static MyTuple<float, float> GetPhysicsData(long ID)
        {
            Water Water = WaterMod.Static.Waters[ID];
            return new MyTuple<float, float>(Water.viscosity, Water.buoyancy);
        }

        private static MyTuple<Vector3D, bool, bool> GetRenderData(long ID)
        {
            Water Water = WaterMod.Static.Waters[ID];
            return new MyTuple<Vector3D, bool, bool>(Water.fogColor, Water.transparent, Water.lit);
        }

        private static MyTuple<float, float, float, int> GetWaveData(long ID)
        {
            Water Water = WaterMod.Static.Waters[ID];
            return new MyTuple<float, float, float, int>(Water.waveHeight, Water.waveSpeed, Water.waveScale, Water.seed);
        }

        private static MyTuple<Vector3D, float, float, float> GetPhysicalData(long ID)
        {
            Water Water = WaterMod.Static.Waters[ID];
            return new MyTuple<Vector3D, float, float, float>(Water.position, Water.radius, Water.radius - Water.waveHeight - Water.tideHeight, Water.radius + Water.waveHeight + Water.tideHeight);
        }

        private static int GetCrushDepth(long ID)
        {
            return WaterMod.Static.Waters[ID].crushDepth;
        }

        private static float GetBuoyancyMultiplier(Vector3D Position, MyCubeSize GridSize, long? ID = 0)
        {
            Water Water = (ID == null) ? WaterMod.Static.GetClosestWater(Position) : WaterMod.Static.Waters[ID.Value];

            if (GridSize == MyCubeSize.Large)
                return (1 + (-Water.GetDepth(Position) / 5000f)) / 50f * Water.buoyancy;
            else
                return (1 + (-Water.GetDepth(Position) / 5000f)) / 20f * Water.buoyancy;
        }

        private static Vector3D GetUpDirection(Vector3D Position, long? ID = 0)
        {
            if (ID == null)
                return WaterMod.Static.GetClosestWater(Position)?.GetUpDirection(Position) ?? Vector3D.Up;
            else
                return WaterMod.Static.Waters[ID.Value].GetUpDirection(Position);
        }

        private static int SphereIntersects(BoundingSphereD Sphere, long? ID = 0)
        {
            if (ID == null)
                return WaterMod.Static.GetClosestWater(Sphere.Center)?.Intersects(ref Sphere) ?? 0;
            else
                return WaterMod.Static.Waters[ID.Value].Intersects(ref Sphere);
        }

        private static void SphereIntersects(ICollection<BoundingSphereD> Spheres, ICollection<int> Intersections, long? ID = 0)
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

        private static int LineIntersectsWater(LineD Line, long? ID = 0)
        {
            if (ID == null)
                return WaterMod.Static.GetClosestWater(Line.From)?.Intersects(ref Line) ?? 0;
            else
                return WaterMod.Static.Waters[ID.Value].Intersects(ref Line);
        }

        private static void LineIntersectsWaterList(List<LineD> Lines, ICollection<int> Intersections, long? ID)
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

        private static void RunCommand(string MessageText)
        {
            bool SendToOthers = false;
            WaterMod.Static.Utilities_MessageEntered(MessageText, ref SendToOthers);
        }

        public static void BeforeStart()
        {
            MyAPIGateway.Utilities.SendModMessage(ModHandlerID, ModAPIMethods);
        }

        #region Water
        private static void CreateBubble(Vector3D Position, float Radius)
        {
            WaterMod.Static.CreateBubble(Position, Radius);
        }

        private static void CreateSplash(Vector3D Position, float Radius, bool Audible)
        {
            WaterMod.Static.CreateSplash(Position, Radius, Audible);
        }

        private static float? GetDepth(Vector3D Position, long? ID = 0)
        {
            if (ID == null)
                return WaterMod.Static.GetClosestWater(Position)?.GetDepth(Position);
            else
                return WaterMod.Static.Waters[ID.Value].GetDepth(Position);
        }

        private static Vector3D GetClosestSurfacePoint(Vector3D Position, long? ID = 0)
        {
            if (ID == null)
                return WaterMod.Static.GetClosestWater(Position)?.GetClosestSurfacePoint(Position) ?? Position;
            else
                return WaterMod.Static.Waters[ID.Value].GetClosestSurfacePoint(Position);
        }

        private static void GetClosestSurfacePoint(List<Vector3D> Positions, ICollection<Vector3D> Points, long? ID = 0)
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

        private static void ForceSync()
        {
            if (MyAPIGateway.Session.IsServer)
                MyAPIGateway.Multiplayer.SendMessageToOthers(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(WaterMod.Static.Waters)));
            else
                MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(WaterMod.Static.Waters)));
        }

        private static bool VerifyVersion(int ModAPIVersion, string ModName)
        {
            if (ModAPIVersion < MinVersion)
            {
                WaterUtils.ShowMessage("The Mod '" + ModName + "' is using an oudated Water Mod API (" + ModAPIVersion + "), tell the author to update!");
                return false;
            }

            return true;

        }

        private static long? GetClosestWater(Vector3D Position)
        {
            return WaterMod.Static.GetClosestWater(Position)?.planetID;
        }

        public static bool IsUnderwater(Vector3D Position, long? ID = 0)
        {
            if (ID == null)
                return WaterMod.Static.GetClosestWater(Position).IsUnderwater(ref Position);
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