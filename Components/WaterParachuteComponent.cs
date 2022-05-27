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
        private IMyParachute _parachute;
        private WaterPhysicsComponentGrid _waterComponent;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                _parachute = Entity as IMyParachute;
                NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME;
            }
        }

        public override void UpdateAfterSimulation10()
        {
            if(_waterComponent == null)
                _parachute.CubeGrid.Components.TryGet<WaterPhysicsComponentGrid>(out _waterComponent);

            if (_parachute.CubeGrid?.Physics == null)
                return;

            if (_waterComponent != null)
            {
                //opens the parachute if the altitude above the water is less than the autodeploy height
                if (_waterComponent.FluidDepth > _parachute.CubeGrid.LocalVolume.Radius)
                {
                    if (_parachute.AutoDeploy && _waterComponent.FluidDepth < _parachute.AutoDeployHeight && _parachute.OpenRatio == 0)
                    {
                        _parachute.OpenDoor();
                    }
                }
                else if(_parachute.CubeGrid.Physics?.Speed < 3)
                    _parachute.CloseDoor();
            }
        }
    }
}