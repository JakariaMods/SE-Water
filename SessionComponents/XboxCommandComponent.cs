using Jakaria.Components;
using Jakaria.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRageMath;

namespace Jakaria.SessionComponents
{
    internal class XboxCommandComponent : SessionComponentBase
    {
        private WaterModComponent _modComponent;

        public override void LoadData()
        {
            _modComponent = Session.Instance.Get<WaterModComponent>();

            MyAPIGateway.Utilities.MessageEntered += Utilities_MessageEntered;
            MyAPIGateway.Utilities.MessageRecieved += Utilities_MessageRecieved;
        }

        private void Utilities_MessageEntered(string messageText, ref bool sendToOthers)
        {
            Utilities_MessageRecieved(MyAPIGateway.Session.Player.SteamUserId, messageText);
        }

        private void Utilities_MessageRecieved(ulong sender, string messageText)
        {
            if (MyAPIGateway.Session.IsUserAdmin(sender))
            {
                if (!messageText.StartsWith("/") || messageText.Length == 0)
                    return;

                string[] args = messageText.TrimStart('/').Split(' ');

                if (args.Length == 0)
                    return;

                IMyPlayer player = null;
                List<IMyPlayer> players = new List<IMyPlayer>();
                MyAPIGateway.Multiplayer.Players.GetPlayers(players);

                foreach (var tplayer in players)
                {
                    if (tplayer.SteamUserId == sender)
                    {
                        player = tplayer;
                        break;
                    }
                }

                if (player == null)
                    return;

                if (args[0] == "wcreate")
                    CreateWater(player, args);
                if (args[0] == "wremove")
                    RemoveWater(player, args);
                if (args[0] == "wradius")
                    Radius(player, args);
            }
        }

        private void CreateWater(IMyPlayer player, string[] args)
        {
            IMyCharacter character = player.Character;

            if (character == null)
                return;

            Vector3D position = character.GetPosition();
            MyPlanet planet = MyGamePruningStructure.GetClosestPlanet(position);

            if (!planet.Components.Has<WaterComponent>())
            {
                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.HasWater, true);
                return;
            }

            if (args.Length == 1)
            {
                WaterComponent water = planet.Components.Get<WaterComponent>();
                water.Settings.WaveHeight = 0;
                water.Settings.TideHeight = 0;

                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.CreateWater, true);
            }

            if (args.Length == 2)
            {
                float radius;
                if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out radius))
                {
                    WaterComponent water = planet.Components.Get<WaterComponent>();
                    water.Settings.WaveHeight = 0;
                    water.Settings.TideHeight = 0;

                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.CreateWater, true);
                }
                else
                {
                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoParse, true);
                }
            }
        }

        private void Radius(IMyPlayer player, string[] args)
        {
            IMyCharacter character = player.Character;

            if (character == null)
                return;

            Vector3D position = character.GetPosition();
            MyPlanet planet = MyGamePruningStructure.GetClosestPlanet(position);

            if (planet.Components.Has<WaterComponent>())
            {
                WaterComponent water = planet.Components.Get<WaterComponent>();

                if (water == null)
                    return;

                //Get Radius
                if (args.Length == 1)
                {
                    WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.GetRadius, (water.Radius / water.Planet.MinimumRadius)), true);
                }

                //Set Radius
                if (args.Length == 2)
                {
                    float radius;
                    if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out radius))
                    {
                        radius = MathHelper.Clamp(radius, 0.95f, 1.75f);

                        water.Radius = radius * water.Planet.MinimumRadius;

                        WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetRadius, (water.Radius / water.Planet.MinimumRadius)), true);
                    }
                    else
                        WaterUtils.ShowMessage(string.Format(WaterLocalization.CurrentLanguage.SetRadiusNoParse, args[1]), true);
                }
            }
            else
            {
                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoWater, true);
            }
        }

        private void RemoveWater(IMyPlayer player, string[] args)
        {
            IMyCharacter character = player.Character;

            if (character == null)
                return;

            Vector3D position = character.GetPosition();
            MyPlanet planet = MyGamePruningStructure.GetClosestPlanet(position);

            if (planet != null)
            {
                if(_modComponent.RemoveWater(planet))
                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.RemoveWater, true);
            }
        }

        public override void UnloadData()
        {
            MyAPIGateway.Utilities.MessageEntered -= Utilities_MessageEntered;
            MyAPIGateway.Utilities.MessageRecieved -= Utilities_MessageRecieved;
        }
    }
}
