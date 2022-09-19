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

    public struct BlockDamagePair
    {
        public IMySlimBlock Block;
        public float Damage;

        public BlockDamagePair(IMySlimBlock block, float damage)
        {
            Block = block;
            Damage = damage;
        }
    }
}