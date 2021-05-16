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
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Serialization;
using VRage.Utils;
using VRageMath;

namespace Jakaria
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Wheel), false)]
    public class WaterWheelComponent : MyGameLogicComponent
    {
        IMyWheel wheel;
        IMyMotorSuspension suspension;
        Water water;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                wheel = Entity as IMyWheel;
                NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
                suspension = wheel.Base as IMyMotorSuspension;
            }
        }

        public override void UpdateAfterSimulation100()
        {
            if (suspension == null)
                suspension = wheel.Base as IMyMotorSuspension;

            if (wheel == null || !wheel.IsFunctional || suspension == null)
                return;

            water = WaterMod.Static.GetClosestWater(wheel.GetPosition());

            MyTuple<float, float> WheelSettings;
            if (water.IsUnderwater(wheel.PositionComp.GetPosition()))
            {
                if (suspension.Friction > 20f || suspension.Power > 40f)
                {
                    WaterMod.Static.WheelStorage[wheel.EntityId] = new MyTuple<float, float>(suspension.Friction, suspension.Power);
                    suspension.Friction = Math.Min(suspension.Friction, 20f);
                    suspension.Power = Math.Min(suspension.Power, 40f);
                }
            }
            else if (WaterMod.Static.WheelStorage.TryGetValue(wheel.EntityId, out WheelSettings))
            {
                suspension.Friction = WheelSettings.Item1;
                suspension.Power = WheelSettings.Item2;
                WaterMod.Static.WheelStorage.Remove(wheel.EntityId);
            }
        }

        public override void MarkForClose()
        {
            MyTuple<float, float> WheelSettings;
            if (WaterMod.Static.WheelStorage.TryGetValue(wheel.EntityId, out WheelSettings))
            {
                suspension.Friction = WheelSettings.Item1;
                suspension.Power = WheelSettings.Item2;
                WaterMod.Static.WheelStorage.Remove(wheel.EntityId);
            }
        }

        public override MyObjectBuilder_ComponentBase Serialize(bool copy = false)
        {
            MyTuple<float, float> WheelSettings;
            if (WaterMod.Static.WheelStorage.TryGetValue(wheel.EntityId, out WheelSettings))
            {
                suspension.Friction = WheelSettings.Item1;
                suspension.Power = WheelSettings.Item2;
                WaterMod.Static.WheelStorage.Remove(wheel.EntityId);
            }

            return base.Serialize(copy);
        }
    }
}