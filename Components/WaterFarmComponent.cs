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
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OxygenFarm), false)]
    public class WaterFarmComponent : MyGameLogicComponent
    {
        IMyFunctionalBlock oxygenFarm;
        Water water;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                oxygenFarm = Entity as IMyFunctionalBlock;
                NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
            }
        }

        public override void UpdateAfterSimulation100()
        {
            if (!oxygenFarm.IsFunctional)
                return;

            water = WaterMod.Static.GetClosestWater(oxygenFarm.PositionComp.GetPosition());

            if (water != null)
            {
                oxygenFarm.Enabled = !water.IsUnderwater(oxygenFarm.PositionComp.GetPosition());
            }
        }
    }
}