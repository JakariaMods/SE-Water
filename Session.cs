using Jakaria.Configs;
using Jakaria.SessionComponents;
using Jakaria.Utils;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace Jakaria
{
    /// <summary>
    /// Manages all components
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class Session : MySessionComponentBase
    {
        public const bool XBOX_MODE = false;

        private readonly Dictionary<Type, SessionComponentBase> _sessionComponents = new Dictionary<Type, SessionComponentBase>();

        private readonly Exception _exception;

        public static Session Instance;

        public Action UpdateAfterSimulationAction;
        public Action DrawAction;

        public Session()
        {
            Instance = this;
            
            try
            {
                if(XBOX_MODE)
                {
                    AddComponent(new WaterConfigComponent());
                    AddComponent(new WaterModComponent());
                    AddComponent(new WaterRespawnPodComponent());
                    AddComponent(new BlockDamageComponent());
                    AddComponent(new XboxRenderComponent());
                    AddComponent(new XboxCommandComponent());
                }
                else
                {
                    if (((IMyUtilities)MyAPIUtilities.Static).IsDedicated)
                    {
                        AddComponent(new WaterConfigComponent());
                        AddComponent(new WaterModComponent());
                        AddComponent(new BackwardsCompatibilityComponent());
                        AddComponent(new WaterRespawnPodComponent());
                        AddComponent(new BlockDamageComponent());
                        AddComponent(new WaterAPIComponent());
                        AddComponent(new WaterSyncComponent());
                    }
                    else
                    {
                        AddComponent(new WaterConfigComponent());
                        AddComponent(new WaterModComponent());
                        AddComponent(new BackwardsCompatibilityComponent());
                        AddComponent(new WaterFishComponent());
                        AddComponent(new WaterDescriptionComponent());
                        AddComponent(new WaterSettingsComponent());
                        AddComponent(new WaterCommandComponent());
                        AddComponent(new WaterSoundComponent());
                        AddComponent(new WaterUIComponent());
                        AddComponent(new WaterRenderSessionComponent());
                        AddComponent(new WaterEffectsComponent());
                        AddComponent(new WaterRespawnPodComponent());
                        AddComponent(new BlockDamageComponent());
                        AddComponent(new WaterAPIComponent());
                        AddComponent(new WaterSyncComponent());
                    }
                }
            }
            catch (Exception e)
            {
                _exception = e;
                _sessionComponents.Clear();
            }
        }

        private void AddComponent<T>(T component) where T : SessionComponentBase
        {
            _sessionComponents[typeof(T)] = component;

            UpdateAfterSimulationAction += component.UpdateAfterSimulation;

            DrawAction += component.Draw;
        }

        public override void BeforeStart()
        {
            if(ModContext != null)
            {
                foreach (var componentPair in _sessionComponents)
                {
                    componentPair.Value.ModContext = ModContext;
                    componentPair.Value.BeforeStart();
                }
            }
        }

        public override void Draw()
        {
            foreach (var componentPair in _sessionComponents)
            {
                componentPair.Value.Draw();
            }
        }
        
        public override void LoadData()
        {
            foreach (var componentPair in _sessionComponents)
            {
                componentPair.Value.LoadData();
            }
        }

        public override void SaveData()
        {
            foreach (var componentPair in _sessionComponents)
            {
                componentPair.Value.SaveData();
            }
        }

        protected override void UnloadData()
        {
            foreach (var componentPair in _sessionComponents)
            {
                componentPair.Value.UnloadData();
            }
        }

        public override void UpdateAfterSimulation() => UpdateAfterSimulationAction?.Invoke();


        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            if (_exception != null)
            {
                WaterUtils.WriteLog(_exception.ToString());
                WaterUtils.ShowMessage(_exception.ToString());

                _sessionComponents.Clear();
            }

            foreach (var componentPair in _sessionComponents)
            {
                componentPair.Value.Init();
            }
        }

        /// <summary>
        /// Tries to get a component and returns null if it does not exist
        /// </summary>
        public T TryGet<T>() where T : SessionComponentBase
        {
            SessionComponentBase component;
            if (_sessionComponents.TryGetValue(typeof(T), out component))
                return component as T;
            return null;
        }

        public T Get<T>() where T : SessionComponentBase
        {
            T component = TryGet<T>();

            if (component == null)
                throw new KeyNotFoundException();

            return component;
        }

        private bool TryRemoveComponent(Type type)
        {
            return _sessionComponents.Remove(type);
        }
    }
}
