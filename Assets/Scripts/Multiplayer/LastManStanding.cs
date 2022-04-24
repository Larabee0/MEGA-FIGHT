using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MultiplayerRunTime
{
    public enum GameState : byte
    {
        Prepare,
        InGame,
        End
    }

    public class LastManStanding : MonoBehaviour
    {
        private NetworkManager networkManager;
        private NetworkSpawnManager spawnManager;
        private PasswordLobbyMP lobby;

        private float StartDelayTime = 7f;
        private float ResetDelayTime = 15f;

        private SpawnPoint[] spawnPoints;
        private List<PlayerManagerMP> connectedPlayers = new();
        private int spawnedPlayers = 0;
        private bool CanStartGame = false;
        private GameState GameState = GameState.Prepare;

        private void Awake()
        {
            networkManager = NetworkManager.Singleton;
            spawnManager = networkManager.SpawnManager;
            lobby = PasswordLobbyMP.Singleton;
            lobby.OnClientConnectsServer += HandleClientConnect;
            lobby.OnClientDisconnects += HandleClientDisconnect;
        }

        void Start()
        {
            spawnPoints = FindObjectsOfType<SpawnPoint>();
        }

        void Update()
        {
            if (CanStartGame && GameState == GameState.Prepare)
            {
                PreStartGame();
            }
            if (GameState == GameState.InGame && spawnedPlayers == 1)
            {
                EndGame();
            }

            UpdateSpawnedPlayerCount();
        }

        private void ResetGame()
        {
            PlayerManagerMP.AllowRespawn = true;
            connectedPlayers.ForEach((PlayerManagerMP pMMP) => pMMP.AllowRespawnClientRpc(true));
            GameState = GameState.Prepare;
            UpdateSpawnedPlayerCount();
        }

        private void EndGame()
        {
            GameState = GameState.End;
            ShipHealthManagerMP.PVPEnabled = false;
            StartCoroutine(ResetCountDown());
            //Invoke(nameof(ResetGame), ResetDelayTime);
        }

        private void PreStartGame()
        {
            // put all players to random spawn point
            // enable damage model
            // disable player controls
            // count down to start
            SetPlayerControlStates(false);
            SetShipPositions();
            ShipHealthManagerMP.PVPEnabled = true;
            PlayerManagerMP.AllowRespawn = false;
            connectedPlayers.ForEach((PlayerManagerMP pMMP) => pMMP.AllowRespawnClientRpc(false));
            GameState = GameState.InGame;
            StartCoroutine(StartCountDown());
        }

        private void StartGame()
        {
            // unlock player controls
            SetPlayerControlStates(true);
        }

        private void SetPlayerControlStates(bool enabled)
        {
            connectedPlayers.ForEach((PlayerManagerMP PMMP) => PMMP.SetClientControlsClientRpc(enabled));
        }

        private void UpdateSpawnedPlayerCount()
        {
            spawnedPlayers = 0;
            for (int i = 0; i < connectedPlayers.Count; i++)
            {
                if (connectedPlayers[i] == null)
                {
                    connectedPlayers.RemoveAt(i);
                    UpdateSpawnedPlayerCount();
                    return;
                }
                PlayerManagerMP PMMP = connectedPlayers[i];
                //Debug.Log(PMMP.LocalSpaceship);
                spawnedPlayers += PMMP.ShipSpawned ? 1 : 0;

            }

            //connectedPlayers.ForEach((PlayerManagerMP PMMP) => );
            if (spawnedPlayers == connectedPlayers.Count && connectedPlayers.Count > 1)
            {
                CanStartGame = true;
            }
            else
            {
                CanStartGame = false;
            }
        }

        private IEnumerator StartCountDown()
        {
            for (float t = StartDelayTime; t > 0; t--)
            {
                connectedPlayers.ForEach((PlayerManagerMP pMMP) => pMMP.SetClientCountDownClientRpc(t-1));
                yield return new WaitForSeconds(1f);
            }


            connectedPlayers.ForEach((PlayerManagerMP pMMP) => pMMP.SetClientLowerLabelClientRpc("Fight"));
            yield return new WaitForSeconds(0.5f);
            connectedPlayers.ForEach((PlayerManagerMP pMMP)=> pMMP.ResetClientCountDownClientRpc());
            StartGame();
        }

        private IEnumerator ResetCountDown()
        {
            for (float t = ResetDelayTime; t > 0; t--)
            {
                for (int i = 0; i < connectedPlayers.Count; i++)
                {
                    PlayerManagerMP pMMP = connectedPlayers[i];
                    if (pMMP.ShipSpawned)
                    {
                        pMMP.SetClientLabelsClientRpc("Victory!", "You are the last man standing.");
                    }
                    else
                    {
                        pMMP.SetClientLabelsClientRpc("Failure!", string.Format("{0} is the last man standing.", pMMP.DisplayedName));
                    }
                }
                yield return new WaitForSeconds(1f);
            }
            connectedPlayers.ForEach((PlayerManagerMP pMMP) => pMMP.ResetStateClientRpc());
            ResetGame();
        }

        private void HandleClientConnect(GameObject clientRoot)
        {
            PlayerManagerMP player = clientRoot.GetComponent<PlayerManagerMP>();
            connectedPlayers.Add(player);
        }

        private void HandleClientDisconnect()
        {
            for (int i = connectedPlayers.Count - 1; i >= 0; i--)
            {
                if (connectedPlayers[i] == null)
                {
                    connectedPlayers.RemoveAt(i);
                }
            }
        }

        private void SetShipPositions()
        {
            HashSet<SpawnPoint> usedPoints = new();
            for (int i = 0; i < connectedPlayers.Count; i++)
            {
                SpawnPoint point = null;
                while (point == null)
                {
                    point = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
                    if (!usedPoints.Contains(point))
                    {
                        usedPoints.Add(point);
                    }
                    else
                    {
                        point = null;
                    }
                }

                connectedPlayers[i].SetClientShipPositionClientRpc(point.Position, point.Forward);
            }
        }
    }
}