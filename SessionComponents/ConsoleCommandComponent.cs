using Jakaria.Components;
using Jakaria.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRageMath;

namespace Jakaria.SessionComponents
{
    internal class ConsoleCommandComponent : SessionComponentBase
    {
        private WaterModComponent _modComponent;
        private WaterCommandComponent _commandComponent;

        public override void LoadData()
        {
            _modComponent = Session.Instance.Get<WaterModComponent>();
            _commandComponent = Session.Instance.Get<WaterCommandComponent>();

            MyAPIGateway.Utilities.MessageRecieved += Utilities_MessageRecieved;
        }

        private void Utilities_MessageRecieved(ulong sender, string messageText)
        {
            _commandComponent.SendCommand(messageText, sender);
        }

        public override void UnloadData()
        {
            MyAPIGateway.Utilities.MessageRecieved -= Utilities_MessageRecieved;
        }
    }
}
