using Jakaria.Utils;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character.Components;
using Sandbox.ModAPI;
using System;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;
using Jakaria.Configs;
using Jakaria.SessionComponents;
using Sandbox.Engine.Physics;
using VRageRender;

namespace Jakaria.Components
{
    public class WaterPhysicsComponentCharacter : WaterPhysicsComponentBase
    {
        private double _maxRadius;
        private bool _airtight;
        private MyCubeGrid _nearestGrid;
        private int _breath;
        private Vector3D _characterHeadPosition;
        private bool _headUnderwater;
        private bool _underwater;

        public IMyCharacter Character;
        public CharacterConfig PlayerConfig;

        public WaterPhysicsComponentCharacter() : base() { }

        /// <summary>
        /// Initalize the component
        /// </summary>
        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();

            NeedsRecalculateBuoyancy = true;

            Character = Entity as IMyCharacter;
        }

        /// <summary>
        /// Called before the entity is removed from the scene.
        /// </summary>
        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();
        }

        /// <summary>
        /// Secondary simulation method for updating low priority/frequency stuff like drowning
        /// </summary>
        public override void UpdateAfter60()
        {
            base.UpdateAfter60();

            _maxRadius = Character.PositionComp.LocalVolume.Radius;
            _nearestGrid = WaterUtils.GetApproximateGrid(_position);

            if (PlayerConfig != null && ClosestWater != null)
            {
                _headUnderwater = ClosestWater.IsUnderwaterGlobal(ref _characterHeadPosition);

                MyCharacterOxygenComponent oxygenComponent;
                if (_nearestGrid != null)
                    _airtight = _nearestGrid.IsRoomAtPositionAirtight(_nearestGrid.WorldToGridInteger(_characterHeadPosition));
                else
                    _airtight = false;

                if (_airtight || !_headUnderwater)
                {
                    _breath = PlayerConfig.Breath;
                }
                else if (_headUnderwater)
                {
                    //Drowning
                    if (!PlayerConfig.CanBreathUnderwater && (!Character.Components.TryGet<MyCharacterOxygenComponent>(out oxygenComponent) || !oxygenComponent.HelmetEnabled))
                    {
                        _breath = Math.Max(_breath - 1, 0);

                        if (_effectsComponent != null && ClosestWater.Settings.Material.DrawBubbles)
                            _effectsComponent.CreateBubble(ref _characterHeadPosition, (float)_maxRadius / 4);

                        if (_breath <= 0 && (Character.ControllerInfo?.Controller?.ControlledEntity is IMyCharacter == true))
                        {
                            if (MyAPIGateway.Session.IsServer)
                                Character.DoDamage(PlayerConfig.DrowningDamage, MyDamageType.Asphyxia, true);
                        }
                    }

                    //Energy Draining


                    //Pressure Crushing
                    if (FluidPressure > PlayerConfig.MaximumPressure)
                    {
                        if (MyAPIGateway.Session.IsServer && Character.ControllerInfo?.Controller?.ControlledEntity is IMyCharacter == true)
                            Character.DoDamage(ClosestWater.Settings.CrushDamage * (FluidPressure / PlayerConfig.MaximumPressure), MyDamageType.Temperature, true);

                        if (_effectsComponent != null && ClosestWater.Settings.Material.DrawBubbles)
                            _effectsComponent.CreateBubble(ref _characterHeadPosition, (float)_maxRadius / 4);
                    }
                }
            }
        }

        /// <summary>
        /// Simulation method for physics
        /// </summary>
        public override void UpdateAfter1()
        {
            base.UpdateAfter1();

            if (Entity.Physics == null || ClosestWater == null || ClosestWater.Settings.Material == null)
                return;

            if (SimulatePhysics)
            {
                if (ClosestWater == null || !Entity.InScene || Entity.MarkedForClose || _gravityStrength == 0)
                    return;

                //Character Physics
                _characterHeadPosition = Character.GetHeadMatrix(false, false).Translation;

                if (PlayerConfig == null && Character.Definition?.Id.SubtypeName != null)
                    WaterData.CharacterConfigs.TryGetValue(((MyCharacterDefinition)Character.Definition).Id, out PlayerConfig);

                if (FluidDepth < _maxRadius && !_airtight)
                {
                    Vector3 GridVelocity = Vector3.Zero;

                    //Buoyancy is disabled when near grids to prevent bouncing
                    bool shouldSimulateBuoyancy = true;

                    if (_nearestGrid?.Physics != null)
                    {
                        if (_nearestGrid.RayCastBlocks(_characterHeadPosition, _characterHeadPosition + (Vector3.Normalize(_nearestGrid.Physics.LinearVelocity) * 10)) != null)
                        {
                            GridVelocity = (_nearestGrid?.Physics?.LinearVelocity ?? Vector3.Zero);
                        }
                    }
                    
                    IHitInfo hitInfo;
                    shouldSimulateBuoyancy = !MyAPIGateway.Physics.CastRay(_position, _position + (_gravityDirection * 2), out hitInfo, 30);

                    PercentUnderwater = (float)Math.Max(Math.Min(-FluidDepth / _maxRadius, 1), 0);
                    _velocity = (-Character.Physics.LinearVelocity + GridVelocity + FluidVelocity);
                    DragForce = ClosestWater.Settings.PlayerDrag ? (ClosestWater.Settings.Material.Density * (_velocity * _speed)) * (Character.PositionComp.LocalVolume.Radius * Character.PositionComp.LocalVolume.Radius) * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS : Vector3.Zero;

                    //Buoyancy
                    if (shouldSimulateBuoyancy && (!Character.EnabledThrusts || !Character.EnabledDamping))
                    {
                        DragForce += -_gravity * ClosestWater.Settings.Material.Density * PlayerConfig.Volume;
                    }

                    DragForce *= PercentUnderwater;

                    //Drag
                    if (DragForce.IsValid())
                        Character.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, DragForce, null, null);

                    //Swimming
                    if (PlayerConfig.SwimForce > 0 && !Character.EnabledThrusts)
                        Character.Physics.AddForce(MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, Vector3.ClampToSphere(Character.LastMotionIndicator, PercentUnderwater) * PlayerConfig.SwimForce, null, null);
                }

                //Simple Splash Effects
                if (_effectsComponent != null && ClosestWater.Settings.Material.DrawSplashes)
                {
                    bool underwater = FluidDepth < _maxRadius && !_airtight;

                    if(_underwater != underwater)
                    {
                        _underwater = underwater;

                        Vector3D surfacePosition = ClosestWater.GetClosestSurfacePointGlobal(ref _position);
                        _effectsComponent.CreateSplash(surfacePosition, Math.Min(_speed, 2f), true);

                        MatrixD matrix = MatrixD.CreateWorld(surfacePosition, -_renderComponent.CameraGravityDirection, _renderComponent.GravityAxisA);
                        MyParticleEffect effect;
                        if (MyParticlesManager.TryCreateParticleEffect("WaterSplashSmall", ref matrix, ref surfacePosition, 0, out effect))
                        {
                            effect.UserColorIntensityMultiplier = ClosestWater.PlanetConfig.ColorIntensity;
                        }
                    }
                }
            }
        }
    }
}