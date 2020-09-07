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
    /*[MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeGatlingTurret), false, "SmallGatlingTurret", "SmallMissileTurret", "LargeInteriorTurret", "LargeGatlingTurret", "LargeMissileTurret")]
    public class WaterTurret : MyGameLogicComponent
    {
        IMyLargeTurretBase turret;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                turret = Entity as IMyLargeTurretBase;
                NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            CheckWater();
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation100()
        {
            CheckWater();
        }

        private void CheckWater()
        {
            try
            {
                if (turret == null || turret.MarkedForClose || WaterMod.Static?.waters == null || !turret.HasTarget)
                    return;

                Water water = WaterMod.Static.GetClosestWater(turret.PositionComp.GetPosition());

                if (water != null)
                {
                    bool underwater = water.IsUnderwater(turret.PositionComp.GetPosition(), -5f);

                    //MyAPIGateway.Utilities.ShowMessage("yea", underwater.ToString() + " " + water.IsUnderwater(turret.Target.PositionComp.GetPosition(), -5f).ToString());
                    if (underwater != water.IsUnderwater(turret.Target.PositionComp.GetPosition(), -5f))
                    {
                        turret.SetTarget(turret);
                        MyAPIGateway.Utilities.ShowMessage("", "reset");
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