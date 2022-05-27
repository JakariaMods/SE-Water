using Jakaria.Configs;
using Jakaria.Utils;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace Jakaria.SessionComponents
{
    /// <summary>
    /// Component used to manually update components and allow easier inter-component communication
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class WaterManagerComponent : MySessionComponentBase
    {
        private HashSet<SessionComponentBase> _components = new HashSet<SessionComponentBase>();
        private Exception _exception;

        public static WaterManagerComponent Static;

        public WaterManagerComponent()
        {
            Static = this;

            try
            {
                _components.Add(new WaterConfigComponent());
                _components.Add(new WaterModComponent());
                _components.Add(new WaterDescriptionComponent());
                _components.Add(new WaterSettingsComponent());
                _components.Add(new WaterCommandComponent());
                _components.Add(new WaterSoundComponent());
                _components.Add(new WaterUIComponent());
                _components.Add(new WaterRenderComponent());
                _components.Add(new WaterEffectsComponent());
                _components.Add(new WaterRespawnPodComponent());

                foreach (var component in _components)
                {
                    component.LoadDependencies();
                }
            }
            catch (Exception e)
            {
                _exception = e;
                _components.Clear();
            }
        }

        public override void BeforeStart()
        {
            foreach (var component in _components)
            {
                component.ModContext = ModContext;
                component.BeforeStart();
            }
        }

        public override void Draw()
        {
            foreach (var component in _components)
            {
                component.Draw();
            }
        }

        public override void LoadData()
        {
            //Due to entity component timing, Components need to be opted-out instead of being opted in.
            if (MyAPIGateway.Utilities.IsDedicated)
            {
                RemoveComponent<WaterDescriptionComponent>();
                RemoveComponent<WaterCommandComponent>();
                RemoveComponent<WaterSoundComponent>();
                RemoveComponent<WaterUIComponent>();
                RemoveComponent<WaterRenderComponent>();
                RemoveComponent<WaterEffectsComponent>();
                RemoveComponent<WaterSettingsComponent>();
            }

            if(!MyAPIGateway.Session.IsServer)
            {
                RemoveComponent<WaterRespawnPodComponent>();
            }

            foreach (var component in _components)
            {
                component.LoadData();
            }
        }

        public override void SaveData()
        {
            foreach (var component in _components)
            {
                component.SaveData();
            }
        }

        protected override void UnloadData()
        {
            foreach (var component in _components)
            {
                component.UnloadData();
            }

            foreach (var component in _components)
            {
                component.UnloadDependencies();
            }
        }

        public override void UpdateAfterSimulation()
        {
            foreach (var component in _components)
            {
                if(component.UpdateOrder == MyUpdateOrder.AfterSimulation)
                    component.UpdateAfterSimulation();
            }
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            if(_exception != null)
            {
                WaterUtils.WriteLog(_exception.ToString());
                WaterUtils.ShowMessage(_exception.ToString());

                _components.Clear();
            }
                
            foreach (var component in _components)
            {
                component.Init();
            }
        }

        public T TryGet<T>() where T : SessionComponentBase
        {
            foreach (var component in _components)
            {
                if (component is T)
                    return component as T;
            }

            return null;
        }

        private void RemoveComponent<T>()
        {
            foreach (var component in _components)
            {
                if(component is T)
                {
                    component.UnloadData();
                    _components.Remove(component);
                    return;
                }
            }
        }
    }
}
