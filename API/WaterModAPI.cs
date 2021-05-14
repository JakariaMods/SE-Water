using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;

namespace Jakaria.API
{
    //Only Include this file in your project

    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class WaterAPI : MySessionComponentBase
    {
        public static string ModName = MyAPIGateway.Utilities.GamePaths.ModScopeName.Split('_')[1];
        public const ushort ModHandlerID = 50271;
        public const int ModAPIVersion = 12;
        public bool Registered { get; private set; } = false;

        private static Dictionary<string, Delegate> ModAPIMethods;

        private static Func<int, string, bool> _VerifyVersion;

        private static Func<Vector3D, long?, bool> _IsUnderwater;
        private static Func<LineD, long?, int> _LineIntersectsWater;
        private static Func<List<LineD>, long?, List<int>> _LineIntersectsWaterList;
        private static Func<Vector3D, long?> _GetClosestWater;
        private static Func<BoundingSphereD, long?, int> _SphereIntersectsWater;
        private static Func<List<BoundingSphereD>, long?, List<int>> _SphereIntersectsWaterList;
        private static Func<Vector3D, long?, Vector3D> _GetClosestSurfacePoint;
        private static Func<List<Vector3D>, long?, List<Vector3D>> _GetClosestSurfacePointList;
        private static Func<Vector3D, long?, float?> _GetDepth;
        private static Action _ForceSync;
        private static Action<string> _RunCommand;
        private static Func<Vector3D, long?, Vector3D> _GetUpDirection;
        private static Func<long, bool> _HasWater;
        private static Func<Vector3D, MyCubeSize, long?, float> _GetBuoyancyMultiplier;
        private static Func<long, int> _GetCrushDepth;

        private static Action<Vector3D, float, bool> _CreateSplash;
        private static Action<Vector3D, float> _CreateBubble;

        /// <summary>
        /// Returns true if the version is compatibile with the API Backend, this is automatically called
        /// </summary>
        public static bool VerifyVersion(int Version, string ModName) => _VerifyVersion?.Invoke(Version, ModName) ?? false;

        public static bool HasWater(long ID) => _HasWater?.Invoke(ID) ?? false;

        /// <summary>
        /// Returns true if the position is underwater
        /// </summary>
        public static bool IsUnderwater(Vector3D Position, long? ID = null) => _IsUnderwater?.Invoke(Position, ID) ?? false;

        /// <summary>
        /// Overwater = 0, ExitsWater = 1, EntersWater = 2, Underwater = 3
        /// </summary>
        public static int LineIntersectsWater(LineD Line, long? ID = null) => _LineIntersectsWater?.Invoke(Line, ID) ?? 0;

        /// <summary>
        /// Overwater = 0, ExitsWater = 1, EntersWater = 2, Underwater = 3
        /// </summary>
        public static List<int> LineIntersectsWater(List<LineD> Lines, long? ID = null) => _LineIntersectsWaterList?.Invoke(Lines, ID) ?? null;

        /// <summary>
        /// Gets the closest water to the provided water
        /// </summary>
        public static long? GetClosestWater(Vector3D Position) => _GetClosestWater?.Invoke(Position) ?? null;

        /// <summary>
        /// Overwater = 0, ExitsWater = 1, EntersWater = 2, Underwater = 3
        /// </summary>
        public static int SphereIntersectsWater(BoundingSphereD Sphere, long? ID = null) => _SphereIntersectsWater?.Invoke(Sphere, ID) ?? 0;

        /// <summary>
        /// Overwater = 0, ExitsWater = 1, EntersWater = 2, Underwater = 3
        /// </summary>
        public static List<int> SphereIntersectsWater(List<BoundingSphereD> Spheres, long? ID = null) => _SphereIntersectsWaterList?.Invoke(Spheres, ID) ?? null;


        /// <summary>
        /// Returns the closest position on the water surface
        /// </summary>
        public static Vector3D GetClosestSurfacePoint(Vector3D Position, long? ID = null) => _GetClosestSurfacePoint?.Invoke(Position, ID) ?? Position;

        /// <summary>
        /// Returns the closest position on the water surface
        /// </summary>
        public static List<Vector3D> GetClosestSurfacePoint(List<Vector3D> Positions, long? ID = null) => _GetClosestSurfacePointList?.Invoke(Positions, ID) ?? Positions;


        /// <summary>
        /// Returns the depth the position is underwater
        /// </summary>
        public static float? GetDepth(Vector3D Position, long? ID = null) => _GetDepth?.Invoke(Position, ID) ?? null;

        /// <summary>
        /// Creates a splash at the provided position
        /// </summary>
        public static void CreateSplash(Vector3D Position, float Radius, bool Audible) => _CreateSplash?.Invoke(Position, Radius, Audible);

        /// <summary>
        /// Creates a bubble at the provided position
        /// </summary>
        public static void CreateBubble(Vector3D Position, float Radius) => _CreateBubble?.Invoke(Position, Radius);

        /// <summary>
        /// Forces the server to sync with the client
        /// </summary>
        public static void ForceSync() => _ForceSync?.Invoke();

        /// <summary>
        /// Simulates a command being run by the client, EX: /wcreate, client must have permissions to run the command
        /// </summary>
        public static void RunCommand(string MessageText) => _RunCommand?.Invoke(MessageText);

        /// <summary>
        /// Gets the up direction at the position
        /// </summary>
        public static Vector3D GetUpDirection(Vector3D Position, long? ID = null) => _GetUpDirection?.Invoke(Position, ID) ?? Vector3D.Up;

        /// <summary>
        /// Gets the buoyancy multiplier to help calculate buoyancy of a grid, used in the final calculation of grid buoyancy.
        /// </summary>
        public static float GetBuoyancyMultiplier(Vector3D Position, MyCubeSize GridSize, long? ID = null) => _GetBuoyancyMultiplier?.Invoke(Position, GridSize, ID) ?? 0;

        /// <summary>
        /// Gets crush depth
        /// </summary>
        public static int GetCrushDepth(long ID) => _GetCrushDepth?.Invoke(ID) ?? 500;
        public override void LoadData()
        {
            MyAPIGateway.Utilities.RegisterMessageHandler(ModHandlerID, ModHandler);
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(ModHandlerID, ModHandler);
        }

        private void ModHandler(object obj)
        {
            if (obj == null)
            {
                return;
            }

            if (obj is Dictionary<string, Delegate>)
            {
                ModAPIMethods = (Dictionary<string, Delegate>)obj;
                _VerifyVersion = (Func<int, string, bool>)ModAPIMethods["VerifyVersion"];
            }

            Registered = VerifyVersion(ModAPIVersion, ModName);

            if (Registered)
            {
                _IsUnderwater = (Func<Vector3D, long?, bool>)ModAPIMethods["IsUnderwater"];
                _GetClosestWater = (Func<Vector3D, long?>)ModAPIMethods["GetClosestWater"];
                _SphereIntersectsWater = (Func<BoundingSphereD, long?, int>)ModAPIMethods["SphereIntersectsWater"];
                _SphereIntersectsWaterList = (Func<List<BoundingSphereD>, long?, List<int>>)ModAPIMethods["SphereIntersectsWaterList"];
                _GetClosestSurfacePoint = (Func<Vector3D, long?, Vector3D>)ModAPIMethods["GetClosestSurfacePoint"];
                _GetClosestSurfacePointList = (Func<List<Vector3D>, long?, List<Vector3D>>)ModAPIMethods["GetClosestSurfacePointList"];
                _LineIntersectsWater = (Func<LineD, long?, int>)ModAPIMethods["LineIntersectsWater"];
                _LineIntersectsWaterList = (Func<List<LineD>, long?, List<int>>)ModAPIMethods["LineIntersectsWaterList"];
                _GetDepth = (Func<Vector3D, long?, float?>)ModAPIMethods["GetDepth"];
                _CreateSplash = (Action<Vector3D, float, bool>)ModAPIMethods["CreateSplash"];
                _CreateBubble = (Action<Vector3D, float>)ModAPIMethods["CreateBubble"];
                _ForceSync = (Action)ModAPIMethods["ForceSync"];
                _RunCommand = (Action<string>)ModAPIMethods["RunCommand"];
                _GetUpDirection = (Func<Vector3D, long?, Vector3D>)ModAPIMethods["GetUpDirection"];
                _HasWater = (Func<long, bool>)ModAPIMethods["HasWater"];
                _GetBuoyancyMultiplier = (Func<Vector3D, MyCubeSize, long?, float>)ModAPIMethods["GetBuoyancyMultiplier"];
                _GetCrushDepth = (Func<long, int>)ModAPIMethods["GetCrushDepth"];
            }
        }
    }
}