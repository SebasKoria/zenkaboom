using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;

namespace Mirror.Examples.Chat
{
    public class GameManager : NetworkBehaviour
    {
        public static GameManager instance;

        private readonly Player[] players = { null, null, null, null };
        private readonly Vector2[] positions = { new(1, 1), new(1, 11), new(13, 11), new(13, 1) };
        private readonly bool[] playersAlive = { false, false, false, false };

        private bool countdown = false;
        private bool matchStarted = false;
        private bool restartingServer = false;
        private float checkPlayersTimer = 2f;
        private float countdown_timer = 30f;

        public Canvas matchCanvas;
        public TextMeshProUGUI matchCanvasText;
        public GameObject block;

        private List<GameObject> spawnedBlocks = new();

        private string[] grid = {
                    "wwwwwwwwwwwwwww",
                    "wxx.........xxw",
                    "wxw.w.w.w.w.wxw",
                    "w.............w",
                    "w.w.w.w.w.w.w.w",
                    "w.............w",
                    "w.w.w.w.w.w.w.w",
                    "w.............w",
                    "w.w.w.w.w.w.w.w",
                    "w.............w",
                    "wxw.w.w.w.w.wxw",
                    "wxx.........xxw",
                    "wwwwwwwwwwwwwww"
        };

        private void Awake()
        {
            if (instance == null) instance = this;
            checkPlayersTimer = 2f;
        }

        private void Start()
        {
            if (isServer) GenerateMap();
        }

        private void Update()
        {
            if (!isServer) return;

            checkPlayersTimer += Time.deltaTime;
            if (checkPlayersTimer > 1f)
            {
                CheckPlayers();
                checkPlayersTimer = 0f;
            }

            if (countdown)
            {
                countdown_timer -= Time.deltaTime;
                if(countdown_timer <= 0)
                {
                    StartMatch();

                    matchStarted = true;
                    countdown = false;
                }
            }
        }

        private void CheckPlayers()
        {
            if (isServer)
            {
                int cnt = 0;
                for (int i = 0; i < 4; i++)
                {
                    if (players[i] != null) cnt++;
                }

                if (!matchStarted)
                {
                    string matchText = $"{cnt}/2 players connected\n";
                    Color matchColor;
                    if (cnt >= 2)
                    {
                        matchText += $"Match starting in {(int)countdown_timer}...";
                        matchColor = Color.green;
                        countdown = true;
                    }
                    else
                    {
                        matchText += $"Not enough players to begin";
                        matchColor = Color.red;
                        countdown = false;
                        countdown_timer = 30f;
                    }

                    UpdateMatchStartText(matchText, matchColor);
                }
                else VerifyWinCondition();
            }
        }

        private void GenerateMap()
        {
            if (isServer)
            {
                for(int i = 0; i < spawnedBlocks.Count; i++)
                {
                    if(spawnedBlocks[i] != null)
                        NetworkServer.Destroy(spawnedBlocks[i]);
                }
                spawnedBlocks.Clear();

                Debug.Log("===== GENERATED MAP =====");
                for(int i = 0; i < grid.Length; i++)
                {
                    string newLine = "";
                    for(int j = 0; j < grid[i].Length; j++)
                    {
                        if (grid[i][j] == '.')
                        {
                            if (Random.value > 0.2f)
                            {
                                GameObject newBlock = Instantiate(block, new Vector3(j, 12 - i, 0), Quaternion.identity);
                                NetworkServer.Spawn(newBlock);
                                spawnedBlocks.Add(newBlock);
                                newLine += 'b';
                            }
                            else newLine += '.';
                        }
                        else if (grid[i][j] == 'x') newLine += '.';
                        else newLine += grid[i][j];
                    }
                    Debug.Log(newLine);
                }
                Debug.Log("=========================");
            }
        }

        [ClientRpc]
        private void UpdateMatchStartText(string text, Color color)
        {
            matchCanvasText.text = text;
            matchCanvasText.color = color;
            matchCanvas.gameObject.SetActive(true);
        }

        private void VerifyWinCondition()
        {
            if (isServer)
            {
                int cnt = 0;
                for (int i = 0; i < 4; i++)
                {
                    if (players[i] != null && playersAlive[i]) cnt++;
                }

                if (cnt <= 1 && !restartingServer)
                {
                    Debug.Log($"Someone won! who? I don't care, the game has ended");
                    restartingServer = true;
                    StartCoroutine(RestartServer());
                }
                else
                {
                    Debug.Log($"Game Continues, {cnt} players alive");
                }
            }
        }

        [Command(requiresAuthority = false)]
        public void PlayerDie(Player player)
        {
            if (isServer)
            {
                for(int i = 0; i < 4; i++)
                {
                    if (players[i] == player) playersAlive[i] = false;
                }

                DrawPlayerDie(player);
                VerifyWinCondition();
            }
        }

        [ClientRpc]
        public void DrawPlayerDie(Player player)
        {
            Color transparent = new(1, 1, 1, 0);
            player.transform.GetChild(0).GetComponent<SpriteRenderer>().color = transparent;
        }

        private IEnumerator RestartServer()
        {
            yield return new WaitForSeconds(1f);

            for(int i = 0; i < 4; i++)
            {
                if (players[i] != null)
                {
                    TargetRPC_DeactivateMatchCanvas(players[i].GetComponent<NetworkIdentity>().connectionToClient);
                    players[i].Disconnect();
                    players[i] = null;
                }
            }

            GenerateMap();
            matchStarted = false;
            restartingServer = false;
        }

        [Command(requiresAuthority = false)]
        public void AddPlayer(Player player)
        {
            if (isServer)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (players[i] == null)
                    {
                        Debug.Log($"Adding {player.playerName} to list of connected players");

                        players[i] = player;
                        players[i].TeleportTo(positions[i]);
                        playersAlive[i] = true;
                        break;
                    }
                }
            }
        }

        [Command(requiresAuthority = false)]
        public void RemovePlayer(Player player)
        {
            if (isServer)
            {
                bool found = false;
                for(int i = 0; i < 4 && !found; i++)
                {
                    if(players[i] == player)
                    {
                        Debug.Log($"Removing {player.playerName} from list of connected players");
                        found = true;
                        players[i] = null;
                        playersAlive[i] = false;
                        TargetRPC_DeactivateMatchCanvas(player.GetComponent<NetworkIdentity>().connectionToClient);
                        player.Disconnect();
                    }
                }

                if (!found)
                {
                    Debug.Log($"Attempted to remove {player.playerName} from the list but it was not found!!");
                }
            }
        }

        private void StartMatch()
        {
            Debug.Log("StartMatch!");
            RPC_DeactivateMatchCanvas();

            for(int i = 0; i < 4; i++)
            {
                if (players[i] != null) players[i].StartMatch();
                else playersAlive[i] = false;
            }
        }

        [ClientRpc] private void RPC_DeactivateMatchCanvas() => matchCanvas.gameObject.SetActive(false);
        [TargetRpc] public void TargetRPC_DeactivateMatchCanvas(NetworkConnectionToClient conn) => matchCanvas.gameObject.SetActive(false);
    }
}
