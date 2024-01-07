using Jakaria.Components;
using Jakaria.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Jakaria
{
    public class WaterOxygenProvider : IMyOxygenProvider
    {
        private WaterComponent _water;

        public WaterOxygenProvider(WaterComponent water)
        {
            _water = water;
        }

        public float GetOxygenForPosition(Vector3D worldPoint)
        {
            MyTransparentGeometry.AddPointBillboard(MyStringId.GetOrCompute("Square"), _water.IsUnderwaterGlobal(ref worldPoint) ? WaterData.GreenColor : WaterData.RedColor, worldPoint, 1, 0);
            return float.MinValue;
        }

        public bool IsPositionInRange(Vector3D worldPoint)
        {
            //MyTransparentGeometry.AddPointBillboard(MyStringId.GetOrCompute("Square"), _water.IsUnderwaterGlobal(ref worldPoint) ? WaterData.GreenColor : WaterData.RedColor, worldPoint, 1, 0);
            return _water.IsUnderwaterGlobal(ref worldPoint);
        }
    }
}
