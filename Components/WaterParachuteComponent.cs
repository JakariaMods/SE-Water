using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Jakaria.Utils;
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

namespace Jakaria.Components
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Parachute), false)]
    public class WaterParachuteComponent : MyGameLogicComponent
    {
        IMyParachute parachute;
        WaterPhysicsComponentGrid physComponent;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                parachute = Entity as IMyParachute;
                NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME;
            }
        }

        public override void UpdateAfterSimulation10()
        {
            if(physComponent == null)
                parachute.CubeGrid.Components.TryGet<WaterPhysicsComponentGrid>(out physComponent);

            if (parachute.CubeGrid?.Physics == null)
                return;

            if (physComponent != null)
            {
                //opens the parachute if the altitude above the water is less than the autodeploy height
                if (physComponent.FluidDepth > parachute.CubeGrid.LocalVolume.Radius)
                {
                    if (parachute.AutoDeploy && physComponent.FluidDepth < parachute.AutoDeployHeight && parachute.OpenRatio == 0)
                    {
                        parachute.OpenDoor();
                    }
                }
                else if(parachute.CubeGrid.Physics?.Speed < 3)
                    parachute.CloseDoor();
            }
        }
    }
}