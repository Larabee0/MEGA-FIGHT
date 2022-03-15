using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Text;

namespace MultiplayerRunTime
{
    public class PlayerManagerMP : NetworkBehaviour
    {
        [SerializeField] private NetworkObject[] ships;
        private NetworkList<byte> displayedName;
        private SpaceshipMP localSpaceship;
        public SpaceshipMP LocalSpaceship
        {
            get => localSpaceship;
            set
            {
                localSpaceship = value;
                if (value != null)
                {
                    OnShipGained?.Invoke(localSpaceship);
                }
                else
                {
                    OnShipLost?.Invoke();
                }
            }
        }

        public string DisplayedName
        {
            get
            {
                byte[] instigatorByteArray = new byte[displayedName.Count];
                for (int i = 0; i < displayedName.Count; i++)
                {
                    instigatorByteArray[i] = displayedName[i];
                }
                return Encoding.ASCII.GetString(instigatorByteArray);
            }
        }

        public delegate void PlayerGainsSpaceship(SpaceshipMP ship);
        public delegate void PlayerLosesSpaceship();
        public PlayerGainsSpaceship OnShipGained;
        public PlayerLosesSpaceship OnShipLost;
        public LocalPlayerManager LPM;

        private void Awake()
        {
            displayedName = new();
        }

        public void SetDisplayedName(string name)
        {
            SetDisplayedNameServerRpc(Encoding.ASCII.GetBytes(name));
        }

        [ServerRpc(Delivery = RpcDelivery.Reliable)]
        private void SetDisplayedNameServerRpc(byte[] name)
        {
            displayedName = new NetworkList<byte>(name);
        }

        [ServerRpc(Delivery = RpcDelivery.Reliable)]
        public void SpawnShipServerRpc(Vector3 spawnPos, byte index)
        {
            NetworkObject shipInstance = Instantiate(ships[index], spawnPos, Quaternion.identity);

            shipInstance.SpawnWithOwnership(OwnerClientId);
            SetShipReferenceClientRpc(shipInstance.GetComponent<SpaceshipMP>(), this);
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void SetShipReferenceClientRpc(NetworkBehaviourReference Ship, NetworkBehaviourReference Player)
        {
            if(Ship.TryGet(out SpaceshipMP spaceship) && Player.TryGet(out PlayerManagerMP player))
            {
                player.localSpaceship = spaceship;
            }
        }

        public void HandleShipDestroyed()
        {
            LocalSpaceship = null;
        }
    }
}