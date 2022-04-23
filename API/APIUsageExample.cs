/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jakaria.API;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRageMath;

namespace Jakaria
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    class APIUsageExample : MySessionComponentBase
    {
        public override void BeforeStart()
        {
            if (WaterModAPI.Registered)
            {
                MyAPIGateway.Utilities.ShowMessage("Mod", "Joe Mama Water Registered");
            }
        }
    }
}
*/