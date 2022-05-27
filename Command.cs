using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;

namespace Jakaria
{
    public struct Command
    {
        public readonly Action<string[]> Action;
        public string Description;
        public MyPromoteLevel PromoteLevel;

        public int MinArgs;
        public int MaxArgs;

        public bool RequirePlanet;
        public bool RequireWater;

        public bool SyncWater;

        public Command(Action<string[]> action)
        {
            Action = action;

            Description = "Empty Description";
            PromoteLevel = MyPromoteLevel.None;
            MinArgs = 1;
            MaxArgs = 1;
            RequirePlanet = false;
            RequireWater = false;
            SyncWater = false;
        }
    }
}
