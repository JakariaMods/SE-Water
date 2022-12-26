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
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Collector), false)]
    public class WaterCollectorComponent : MyGameLogicComponent
    {
        private IMyCollector _collector;
        private WaterPhysicsComponentGrid _waterComponent;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            _collector = Entity as IMyCollector;

            if (MyAPIGateway.Session.IsServer)
            {
                NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME;
            }
        }

        public override void UpdateAfterSimulation10()
        {
            if(_waterComponent == null)
                _collector.CubeGrid.Components.TryGet<WaterPhysicsComponentGrid>(out _waterComponent);
            else
            {
                if (_waterComponent.Grid.IsPreview || !_collector.HasInventory || _waterComponent.ClosestWater == null || !_collector.IsWorking)
                    return;
                
                if (_waterComponent.ClosestWater.Settings.Material.CollectedItem != null)
                {
                    Vector3D worldPosition = _collector.GetPosition();
                    if (_waterComponent.ClosestWater.IsUnderwaterGlobal(ref worldPosition))
                    {
                        IMyInventory inventory = _collector.GetInventory();
                        if (inventory != null)
                        {
                            if (_collector.CubeGrid.GridSizeEnum == MyCubeSize.Large)
                                inventory.AddItems(_waterComponent.ClosestWater.Settings.Material.CollectedAmount, _waterComponent.ClosestWater.Settings.Material.CollectedItem);
                            else
                                inventory.AddItems(_waterComponent.ClosestWater.Settings.Material.CollectedAmount / 5, _waterComponent.ClosestWater.Settings.Material.CollectedItem);
                        }
                    }
                }
            }
        }
    }
}