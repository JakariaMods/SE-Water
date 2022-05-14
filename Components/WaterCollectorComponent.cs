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
using Sandbox.Game.Localization;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
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
        public IMyCollector Block;
        public Water ClosestWater;

        private IMyInventory _inventory;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            Block = Entity as IMyCollector;
            _inventory = Block.GetInventory();
            Block.UseConveyorSystem = false;
            NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        public override void UpdateAfterSimulation10()
        {
            if (Block.CubeGrid?.Physics == null || !Block.IsWorking)
                return;

            if (_inventory == null)
                _inventory = Block.GetInventory();

            if (_inventory != null)
            {
                ClosestWater = WaterModComponent.Static.GetClosestWater(Block.PositionComp.GetPosition());

                if (ClosestWater != null && ClosestWater.CollectionRate > 0)
                {
                    Vector3D blockPosition = Block.CubeGrid.GridIntegerToWorld(Block.Position);

                    if (ClosestWater.IsUnderwater(ref blockPosition))
                    {
                        if (!_inventory.IsFull && ClosestWater.Material.CollectedItem != null)
                        {
                            if (Block.CubeGrid.GridSizeEnum == MyCubeSize.Large)
                                _inventory.AddItems((int)(ClosestWater.Material.CollectedAmount * ClosestWater.CollectionRate), ClosestWater.Material.CollectedItem);
                            else
                                _inventory.AddItems((int)((ClosestWater.Material.CollectedAmount / 5f) * ClosestWater.CollectionRate), ClosestWater.Material.CollectedItem);
                        }
                    }
                }
            }
        }
    }
}