using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;

namespace Jakaria
{
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
