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
        public UserMenu menu;

        public ClientConnects OnClientConnects;
        public ClientDisconnects OnClientDisconnects;

        public delegate void ClientConnects(GameObject ClientObject);
        public delegate void ClientDisconnects();

        public RelayHostData hostData;
        public RelayJoinData clientData;

        RelayUTPHandler relayUTPHandler;

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
            relayUTPHandler = new RelayUTPHandler();
            Singleton = this;
        }

        private void Start()
        {
            NetworkManager.Singleton.OnClientConnectedCallback +=HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback +=HandleClientDisconnected;
        }

        private void Update()
        {
            relayUTPHandler.RelayHandlerUpdate();
        }

        private void OnDestroy()
        {
            if(NetworkManager.Singleton == null ) return;
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
        
        public IEnumerator StartHostService()
        {
            var serverRelayUtilityTask = UnityRelayHandler.AllocateRelayServerAndGetJoinCode(8);
            while (!serverRelayUtilityTask.IsCompleted)
            {
                yield return null;
            }
            if (serverRelayUtilityTask.IsFaulted)
            {
                Debug.LogError("Exception thrown when attempting to start Relay Server. Server not started. Exception: " + serverRelayUtilityTask.Exception.Message);
                yield break;
            }

            var (ipv4address, port, allocationIdBytes, connectionData, key, joinCode) = serverRelayUtilityTask.Result;

            NetworkManager.Singleton.gameObject.GetComponent<UnityTransport>().SetHostRelayData(ipv4address, port, allocationIdBytes, key, connectionData, false);

            NetworkManager.Singleton.StartHost();
            menu.spawnPopUp.DisplayJoinCode = joinCode;
        }

        public IEnumerator StartClientService(string joinCode)
        {
            Debug.Log(joinCode);
            var clientRelayUtilityTask = UnityRelayHandler.JoinRelayServerFromJoinCode(joinCode);

            while (!clientRelayUtilityTask.IsCompleted)
            {
                yield return null;
            }

            if (clientRelayUtilityTask.IsFaulted)
            {
                Debug.LogError("Exception thrown when attempting to connect to Relay Server. Exception: " + clientRelayUtilityTask.Exception.Message);
                yield break;
            }

            var (ipv4address, port, allocationIdBytes, connectionData, hostConnectionData, key) = clientRelayUtilityTask.Result;


            NetworkManager.Singleton.gameObject.GetComponent<UnityTransport>().SetClientRelayData(ipv4address, port, allocationIdBytes, key, connectionData, hostConnectionData, true);


            NetworkManager.Singleton.StartClient();
            menu.spawnPopUp.DisplayJoinCode = JoinCode;
        }

        public void Host()
        {
            StopAllCoroutines();
            StartCoroutine(relayUTPHandler.StartRelayServer(8));
        }

        public void Client(string joinCode)
        {
            StopAllCoroutines();
            StartCoroutine(relayUTPHandler.StartClient(joinCode));
        }

        public void Leave()
        {
            if (NetworkManager.Singleton.IsHost)
            {
                NetworkManager.Singleton.Shutdown();
                menu.ShowConnectionOverlay(true);
                Cursor.lockState = CursorLockMode.None;
                //NetworkManager.Singleton.ConnectionApprovalCallback-= ApprovalCheck;
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.Shutdown();
                menu.ShowConnectionOverlay(true);
                Cursor.lockState = CursorLockMode.None;
            }
            clientData = new();
            hostData = new();
        }

        private void HandleClientConnected(ulong clientId)
        {
            if(clientId == NetworkManager.Singleton.LocalClientId)
            {
                menu.ShowConnectionOverlay(false);
                OnClientConnects?.Invoke(NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().gameObject);
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                OnClientDisconnects?.Invoke();
                menu.ShowConnectionOverlay(true);
            }
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