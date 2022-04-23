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
        Water water;

        public bool underWater = false;
        bool isHotTub = false;
        public bool airtight = false;

        public IMyInventory inventory;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            Block = Entity as IMyCollector;
            inventory = Block.GetInventory();
            Block.UseConveyorSystem = false;
            NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        public override void UpdateAfterSimulation10()
        {
            try
            {
                if ((Block as MyEntity).IsPreview || !(Block as MyEntity).InScene || !Block.IsFunctional || Block.Transparent || Block.CubeGrid.Physics == null)
                    return;

                if (inventory == null)
                    inventory = Block.GetInventory();

                if (inventory == null)
                    return;

                if (!isHotTub)
                {
                    if (Block.BlockDefinition.SubtypeId.Contains("Hottub"))
                        isHotTub = true;

                    if (isHotTub && water.Material.CollectedItem != null)
                    {
                        ((MyInventory)inventory).Constraint = new MyInventoryConstraint(MySpaceTexts.ToolTipItemFilter_AnyOre, null, true).Add(water.Material.CollectedItem.GetId());

                        WaterMod.Static.HotTubs.Add(this);
                        WaterMod.Static.HotTubs.ApplyAdditions();
                    }
                }

                if (Block.CubeGrid?.Physics?.Gravity != null)
                    water = WaterMod.Static.GetClosestWater(Block.PositionComp.GetPosition());

                if (water != null)
                {
                    airtight = Block.CubeGrid.IsRoomAtPositionAirtight(Block.Position) ? true : Block.CubeGrid.IsRoomAtPositionAirtight(Block.Position + (Vector3I)Base6Directions.Directions[(int)Block.Orientation.Up]);

                    Vector3D blockPosition = Block.PositionComp.GetPosition();
                    double depth = water?.GetDepth(ref blockPosition) ?? 0;
                    underWater = depth < 0;

                    if (water?.CollectionRate > 0 && !Block.GetInventory().IsFull && underWater && !airtight && water.Material.CollectedItem != null)
                    {
                        if (isHotTub)
                        {
                            inventory.AddItems((int)(Math.Max(depth, 1) * 200), water.Material.CollectedItem);
                        }
                        else
                        {
                            if (Block.IsWorking)
                                if (Block.CubeGrid.GridSizeEnum == MyCubeSize.Large)
                                    inventory.AddItems((int)(water.Material.CollectedAmount * water.CollectionRate), water.Material.CollectedItem);
                                else
                                    inventory.AddItems((int)((water.Material.CollectedAmount / 5f) * water.CollectionRate), water.Material.CollectedItem);
                        }
                    }

                    if (isHotTub)
                    {
                        if (!airtight && underWater)
                            return;

                        if (Block.CubeGrid.Physics != null && !Block.CubeGrid.Physics.IsStatic)
                        {
                            Vector3D acccelDirection = Vector3D.Normalize(Block.CubeGrid.Physics.LinearAcceleration - (Block.CubeGrid.Physics.Gravity * 2));

                            double dot = Vector3D.Dot(acccelDirection, Block.PositionComp.WorldMatrixRef.Up);
                            //double dot = Vector3D.Dot(water.GetUpDirection(Block.PositionComp.GetPosition()), Block.PositionComp.WorldMatrixRef.Up);
                            if (dot < .5)
                            {
                                if (MyAPIGateway.Session.IsServer)
                                {
                                    MyFixedPoint amount = (MyFixedPoint)(dot > 0 ? (1f - Math.Abs(dot)) * 25 : 25);
                                    inventory.RemoveItemsAt(0, amount, spawn: true);
                                }
                            }
                        }
                        else
                        {
                            double dot = Vector3D.Dot(water.GetUpDirection(ref blockPosition), Block.PositionComp.WorldMatrixRef.Up);
                            if (dot < .75)
                            {
                                if (MyAPIGateway.Session.IsServer)
                                {
                                    MyFixedPoint amount = (MyFixedPoint)(dot > 0 ? (1f - Math.Abs(dot)) * 25 : 25);

                                    inventory.RemoveItemsAt(0, amount, spawn: true);
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("Issue", 16 * 10);
            }
        }

        public override void OnRemovedFromScene()
        {
            if (isHotTub)
            {
                WaterMod.Static.HotTubs.Remove(this);
                WaterMod.Static.HotTubs.ApplyRemovals();
            }
        }
    }
}//todo rewrite again this is shit