using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System;
using System.Text;
using System.Net;

namespace MultiplayerRunTime
{
    public class PasswordLobbyMP : MonoBehaviour
    {
        public static PasswordLobbyMP Singleton;

        public ClientConnects OnClientConnects;
        public ClientDisconnects OnClientDisconnects;

        public delegate void ClientConnects(GameObject ClientObject);
        public delegate void ClientDisconnects();

        [SerializeField] private InputField passwordInputField;
        [SerializeField] private GameObject passwordEntryUI;
        [SerializeField] private GameObject leaveButton;

        private void Awake()
        {
            Singleton = this;
            leaveButton.SetActive(false);
            passwordEntryUI.SetActive(true);
            //Debug.Log(Dns.GetHostAddresses(Dns.GetHostName())[1].ToString());
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
            NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(passwordInputField.text);
            NetworkManager.Singleton.StartClient();
        }

        public void Leave()
        {
            if (NetworkManager.Singleton.IsHost)
            {
                NetworkManager.Singleton.Shutdown();
                NetworkManager.Singleton.ConnectionApprovalCallback-= ApprovalCheck;
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.Shutdown();
            }
            passwordEntryUI.SetActive(true);
            leaveButton.SetActive(false);
        }

        private void HandleClientConnected(ulong clientId)
        {
            if(clientId == NetworkManager.Singleton.LocalClientId)
            {
                passwordEntryUI.SetActive(false);
                leaveButton.SetActive(true);
                OnClientConnects?.Invoke(NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().gameObject);
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                passwordEntryUI.SetActive(true);
                leaveButton.SetActive(false);
                OnClientDisconnects?.Invoke();
            }
        }

        private void ApprovalCheck(byte[] connectionData, ulong clientID, NetworkManager.ConnectionApprovedDelegate callback)
        {
            string password = Encoding.ASCII.GetString(connectionData);
            bool approveConnection = password == passwordInputField.text;

            Vector3 spawnPos = new(0f, 200f, 100f);

            spawnPos.x -= NetworkManager.Singleton.ConnectedClients.Count*100f;
            callback(true, null, approveConnection, spawnPos, null);

        }
    }
}