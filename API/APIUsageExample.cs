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
            if (WaterAPI.Registered)
            {
                MyAPIGateway.Utilities.ShowMessage("Mod", "Joe Mama Water Registered");
            }
        }

        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Session?.Camera?.Position != null)
                if (WaterAPI.IsUnderwater(MyAPIGateway.Session.Camera.Position))
                {
                    MyAPIGateway.Utilities.ShowNotification("Camera is underwater at depth " + (int)WaterAPI.GetDepth(MyAPIGateway.Session.Camera.Position) + "m", 16);
                }

            WaterAPI.CreatePhysicsSplash(MyAPIGateway.Session.Player.GetPosition() + (MyAPIGateway.Session.Player.Character.WorldMatrix.Up), MyAPIGateway.Session.Player.Character.WorldMatrix.Forward, 0.5f, 5);
        }
    }
}
*/