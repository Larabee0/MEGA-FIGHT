using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Text;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UNET;

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
            //Allocation allocation = await Relay.Instance.CreateAllocationAsync(8);
            Singleton = this;
        }

        private void Start()
        {
            NetworkManager.Singleton.OnClientConnectedCallback +=HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback +=HandleClientDisconnected;
        }

        private void OnDestroy()
        {
            if(NetworkManager.Singleton == null ) return;
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
        
        public IEnumerator StartHostService()
        {
            var task = UnityRelayHandler.HostGame(8);
            while (!task.IsCompleted)
            {
                yield return null;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("Exception thrown when attempting to start Relay Server.Server not started.Exception: " + task.Exception.Message);
                yield break;
            }
            hostData = task.Result;
            NetworkManager.Singleton.gameObject.GetComponent<UnityTransport>().SetRelayServerData(
                hostData.IPv4Address, hostData.Port, hostData.AllocationIDBytes, hostData.Key, hostData.ConnectionData);
            NetworkManager.Singleton.StartHost();
            menu.spawnPopUp.DisplayJoinCode = JoinCode;
        }

        public IEnumerator StartClientService(string joinCode)
        {
            Debug.Log(joinCode);
            var task = UnityRelayHandler.JoinGame(joinCode);
            while (!task.IsCompleted)
            {
                yield return null;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("Exception thrown when attempting to start Relay Server.Server not started.Exception: " + task.Exception.Message);
                yield break;
            }
            clientData = task.Result;
            NetworkManager.Singleton.gameObject.GetComponent<UnityTransport>().SetRelayServerData(
                clientData.IPv4Address, clientData.Port, clientData.AllocationIDBytes, clientData.Key, clientData.ConnectionData, clientData.HostConnectionData);
            NetworkManager.Singleton.StartClient();
            menu.spawnPopUp.DisplayJoinCode = JoinCode;
        }

        public void Host()
        {
            StopAllCoroutines();
            StartCoroutine(StartHostService());
        }

        public void Client(string joinCode)
        {
            StopAllCoroutines();
            StartCoroutine(StartClientService(joinCode));
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