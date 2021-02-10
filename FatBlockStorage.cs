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
        public static ConcurrentDictionary<long, ListReader<MyCubeBlock>> Storage { get; private set; } = new ConcurrentDictionary<long, ListReader<MyCubeBlock>>();

        private ConcurrentCachingList<MyCubeGrid> GridsToAdd = new ConcurrentCachingList<MyCubeGrid>();

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

                        if (Storage.TryAdd(grid.EntityId, grid.GetFatBlocks()))
                        {
                            grid.OnFatBlockAdded += FatBlockStorage_OnFatBlockAdded;
                            grid.OnFatBlockClosed += FatBlockStorage_OnFatBlockRemoved;
                        }
                    }

                    GridsToAdd.ClearImmediate();
                });
            }
        }

        private void Entities_OnEntityRemove(MyEntity obj)
        {
            if (!obj.IsPreview && obj is MyCubeGrid)
            {
                ListReader<MyCubeBlock> fatBlocks;
                Storage.TryRemove(obj.EntityId, out fatBlocks);

                (obj as MyCubeGrid).OnFatBlockAdded -= FatBlockStorage_OnFatBlockAdded;
                (obj as MyCubeGrid).OnFatBlockClosed -= FatBlockStorage_OnFatBlockRemoved;
            }
        }

        private void FatBlockStorage_OnFatBlockAdded(MyCubeBlock obj)
        {
            ListReader<MyCubeBlock> fatBlocks;
            Storage.TryRemove(obj.CubeGrid.EntityId, out fatBlocks);
            Storage.TryAdd(obj.CubeGrid.EntityId, obj.CubeGrid.GetFatBlocks());
        }

        private void FatBlockStorage_OnFatBlockRemoved(MyCubeBlock obj)
        {
            ListReader<MyCubeBlock> fatBlocks;
            Storage.TryRemove(obj.CubeGrid.EntityId, out fatBlocks);
            Storage.TryAdd(obj.CubeGrid.EntityId, obj.CubeGrid.GetFatBlocks());
        }

        protected override void UnloadData()
        {
            MyEntities.OnEntityAdd -= Entities_OnEntityCreate;
            MyEntities.OnEntityRemove -= Entities_OnEntityRemove;
        }
    }
}
