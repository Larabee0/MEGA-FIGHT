using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Text;

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
        public string password;

        private void Awake()
        {
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

        public void Host()
        {
            NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
            NetworkManager.Singleton.StartHost();
        }

        public void Client()
        {
            NetworkManager.Singleton.StartClient();
        }

        public void Leave()
        {
            if (NetworkManager.Singleton.IsHost)
            {
                NetworkManager.Singleton.Shutdown();
                menu.ShowConnectionOverlay(true);
                NetworkManager.Singleton.ConnectionApprovalCallback-= ApprovalCheck;
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.Shutdown();
                menu.ShowConnectionOverlay(true);
            }
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

        private void ApprovalCheck(byte[] connectionData, ulong clientID, NetworkManager.ConnectionApprovedDelegate callback)
        {
            string password = Encoding.ASCII.GetString(connectionData);

            Vector3 spawnPos = new(0f, 200f, 100f);
            spawnPos.x -= NetworkManager.Singleton.ConnectedClients.Count * 100f;

            callback(true, null, password == this.password, spawnPos, null);
        }
    }
}