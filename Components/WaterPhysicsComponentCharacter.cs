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

namespace Jakaria.Components
{
    public class WaterPhysicsComponentCharacter : WaterPhysicsComponentBase
    {
        double MaxRadius;
        bool Airtight;
        MyCubeGrid NearestGrid;

        //Character Stuff
        IMyCharacter Character;
        CharacterConfig PlayerConfig;
        Vector3D CharacterHeadPosition;

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

            MaxRadius = Character.PositionComp.LocalVolume.Radius;
            NearestGrid = WaterUtils.GetApproximateGrid(position);

            if (PlayerConfig != null)
            {
                MyCharacterOxygenComponent OxygenComponent;
                if (NearestGrid != null)
                    Airtight = NearestGrid.IsRoomAtPositionAirtight(NearestGrid.WorldToGridInteger(CharacterHeadPosition));
                else
                    Airtight = false;

                if (!Airtight && FluidDepth < 0)
                {
                    //Drowning
                    if (!PlayerConfig.CanBreathUnderwater && (!Character.Components.TryGet<MyCharacterOxygenComponent>(out OxygenComponent) || !OxygenComponent.HelmetEnabled))
                    {
                        if ((Character.ControllerInfo?.Controller?.ControlledEntity is IMyCharacter == true))
                        {
                            if (MyAPIGateway.Session.IsServer)
                                Character.DoDamage(3f, MyDamageType.Asphyxia, true);

                            if (SimulateEffects)
                                WaterModComponent.Static.CreateBubble(ref CharacterHeadPosition, (float)MaxRadius / 4);
                        }
                    }

                    //Pressure Crushing
                    if (FluidPressure > PlayerConfig.MaximumPressure)
                    {
                        if (MyAPIGateway.Session.IsServer && Character.ControllerInfo?.Controller?.ControlledEntity is IMyCharacter == true)
                            Character.DoDamage(ClosestWater.CrushDamage * (FluidPressure / PlayerConfig.MaximumPressure), MyDamageType.Temperature, true);

                        if (SimulateEffects)
                            WaterModComponent.Static.CreateBubble(ref CharacterHeadPosition, (float)MaxRadius / 4);
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

            if (Entity.Physics == null || ClosestWater == null || ClosestWater.Material == null)
                return;

            try
            {
                if (SimulatePhysics || SimulateEffects)
                {
                    if (ClosestWater == null || !Entity.InScene || Entity.MarkedForClose || gravity == 0)
                        return;

                    //Character Physics
                    CharacterHeadPosition = Character.GetHeadMatrix(false, false).Translation;

                    if (PlayerConfig == null && Character.Definition?.Id.SubtypeName != null)
                        WaterData.CharacterConfigs.TryGetValue(((MyCharacterDefinition)Character.Definition).Id, out PlayerConfig);

                    if (FluidDepth < MaxRadius && !Airtight)
                    {
                        if (ClosestWater.Material.DrawSplashes && SimulateEffects && FluidDepth > -1 && FluidDepth < 1 && verticalSpeed > 2f) //Splash effect
                        {
                            WaterModComponent.Static.CreateSplash(position, Math.Min(speed, 2f), true);
                        }

                        Vector3 GridVelocity = Vector3.Zero;

                        if (WaterUtils.IsPlayerStateFloating(Character.CurrentMovementState))
                        {
                            if (NearestGrid?.Physics != null)
                            {
                                if (NearestGrid.RayCastBlocks(CharacterHeadPosition, CharacterHeadPosition + (Vector3.Normalize(NearestGrid.Physics.LinearVelocity) * 10)) != null)
                                {
                                    GridVelocity = (NearestGrid?.Physics?.LinearVelocity ?? Vector3.Zero);
                                }
                            }

                            PercentUnderwater = (float)Math.Max(Math.Min(-FluidDepth / MaxRadius, 1), 0);
                            velocity = (-Character.Physics.LinearVelocity + GridVelocity + FluidVelocity);
                            DragForce = ClosestWater.PlayerDrag ? ((ClosestWater.Material.Density * (velocity * speed)) / 2f) * (((MyCharacterDefinition)Character.Definition).CharacterHeadSize * Character.PositionComp.LocalVolume.Radius) * WaterData.CharacterDragCoefficient : Vector3.Zero;

                            //Buoyancy
                            if ((!Character.EnabledThrusts || !Character.EnabledDamping))
                            {
                                DragForce += -Character.Physics.Gravity * ClosestWater.Material.Density * PlayerConfig.Volume;
                            }

                            DragForce *= PercentUnderwater;

                            //Drag
                            if (DragForce.IsValid())
                                Character.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, DragForce, null, null);

                            //Swimming
                            if (PlayerConfig.SwimForce > 0 && !Character.EnabledThrusts)
                                Character.Physics.AddForce(MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, Vector3.ClampToSphere(Character.LastMotionIndicator, PercentUnderwater) * PlayerConfig.SwimForce, null, null);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (!MyAPIGateway.Utilities.IsDedicated)
                    MyAPIGateway.Utilities.ShowNotification("Water Error on Character. Phys-" + SimulatePhysics + "Efct-" + SimulateEffects, 16 * recalculateFrequency);

                WaterUtils.WriteLog(e.ToString());
            }
        }
    }
}