using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;

namespace Jakaria
{

    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class FatBlockStorage : MySessionComponentBase
    {
        public static ConcurrentDictionary<long, ListReader<BlockStorage>> Storage { get; private set; } = new ConcurrentDictionary<long, ListReader<BlockStorage>>();

        private ConcurrentCachingList<MyCubeGrid> GridsToAdd = new ConcurrentCachingList<MyCubeGrid>();

        public static ConcurrentCachingList<WaterCollectorComponent> HotTubs = new ConcurrentCachingList<WaterCollectorComponent>();

        public override void LoadData()
        {
            MyEntities.OnEntityRemove += Entities_OnEntityRemove;
            MyEntities.OnEntityCreate += Entities_OnEntityCreate;
        }

        private void Entities_OnEntityCreate(MyEntity obj)
        {
            if (!obj.IsPreview && obj is MyCubeGrid)
            {
                GridsToAdd.Add(obj as MyCubeGrid);
                GridsToAdd.ApplyAdditions();
            }
        }

        public override void UpdateAfterSimulation()
        {
            if (!GridsToAdd.IsEmpty)
            {
                MyAPIGateway.Parallel.StartBackground(() =>
                {
                    foreach (var grid in GridsToAdd)
                    {
                        if (Storage.ContainsKey(grid.EntityId))
                        {
                            Storage.Remove(grid.EntityId);
                        }

                        if (Storage.TryAdd(grid.EntityId, ConstructList(grid.GetFatBlocks())))
                        {
                            grid.OnFatBlockAdded += FatBlockStorage_OnFatBlockAdded;
                            grid.OnFatBlockClosed += FatBlockStorage_OnFatBlockRemoved;
                        }
                    }

                    GridsToAdd.ClearImmediate();
                });
            }
        }

        ListReader<BlockStorage> ConstructList(ListReader<MyCubeBlock> Blocks)
        {
            List<BlockStorage> List = new List<BlockStorage>();

            foreach (var Block in Blocks)
            {
                List.Add(new BlockStorage(Block));
            }

            return List;
        }

        private void Entities_OnEntityRemove(MyEntity obj)
        {
            if (!obj.IsPreview && obj is MyCubeGrid)
            {
                ListReader<BlockStorage> fatBlocks;
                Storage.TryRemove(obj.EntityId, out fatBlocks);

                (obj as MyCubeGrid).OnFatBlockAdded -= FatBlockStorage_OnFatBlockAdded;
                (obj as MyCubeGrid).OnFatBlockClosed -= FatBlockStorage_OnFatBlockRemoved;
            }
        }

        private void FatBlockStorage_OnFatBlockAdded(MyCubeBlock obj)
        {
            ListReader<BlockStorage> fatBlocks;
            Storage.TryRemove(obj.CubeGrid.EntityId, out fatBlocks);
            Storage.TryAdd(obj.CubeGrid.EntityId, ConstructList(obj.CubeGrid.GetFatBlocks()));
        }

        private void FatBlockStorage_OnFatBlockRemoved(MyCubeBlock obj)
        {
            ListReader<BlockStorage> fatBlocks;
            Storage.TryRemove(obj.CubeGrid.EntityId, out fatBlocks);
            Storage.TryAdd(obj.CubeGrid.EntityId, ConstructList(obj.CubeGrid.GetFatBlocks()));

            if (WaterMod.Static.WheelStorage.ContainsKey(obj.EntityId))
                WaterMod.Static.WheelStorage.Remove(obj.EntityId);
        }

        protected override void UnloadData()
        {
            MyEntities.OnEntityAdd -= Entities_OnEntityCreate;
            MyEntities.OnEntityRemove -= Entities_OnEntityRemove;
        }

        public class BlockStorage
        {
            public MyCubeBlock Block;
            public bool PreviousUnderwater = false;

            public BlockStorage(MyCubeBlock Block)
            {
                this.Block = Block;
            }
        }
    }
}
