using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;
using Sandbox.Game.Entities;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Utils;
using Sandbox.Definitions;
using VRage.Game;
using VRage.Game.Entity;
using System.Diagnostics;
using VRage.Game.ModAPI;
using VRageRender;
using VRage.Game.ModAPI.Ingame.Utilities;
using Sandbox.Game.GameSystems;

namespace Jakaria.Utils
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

        public static void WriteLog(string message)
        {
            MyLog.Default.WriteLine("WaterMod: " + message);
        }

        /// <summary>
        /// Removes brackets to help players parse their commands if for some reason they put them
        /// </summary>
        public static string ValidateCommandData(string input)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var letter in input)
            {
                if (letter == '[' || letter == ']')
                    continue;

                if (letter == ',')
                    sb.Append('.');
                else
                    sb.Append(letter);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Returns a random meteor material name
        /// </summary>
        /// <returns></returns>
        public static string GetRandomMeteorMaterial()
        {
            MyVoxelMaterialDefinition material = null;
            int tries = MyDefinitionManager.Static.GetVoxelMaterialDefinitions().Count() * 2; // max amount of tries

            while (material == null || !material.IsRare || !material.SpawnsFromMeteorites)
            {
                if (--tries < 0) // to prevent infinite loops in case all materials are disabled just use the meteorites' initial material
                {
                    return "Stone";
                }
                material = MyDefinitionManager.Static.GetVoxelMaterialDefinitions().ElementAt(MyUtils.GetRandomInt(MyDefinitionManager.Static.GetVoxelMaterialDefinitions().Count() - 1));
            }
            string materialName = material.MinedOre;

            if (materialName == null)
                materialName = "Stone";

            return materialName;
        }

        /// <summary>
        /// Checks if a position is airtight
        /// </summary>
        public static bool IsPositionAirtight(Vector3D position)
        {
            if (!MyAPIGateway.Session.SessionSettings.EnableOxygenPressurization)
                return false;

            BoundingSphereD sphere = new BoundingSphereD(position, 5);
            List<MyEntity> entities = new List<MyEntity>();
            MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref sphere, entities);

            foreach (var entity in entities)
            {
                MyCubeGrid grid = entity as MyCubeGrid;

                if (grid != null && grid.IsRoomAtPositionAirtight(grid.WorldToGridInteger(position)))
                    return true;
            }
            return false;
        }

        public static bool HotTubUnderwater(Vector3D Position)
        {
            foreach (var Tub in FatBlockStorage.HotTubs)
            {
                if (Tub.Block.PositionComp.WorldAABB.Contains(Position) > 0)
                {
                    float HeightOffset = -(Tub.Block.CubeGrid.GridSize / 2) + (((float)Tub.inventory.CurrentVolume / (float)Tub.inventory.MaxVolume) * (Tub.Block.CubeGrid.GridSize / 2)) + 0.1f;

                    if (Vector3D.Dot(Tub.Block.PositionComp.WorldMatrixRef.Up, Vector3D.Normalize(Position - (Tub.Block.PositionComp.GetPosition() + (Tub.Block.PositionComp.WorldMatrixRef.Up * HeightOffset)))) < 0)
                        return true;
                }
            }
            return false;
        }

        public static bool HotTubUnderwater(Vector3D Position, out IMyCubeGrid grid)
        {
            foreach (var Tub in FatBlockStorage.HotTubs)
            {
                if (Tub.Block.PositionComp.WorldAABB.Contains(Position) > 0)
                {
                    float HeightOffset = -(Tub.Block.CubeGrid.GridSize / 2) + (((float)Tub.inventory.CurrentVolume / (float)Tub.inventory.MaxVolume) * (Tub.Block.CubeGrid.GridSize / 2)) + 0.1f;

                    if (Vector3D.Dot(Tub.Block.PositionComp.WorldMatrixRef.Up, Vector3D.Normalize(Position - (Tub.Block.PositionComp.GetPosition() + (Tub.Block.PositionComp.WorldMatrixRef.Up * HeightOffset)))) < 0)
                    {
                        grid = Tub.Block.CubeGrid;
                        return true;
                    }

                }
            }
            grid = null;
            return false;
        }

        /// <summary>
        /// Checks if a position is airtight
        /// </summary>
        public static bool IsNearGrid(Vector3 position, ref MyCubeGrid grid)
        {
            if (WaterMod.Static.nearbyEntities != null)
                foreach (var entity in WaterMod.Static.nearbyEntities)
                {
                    if (entity is MyCubeGrid)
                    {
                        if (!entity.IsPreview && (entity as MyCubeGrid).GridSizeEnum == MyCubeSize.Large && entity.Physics?.IsStatic == false)
                            if (entity.PositionComp.WorldAABB.Contains(position) > ContainmentType.Disjoint)
                            {
                                grid = entity as MyCubeGrid;
                                return true;
                            }

                    }
                }
            return false;
        }

        /// <summary>
        /// Returns the closest grid to the position
        /// </summary>
        public static MyCubeGrid GetApproximateGrid(Vector3 position, MyEntityQueryType queryType = MyEntityQueryType.Both)
        {
            BoundingSphereD sphere = new BoundingSphereD(position, 1);
            List<MyEntity> entities = new List<MyEntity>();
            MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref sphere, entities, queryType);

            foreach (var entity in entities)
            {
                if (entity.IsPreview || entity.Physics == null)
                    continue;

                if (entity is MyCubeGrid)
                    return entity as MyCubeGrid;
            }
            entities.Clear();
            return null;
        }

        /// <summary>
        /// Checks if a position on a planet is underground
        /// </summary>
        public static bool IsUnderGround(MyPlanet planet, Vector3D position, double altitudeOffset = 0)
        {
            if (planet == null)
                return false;

            double altitude = (position - planet.WorldMatrix.Translation).Length() + altitudeOffset;

            if (altitude < planet.MinimumRadius)
                return true;

            if (altitude > planet.MaximumRadius)
                return false;

            if ((altitude - (planet.GetClosestSurfacePointGlobal(position) - planet.WorldMatrix.Translation).Length()) < 0)
                return true;

            return false;
        }

        /// <summary>
        /// Checks if a position on a planet is underground
        /// </summary>
        public static double GetAltitude(MyPlanet planet, Vector3D position, double altitudeOffset = 0)
        {
            double altitude = (position - planet.WorldMatrix.Translation).Length() + altitudeOffset;

            if (altitude < planet.MinimumRadius || altitude > planet.MaximumRadius)
                return altitude;

            return (altitude - (planet.GetClosestSurfacePointGlobal(position) - planet.WorldMatrix.Translation).Length());
        }

        /// <summary>
        /// Checks if a planet has water
        /// </summary>
        public static bool HasWater(MyPlanet planet)
        {
            return WaterMod.Static.Waters.ContainsKey(planet.EntityId);
        }

        /// <summary>
        /// Checks if a player is in a floating state
        /// </summary>
        public static bool IsPlayerFloating(IMyCharacter player)
        {
            return (player.CurrentMovementState == MyCharacterMovementEnum.Falling || player.CurrentMovementState == MyCharacterMovementEnum.Jump || player.CurrentMovementState == MyCharacterMovementEnum.Flying);
        }

        /// <summary>
        /// Checks if a player is in a floating state
        /// </summary>
        public static bool IsPlayerStateFloating(MyCharacterMovementEnum state)
        {
            return state == MyCharacterMovementEnum.Falling || state == MyCharacterMovementEnum.Jump || state == MyCharacterMovementEnum.Flying;
        }

        public static Vector2 MeasureString(StringBuilder text, float scale, bool useMyRenderGuiConstants = true)
        {
            if (useMyRenderGuiConstants)
                scale *= WaterData.TSSFontScale;
            float pxWidth = 0;
            float maxPxWidth = 0;
            int lines = 1;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                //  New line
                if (c == WaterData.TSSNewLine)
                {
                    lines++;
                    pxWidth = 0;
                    continue;
                }

                //  Because new line
                if (pxWidth > maxPxWidth) maxPxWidth = pxWidth;
            }

            return new Vector2(maxPxWidth * scale, lines * 1 * scale);
        }

        public static double Clamp(double val, double min, double max)
        {
            if (val < min)
                return min;

            if (val > max)
                return max;

            return val;
        }
    }
}
