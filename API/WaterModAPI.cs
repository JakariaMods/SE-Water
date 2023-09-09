using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Utils;
using VRageMath;

namespace Jakaria.API
{
    //See the steam guide for how to use this
    //https://steamcommunity.com/sharedfiles/filedetails/?id=2639207010
    /// <summary>
    /// https://github.com/jakarianstudios/SE-Water/blob/master/API/WaterModAPI.cs
    /// </summary>

    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class WaterModAPI : MySessionComponentBase
    {
        public static string ModName = "";
        public const ushort ModHandlerID = 50271;
        public const int ModAPIVersion = 21;
        public static bool Registered { get; private set; } = false;

        private static Dictionary<string, Delegate> ModAPIMethods;

        private static Func<int, string, bool> _VerifyVersion;

        private static Func<Vector3D, MyPlanet, bool> _IsUnderwater;
        private static Func<LineD, MyPlanet, int> _LineIntersectsWater;
        private static Action<List<LineD>, ICollection<int>, MyPlanet> _LineIntersectsWaterList;
        private static Func<Vector3D, MyPlanet> _GetClosestWater;
        private static Func<BoundingSphereD, MyPlanet, int> _SphereIntersectsWater;
        private static Action<List<BoundingSphereD>, ICollection<int>, MyPlanet> _SphereIntersectsWaterList;
        private static Func<Vector3D, MyPlanet, Vector3D> _GetClosestSurfacePoint;
        private static Action<List<Vector3D>, ICollection<Vector3D>, MyPlanet> _GetClosestSurfacePointList;
        private static Func<Vector3D, MyPlanet, float?> _GetDepth;
        private static Action _ForceSync;
        private static Action<string> _RunCommand;
        private static Func<Vector3D, MyPlanet, Vector3D> _GetUpDirection;
        private static Func<MyPlanet, bool> _HasWater;
        private static Func<Vector3D, MyCubeSize, MyPlanet, float> _GetBuoyancyMultiplier;
        private static Func<MyPlanet, int> _GetCrushDepth;

        private static Func<MyPlanet, MyTuple<Vector3D, float, float, float>> _GetPhysicalData;
        private static Func<MyPlanet, MyTuple<float, float, float, int>> _GetWaveData;
        private static Func<MyPlanet, MyTuple<Vector3D, bool, bool>> _GetRenderData;
        private static Func<MyPlanet, MyTuple<float, float>> _GetPhysicsData;
        private static Func<MyPlanet, MyTuple<float, float>> _GetTideData;
        private static Func<MyPlanet, Vector3D> _GetTideDirection;

        private static Action<Vector3D, float, bool> _CreateSplash;
        private static Action<Vector3D, float> _CreateBubble;
        private static Action<Vector3D, Vector3D, float, int> _CreatePhysicsSplash;

        private static Func<MyEntity, float> _Entity_FluidPressure;
        private static Func<MyEntity, double> _Entity_FluidDepth;
        private static Func<MyEntity, Vector3> _Entity_FluidVelocity;
        private static Func<MyEntity, Vector3> _Entity_BuoyancyForce;
        private static Func<MyEntity, Vector3D> _Entity_CenterOfBuoyancy;
        private static Func<MyEntity, Vector3D> _Entity_DragForce;
        private static Func<MyEntity, float> _Entity_PercentUnderwater;

        /// <summary>
        /// Returns true if the version is compatibile with the API Backend, this is automatically called
        /// </summary>
        public static bool VerifyVersion(int Version, string ModName) => _VerifyVersion?.Invoke(Version, ModName) ?? false;

        /// <summary>
        /// Returns true if the provided planet entity ID has water
        /// </summary>
        public static bool HasWater(MyPlanet planet) => _HasWater?.Invoke(planet) ?? false;

        /// <summary>
        /// Returns true if the position is underwater
        /// </summary>
        public static bool IsUnderwater(Vector3D Position, MyPlanet ID = null) => _IsUnderwater?.Invoke(Position, ID) ?? false;

        /// <summary>
        /// Overwater = 0, ExitsWater = 1, EntersWater = 2, Underwater = 3
        /// </summary>
        public static int LineIntersectsWater(LineD Line, MyPlanet ID = null) => _LineIntersectsWater?.Invoke(Line, ID) ?? 0;

        /// <summary>
        /// Overwater = 0, ExitsWater = 1, EntersWater = 2, Underwater = 3
        /// </summary>
        public static void LineIntersectsWater(List<LineD> Lines, ICollection<int> Intersections, MyPlanet ID = null) => _LineIntersectsWaterList?.Invoke(Lines, Intersections, ID);

        /// <summary>
        /// Gets the closest water to the provided water
        /// </summary>
        public static MyPlanet GetClosestWater(Vector3D Position) => _GetClosestWater?.Invoke(Position) ?? null;

        /// <summary>
        /// Overwater = 0, ExitsWater = 1, EntersWater = 2, Underwater = 3
        /// </summary>
        public static int SphereIntersectsWater(BoundingSphereD Sphere, MyPlanet ID = null) => _SphereIntersectsWater?.Invoke(Sphere, ID) ?? 0;

        /// <summary>
        /// Overwater = 0, ExitsWater = 1, EntersWater = 2, Underwater = 3
        /// </summary>
        public static void SphereIntersectsWater(List<BoundingSphereD> Spheres, ICollection<int> Intersections, MyPlanet ID = null) => _SphereIntersectsWaterList?.Invoke(Spheres, Intersections, ID);


        /// <summary>
        /// Returns the closest position on the water surface
        /// </summary>
        public static Vector3D GetClosestSurfacePoint(Vector3D Position, MyPlanet ID = null) => _GetClosestSurfacePoint?.Invoke(Position, ID) ?? Position;

        /// <summary>
        /// Returns the closest position on the water surface
        /// </summary>
        public static void GetClosestSurfacePoint(List<Vector3D> Positions, ICollection<Vector3D> Points, MyPlanet ID = null) => _GetClosestSurfacePointList?.Invoke(Positions, Points, ID);


        /// <summary>
        /// Returns the depth the position is underwater
        /// </summary>
        public static float? GetDepth(Vector3D Position, MyPlanet ID = null) => _GetDepth?.Invoke(Position, ID) ?? null;

        /// <summary>
        /// Creates a splash at the provided position
        /// </summary>
        public static void CreateSplash(Vector3D Position, float Radius, bool Audible) => _CreateSplash?.Invoke(Position, Radius, Audible);

        /// <summary>
        /// Creates a physical splash at the provided position (Particles outside of the water)
        /// </summary>
        public static void CreatePhysicsSplash(Vector3D Position, Vector3D Velocity, float Radius, int Count = 1) => _CreatePhysicsSplash?.Invoke(Position, Velocity, Radius, Count);

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
        public static Vector3D GetUpDirection(Vector3D Position, MyPlanet ID = null) => _GetUpDirection?.Invoke(Position, ID) ?? Vector3D.Up;

        /// <summary>
        /// Gets the buoyancy multiplier to help calculate buoyancy of a grid, used in the final calculation of grid buoyancy.
        /// </summary>
        public static float GetBuoyancyMultiplier(Vector3D Position, MyCubeSize GridSize, MyPlanet ID = null) => _GetBuoyancyMultiplier?.Invoke(Position, GridSize, ID) ?? 0;

        /// <summary>
        /// Gets crush damage
        /// </summary>
        [Obsolete]
        public static float GetCrushDepth(MyPlanet planet) => _GetCrushDepth?.Invoke(planet) ?? 500;

        /// <summary>
        /// Gets position, radius, minimum radius, and maximum radius- in that order.
        /// </summary>
        public static MyTuple<Vector3D, float, float, float> GetPhysical(MyPlanet planet) => (MyTuple<Vector3D, float, float, float>)(_GetPhysicalData?.Invoke(planet) ?? null);

        /// <summary>
        /// Gets wave height, wave speed, wave scale, and seed- in that order.
        /// </summary>
        public static MyTuple<float, float, float, int> GetWaveData(MyPlanet planet) => (MyTuple<float, float, float, int>)(_GetWaveData?.Invoke(planet) ?? null);

        /// <summary>
        /// Gets fog color, transparency toggle, and lighting toggle- in that order.
        /// </summary>
        public static MyTuple<Vector3D, bool, bool> GetRenderData(MyPlanet planet) => (MyTuple<Vector3D, bool, bool>)(_GetRenderData?.Invoke(planet) ?? null);

        /// <summary>
        /// Gets tide height and tide speed- in that order.
        /// </summary>
        public static MyTuple<float, float> GetTideData(MyPlanet planet) => (MyTuple<float, float>)(_GetTideData?.Invoke(planet) ?? null);

        /// <summary>
        /// Gets density and buoyancy multiplier- in that order.
        /// </summary>
        public static MyTuple<float, float> GetPhysicsData(MyPlanet planet) => (MyTuple<float, float>)(_GetPhysicsData?.Invoke(planet) ?? null);

        /// <summary>
        /// Gets the direction of high tide, from center of the water to the surface
        /// </summary>
        public static Vector3D GetTideDirection(MyPlanet planet) => (Vector3D)(_GetTideDirection?.Invoke(planet) ?? null);

        /// <summary>
        /// Gets the hydrostatcic Pressure of the fluid the entity is at. The Unit is kPa (KiloPascals)
        /// </summary>
        public static float Entity_FluidPressure(MyEntity entity) => _Entity_FluidPressure?.Invoke(entity) ?? 0;

        /// <summary>
        /// Depth of the entity in the fluid. Unit is m (Meters) Positive is above water, negative is below water. Returns NaN when no water is present
        /// </summary>
        public static double Entity_FluidDepth(MyEntity entity) => _Entity_FluidDepth?.Invoke(entity) ?? double.PositiveInfinity;

        /// <summary>
        /// Velocity of the fluid around the entity (Currents, Wave Oscillation)
        /// </summary>
        public static Vector3 Entity_FluidVelocity(MyEntity entity) => _Entity_FluidVelocity?.Invoke(entity) ?? Vector3.Zero;

        /// <summary>
        /// Force vector for buoyancy. Unit is N (Newtons)
        /// </summary>
        public static Vector3 Entity_BuoyancyForce(MyEntity entity) => _Entity_BuoyancyForce?.Invoke(entity) ?? Vector3.Zero;

        /// <summary>
        /// Center of buoyancy position of the entity in World Space
        /// </summary>
        public static Vector3D Entity_CenterOfBuoyancy(MyEntity entity) => _Entity_CenterOfBuoyancy?.Invoke(entity) ?? Vector3D.Zero;

        /// <summary>
        /// Force vector for drag. Unit is N (Newtons)
        /// </summary>
        public static Vector3D Entity_DragForce(MyEntity entity) => _Entity_DragForce?.Invoke(entity) ?? Vector3D.Zero;

        /// <summary>
        /// Percentage of the entity being underwater where 1 is 100%
        /// </summary>
        public static float Entity_PercentUnderwater(MyEntity entity) => _Entity_PercentUnderwater?.Invoke(entity) ?? 0;

        /// <summary>
        /// Do not use. This is for the session component to register automatically
        /// </summary>
        public override void LoadData()
        {
            Register();
        }

        /// <summary>
        /// Do not use. This is for the session component to unregister automatically
        /// </summary>
        protected override void UnloadData()
        {
            Unregister();
        }

        /// <summary>
        /// Registers the mod and sets the mod name if it is not already set
        /// </summary>
        public void Register()
        {
            MyAPIGateway.Utilities.RegisterMessageHandler(ModHandlerID, ModHandler);

            if (ModName == "")
            {
                if (MyAPIGateway.Utilities.GamePaths.ModScopeName.Contains("_"))
                    ModName = MyAPIGateway.Utilities.GamePaths.ModScopeName.Split('_')[1];
                else
                    ModName = MyAPIGateway.Utilities.GamePaths.ModScopeName;
            }
        }

        /// <summary>
        /// Unregisters the mod
        /// </summary>
        public void Unregister()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(ModHandlerID, ModHandler);
            Registered = false;
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
                _VerifyVersion = TryGetMethod<Func<int, string, bool>>(ModAPIMethods, "VerifyVersion");

                Registered = VerifyVersion(ModAPIVersion, ModName);

                MyLog.Default.WriteLine("Registering WaterAPI for Mod '" + ModName + "'");

                if (Registered)
                {
                    try
                    {
                        _IsUnderwater = TryGetMethod<Func<Vector3D, MyPlanet, bool>>(ModAPIMethods, "IsUnderwater");
                        _GetClosestWater = TryGetMethod<Func<Vector3D, MyPlanet>>(ModAPIMethods, "GetClosestWater");
                        _SphereIntersectsWater = TryGetMethod<Func<BoundingSphereD, MyPlanet, int>>(ModAPIMethods, "SphereIntersectsWater");
                        _SphereIntersectsWaterList = TryGetMethod<Action<List<BoundingSphereD>, ICollection<int>, MyPlanet>>(ModAPIMethods, "SphereIntersectsWaterList");
                        _GetClosestSurfacePoint = TryGetMethod<Func<Vector3D, MyPlanet, Vector3D>>(ModAPIMethods, "GetClosestSurfacePoint");
                        _GetClosestSurfacePointList = TryGetMethod<Action<List<Vector3D>, ICollection<Vector3D>, MyPlanet>>(ModAPIMethods, "GetClosestSurfacePointList");
                        _LineIntersectsWater = TryGetMethod<Func<LineD, MyPlanet, int>>(ModAPIMethods, "LineIntersectsWater");
                        _LineIntersectsWaterList = TryGetMethod<Action<List<LineD>, ICollection<int>, MyPlanet>>(ModAPIMethods, "LineIntersectsWaterList");
                        _GetDepth = TryGetMethod<Func<Vector3D, MyPlanet, float?>>(ModAPIMethods, "GetDepth");
                        _CreateSplash = TryGetMethod<Action<Vector3D, float, bool>>(ModAPIMethods, "CreateSplash");
                        _CreatePhysicsSplash = TryGetMethod<Action<Vector3D, Vector3D, float, int>>(ModAPIMethods, "CreatePhysicsSplash");
                        _CreateBubble = TryGetMethod<Action<Vector3D, float>>(ModAPIMethods, "CreateBubble");
                        _ForceSync = TryGetMethod<Action>(ModAPIMethods, "ForceSync");
                        _RunCommand = TryGetMethod<Action<string>>(ModAPIMethods, "RunCommand");
                        _GetUpDirection = TryGetMethod<Func<Vector3D, MyPlanet, Vector3D>>(ModAPIMethods, "GetUpDirection");
                        _HasWater = TryGetMethod<Func<MyPlanet, bool>>(ModAPIMethods, "HasWater");
                        _GetBuoyancyMultiplier = TryGetMethod<Func<Vector3D, MyCubeSize, MyPlanet, float>>(ModAPIMethods, "GetBuoyancyMultiplier");
                        _GetCrushDepth = TryGetMethod<Func<MyPlanet, int>>(ModAPIMethods, "GetCrushDepth");
                        _GetPhysicalData = TryGetMethod<Func<MyPlanet, MyTuple<Vector3D, float, float, float>>>(ModAPIMethods, "GetPhysicalData");
                        _GetWaveData = TryGetMethod<Func<MyPlanet, MyTuple<float, float, float, int>>>(ModAPIMethods, "GetWaveData");
                        _GetRenderData = TryGetMethod<Func<MyPlanet, MyTuple<Vector3D, bool, bool>>>(ModAPIMethods, "GetRenderData");
                        _GetPhysicsData = TryGetMethod<Func<MyPlanet, MyTuple<float, float>>>(ModAPIMethods, "GetPhysicsData");
                        _GetTideData = TryGetMethod<Func<MyPlanet, MyTuple<float, float>>>(ModAPIMethods, "GetTideData");
                        _GetTideDirection = TryGetMethod<Func<MyPlanet, Vector3D>>(ModAPIMethods, "GetTideDirection");

                        _Entity_FluidPressure = TryGetMethod<Func<MyEntity, float>>(ModAPIMethods, "EntityGetFluidPressure");
                        _Entity_FluidDepth = TryGetMethod<Func<MyEntity, double>>(ModAPIMethods, "EntityGetFluidDepth");
                        _Entity_FluidVelocity = TryGetMethod<Func<MyEntity, Vector3>>(ModAPIMethods, "EntityGetFluidVelocity");
                        _Entity_BuoyancyForce = TryGetMethod<Func<MyEntity, Vector3>>(ModAPIMethods, "EntityGetBuoyancyForce");
                        _Entity_CenterOfBuoyancy = TryGetMethod<Func<MyEntity, Vector3D>>(ModAPIMethods, "EntityGetCenterOfBuoyancy");
                        _Entity_DragForce = TryGetMethod<Func<MyEntity, Vector3D>>(ModAPIMethods, "EntityGetDragForce");
                        _Entity_PercentUnderwater = TryGetMethod<Func<MyEntity, float>>(ModAPIMethods, "EntityGetPercentUnderwater");
    }
                    catch (Exception e)
                    {
                        MyAPIGateway.Utilities.ShowMessage("WaterMod", "Mod '" + ModName + "' encountered an error when registering the Water Mod API, see log for more info.");
                        MyLog.Default.WriteLine("WaterMod: " + e);

                        Registered = false;
                    }
                }
            }
            else
            {
                MyAPIGateway.Utilities.ShowMessage("WaterMod", $"Received a message with completely unrelated content? {obj.GetType()}");
                MyLog.Default.WriteLine("WaterMod: " + $"Received a message with completely unrelated content? {obj.GetType()}");
            }
        }

        private T TryGetMethod<T>(Dictionary<string, Delegate> modContent, string methodName) where T : class
        {
            Delegate method;
            if(modContent.TryGetValue(methodName, out method))
            {
                return method as T;
            }

            MyLog.Default.WriteLine($"WatermMod: API method '{methodName}' not found");

            return null;
        }
    }
}