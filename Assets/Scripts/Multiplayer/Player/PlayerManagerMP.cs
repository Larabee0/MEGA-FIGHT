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
        [SerializeField] private SpawnPoint[] spawnPoints;
        [SerializeField] private NetworkObject[] SpawnableShips;
        [HideInInspector] public LocalPlayerManager LPM;
        [SerializeField] private string displayedName;
        public bool useSpawnPoints = true;
        private NetworkVariable<NetworkBehaviourReference> shipReference = new();

        public SpaceshipMP LocalSpaceship
        {
            get { shipReference.Value.TryGet(out SpaceshipMP ship); return ship; }
            private set { shipReference.Value = value; }
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
        private ShipUITracking UITrakcer;

        private void Awake()
        {
            displayedName = string.Empty;
            shipReference.OnValueChanged += ShipRefereceChanged;
        }

        private void Start()
        {
            UITrakcer = FindObjectOfType<ShipUITracking>();
            spawnPoints = FindObjectsOfType<SpawnPoint>();

            if (IsHost || IsServer)
            {
                if (useSpawnPoints && spawnPoints != null && spawnPoints.Length != 0)
                {
                    for (int i = 0; i < spawnPoints.Length; i++)
                    {
                        spawnPoints[i].IsServer = true;
                    }
                }
                else
                {
                    Debug.LogWarning("Map is not configured for Spawn Points! Player's will be spawned at world origin!");
                    useSpawnPoints = false;
                }
            }
        }

        private void ShipRefereceChanged(NetworkBehaviourReference previousValue, NetworkBehaviourReference newValue)
        {
            if (LocalSpaceship != null)
            {
                OnShipGained?.Invoke(LocalSpaceship);
                LocalSpaceship.OnShipDestroyed += HandleShipDestroyed;
                UITrakcer.AddName(OwnerClientId, displayedName, LocalSpaceship.shipHealthManagerMP);
            }
        }

        [ServerRpc(Delivery = RpcDelivery.Reliable)]
        private void SetDisplayedNameServerRpc(string name)
        {
            SetDisplayedNameClientRpc(name);
        }

        [ServerRpc(Delivery = RpcDelivery.Reliable)]
        public void SpawnShipServerRpc(byte index)
        {
            NetworkObject shipInstance = Instantiate(SpawnableShips[index], GetSpawnPos(), Quaternion.identity);

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
            UITrakcer.RemoveName(OwnerClientId);
        }

        private Vector3 GetSpawnPos()
        {
            if (useSpawnPoints)
            {
                List<SpawnPoint> points = new();
                for (int i = 0; i < spawnPoints.Length; i++)
                {
                    if (spawnPoints[i].SpawnEmpty)
                    {
                        points.Add(spawnPoints[i]);
                    }
                }
                if(points.Count > 0)
                {
                    return points[UnityEngine.Random.Range(0, points.Count)].transform.position;
                }
                else
                {
                    return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].transform.position;
                }
            }
            return Vector3.zero;
        }
    }
}