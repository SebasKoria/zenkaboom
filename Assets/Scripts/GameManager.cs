using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Mirror.Examples.Chat
{
    public class GameManager : NetworkBehaviour
    {
        public static GameManager instance;

        private readonly List<Player> players = new();
        private Player admin = null;
        private float checkPlayersTimer = 0f;

        private void Awake()
        {
            if (instance == null) instance = this;
            checkPlayersTimer = 0f;
        }

        private void Update()
        {
            checkPlayersTimer += Time.deltaTime;
            if (checkPlayersTimer > 1f)
            {
                CheckPlayers();
                checkPlayersTimer = 0f;
            }
        }

        private void CheckPlayers()
        {
            foreach(Player player in players)
            {
                if (player == null) players.Remove(player);
            }
            UpdateAdmin();
        }

        [Command(requiresAuthority = false)]
        public void AddPlayer(Player player)
        {
            if (isServer)
            {
                if (!players.Contains(player))
                {
                    Debug.Log($"Adding {player.playerName} to list of connected players");
                    players.Add(player);
                    UpdateAdmin();
                }
                else
                {
                    Debug.Log($"Player {player.playerName} already connected (?!?!)");
                }
            }
        }

        [Command(requiresAuthority = false)]
        public void RemovePlayer(Player player)
        {
            if (isServer)
            {
                if (players.Contains(player))
                {
                    Debug.Log($"Removing {player.playerName} from list of connected players");
                    players.Remove(player);
                    UpdateAdmin();
                    player.Disconnect();
                }
                else
                {
                    Debug.Log($"Attempted to remove {player.playerName}, but they were not in the list!");
                }
            }
        }

        private void UpdateAdmin()
        {
            if (isServer)
            {
                if (players.Count > 0)
                {
                    if (admin != players[0])
                    {
                        if (admin != null) admin.LeaveAdminRole();
                        players[0].BecomeAdmin();
                        admin = players[0];
                        Debug.Log($"New admin: {players[0].playerName}");
                    }
                }
                else
                {
                    admin = null;
                }
            }
        }
    }
}
