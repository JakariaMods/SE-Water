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
using VRage;
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

                if (_waterComponent.ClosestWater.Settings.Material.CollectedItem != null && _waterComponent.ClosestWater.Settings.CollectionRate != 0)
                {
                    Vector3D worldPosition = _collector.PositionComp.GetPosition();
                    if (_waterComponent.ClosestWater.IsUnderwaterGlobal(ref worldPosition, ref _waterComponent.WaveModifier))
                    {
                        IMyInventory inventory = _collector.GetInventory();
                        if (inventory != null)
                        {
                            float amount = _waterComponent.ClosestWater.Settings.Material.CollectedAmount * _waterComponent.ClosestWater.Settings.CollectionRate;
                            
                            if (_waterComponent.ClosestWater.Volumetrics == null)
                            {
                                if (_collector.CubeGrid.GridSizeEnum == MyCubeSize.Large)
                                    inventory.AddItems((MyFixedPoint)amount, _waterComponent.ClosestWater.Settings.Material.CollectedItem);
                                else
                                    inventory.AddItems((MyFixedPoint)(amount / 5f), _waterComponent.ClosestWater.Settings.Material.CollectedItem);
                            }
                            else
                            {
                                float removed = _waterComponent.ClosestWater.Volumetrics.AdjustFluid(Vector3D.Normalize(worldPosition - _waterComponent.ClosestWater.WorldMatrix.Translation), -5);

                                inventory.AddItems((MyFixedPoint)(amount / 5f), _waterComponent.ClosestWater.Settings.Material.CollectedItem);
                            }
                        }
                    }
                }
            }
        }
    }
}