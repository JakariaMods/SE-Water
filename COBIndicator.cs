using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Jakaria
{
    public class COBIndicator
    {
        public Vector3 position;
        public int life = 1;
        public int maxLife;
        public MyCubeSize gridSize;

        public COBIndicator(Vector3D Position, MyCubeSize GridSize)
        {
            position = Position;
            maxLife = (int)MyUtils.GetRandomFloat(150, 250);
            gridSize = GridSize;
        }
    }
}
