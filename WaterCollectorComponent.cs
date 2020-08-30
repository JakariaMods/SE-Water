using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Serialization;
using VRage.Utils;
using VRageMath;

namespace Jakaria
{
    /*[MyEntityComponentDescriptor(typeof(MyObjectBuilder_Collector), false, "Collector", "CollectorSmall")]
    public class WaterCollectorComponent : MyGameLogicComponent
    {
        IMyCollector collector;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                collector = Entity as IMyCollector;
                NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation10()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            try
            {

                MyAPIGateway.Utilities.ShowNotification("ye", 1);
                Water water = WaterMod.Static.GetClosestWater(collector.PositionComp.GetPosition());

                if (water != null)
                {
                    if(water.IsUnderwater(collector.PositionComp.GetPosition()))
                    {
                        collector.GetInventory().AddItems((MyFixedPoint)10, new MyObjectBuilder_Ore() { SubtypeName = "Ice" });
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine(e);
                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, e.ToString());
            }
        }
    }*/
}