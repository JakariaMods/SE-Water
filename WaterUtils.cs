using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;
using Sandbox.Game.Entities;
using Sandbox.Game;
using Sandbox.ModAPI;

namespace Jakaria
{
    public static class WaterUtils
    {
        /// <summary>
        /// Returns a vector perpendicular to a vector, takes an angle
        /// </summary>
        public static Vector3D GetPerpendicularVector(Vector3D vector, double angle)
        {
            Vector3D perpVector = Vector3D.CalculatePerpendicularVector(Vector3.Normalize(vector));
            Vector3D bitangent; Vector3D.Cross(ref vector, ref perpVector, out bitangent);
            return Vector3D.Normalize(Math.Cos(angle) * perpVector + Math.Sin(angle) * bitangent);
        }

        /// <summary>
        /// Turns certain special characters into an xml compatible string
        /// </summary>
        public static string ValidateXMLData(string input)
        {
            input = input.Replace("<", "&lt;");
            input = input.Replace(">", "&gt;");
            return input;
        }

        /// <summary>
        /// Returns how far a position is into the night on a planet
        /// </summary>
        public static float GetNightValue(MyPlanet planet, Vector3 position)
        {
            if (planet == null)
                return 0;

            return Vector3.Dot(MyVisualScriptLogicProvider.GetSunDirection(), Vector3.Normalize(position - planet.PositionComp.GetPosition()));
        }

        /// <summary>
        /// Sends a chat message using WaterMod as the sender, not synced
        /// </summary>
        public static void ShowMessage(string message)
        {
            MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, message);
        }

        /// <summary>
        /// Removes brackets to help players parse their commands if for some reason they put them
        /// </summary>
        public static string ValidateCommandData(string input)
        {
            input = input.Replace("[", "");
            input = input.Replace("]", "");
            return input;
        }
    }
}
