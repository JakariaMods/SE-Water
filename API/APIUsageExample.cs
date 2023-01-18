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
using VRage.Game.Entity;
using VRage.Game.ModAPI.Ingame;
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
                MyAPIGateway.Utilities.ShowMessage("Mod", "Water API is registered");
            }
        }

        public override void UpdateAfterSimulation()
        {
            MyEntity entity = (MyEntity)(MyAPIGateway.Session?.Player?.Controller?.ControlledEntity ?? null);

            if(entity != null)
            {
                MyAPIGateway.Utilities.ShowNotification($"Depth: {WaterModAPI.Entity_FluidDepth(entity)}", 16);
                MyAPIGateway.Utilities.ShowNotification($"%Underwater: {WaterModAPI.Entity_PercentUnderwater(entity) * 100}%", 16);
                MyAPIGateway.Utilities.ShowNotification($"Pressure: {WaterModAPI.Entity_FluidPressure(entity)}kPa", 16);
            }
        }
    }
}
*/