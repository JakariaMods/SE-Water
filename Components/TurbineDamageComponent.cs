using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Jakaria.Utils;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
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
using SpaceEngineers.Game.Entities.Blocks;

namespace Jakaria
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_WindTurbine), false)]
    public class TurbineDamageComponent : MyGameLogicComponent
    {
        IMyPowerProducer block;
        float normalMaxOutput;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (MyAPIGateway.Session.IsServer) //server side only
            {
                normalMaxOutput = 0.4f; //hardcoded, but if actually used would be using the blockdefinition
                block = Entity as IMyPowerProducer; //would not be needed if used
                NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
            }
        }

        public override void UpdateAfterSimulation100()
        {
            if (block.IsWorking && block.MaxOutput / normalMaxOutput > 1.5f) //If power is 1.5x that of the normal output without weather (should be defined inside weather)
            {
                MyVisualScriptLogicProvider.CreateParticleEffectAtPosition("Collision_Sparks", block.PositionComp.GetPosition()); //Dummy particle, should definitely change to use a particle defined inside weather definition
                block.SlimBlock.DoDamage(5f, MyDamageType.Environment, true); //maybe add MyDamageType.Weather?
            }
        }
    }
}