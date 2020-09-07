/*using System;
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
Keen ree
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SolarPanel), false)]
    public class WaterSolarComponent : MyGameLogicComponent
    {
        IMySolarPanel solarPanel;
        Water water;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            WaterUtils.ShowMessage("so");
            if (MyAPIGateway.Session.IsServer)
            {
                solarPanel = Entity as IMySolarPanel;
                NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
            }
        }

        public override void UpdateAfterSimulation100()
        {
            if (!solarPanel.IsFunctional || !solarPanel.Enabled)
                return;

            water = WaterMod.Static.GetClosestWater(solarPanel.PositionComp.GetPosition());

            if (water != null)
            {
                solarPanel.Enabled = !water.IsUnderwater(solarPanel.PositionComp.GetPosition());
            }
        }
    }
}*/