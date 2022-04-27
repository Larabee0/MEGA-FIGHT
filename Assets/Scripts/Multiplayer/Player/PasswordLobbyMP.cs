using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Text;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UNET;
using Unity.Networking.Transport;

namespace MultiplayerRunTime
{
    public class PasswordLobbyMP : MonoBehaviour
    {
        public static PasswordLobbyMP Singleton;
        [HideInInspector] public UserMenu menu;

        public ClientConnects OnClientConnects;
        public ClientConnects OnClientConnectsServer;
        public ClientDisconnects OnClientDisconnects;

        public delegate void ClientConnects(GameObject ClientObject);
        public delegate void ClientDisconnects();

        public RelayHostData hostData;
        public RelayJoinData clientData;

        private NetworkManager networkManager;

        [SerializeField] private GameObject[] ExplsionPrefabs;
        public string JoinCode
        {
            get
            {
                if (!string.IsNullOrEmpty(hostData.JoinCode))
                {
                    return hostData.JoinCode;
                }
                if (!string.IsNullOrEmpty(clientData.JoinCode))
                {
                    return clientData.JoinCode;
                }
                return "";
            }
        }


        private void Awake()
        {
            Singleton = this;
        }

        private void Start()
        {
            networkManager = NetworkManager.Singleton;
            networkManager.OnClientConnectedCallback +=HandleClientConnected;
            networkManager.OnClientDisconnectCallback +=HandleClientDisconnected;
        }

        float timeOutTime = 30f;
        float timeOutCurrnet = 0;
        bool allowTextMod = false;
        private void FixedUpdate()
        {
            if (allowTextMod&&networkManager.IsClient && !(networkManager.IsServer || networkManager.IsHost)&& !networkManager.IsConnectedClient)
            {
                //Debug.LogFormat("No Connection, {0}s till timeout!",(timeOutTime-timeOutCurrnet).ToString("F0"));
                menu.connectionPopUp.JoinCodeText = string.Format("Timing out in {0}s", (timeOutTime - timeOutCurrnet).ToString("F0"));
                timeOutCurrnet += Time.fixedDeltaTime;
            }
            if (allowTextMod&&timeOutCurrnet >= timeOutTime)
            {
                //Debug.LogFormat("Timed out after {0}s", timeOutCurrnet.ToString("F0"));
                menu.connectionPopUp.EnableButtons = true;
                menu.connectionPopUp.JoinCodeText = string.Format("Timed out after {0}s", timeOutCurrnet.ToString("F0"));
                networkManager.Shutdown();
                allowTextMod = false;
                timeOutCurrnet = 0;
            }
        }

        private void OnDestroy()
        {
            if(networkManager == null ) return;
            networkManager.OnClientConnectedCallback -= HandleClientConnected;
            networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        }

        public IEnumerator StartHost()
        {
            if (UnityRelayHandlerV2.IsRelayEnabled)
            {
                Debug.Log("Starting Relay");
                var hostDataTask = UnityRelayHandlerV2.SetupRelay();
                while (!hostDataTask.IsCompleted)
                {
                    yield return null;
                }
                if (hostDataTask.IsFaulted)
                {
                    Debug.LogError("Exception thrown when attempting to start Relay Server. Server not started. Exception: " + hostDataTask.Exception.Message);
                    yield break;
                }
                networkManager.StartHost();
                hostData = hostDataTask.Result;
                menu.spawnPopUp.DisplayJoinCode = JoinCode;
            }
        }

        public IEnumerator StartClient(string joinCode)
        {
            if (UnityRelayHandlerV2.IsRelayEnabled && !string.IsNullOrEmpty(joinCode))
            {
                var clientDataTask = UnityRelayHandlerV2.JoinRelay(joinCode);

                while (!clientDataTask.IsCompleted)
                {
                    yield return null;
                }
                if (clientDataTask.IsFaulted)
                {
                    Debug.LogError("Exception thrown when attempting to join Relay Server. Failed to join. Exception: " + clientDataTask.Exception.Message);
                    yield break;
                }
                networkManager.StartClient();
                clientData = clientDataTask.Result;
                menu.spawnPopUp.DisplayJoinCode = JoinCode;
            }
        }
        
        public void Host(int port = int.MinValue)
        {
            if (UserCustomisableSettings.UseLocal)
            {
                if(port != int.MinValue)
                {
                    UNetTransport transport = networkManager.GetComponent<UNetTransport>();
                    transport.ConnectPort = transport.ServerListenPort = port;
                }
                networkManager.StartHost();
            }
            else
            {
                StopAllCoroutines();
                StartCoroutine(StartHost());
            }
        }

        public void Client(string joinCode)
        {
            allowTextMod = true;
            if (UserCustomisableSettings.UseLocal)
            {
                string[] ipAndPort = joinCode.Split(':');

                UNetTransport transport = networkManager.GetComponent<UNetTransport>();
                transport.ConnectAddress = ipAndPort[0];
                transport.ConnectPort = transport.ServerListenPort = int.Parse(ipAndPort[1]);
                networkManager.StartClient();
            }
            else
            {
                StopAllCoroutines();
                StartCoroutine(StartClient(joinCode));
            }
        }

        public void Leave()
        {

            OnClientDisconnects?.Invoke();
            //Debug.Log("Properly shutting down");
            //menu.ShowConnectionOverlay(true);
            //Cursor.lockState = CursorLockMode.None;
            if (networkManager.IsHost)
            {
                networkManager.Shutdown();
                menu.ShowConnectionOverlay(true);
                Cursor.lockState = CursorLockMode.None;
            }
            else if (networkManager.IsClient)
            {
                networkManager.Shutdown();
                menu.ShowConnectionOverlay(true);
                allowTextMod = false;
                menu.connectionPopUp.JoinCodeText = "";
                Cursor.lockState = CursorLockMode.None;
            }
            clientData = new();
            hostData = new();
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (networkManager.IsClient && networkManager.LocalClientId == clientId)
            {
                menu.ShowConnectionOverlay(false);
                OnClientConnects?.Invoke(networkManager.SpawnManager.GetLocalPlayerObject().gameObject);
            }
            if (networkManager.IsServer || networkManager.IsHost)
            {
                if (!networkManager.gameObject.TryGetComponent<LastManStanding>(out _))
                {
                    networkManager.gameObject.AddComponent<LastManStanding>();
                }
                OnClientConnectsServer?.Invoke(networkManager.SpawnManager.GetPlayerNetworkObject(clientId).gameObject);
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if(networkManager.IsServer && networkManager.LocalClientId != clientId)
            {
                Debug.Log("Non-server shut down");
                return;
            }
            OnClientDisconnects?.Invoke();
            menu.ShowConnectionOverlay(true);
            Debug.Log("Client Properly shutting down");
        }


        public void SpawnExplosion(Vector3 position,Transform parent)
        {
            Instantiate(ExplsionPrefabs[UnityEngine.Random.Range(0, ExplsionPrefabs.Length)], position, Quaternion.identity, parent);
        }
        //private void ApprovalCheck(byte[] connectionData, ulong clientID, NetworkManager.ConnectionApprovedDelegate callback)
        //{
        //    string password = Encoding.ASCII.GetString(connectionData);
        //    Debug.Log(clientID);
        //    Vector3 spawnPos = new(0f, 200f, 100f);
        //    spawnPos.x -= NetworkManager.Singleton.ConnectedClients.Count * 100f;
        //
        //    callback(true, null, password == this.password, spawnPos, null);
        //}
    }
}