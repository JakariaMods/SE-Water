using Jakaria.Utils;
using Sandbox.ModAPI;
using System;
using VRage.Game.Components;
using VRage.ModAPI;
using VRageMath;

namespace Jakaria.Components
{
    public class WaterPhysicsComponentBase : MyGameLogicComponent
    {
        /// <summary>
        /// The Water that the entity will use to simulate with, can be null
        /// </summary>
        public Water ClosestWater { get; protected set; }

        /// <summary>
        /// If this it true, it will recalculate buoyancy the next frame
        /// </summary>
        public bool NeedsRecalculateBuoyancy;

        /// <summary>
        /// Simulates client-effects if true
        /// </summary>
        public bool SimulateEffects;

        /// <summary>
        /// Simulates the phyics if true
        /// </summary>
        public bool SimulatePhysics;

        /// <summary>
        /// Hydrostatcic Pressure of the fluid the entity is at. The Unit is kPa (KiloPascals)
        /// </summary>
        public float FluidPressure { get; protected set; }

        /// <summary>
        /// Depth of the entity in the fluid. Unit is m (Meters) Positive is above water, negative is below water
        /// </summary>
        public double FluidDepth { get; protected set; }

        /// <summary>
        /// Velocity of the fluid (Currents, Wave Oscillation)
        /// </summary>
        public Vector3 FluidVelocity { get; protected set; }

        /// <summary>
        /// Unit is N (Newtons) Force vector for buoyancy
        /// </summary>
        public Vector3 BuoyancyForce { get; protected set; }

        /// <summary>
        /// Center of buoyancy position of the entity in World Space
        /// </summary>
        public Vector3D CenterOfBuoyancy { get; protected set; }

        /// <summary>
        /// Unit is N (Newtons) Force vector for drag
        /// </summary>
        public Vector3 DragForce { get; protected set; }

        /// <summary>
        /// Percentage of the entity being underwater where 1 is 100%
        /// </summary>
        public float PercentUnderwater { get; protected set; }

        protected Vector3 gravityDirection;
        protected float gravity;
        protected Vector3D position;

        protected Vector3 velocity;
        protected float speed;
        protected Vector3 verticalVelocity;
        protected float verticalSpeed;
        protected float angularSpeed;

        protected int nextRecalculate = 0;
        protected int recalculateFrequency = 0;

        public override void OnAddedToContainer()
        {
            WaterModComponent.Static.UpdateAfter1 += UpdateAfter1;
            WaterModComponent.Static.UpdateAfter60 += UpdateAfter60;
            Entity.OnPhysicsChanged += Entity_OnPhysicsChanged;

            UpdateClosestWater();
        }

        public override void OnBeforeRemovedFromContainer()
        {
            WaterModComponent.Static.UpdateAfter1 -= UpdateAfter1;
            WaterModComponent.Static.UpdateAfter60 -= UpdateAfter60;
            Entity.OnPhysicsChanged -= Entity_OnPhysicsChanged;
        }

        public virtual void UpdateAfter60()
        {
            UpdateClosestWater();

            if (!MyAPIGateway.Utilities.IsDedicated && Vector3D.DistanceSquared(position, WaterModComponent.Static.Session.CameraPosition) < MyAPIGateway.Session.SessionSettings.SyncDistance * MyAPIGateway.Session.SessionSettings.SyncDistance)
            {
                SimulateEffects = true;
            }
            else
                SimulateEffects = false;

            if(ClosestWater != null && Entity.Physics != null)
            {
                FluidPressure = (ClosestWater.Material.Density * gravity * (float)Math.Max(-FluidDepth, 0)) / 1000;

                if (ClosestWater.planet?.HasAtmosphere == true)
                {
                    //(101325 / 1000) Converts atm to kPa. 1atm = 101.325kPa
                    FluidPressure += ClosestWater.planet.GetOxygenForPosition(position) * (101325 / 1000);
                }

                gravityDirection = Entity.Physics.Gravity;
                gravity = gravityDirection.Normalize();
            }
        }

        public virtual void UpdateAfter1()
        {
            if (Entity.Physics == null || ClosestWater == null || ClosestWater.Material == null)
                return;

            if(SimulatePhysics || SimulateEffects)
            {
                position = Entity.PositionComp.WorldVolume.Center;

                FluidDepth = ClosestWater.GetDepth(ref position);
                FluidVelocity = ClosestWater.GetFluidVelocity(-gravityDirection);

                if (SimulatePhysics)
                {
                    velocity = Entity.Physics.LinearVelocity - FluidVelocity;
                    speed = velocity.Length();

                    verticalVelocity = Vector3.ProjectOnVector(ref velocity, ref gravityDirection);
                    verticalSpeed = verticalVelocity.Length();

                    angularSpeed = Entity.Physics.AngularVelocity.Length();
                }
            }
        }

        /// <summary>
        /// Updates the closest water to the entity
        /// </summary>
        protected void UpdateClosestWater()
        {
            ClosestWater = WaterModComponent.Static.GetClosestWater(position);
        }

        /// <summary>
        /// When the entity changes from static to ship or vise versa
        /// </summary>
        private void Entity_OnPhysicsChanged(IMyEntity obj)
        {
            if (obj.Physics == null)
            {
                SimulatePhysics = false;
            }
            else
            {
                if (SimulatePhysics == obj.Physics.IsStatic)
                {
                    SimulatePhysics = !obj.Physics.IsStatic;

                    OnSimulatePhysicsChanged(SimulatePhysics);
                }
            }
        }

        /// <summary>
        /// Called when the SimulatePhysics boolean changes value
        /// </summary>
        protected virtual void OnSimulatePhysicsChanged(bool value)
        {

        }
    }
}
