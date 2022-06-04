using Jakaria.Utils;
using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace Jakaria.SessionComponents
{
    /// <summary>
    /// Component that handles block-damaging with support for parallel access
    /// </summary>
    public class BlockDamageComponent : SessionComponentBase
    {
        private List<BlockDamagePair> _damageQueue = new List<BlockDamagePair>();

        public static BlockDamageComponent Static;

        public BlockDamageComponent()
        {
            Static = this;
            UpdateOrder = MyUpdateOrder.AfterSimulation;
        }

        public override void LoadDependencies()
        {

        }

        public override void UnloadDependencies()
        {
            Static = null;
        }

        public void DoDamage(IMySlimBlock block, float damage)
        {
            lock (_damageQueue)
                _damageQueue.Add(new BlockDamagePair(block, damage));
        }

        public override void UpdateAfterSimulation()
        {
            if (_damageQueue.Count > 0)
            {
                foreach (var damagePair in _damageQueue)
                {
                    if (damagePair.Block.CubeGrid == null || damagePair.Block.CubeGrid.MarkedForClose)
                        continue;

                    if (damagePair.Block != null)
                    {
                        damagePair.Block.DoDamage(damagePair.Damage, MyDamageType.Fall, true);
                    }
                }

                _damageQueue.Clear();
            }
        }
    }
}
