using Jakaria.Utils;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jakaria.Components;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Jakaria.SessionComponents
{
    internal class XboxRenderComponent : SessionComponentBase
    {
        public const string RENDER_ENTITY_PREFIX = "WaterRender_";
        public const float RENDER_ALTITUDE_OFFSET = 100;

        private Dictionary<long, XboxRenderData> _renderData = new Dictionary<long, XboxRenderData>();
        private List<IMyPlayer> players = new List<IMyPlayer>();

        private WaterModComponent _modComponent;

        public override void LoadData()
        {
            _modComponent = Session.Instance.Get<WaterModComponent>();

            MyVisualScriptLogicProvider.PlayerConnected += PlayerConnected;
            MyVisualScriptLogicProvider.PlayerDisconnected += PlayerDisconnected;

            MyAPIGateway.Entities.OnEntityAdd += Entities_OnEntityAdd;
        }

        private void Entities_OnEntityAdd(IMyEntity obj)
        {
            foreach (var data in _renderData)
            {
                if (obj.DisplayName == "XboxWaterPrefab")
                {
                    obj.DisplayName = RENDER_ENTITY_PREFIX + data.Key;

                    data.Value.RenderEntity = obj;

                    IMyCubeGrid igrid = obj as IMyCubeGrid;

                    if (igrid != null)
                    {
                        MyCubeGrid grid = (obj as MyCubeGrid);

                        igrid.IsStatic = false;
                        igrid.Save = false;
                        grid.Flags |= ~EntityFlags.Save;
                        grid.ActivatePhysics();

                        igrid.IsRespawnGrid = true;
                    }

                    break;
                }
            }
        }

        private void PlayerConnected(long playerId)
        {
            WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.WaterModVersion, WaterData.Version + (WaterData.EarlyAccess ? "EA" : "")), true);
        }

        private void PlayerDisconnected(long playerId)
        {
            XboxRenderData data;
            if (_renderData.TryGetValue(playerId, out data))
            {
                if (data.RenderEntity != null)
                    data.RenderEntity.Close();

                _renderData.Remove(playerId);
            }
        }

        public override void UnloadData()
        {
            MyVisualScriptLogicProvider.PlayerConnected -= PlayerConnected;
            MyVisualScriptLogicProvider.PlayerDisconnected -= PlayerDisconnected;

            MyAPIGateway.Entities.OnEntityAdd -= Entities_OnEntityAdd;
        }

        public override void UpdateAfterSimulation()
        {
            players.Clear();
            MyAPIGateway.Multiplayer.Players.GetPlayers(players);

            foreach (var player in players)
            {
                if (!_renderData.ContainsKey(player.IdentityId))
                {
                    MyVisualScriptLogicProvider.SpawnPrefab("XboxWaterPrefab", Vector3D.Zero, Vector3D.Forward, Vector3D.Up, 0, null, null, SpawningOptions.DisableSave);
                    
                    _renderData[player.IdentityId] = new XboxRenderData();
                }
            }

            foreach (var data in _renderData)
            {
                if (data.Value.AttachedEntity == null)
                {
                    foreach (var player in players)
                    {
                        if (player.IdentityId == data.Key)
                        {
                            data.Value.AttachedEntity = player.Character;
                            break;
                        }
                    }
                }

                if (data.Value.RenderEntity == null || data.Value.AttachedEntity == null)
                    continue;

                Vector3D playerPosition = data.Value.AttachedEntity.GetPosition();

                WaterComponent water = _modComponent.GetClosestWater(playerPosition);

                if (water != null)
                {
                    Vector3D up = water.GetUpDirectionGlobal(ref playerPosition);

                    Quaternion orientation;
                    Vector3D surfacePosition;

                    if (water.IsUnderwaterGlobal(ref playerPosition))
                    {
                        orientation = Quaternion.CreateFromForwardUp(Vector3.CalculatePerpendicularVector(up), -up);
                        surfacePosition = water.GetClosestSurfacePointFromNormalLocal(ref up, RENDER_ALTITUDE_OFFSET);
                    }
                    else
                    {
                        orientation = Quaternion.CreateFromForwardUp(Vector3.CalculatePerpendicularVector(up), up);
                        surfacePosition = water.GetClosestSurfacePointFromNormalLocal(ref up, -RENDER_ALTITUDE_OFFSET);
                    }

                    MatrixD matrix = MatrixD.CreateFromTransformScale(orientation, surfacePosition, Vector3D.One);

                    data.Value.RenderEntity.SetWorldMatrix(matrix);

                    if (data.Value.RenderEntity is IMyCubeGrid)
                        ((IMyCubeGrid)data.Value.RenderEntity).IsStatic = !((IMyCubeGrid)data.Value.RenderEntity).IsStatic;
                }
            }
        }
    }
}
