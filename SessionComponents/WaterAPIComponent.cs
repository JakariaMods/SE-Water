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
using VRage.Game.Entity;
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

        private WaterModComponent _modComponent;
        private WaterCommandComponent _commandComponent;
        private WaterEffectsComponent _effectsComponent;
        private WaterSyncComponent _syncComponent;

        private Dictionary<string, Delegate> _modAPIMethods;

        public override void LoadData()
        {
            _modComponent = Session.Instance.Get<WaterModComponent>();
            _commandComponent = Session.Instance.TryGet<WaterCommandComponent>();
            _effectsComponent = Session.Instance.TryGet<WaterEffectsComponent>();
            _syncComponent = Session.Instance.TryGet<WaterSyncComponent>();

            _modAPIMethods = new Dictionary<string, Delegate>()
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

                ["EntityGetFluidPressure"] = new Func<MyEntity, float>(Entity_FluidPressure),
                ["EntityGetFluidDepth"] = new Func<MyEntity, double>(Entity_FluidDepth),
                ["EntityGetFluidVelocity"] = new Func<MyEntity, Vector3>(Entity_FluidVelocity),
                ["EntityGetBuoyancyForce"] = new Func<MyEntity, Vector3>(Entity_BuoyancyForce),
                ["EntityGetCenterOfBuoyancy"] = new Func<MyEntity, Vector3D>(Entity_CenterOfBuoyancy),
                ["EntityGetDragForce"] = new Func<MyEntity, Vector3D>(Entity_DragForce),
                ["EntityGetPercentUnderwater"] = new Func<MyEntity, float>(Entity_PercentUnderwater),
            };

            DragClientAPI.Init();
            RemoteDragAPI.Register();
        }

        private float Entity_PercentUnderwater(MyEntity entity)
        {
            return GetPhysicsComponent(entity)?.PercentUnderwater ?? 0;
        }

        private Vector3D Entity_DragForce(MyEntity entity)
        {
            return GetPhysicsComponent(entity)?.DragForce ?? Vector3D.Zero;
        }

        private Vector3D Entity_CenterOfBuoyancy(MyEntity entity)
        {
            return GetPhysicsComponent(entity)?.CenterOfBuoyancy ?? Vector3D.Zero;
        }

        private Vector3 Entity_BuoyancyForce(MyEntity entity)
        {
            return GetPhysicsComponent(entity)?.BuoyancyForce ?? Vector3.Zero;
        }

        private Vector3 Entity_FluidVelocity(MyEntity entity)
        {
            return GetPhysicsComponent(entity)?.FluidVelocity ?? Vector3.Zero;
        }

        private double Entity_FluidDepth(MyEntity entity)
        {
            return GetPhysicsComponent(entity)?.FluidDepth ?? double.PositiveInfinity;
        }

        private float Entity_FluidPressure(MyEntity entity)
        {
            return GetPhysicsComponent(entity)?.FluidPressure ?? 0;
        }

        private WaterPhysicsComponentBase GetPhysicsComponent(MyEntity entity)
        {
            foreach (var component in entity.Components)
            {
                if (component is WaterPhysicsComponentBase)
                    return (WaterPhysicsComponentBase)component;
            }

            return null;
        }

        public override void UnloadData()
        {
            DragClientAPI.Close();
            RemoteDragAPI.UnRegister();
        }

        public Vector3D GetTideDirection(MyPlanet planet)
        {
            WaterComponent water = planet.Components.Get<WaterComponent>();
            return water.TideDirection;
        }

        public MyTuple<float, float> GetTideData(MyPlanet planet)
        {
            WaterComponent water = planet.Components.Get<WaterComponent>();
            return new MyTuple<float, float>(water.Settings.TideHeight, water.Settings.TideSpeed);
        }

        public MyTuple<float, float> GetPhysicsData(MyPlanet planet)
        {
            WaterComponent water = planet.Components.Get<WaterComponent>();
            return new MyTuple<float, float>(water.Settings.Material.Density, water.Settings.Buoyancy);
        }

        public MyTuple<Vector3D, bool, bool> GetRenderData(MyPlanet planet)
        {
            WaterComponent water = planet.Components.Get<WaterComponent>();
            return new MyTuple<Vector3D, bool, bool>(water.Settings.FogColor, water.Settings.Transparent, water.Settings.Lit);
        }

        public MyTuple<float, float, float, int> GetWaveData(MyPlanet planet)
        {
            WaterComponent water = planet.Components.Get<WaterComponent>();
            return new MyTuple<float, float, float, int>(water.Settings.WaveHeight, water.Settings.WaveSpeed, water.Settings.WaveScale, 0);
        }

        public MyTuple<Vector3D, float, float, float> GetPhysicalData(MyPlanet planet)
        {
            WaterComponent water = planet.Components.Get<WaterComponent>();
            return new MyTuple<Vector3D, float, float, float>(water.Entity.GetPosition(), (float)water.Radius, (float)water.Radius - water.Settings.WaveHeight - water.Settings.TideHeight, (float)water.Radius + water.Settings.WaveHeight + water.Settings.TideHeight);
        }

        public int GetCrushDepth(MyPlanet planet)
        {
            return 0;
        }

        public float GetBuoyancyMultiplier(Vector3D position, MyCubeSize gridSize, MyPlanet planet = null)
        {
            WaterComponent water = (planet == null) ? _modComponent.GetClosestWater(position) : planet.Components.Get<WaterComponent>();

            if (gridSize == MyCubeSize.Large)
                return (float)(1 + (-water.GetDepthGlobal(ref position) / 5000f)) / 50f * water.Settings.Buoyancy;
            else
                return (float)(1 + (-water.GetDepthGlobal(ref position) / 5000f)) / 20f * water.Settings.Buoyancy;
        }

        public Vector3D GetUpDirection(Vector3D position, MyPlanet planet = null)
        {
            if (planet == null)
                return _modComponent.GetClosestWater(position)?.GetUpDirectionGlobal(ref position) ?? Vector3D.Up;
            else
                return planet.Components.Get<WaterComponent>().GetUpDirectionGlobal(ref position);
        }

        public int SphereIntersects(BoundingSphereD sphere, MyPlanet planet = null)
        {
            if (planet == null)
                return _modComponent.GetClosestWater(sphere.Center)?.IntersectsGlobal(ref sphere) ?? 0;
            else
                return planet.Components.Get<WaterComponent>().IntersectsGlobal(ref sphere);
        }

        public void SphereIntersects(ICollection<BoundingSphereD> spheres, ICollection<int> intersections, MyPlanet planet = null)
        {
            if (planet == null)
            {
                foreach (var sphere in spheres)
                {
                    BoundingSphereD sphereRef = sphere;
                    intersections.Add(_modComponent.GetClosestWater(sphere.Center)?.IntersectsGlobal(ref sphereRef) ?? 0);
                }
            }
            else
            {
                WaterComponent water = planet.Components.Get<WaterComponent>();
                foreach (var sphere in spheres)
                {
                    BoundingSphereD sphereRef = sphere;
                    intersections.Add(water.IntersectsGlobal(ref sphereRef));
                }
            }
        }

        public int LineIntersectsWater(LineD line, MyPlanet planet = null)
        {
            if (planet == null)
                return _modComponent.GetClosestWater(line.From)?.IntersectsGlobal(ref line) ?? 0;
            else
                return planet.Components.Get<WaterComponent>().IntersectsGlobal(ref line);
        }

        public void LineIntersectsWaterList(List<LineD> lines, ICollection<int> intersections, MyPlanet planet = null)
        {
            if (planet == null)
            {
                foreach (var line in lines)
                {
                    LineD lineRef = line;
                    intersections.Add(_modComponent.GetClosestWater(line.From)?.IntersectsGlobal(ref lineRef) ?? 0);
                }
            }
            else
            {
                WaterComponent water = planet.Components.Get<WaterComponent>();
                foreach (var line in lines)
                {
                    LineD lineRef = line;
                    intersections.Add(water?.IntersectsGlobal(ref lineRef) ?? 0);
                }
            }
        }

        public void RunCommand(string messageText)
        {
            if (_commandComponent != null)
            {
                _commandComponent.SendCommand(messageText);
            }
        }

        public override void BeforeStart()
        {
            MyAPIGateway.Utilities.SendModMessage(MOD_SYNC_ID, _modAPIMethods);
        }

        #region Water
        public void CreateBubble(Vector3D position, float radius)
        {
            if (_effectsComponent != null)
            {
                _effectsComponent.CreateBubble(ref position, radius);
            }
        }

        public void CreateSplash(Vector3D position, float radius, bool audible)
        {
            if (_effectsComponent != null)
            {
                _effectsComponent.CreateSplash(position, radius, audible);
            }
        }

        public void CreatePhysicsSplash(Vector3D position, Vector3D velocity, float radius, int count = 1)
        {
            if (_effectsComponent != null)
            {
                _effectsComponent.CreatePhysicsSplash(position, velocity, radius, count);
            }
        }

        public float? GetDepth(Vector3D position, MyPlanet planet = null)
        {
            if (planet == null)
                return (float?)_modComponent.GetClosestWater(position)?.GetDepthGlobal(ref position);
            else
                return (float?)planet.Components.Get<WaterComponent>().GetDepthGlobal(ref position);
        }

        public Vector3D GetClosestSurfacePoint(Vector3D position, MyPlanet planet = null)
        {
            if (planet == null)
                return _modComponent.GetClosestWater(position)?.GetClosestSurfacePointGlobal(position) ?? position;
            else
                return planet.Components.Get<WaterComponent>().GetClosestSurfacePointGlobal(position);
        }

        public void GetClosestSurfacePoints(List<Vector3D> positions, ICollection<Vector3D> points, MyPlanet planet = null)
        {
            if (planet == null)
            {
                foreach (var position in positions)
                {
                    points.Add(_modComponent.GetClosestWater(position)?.GetClosestSurfacePointGlobal(position) ?? position);
                }
            }
            else
            {
                WaterComponent water = planet.Components.Get<WaterComponent>();

                foreach (var position in positions)
                {
                    points.Add(water.GetClosestSurfacePointGlobal(position));
                }
            }
        }

        public void ForceSync()
        {
            if (_syncComponent != null)
            {
                _syncComponent.SyncClients();
            }
        }

        public bool VerifyVersion(int modAPIVersion, string modName)
        {
            if (modAPIVersion < MIN_API_VERSION)
            {
                WaterUtils.ShowMessage("The Mod '" + modName + "' is using an oudated Water Mod API (" + modAPIVersion + "), tell the author to update!");
                return false;
            }

            return true;
        }

        public MyPlanet GetClosestWater(Vector3D position)
        {
            return _modComponent.GetClosestWater(position)?.Planet;
        }

        public bool IsUnderwater(Vector3D position, MyPlanet planet = null)
        {
            if (planet == null)
                return _modComponent.GetClosestWater(position)?.IsUnderwaterGlobal(ref position) ?? false;
            else
                return planet.Components.Get<WaterComponent>()?.IsUnderwaterGlobal(ref position) ?? false;
        }

        public bool HasWater(MyPlanet planet)
        {
            return planet.Components.Has<WaterComponent>();
        }
        #endregion
    }
}
