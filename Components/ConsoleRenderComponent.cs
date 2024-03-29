using Havok;
using Jakaria.SessionComponents;
using Jakaria.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Network;
using VRage.ModAPI;
using VRageMath;

namespace Jakaria.Components
{
    /// <summary>
    /// Component will spawn a console render model and track across the surface. Code can not be run on consoles
    /// </summary>
    public class ConsoleRenderComponent : MyEntityComponentBase
    {
        public const string PREFAB_NAME = "ConsoleWaterPrefab";
        public const float RENDER_ALTITUDE_OFFSET = 100;

        public override string ComponentTypeDebugString => nameof(ConsoleRenderComponent);

        private IMyCharacter _character;
        private IMyCubeGrid _model;

        private bool _needsModel;

        private WaterModComponent _modComponent;

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            
            _modComponent = Session.Instance.Get<WaterModComponent>();
            _character = (IMyCharacter)Entity;

            _needsModel = true;
            MyAPIGateway.Entities.OnEntityAdd += Entities_OnEntityAdd;
            MyVisualScriptLogicProvider.SpawnPrefab(PREFAB_NAME, Vector3D.One, Vector3D.Forward, Vector3D.Up, spawningOptions: SpawningOptions.SetNeutralOwner | SpawningOptions.DisableSave);
        }

        private void Entities_OnEntityAdd(IMyEntity entity)
        {
            if (_needsModel)
            {
                IMyCubeGrid grid = entity as IMyCubeGrid;
                if (grid != null)
                {
                    if (grid.DisplayName == PREFAB_NAME)
                    {
                        grid.DisplayName = GetRenderEntityName(Entity);
                        _model = grid;

                        grid.Save = false;
                        (grid as MyCubeGrid).Immune = true;
                        (grid as MyCubeGrid).AllowPrediction = true;
                        grid.IsStatic = false;

                        _needsModel = false;
                        MyAPIGateway.Entities.OnEntityAdd -= Entities_OnEntityAdd;
                    }
                }
            }
        }

        public override void OnBeforeRemovedFromContainer()
        {
            if (_model != null)
            {
                _model.Close();
                _model = null;
            }

            if (_needsModel)
            {
                MyAPIGateway.Entities.OnEntityAdd -= Entities_OnEntityAdd;
            }
        }

        public void UpdateAfterSimulation()
        {
            if (_model != null)
            {
                Vector3D playerPosition = _character.GetHeadMatrix(false).Translation;

                WaterComponent water = _modComponent.GetClosestWater(playerPosition);

                if (water != null)
                {
                    Vector3D up = water.GetUpDirectionGlobal(ref playerPosition);

                    Quaternion orientation;
                    Vector3D surfacePosition;

                    if (water.IsUnderwaterGlobal(ref playerPosition, ref WaveModifier.Default))
                    {
                        orientation = Quaternion.CreateFromForwardUp(Vector3.CalculatePerpendicularVector(up), -up);
                        surfacePosition = water.GetClosestSurfacePointGlobal(ref playerPosition, ref WaveModifier.Default, RENDER_ALTITUDE_OFFSET);
                    }
                    else
                    {
                        orientation = Quaternion.CreateFromForwardUp(Vector3.CalculatePerpendicularVector(up), up);
                        surfacePosition = water.GetClosestSurfacePointGlobal(ref playerPosition, ref WaveModifier.Default, -RENDER_ALTITUDE_OFFSET);
                    }

                    MatrixD matrix = MatrixD.CreateFromTransformScale(orientation, surfacePosition, Vector3D.One);

                    _model.WorldMatrix = matrix;

                    if(_model.Physics != null)
                    {
                        _model.Physics.LinearVelocity = up;
                        _model.Physics.AngularVelocity = Vector3.Zero;

                        _model.IsStatic = !_model.IsStatic;
                    }
                }
                else
                {
                    _model.WorldMatrix = MatrixD.Identity;
                }
            }
        }

        private static string GetRenderEntityName(IMyEntity entity)
        {
            return $"{PREFAB_NAME}{entity.EntityId}";
        }
    }
}
