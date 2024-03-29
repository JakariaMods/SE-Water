using Jakaria.Components;
using Jakaria.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Character.Components;
using Sandbox.Game.GameSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Jakaria
{
    public class WaterOxygenProvider : IMyOxygenProvider
    {
        /// <summary>
        /// Characters test for oxygen at the position of the character.
        /// To limit the feature to only characters, we must compare the sampled position with all characters in the world.
        /// If it were not limited, some grids would not have oxygen outside of water due to how they sample it.
        /// </summary>
        public List<IMyCharacter> Characters = new List<IMyCharacter>();

        private WaterComponent _water;

        public WaterOxygenProvider(WaterComponent water)
        {
            _water = water;
        }

        public float GetOxygenForPosition(Vector3D worldPoint)
        {
            return float.MinValue;
        }

        public bool IsPositionInRange(Vector3D worldPoint)
        {
            WaveModifier modifier = WaterUtils.GetWaveModifier(worldPoint);
            Vector3D position = worldPoint - (_water.GetUpDirectionGlobal(ref worldPoint) * 2);
            if (!_water.IsUnderwaterGlobal(ref position, ref modifier))
                return false;

            foreach (var character in Characters)
            {
                //MyCharacterOxygenComponent tests oxygen at this position
                if (character.PositionComp.GetPosition() == worldPoint)
                {
                    Vector3D head = character.GetHeadMatrix(false).Translation;
                    return _water.IsUnderwaterGlobal(ref head, ref modifier);
                }
            }

            return false;
        }
    }
}
