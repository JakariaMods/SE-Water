using Jakaria.Components;
using Jakaria.Configs;
using Jakaria.Utils;
using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Jakaria.SessionComponents
{
    /// <summary>
    /// Base mod component
    /// </summary>
    public class WaterModComponent : SessionComponentBase
    {
        public IEnumerable<WaterComponent> Waters => _waters.Values;

        private Dictionary<MyPlanet, WaterComponent> _waters = new Dictionary<MyPlanet, WaterComponent>();

        public event Action<MyEntity> OnWaterAdded;
        public event Action<MyEntity> OnWaterRemoved;

        public event Action UpdateAction;
        public event Action UpdateActionSparse;

        private int _timer;

        public override void UpdateAfterSimulation()
        {
            _timer++;

            if(_timer % MyEngineConstants.UPDATE_STEPS_PER_SECOND == 0)
            {
                UpdateActionSparse?.Invoke();
            }

            UpdateAction?.Invoke();
        }

        public WaterComponent GetWaterById(long id)
        {
            MyPlanet planet = (MyPlanet)MyEntities.GetEntityById(id);
            return planet.Components.Get<WaterComponent>();
        }

        public WaterComponent GetClosestWater(Vector3D position)
        {
            return MyGamePruningStructure.GetClosestPlanet(position)?.Components.Get<WaterComponent>() ?? null;
        }

        public bool AddWater(MyPlanet planet, WaterSettings settings = null, WaterComponentObjectBuilder ob = null)
        {
            WaterComponent water = new WaterComponent(planet, settings);
            if(ob != null)
            {
                water.WaveTimer = ob.Timer;
                water.TideTimer = ob.TideTimer;
            }

            if (planet.Components.Has<WaterComponent>())
            {
                return false;
            }
            else
            {
                planet.Components.Add(water);

                _waters.Add(planet, water);
                
                UpdateAction += water.Simulate;

                OnWaterAdded?.Invoke(planet);

                return true;
            }
        }

        public bool RemoveWater(MyPlanet planet)
        {
            WaterComponent water;
            if(_waters.TryGetValue(planet, out water))
            {
                UpdateAction -= water.Simulate;

                _waters.Remove(planet);

                planet.Components.Remove(typeof(WaterComponent), water);

                OnWaterRemoved?.Invoke(planet);

                return true;
            }

            return false;
        }

        public WaterModComponentObjectBuilder Serialize()
        {
            WaterModComponentObjectBuilder builder = new WaterModComponentObjectBuilder
            {
                Waters = new List<WaterComponentObjectBuilder>(),
            };

            foreach (var water in Waters)
            {
                builder.Waters.Add(water.Serialize());
            }

            return builder;
        }

        private void MyEntities_OnEntityAdd(MyEntity entity)
        {
            TryAddPhysicsComponentToEntity(entity);
        }

        private void MyEntities_OnEntityCreate(MyEntity entity)
        {
            /*MyPlanet planet = entity as MyPlanet;
            if(planet != null)
            {
                if (!planet.Components.Has<WaterComponent>())
                {
                    PlanetConfig planetConfig;
                    if (WaterData.PlanetConfigs.TryGetValue(planet.Generator.Id, out planetConfig) && planetConfig.WaterSettings != null)
                    {
                        AddWater(planet, planetConfig.WaterSettings);
                    }
                }
            }*/

            TryAddPhysicsComponentToEntity(entity);
        }

        private void TryAddPhysicsComponentToEntity(MyEntity entity)
        {
            if (entity is IMyCubeGrid && !entity.Components.Has<WaterPhysicsComponentGrid>())
                entity.Components.Add(new WaterPhysicsComponentGrid());
            else if (entity is IMyFloatingObject && !entity.Components.Has<WaterPhysicsComponentFloatingObject>())
                entity.Components.Add(new WaterPhysicsComponentFloatingObject());
            else if (entity is IMyCharacter && !entity.Components.Has<WaterPhysicsComponentCharacter>())
                entity.Components.Add(new WaterPhysicsComponentCharacter());
            else if (entity is IMyInventoryBag && !entity.Components.Has<WaterPhysicsComponentInventoryBag>())
                entity.Components.Add(new WaterPhysicsComponentInventoryBag());
        }

        public override void BeforeStart()
        {
            string packet;
            if (MyAPIGateway.Utilities.GetVariable<string>(WaterData.SaveVariableName, out packet))
            {
                if (!string.IsNullOrEmpty(packet))
                {
                    WaterModComponentObjectBuilder builder = MyAPIGateway.Utilities.SerializeFromXML<WaterModComponentObjectBuilder>(packet);
                    if (builder != null)
                    {
                        foreach (var water in builder.Waters)
                        {
                            MyPlanet planet = MyEntities.GetEntityById(water.EntityId) as MyPlanet;
                            
                            if (planet != null)
                            {
                                AddWater(planet, water.Settings, water);
                            }
                        }
                    }
                }
            }
        }

        public override void LoadData()
        {
            MyEntities.OnEntityRemove += MyEntities_OnEntityRemove;
            MyEntities.OnEntityAdd += MyEntities_OnEntityAdd;
            MyEntities.OnEntityCreate += MyEntities_OnEntityCreate;
        }

        public override void UnloadData()
        {
            MyEntities.OnEntityRemove -= MyEntities_OnEntityRemove;
            MyEntities.OnEntityAdd -= MyEntities_OnEntityAdd;
            MyEntities.OnEntityCreate -= MyEntities_OnEntityCreate;
        }

        private void MyEntities_OnEntityRemove(MyEntity entity)
        {
            MyPlanet planet = entity as MyPlanet;

            if(planet != null && _waters.ContainsKey(planet))
            {
                RemoveWater(planet);
            }
        }

        public override void SaveData()
        {
            MyAPIUtilities.Static.Variables[WaterData.SaveVariableName] = MyAPIGateway.Utilities.SerializeToXML(Serialize());
        }
    }

    [ProtoContract]
    public class WaterModComponentObjectBuilder
    {
        [XmlElement]
        public List<WaterComponentObjectBuilder> Waters;
    }
}
