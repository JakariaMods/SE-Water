using Jakaria.Configs;
using Jakaria.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Jakaria.SessionComponents
{
    public class WaterRespawnPodComponent : SessionComponentBase
    {
        public WaterModComponent _modComponent;

        public static WaterRespawnPodComponent Static;

        public WaterRespawnPodComponent()
        {
            Static = this;
        }

        public override void LoadDependencies()
        {
            _modComponent = WaterModComponent.Static;
        }

        public override void UnloadDependencies()
        {
            Static = null;
        }

        public override void LoadData()
        {
            MyVisualScriptLogicProvider.RespawnShipSpawned += RespawnShipSpawned;
        }

        public override void UnloadData()
        {
            MyVisualScriptLogicProvider.RespawnShipSpawned -= RespawnShipSpawned;
        }

        private void RespawnShipSpawned(long shipEntityId, long playerId, string respawnShipPrefabName)
        {
            IMyCubeGrid grid = MyEntities.GetEntityById(shipEntityId) as IMyCubeGrid;

            if (grid != null)
            {
                MyPlanet planet = MyGamePruningStructure.GetClosestPlanet(grid.GetPosition());
                Water water = _modComponent.GetClosestWater(grid.GetPosition());
                WaterUtils.ShowMessage(respawnShipPrefabName);
                RespawnPodConfig config;
                if (water != null && planet != null && grid.Physics != null && WaterData.RespawnPodConfigs.TryGetValue(MyDefinitionId.Parse("RespawnShipDefinition/" + respawnShipPrefabName), out config))
                {
                    if (config.SpawnOnLand == config.SpawnOnWater)
                        return;

                    for (int i = 0; i < WaterData.MaxRespawnIterations; i++)
                    {
                        Vector3D normal = MyUtils.GetRandomVector3Normalized();
                        Vector3D closestPlanetPosition = planet.GetClosestSurfacePointGlobal(water.Position + (normal * planet.AverageRadius));
                        if (water.IsUnderwater(ref closestPlanetPosition))
                        {
                            if (config.SpawnOnWater)
                            {
                                Vector3D closestWaterPoint = water.GetClosestSurfacePointGlobal(closestPlanetPosition, config.SpawnAltitude);
                                if (!planet.IsUnderGround(closestWaterPoint))
                                {
                                    MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                                    {
                                        Quaternion orientation = Quaternion.CreateFromForwardUp(MyUtils.GetRandomPerpendicularVector(normal), normal);
                                        MatrixD matrix = MatrixD.CreateFromTransformScale(orientation, closestWaterPoint, Vector3D.One);
                                        grid.Teleport(matrix);
                                        grid.Physics.LinearVelocity = Vector3.Zero;
                                        grid.Physics.AngularVelocity = Vector3.Zero;
                                        grid.WorldMatrix = matrix;
                                    });
                                    return;
                                }
                            }
                        }
                        else
                        {
                            if (config.SpawnOnLand)
                            {
                                MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                                {
                                    Quaternion orientation = Quaternion.CreateFromForwardUp(MyUtils.GetRandomPerpendicularVector(normal), normal);
                                    MatrixD matrix = MatrixD.CreateFromTransformScale(orientation, planet.GetClosestSurfacePointGlobal(closestPlanetPosition) + (normal * Math.Abs(config.SpawnAltitude)), Vector3D.One);
                                    grid.Teleport(matrix);
                                    grid.Physics.LinearVelocity = Vector3.Zero;
                                    grid.Physics.AngularVelocity = Vector3.Zero;
                                    grid.WorldMatrix = matrix;
                                });
                                return;
                            }
                        }

                        if (i == WaterData.MaxRespawnIterations - 1)
                        {
                            WaterUtils.WriteLog("Could not find suitable respawn area for '" + respawnShipPrefabName + "'");
                        }
                    }
                }
            }
        }
    }
}
