using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Utils;

namespace Jakaria.API
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class WaterModAPI : MySessionComponentBase
    {
        public const ushort ModHandlerID = 50271;
        public const int ModAPIVersion = 10;

        /// <summary>
        /// List of all water objects in the world, null if not registered
        /// </summary>
        public static List<Water> Waters { get; private set; } = new List<Water>();

        /// <summary>
        /// Invokes when the API recieves data from the Water Mod
        /// </summary>
        public static event Action RecievedData;

        /// <summary>
        /// Invokes when a water is added to the Waters list
        /// </summary>
        public static event Action WaterCreatedEvent;

        /// <summary>
        /// Invokes when a water is removed from the Waters list
        /// </summary>
        public static event Action WaterRemovedEvent;

        /// <summary>
        /// Invokes when the water API becomes registered and ready to work
        /// </summary>
        public static event Action OnRegisteredEvent;

        /// <summary>
        /// True if the API is registered/alive
        /// </summary>
        public static bool Registered { get; private set; } = false;

        /// <summary>
        /// Used to tell in chat what mod is out of date
        /// </summary>
        public static string ModName = MyAPIGateway.Utilities.GamePaths.ModScopeName.Split('_')[1];

        //Water API Guide
        //Drag WaterModAPI.cs and Water.cs into your mod
        //You no longer have to register your mod or create a new WaterModAPI Object, just grab WaterModAPI.Water itself, it's super easy!
        //

        /// <summary>
        /// Do not use, for interfacing with Water Mod
        /// </summary>
        private void ModHandler(object data)
        {
            if (data == null)
                return;

            if (data is byte[])
            {
                Waters = MyAPIGateway.Utilities.SerializeFromBinary<List<Water>>((byte[])data);

                if (Waters == null)
                    Waters = new List<Water>();
                else foreach (var water in Waters)
                    {
                        MyEntity entity = MyEntities.GetEntityById(water.planetID);

                        if (entity != null)
                            water.planet = MyEntities.GetEntityById(water.planetID) as MyPlanet;
                    }

                int count = Waters.Count;
                RecievedData?.Invoke();

                if (count > Waters.Count)
                    WaterCreatedEvent?.Invoke();
                if (count < Waters.Count)
                    WaterRemovedEvent?.Invoke();
            }

            if (!Registered)
            {
                Registered = true;
                OnRegisteredEvent?.Invoke();
            }

            if (data is int && (int)data != ModAPIVersion)
            {
                MyLog.Default.WriteLine("Water API V" + ModAPIVersion + " for mod '" + ModName + "' is outdated, expected V" + (int)data);
                MyAPIGateway.Utilities.ShowMessage(ModName, "Water API V" + ModAPIVersion + " for mod '" + ModName + "' is outdated, expected V" + (int)data);
            }
        }

        /// <summary>
        /// Do not use, for interfacing with Water Mod
        /// </summary>
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            MyAPIGateway.Utilities.RegisterMessageHandler(ModHandlerID, ModHandler);
        }

        /// <summary>
        /// Do not use, for interfacing with Water Mod
        /// </summary>
        public override void UpdateAfterSimulation()
        {
            if (Waters != null)
                foreach (var water in Waters)
                {
                    water.waveTimer += water.waveSpeed;
                    water.currentRadius = (float)Math.Max(water.radius + (Math.Sin((water.waveTimer) * water.waveSpeed) * water.waveHeight), 0);
                }
        }

        /// <summary>
        /// Do not use, for interfacing with Water Mod
        /// </summary>
        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(ModHandlerID, ModHandler);
        }
    }
}