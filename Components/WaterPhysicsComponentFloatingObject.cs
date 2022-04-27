﻿using Jakaria.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace Jakaria.Components
{
    public class WaterPhysicsComponentFloatingObject : WaterPhysicsComponentBase
    {
        private float WaterDensityMultiplier;

        float DragOptimizer = 0;
        double MaxRadius;

        MyFloatingObject FloatingObject;
        bool Airtight;

        /// <summary>
        /// Initalize the component
        /// </summary>
        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();

            NeedsRecalculateBuoyancy = true;

            FloatingObject = Entity as MyFloatingObject;
        }

        /// <summary>
        /// Secondary simulation method for updating low priority/frequency stuff like drowning
        /// </summary>
        public override void UpdateAfter60()
        {
            base.UpdateAfter60();

            if (ClosestWater != null)
            {
                WaterDensityMultiplier = ClosestWater.Material.Density / 1000f;
                MaxRadius = Entity.PositionComp.LocalVolume.Radius;

                Airtight = WaterUtils.IsPositionAirtight(ref position);
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
                    if (ClosestWater == null || !Entity.InScene || Entity.Physics == null || Entity.MarkedForClose || gravity == 0 && Entity.Physics.Mass == 0)
                        return;

                    if (!Airtight && FluidDepth < 0)
                    {
                        if (FloatingObject.Item.Content?.SubtypeId != null && ClosestWater.Material?.CollectedItem?.SubtypeId != null)
                        {
                            if (FloatingObject.Item.Content.SubtypeId == ClosestWater.Material.CollectedItem.SubtypeId)
                            {
                                Entity.Close();
                                return;
                            }
                        }

                        nextRecalculate--;
                        if (NeedsRecalculateBuoyancy || nextRecalculate <= 0)
                        {
                            nextRecalculate = WaterData.BuoyancyUpdateFrequencyEntity;
                            NeedsRecalculateBuoyancy = false;
                            PercentUnderwater = (float)Math.Max(Math.Min(-FluidDepth / MaxRadius, 1), 0);
                            BuoyancyForce = -Entity.Physics.Gravity * ClosestWater.Material.Density * (WaterData.SphereVolumeOptimizer * (float)(MaxRadius * MaxRadius)) * PercentUnderwater * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS * ClosestWater.Buoyancy;
                            DragOptimizer = 0.5f * ClosestWater.Material.Density * (float)(MaxRadius * MaxRadius) * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;

                            if (Entity is MyMeteor)
                            {
                                MyVisualScriptLogicProvider.CreateExplosion(position, 2, 0);
                                MyFloatingObjects.Spawn(new MyPhysicalInventoryItem(500 * (MyFixedPoint)MyUtils.GetRandomFloat(1f, 3f), MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>(WaterUtils.GetRandomMeteorMaterial())), Entity.WorldMatrix, null, null);
                                Entity.Close();
                                return;
                            }
                        }

                        if (speed > 0.1f)
                        {
                            Vector3 VelocityDamper = gravityDirection * (Vector3.Dot(Vector3.Normalize(Entity.Physics.LinearVelocity), gravityDirection) * speed * ClosestWater.Material.Viscosity * WaterDensityMultiplier);

                            //Vertical Velocity Damping
                            if (VelocityDamper.IsValid() && !Vector3.IsZero(VelocityDamper, 1e-4f))
                                Entity.Physics.LinearVelocity -= VelocityDamper;

                            DragForce = -DragOptimizer * (speed * velocity);

                            //Linear Drag
                            if (DragForce.IsValid() && !Vector3.IsZero(DragForce, 1e-4f))
                                Entity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, Vector3.ClampToSphere(DragForce, Entity.Physics.Mass * speed), null, null);
                        }

                        //Angular Drag
                        if (angularSpeed > 0.03f)
                        {
                            Vector3 AngularDragForce = ((PercentUnderwater * DragOptimizer * angularSpeed) * Entity.Physics.AngularVelocity) / Entity.Physics.Mass;

                            if (AngularDragForce.IsValid() && !Vector3.IsZero(AngularDragForce, 1e-4f))
                                Entity.Physics.AngularVelocity -= Vector3.ClampToSphere(AngularDragForce, angularSpeed / 2);
                        }

                        if (BuoyancyForce.IsValid())
                            Entity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, BuoyancyForce, null, null);

                        //Simple Splash Effects
                        if (SimulateEffects && ClosestWater.Material.DrawSplashes)
                        {
                            if (FluidDepth < 0)
                            {
                                if (SimulateEffects && FluidDepth > -1 && FluidDepth < 1 && verticalSpeed > 2f)
                                {
                                    WaterModComponent.Static.CreateSplash(position, Math.Min(speed, 2f), true);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (!MyAPIGateway.Utilities.IsDedicated)
                    MyAPIGateway.Utilities.ShowNotification("Water Error on Floating Object. Phys-" + SimulatePhysics + "Efct-" + SimulateEffects, 16 * recalculateFrequency);

                WaterUtils.WriteLog(e.ToString());
            }
        }
    }
}