using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Text;
using System;

namespace MultiplayerRunTime
{
    public class PlayerManagerMP : NetworkBehaviour
    {
        [SerializeField] private NetworkObject[] SpawnableShips;
        private string displayedName;
        private NetworkVariable<NetworkBehaviourReference> shipReference = new();
        //private SpaceshipMP localSpaceship;
        public SpaceshipMP LocalSpaceship
        {
            get { shipReference.Value.TryGet(out SpaceshipMP ship); return ship; }
            private set
            {
                shipReference.Value = value;
                //if (value != null)
                //{
                //    OnShipGained?.Invoke(LocalSpaceship);
                //}
                //else
                //{
                //    OnShipLost?.Invoke();
                //}
            }
        }

        public string DisplayedName
        {
            get
            {
                return displayedName;
            }
            set
            {
                SetDisplayedNameServerRpc(value);
            }
        }

        public delegate void PlayerGainsSpaceship(SpaceshipMP ship);
        public delegate void PlayerLosesSpaceship();
        public PlayerGainsSpaceship OnShipGained;
        public PlayerLosesSpaceship OnShipLost;
        public LocalPlayerManager LPM;

        private void Awake()
        {
            displayedName = string.Empty;
            shipReference.OnValueChanged += ShipRefereceChanged;
        }

        private void ShipRefereceChanged(NetworkBehaviourReference previousValue, NetworkBehaviourReference newValue)
        {
            if (LocalSpaceship != null)
            {
                OnShipGained?.Invoke(LocalSpaceship);
                LocalSpaceship.OnShipDestroyed += HandleShipDestroyed;
            }
        }

        [ServerRpc(Delivery = RpcDelivery.Reliable)]
        private void SetDisplayedNameServerRpc(string name)
        {
            SetDisplayedNameClientRpc(name);
        }

        [ServerRpc(Delivery = RpcDelivery.Reliable)]
        public void SpawnShipServerRpc(Vector3 spawnPos, byte index)
        {
            NetworkObject shipInstance = Instantiate(SpawnableShips[index], spawnPos, Quaternion.identity);

            shipInstance.SpawnWithOwnership(OwnerClientId);
            LocalSpaceship = shipInstance.GetComponent<SpaceshipMP>();
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void SetDisplayedNameClientRpc(string name)
        {
            displayedName = name;
        }

        public void HandleShipDestroyed()
        {
            OnShipLost?.Invoke();
        }
    }
}