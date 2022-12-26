/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jakaria.API;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
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

        public override void UpdateAfterSimulation()
        {
            Vector3D position = MyAPIGateway.Session.Camera.Position;

            MyAPIGateway.Utilities.ShowNotification($"Camera Depth: {WaterModAPI.GetDepth(position)}", 16);
        }
    }
}
*/