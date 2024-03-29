using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jakaria
{
    public struct WaveModifier
    {
        public float HeightMultiplier;
        public float ScaleMultiplier;

        public static WaveModifier Default = new WaveModifier
        {
            HeightMultiplier = 1,
            ScaleMultiplier = 1,
        };
    }
}
