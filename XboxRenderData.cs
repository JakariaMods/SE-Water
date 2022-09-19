using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.ModAPI;

namespace Jakaria
{
    /// <summary>
    /// Data struct for binding players and their accomponying render entitiy
    /// </summary>
    public class XboxRenderData
    {
        public IMyEntity RenderEntity;

        public IMyEntity AttachedEntity;
    }
}
