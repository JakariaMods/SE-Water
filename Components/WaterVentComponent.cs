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
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_AirVent), false)]
    public class WaterVentComponent : MyGameLogicComponent
    {
        IMyAirVent vent;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                vent = Entity as IMyAirVent;
                NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
            }
        }

        public override void UpdateAfterSimulation100()
        {
            CheckWater();
        }

        private void CheckWater()
        {
            try
            {
                if (vent == null || vent.MarkedForClose || WaterMod.Static?.waters == null || !vent.IsWorking)
                    return;

                Water water = WaterMod.Static.GetClosestWater(vent.PositionComp.GetPosition());

                if (water != null)
                {
                    if (!vent.CanPressurize && vent.Depressurize && water.IsUnderwater(vent.PositionComp.GetPosition()))
                        vent.Depressurize = false;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine(e);
                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, e.ToString());
            }

        }
    }
}