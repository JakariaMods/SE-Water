using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace Jakaria.SessionComponents
{
    public abstract class SessionComponentBase
    {
        public MyUpdateOrder UpdateOrder;

        public IMyModContext ModContext;

        public SessionComponentBase() { }

        public virtual void Init() { }

        public abstract void LoadDependencies();

        public abstract void UnloadDependencies();

        public virtual void UpdateAfterSimulation() { }

        public virtual void UnloadData() { }

        public virtual void SaveData() { }

        public virtual void LoadData() { }

        public virtual void Draw() { }

        public virtual void BeforeStart() { }

    }
}
