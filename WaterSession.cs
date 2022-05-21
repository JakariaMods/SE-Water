using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Jakaria
{
    public class WaterSession
    {
        public bool CameraAboveWater = false;
        public Vector3D CameraClosestPosition = Vector3D.Zero;
        public bool CameraUnderground = false;
        public double CameraDepth = 0;
        public bool CameraAirtight = false;
        public bool CameraUnderwater = false;
        public int InsideGrid = 0;
        public int InsideVoxel = 0;
        public Vector3D CameraPosition = Vector3.Zero;
        public Vector3D CameraRotation = Vector3.Zero;
        public Vector3 SunDirection = Vector3.Zero;
        public float SessionTimer = 0;
        public Vector3D GravityDirection = Vector3.Zero;
        public Vector3D CameraClosestWaterPosition = Vector3D.Zero;
        public Vector3D Gravity = Vector3.Zero;
        public Vector3D GravityAxisA = Vector3D.Zero;
        public Vector3D GravityAxisB = Vector3D.Zero;
        public Vector3D LastLODBuildPosition = Vector3D.MaxValue;
        public float DistanceToHorizon = 0;
        public double CameraAltitude = 0;
        public MyPlanet ClosestPlanet = null;
        public Water ClosestWater = null;
        public float AmbientColorIntensity = 0;
    }
}
