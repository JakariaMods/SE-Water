using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Jakaria.Volumetrics
{
    /// <summary>
    /// Basic interface for volumetric simulations. Allows easy switching between simulation types
    /// </summary>
    public interface IVolumetricSimulation : IDisposable
    {
        /// <summary>
        /// Adds/Removes volume of fluid at a point of the surface (via normal) in m^3. Returns the actual volume added/removed
        /// Use sign to determine if it is added or removed
        /// </summary>
        float AdjustFluid(Vector3 normal, float targetAmount);

        /// <summary>
        /// Simulates one step of the fluid simulation
        /// </summary>
        void Simulate();

        /// <summary>
        /// Gets data about the fluid from the normal to the surface
        /// </summary>
        void GetFluid(Vector3D localNormal, out double depth, out double surface);
    }
}
