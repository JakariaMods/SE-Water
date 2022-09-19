using Draygo.Drag.API;
using Jakaria.API;
using Jakaria.Components;
using Jakaria.Utils;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRage.ModAPI;
using VRage.Serialization;
using VRageMath;

namespace Jakaria.SessionComponents
{
    /// <summary>
    /// Session Component that handles API invocations and drag API
    /// </summary>
    public class WaterAPIComponent : SessionComponentBase
    {
        public const int MIN_API_VERSION = 19;
        public const ushort MOD_SYNC_ID = 50271;

        public ConcurrentDictionary<IMyEntity, DragClientAPI.DragObject> DragObjects = new ConcurrentDictionary<IMyEntity, DragClientAPI.DragObject>();
        public DragClientAPI DragClientAPI = new DragClientAPI();
        public RemoteDragSettings RemoteDragAPI = new RemoteDragSettings();

        public override void LoadData()
        {
            DragClientAPI.Init();
            RemoteDragAPI.Register();
        }

        public override void UnloadData()
        {
            DragClientAPI.Close();
            RemoteDragAPI.UnRegister();
        }

        private static readonly Dictionary<string, Delegate> _modAPIMethods = new Dictionary<string, Delegate>()
        {
            ["VerifyVersion"] = new Func<int, string, bool>(VerifyVersion),
            ["IsUnderwater"] = new Func<Vector3D, MyPlanet, bool>(IsUnderwater),
            ["GetClosestWater"] = new Func<Vector3D, MyPlanet>(GetClosestWater),
            ["SphereIntersectsWater"] = new Func<BoundingSphereD, MyPlanet, int>(SphereIntersects),
            ["SphereIntersectsWaterList"] = new Action<List<BoundingSphereD>, ICollection<int>, MyPlanet>(SphereIntersects),
            ["GetClosestSurfacePoint"] = new Func<Vector3D, MyPlanet, Vector3D>(GetClosestSurfacePoint),
            ["GetClosestSurfacePointList"] = new Action<List<Vector3D>, ICollection<Vector3D>, MyPlanet>(GetClosestSurfacePoints),
            ["LineIntersectsWater"] = new Func<LineD, MyPlanet, int>(LineIntersectsWater),
            ["LineIntersectsWaterList"] = new Action<List<LineD>, ICollection<int>, MyPlanet>(LineIntersectsWaterList),
            ["GetDepth"] = new Func<Vector3D, MyPlanet, float?>(GetDepth),
            ["CreateSplash"] = new Action<Vector3D, float, bool>(CreateSplash),
            ["CreatePhysicsSplash"] = new Action<Vector3D, Vector3D, float, int>(CreatePhysicsSplash),
            ["CreateBubble"] = new Action<Vector3D, float>(CreateBubble),
            ["ForceSync"] = new Action(ForceSync),
            ["RunCommand"] = new Action<string>(RunCommand),
            ["GetUpDirection"] = new Func<Vector3D, MyPlanet, Vector3D>(GetUpDirection),
            ["HasWater"] = new Func<MyPlanet, bool>(HasWater),
            ["GetBuoyancyMultiplier"] = new Func<Vector3D, MyCubeSize, MyPlanet, float>(GetBuoyancyMultiplier),

            ["GetCrushDepth"] = new Func<MyPlanet, int>(GetCrushDepth),
            ["GetPhysicalData"] = new Func<MyPlanet, MyTuple<Vector3D, float, float, float>>(GetPhysicalData),
            ["GetWaveData"] = new Func<MyPlanet, MyTuple<float, float, float, int>>(GetWaveData),
            ["GetRenderData"] = new Func<MyPlanet, MyTuple<Vector3D, bool, bool>>(GetRenderData),
            ["GetPhysicsData"] = new Func<MyPlanet, MyTuple<float, float>>(GetPhysicsData),
            ["GetTideData"] = new Func<MyPlanet, MyTuple<float, float>>(GetTideData),
            ["GetTideDirection"] = new Func<MyPlanet, Vector3D>(GetTideDirection),
        };

        public static Vector3D GetTideDirection(MyPlanet planet)
        {
            WaterComponent water = planet.Components.Get<WaterComponent>();
            return water.TideDirection;
        }

        public static MyTuple<float, float> GetTideData(MyPlanet planet)
        {
            WaterComponent water = planet.Components.Get<WaterComponent>();
            return new MyTuple<float, float>(water.Settings.TideHeight, water.Settings.TideSpeed);
        }

        public static MyTuple<float, float> GetPhysicsData(MyPlanet planet)
        {
            WaterComponent water = planet.Components.Get<WaterComponent>();
            return new MyTuple<float, float>(water.Settings.Material.Density, water.Settings.Buoyancy);
        }

        public static MyTuple<Vector3D, bool, bool> GetRenderData(MyPlanet planet)
        {
            WaterComponent water = planet.Components.Get<WaterComponent>();
            return new MyTuple<Vector3D, bool, bool>(water.Settings.FogColor, water.Settings.Transparent, water.Settings.Lit);
        }

        public static MyTuple<float, float, float, int> GetWaveData(MyPlanet planet)
        {
            WaterComponent water = planet.Components.Get<WaterComponent>();
            return new MyTuple<float, float, float, int>(water.Settings.WaveHeight, water.Settings.WaveSpeed, water.Settings.WaveScale, 0);
        }

        public static MyTuple<Vector3D, float, float, float> GetPhysicalData(MyPlanet planet)
        {
            WaterComponent water = planet.Components.Get<WaterComponent>();
            return new MyTuple<Vector3D, float, float, float>(water.Entity.GetPosition(), water.Settings.Radius, water.Settings.Radius - water.Settings.WaveHeight - water.Settings.TideHeight, water.Settings.Radius + water.Settings.WaveHeight + water.Settings.TideHeight);
        }

        public static int GetCrushDepth(MyPlanet planet)
        {
            return 0;
        }

        public static float GetBuoyancyMultiplier(Vector3D Position, MyCubeSize GridSize, MyPlanet planet = null)
        {
            WaterComponent water = (planet == null) ? Session.Instance.Get<WaterModComponent>().GetClosestWater(Position) : planet.Components.Get<WaterComponent>();

            if (GridSize == MyCubeSize.Large)
                return (float)(1 + (-water.GetDepthGlobal(ref Position) / 5000f)) / 50f * water.Settings.Buoyancy;
            else
                return (float)(1 + (-water.GetDepthGlobal(ref Position) / 5000f)) / 20f * water.Settings.Buoyancy;
        }

        public static Vector3D GetUpDirection(Vector3D Position, MyPlanet planet = null)
        {
            if (planet == null)
                return Session.Instance.Get<WaterModComponent>().GetClosestWater(Position)?.GetUpDirectionGlobal(ref Position) ?? Vector3D.Up;
            else
                return planet.Components.Get<WaterComponent>().GetUpDirectionGlobal(ref Position);
        }

        public static int SphereIntersects(BoundingSphereD Sphere, MyPlanet planet = null)
        {
            if (planet == null)
                return Session.Instance.Get<WaterModComponent>().GetClosestWater(Sphere.Center)?.IntersectsGlobal(ref Sphere) ?? 0;
            else
                return planet.Components.Get<WaterComponent>().IntersectsGlobal(ref Sphere);
        }

        public static void SphereIntersects(ICollection<BoundingSphereD> Spheres, ICollection<int> Intersections, MyPlanet planet = null)
        {
            if (planet == null)
            {
                foreach (var Sphere in Spheres)
                {
                    BoundingSphereD sphere = Sphere;
                    Intersections.Add(Session.Instance.Get<WaterModComponent>().GetClosestWater(Sphere.Center)?.IntersectsGlobal(ref sphere) ?? 0);
                }
            }
            else
            {
                WaterComponent water = planet.Components.Get<WaterComponent>();
                foreach (var Sphere in Spheres)
                {
                    BoundingSphereD sphere = Sphere;
                    Intersections.Add(water.IntersectsGlobal(ref sphere));
                }
            }
        }

        public static int LineIntersectsWater(LineD Line, MyPlanet planet = null)
        {
            if (planet == null)
                return Session.Instance.Get<WaterModComponent>().GetClosestWater(Line.From)?.IntersectsGlobal(ref Line) ?? 0;
            else
                return planet.Components.Get<WaterComponent>().IntersectsGlobal(ref Line);
        }

        public static void LineIntersectsWaterList(List<LineD> Lines, ICollection<int> Intersections, MyPlanet planet = null)
        {
            if (planet == null)
            {
                foreach (var Line in Lines)
                {
                    LineD line = Line;
                    Intersections.Add(Session.Instance.Get<WaterModComponent>().GetClosestWater(Line.From)?.IntersectsGlobal(ref line) ?? 0);
                }
            }
            else
            {
                WaterComponent water = planet.Components.Get<WaterComponent>();
                foreach (var Line in Lines)
                {
                    LineD line = Line;
                    Intersections.Add(water?.IntersectsGlobal(ref line) ?? 0);
                }
            }
        }

        public static void RunCommand(string MessageText)
        {
            bool SendToOthers = false;
            Session.Instance.Get<WaterCommandComponent>().Utilities_MessageEntered(MessageText, ref SendToOthers);
        }

        public override void BeforeStart()
        {
            MyAPIGateway.Utilities.SendModMessage(MOD_SYNC_ID, _modAPIMethods);
        }

        #region Water
        public static void CreateBubble(Vector3D Position, float Radius)
        {
            Session.Instance.Get<WaterEffectsComponent>().CreateBubble(ref Position, Radius);
        }

        public static void CreateSplash(Vector3D Position, float Radius, bool Audible)
        {
            Session.Instance.Get<WaterEffectsComponent>().CreateSplash(Position, Radius, Audible);
        }

        public static void CreatePhysicsSplash(Vector3D Position, Vector3D Velocity, float Radius, int Count = 1)
        {
            Session.Instance.Get<WaterEffectsComponent>().CreatePhysicsSplash(Position, Velocity, Radius, Count);
        }

        public static float? GetDepth(Vector3D Position, MyPlanet planet = null)
        {
            if (planet == null)
                return (float?)Session.Instance.Get<WaterModComponent>().GetClosestWater(Position)?.GetDepthGlobal(ref Position);
            else
                return (float?)planet.Components.Get<WaterComponent>().GetDepthGlobal(ref Position);
        }

        public static Vector3D GetClosestSurfacePoint(Vector3D Position, MyPlanet planet = null)
        {
            if (planet == null)
                return Session.Instance.Get<WaterModComponent>().GetClosestWater(Position)?.GetClosestSurfacePointGlobal(Position) ?? Position;
            else
                return planet.Components.Get<WaterComponent>().GetClosestSurfacePointGlobal(Position);
        }

        public static void GetClosestSurfacePoints(List<Vector3D> Positions, ICollection<Vector3D> Points, MyPlanet planet = null)
        {
            if (planet == null)
            {
                foreach (var Position in Positions)
                {
                    Points.Add(Session.Instance.Get<WaterModComponent>().GetClosestWater(Position)?.GetClosestSurfacePointGlobal(Position) ?? Position);
                }
            }
            else
            {
                WaterComponent water = planet.Components.Get<WaterComponent>();

                foreach (var Position in Positions)
                {
                    Points.Add(water.GetClosestSurfacePointGlobal(Position));
                }
            }
        }

        public static void ForceSync()
        {
            if(MyAPIGateway.Session.IsServer)
                Session.Instance.Get<WaterSyncComponent>().SyncClients();
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

        public static MyPlanet GetClosestWater(Vector3D Position)
        {
            return Session.Instance.Get<WaterModComponent>().GetClosestWater(Position)?.Planet;
        }

        public static bool IsUnderwater(Vector3D Position, MyPlanet planet = null)
        {
            if (planet == null)
                return Session.Instance.Get<WaterModComponent>().GetClosestWater(Position)?.IsUnderwaterGlobal(ref Position) ?? false;
            else
                return planet.Components.Get<WaterComponent>()?.IsUnderwaterGlobal(ref Position) ?? false;
        }

        public static bool HasWater(MyPlanet planet)
        {
            return planet.Components.Has<WaterComponent>();
        }
        #endregion
    }
}
