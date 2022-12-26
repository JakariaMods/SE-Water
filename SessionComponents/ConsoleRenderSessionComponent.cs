using Jakaria.Utils;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jakaria.Components;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using Sandbox.Game.Components;

namespace Jakaria.SessionComponents
{
    /// <summary>
    /// Code can not be run on consoles, this component streams grids with a water model to the clients.
    /// </summary>
    public class ConsoleRenderSessionComponent : SessionComponentBase
    {
        public const string RENDER_ENTITY_PREFIX = "WaterRender_";

        private WaterModComponent _modComponent;

        private Action _updateAction;

        public override void LoadData()
        {
            _modComponent = Session.Instance.Get<WaterModComponent>();

            if(MyAPIGateway.Session.IsServer)
                MyAPIGateway.Entities.OnEntityAdd += Entities_OnEntityAdd;
        }

        private void Entities_OnEntityAdd(IMyEntity entity)
        {
            IMyCharacter character = entity as IMyCharacter;
            if(character != null)
            {
                ConsoleRenderComponent component = new ConsoleRenderComponent();

                _updateAction += component.UpdateAfterSimulation;
                
                character.Components.Add(component);
            }
        }

        private void Entities_OnEntityRemove(IMyEntity entity)
        {
            IMyCharacter character = entity as IMyCharacter;
            if (character != null)
            {
                ConsoleRenderComponent component;
                if(character.Components.TryGet<ConsoleRenderComponent>(out component))
                {
                    _updateAction -= component.UpdateAfterSimulation;

                    character.Components.Remove<ConsoleRenderComponent>();
                }
            }
        }

        public override void UnloadData()
        {
            if (MyAPIGateway.Session.IsServer)
                MyAPIGateway.Entities.OnEntityRemove -= Entities_OnEntityRemove;
        }

        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Session.IsServer)
                _updateAction?.Invoke();
        }
    }
}
