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
        private bool useSpawnPoints = true;
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
                if (spawnPoints == null)
                {
                    useSpawnPoints = false;
                    Debug.LogWarning("Missing Spawn Points! Player's Will be spawned at World Origin");
                }

                if (useSpawnPoints)
                {
                    for (int i = 0; i < spawnPoints.Length; i++)
                    {
                        spawnPoints[i].IsServer = true;
                    }
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
                List<SpawnPoint> points = new List<SpawnPoint>();
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