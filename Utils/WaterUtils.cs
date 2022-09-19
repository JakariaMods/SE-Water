using Jakaria.Components;
using Jakaria.Configs;
using Jakaria.SessionComponents;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Jakaria.Utils
{
    public static class WaterUtils
    {
        private static List<MyEntity> _entities = new List<MyEntity>();

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
        /// Sends a chat message using WaterMod as the sender, not synced
        /// </summary>
        public static void ShowMessage(object message, bool sync = false)
        {
            ShowMessage(message.ToString(), sync);
        }

        /// <summary>
        /// Sends a chat message using WaterMod as the sender, optionally synced
        /// </summary>
        public static void ShowMessage(string message, bool sync = false)
        {
            if (MyAPIGateway.Utilities.IsDedicated && sync)
                MyVisualScriptLogicProvider.SendChatMessage(message, WaterLocalization.ModChatName);
            else
                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, message);

        }

        /// <summary>
        /// Writes a message to the log with the mod prefixed
        /// </summary>
        public static void WriteLog(string message)
        {
            MyLog.Default.WriteLine(WaterLocalization.ModChatName + ": " + message);
        }

        /// <summary>
        /// Returns percentage value
        /// </summary>
        public static float InvLerp(float a, float b, float value)
        {
            return (value - a) / (b - a);
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
        /// Gets a random meteor material name
        /// </summary>
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
        public static bool IsPositionAirtight(ref Vector3D position)
        {
            if (!MyAPIGateway.Session.SessionSettings.EnableOxygenPressurization || !MyAPIGateway.Session.SessionSettings.EnableOxygen)
                return false;

            _entities.Clear();

            BoundingSphereD sphere = new BoundingSphereD(position, 5);
            
            MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref sphere, _entities);
            
            foreach (var entity in _entities)
            {
                IMyCubeGrid grid = entity as IMyCubeGrid;

                if (grid?.GasSystem != null)
                {
                    return grid.IsRoomAtPositionAirtight(grid.WorldToGridInteger(position));
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the closest grid to the position
        /// </summary>
        public static MyCubeGrid GetApproximateGrid(Vector3D position, MyEntityQueryType queryType = MyEntityQueryType.Both)
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
        /// Gets altitude at a position
        /// </summary>
        public static double GetAltitude(MyPlanet planet, Vector3D position, double altitudeOffset = 0)
        {
            if (planet == null)
                return 0;

            double altitude = (position - planet.WorldMatrix.Translation).Length() + altitudeOffset;

            if (altitude < planet.MinimumRadius || altitude > planet.MaximumRadius)
                return altitude;

            return (altitude - (planet.GetClosestSurfacePointGlobal(position) - planet.WorldMatrix.Translation).Length());
        }

        /// <summary>
        /// Gets altitude at a position in local space
        /// </summary>
        public static double GetAltitudeLocal(MyPlanet planet, Vector3D position, double altitudeOffset = 0)
        {
            if (planet == null)
                return 0;

            double altitude = (position).Length() + altitudeOffset;

            if (altitude < planet.MinimumRadius || altitude > planet.MaximumRadius)
                return altitude;

            return (altitude - planet.GetClosestSurfacePointGlobal(position).Length());
        }

        /// <summary>
        /// Checks if a position on a planet is underground
        /// </summary>
        public static double GetAltitudeSquared(MyPlanet planet, Vector3D position)
        {
            double altitude = (position - planet.WorldMatrix.Translation).LengthSquared();

            if (altitude < planet.MinimumRadius * planet.MinimumRadius || altitude > planet.MaximumRadius * planet.MaximumRadius)
                return altitude;

            return (altitude - (planet.GetClosestSurfacePointGlobal(position) - planet.WorldMatrix.Translation).LengthSquared());
        }

        /// <summary>
        /// Gets the maximum depth a character can be before becoming crushed
        /// </summary>
        public static double GetCrushDepth(WaterComponent water, IMyCharacter character)
        {
            if(water != null)
            {
                //pressure = density * gravity * height
                CharacterConfig characterConfig;

                if (character?.Definition?.Id != null && water.Settings.Material != null && water?.Planet?.Generator != null && WaterData.CharacterConfigs.TryGetValue(character.Definition.Id, out characterConfig))
                {
                    if (characterConfig == null)
                        return double.MaxValue;

                    float gravity;
                    MyAPIGateway.Physics.CalculateNaturalGravityAt(character.GetPosition(), out gravity);
                    return (characterConfig.MaximumPressure * 1000) / (water.Settings.Material.Density * gravity);
                }
            }

            return double.MaxValue;
        }

        /// <summary>
        /// Calculates the amount of ticks a block will take before it recalculates buoyancy
        /// </summary>
        public static int CalculateUpdateFrequency(MyCubeGrid grid)
        {
            if (grid.IsStatic)
                return 15;

            int blockCount = grid.BlocksCount;

            if (grid.GridSizeEnum == MyCubeSize.Large)
            {
                if (blockCount < 50)
                    return 2;
                else if (blockCount < 150)
                    return 3;
                else if (blockCount < 500)
                    return 4;

                return (blockCount / 3000) + 4;
            }
            else
            {
                if (blockCount < 50)
                    return 1;
                else if (blockCount < 150)
                    return 2;
                else if (blockCount < 500)
                    return 3;

                return (blockCount / 3000) + 3;
            }
        }

        /// <summary>
        /// Returns random <see cref="Vector3"/> in the range of (0,1)
        /// </summary>
        /// <returns></returns>
        public static Vector3 GetRandomVector3Abs() => new Vector3(MyUtils.GetRandomFloat(0, 1), MyUtils.GetRandomFloat(0, 1), MyUtils.GetRandomFloat(0, 1));

        public static string GetVersionString()
        {
            return WaterData.Version + (WaterData.EarlyAccess ? "EA" : "");
        }

        /*/// <summary>
        /// Calculates maximum height of waves for the given context. https://planetcalc.com/4442/ Atmosphere density is merely an approximiation
        /// </summary>
        public static double GetWaveHeight(MyPlanet planet, Vector3D position, out double windSpeed)
        {
            WaterModComponent modComponent = Session.Instance.Get<WaterModComponent>();

            windSpeed = 0;

            if (planet == null || !planet.Generator.HasAtmosphere)
                return 0;

            double depth = WaterUtils.GetAltitude(planet, position);
            windSpeed = planet.Generator.Atmosphere.MaxWindSpeed * 0.15;

            *//*if (modComponent.RemoteDragAPI != null)
            {
                Vector3D? velocity = modComponent.RemoteDragAPI.GetWind(planet, position, null);

                if (velocity.HasValue)
                    windSpeed = velocity.Value.Length();
            }
            else
            {
                windSpeed *= MyAPIGateway.Session.WeatherEffects.GetWindMultiplier(position);
            }*//*
            
            depth = Math.Max(depth, 3);
            
            return (Math.Min((0.27 * (windSpeed * windSpeed)) / (planet.Generator.SurfaceGravity * 9.81), depth * 0.6) * planet.Generator.Atmosphere.Density);
        }*/
    }
}
