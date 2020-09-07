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
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Collector), false)]
    public class WaterCollectorComponent : MyGameLogicComponent
    {
        IMyCollector collector;
        Water water;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            WaterUtils.ShowMessage("co");
            if (MyAPIGateway.Session.IsServer)
            {
                collector = Entity as IMyCollector;
                NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME;
            }
        }

        public override void UpdateAfterSimulation10()
        {
            if (!collector.IsWorking)
                return;

            if (collector.CubeGrid?.Physics?.Gravity != null)
                water = WaterMod.Static.GetClosestWater(collector.PositionComp.GetPosition());

            if (collector.IsWorking && water != null)
            {
                if (water.IsUnderwater(collector.PositionComp.GetPosition()))
                {
                    if (collector.CubeGrid.GridSizeEnum == MyCubeSize.Large)
                        collector.GetInventory().AddItems(200, WaterData.IceItem);
                    else
                        collector.GetInventory().AddItems(15, WaterData.IceItem);

                    if(MyUtils.GetRandomInt(0, 2) < 1)
                    WaterMod.Static.CreateBubble(collector.PositionComp.GetPosition(), collector.CubeGrid.GridSize / 2);
                }
            }
        }
    }
}