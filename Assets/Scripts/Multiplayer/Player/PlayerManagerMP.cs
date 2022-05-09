using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Text;
using System;
using Unity.Mathematics;

namespace MultiplayerRunTime
{
    public class PlayerManagerMP : NetworkBehaviour
    {
        public static bool AllowRespawn = true;
        [SerializeField] private SpawnPoint[] spawnPoints;
        [SerializeField] private NetworkObject[] SpawnableShips;
        [HideInInspector] public LocalPlayerManager LPM;
        [SerializeField] private string displayedName;
        public bool useSpawnPoints = true;
        private NetworkVariable<NetworkBehaviourReference> shipReference = new();

        public SpaceshipMP LocalSpaceship
        {
            get { shipReference.Value.TryGet(out SpaceshipMP ship); return ship; }
            private set
            {
                shipReference.Value = value;
            }
        }

        public bool ShipSpawned
        {
            get
            {
                if (shipReference.Value.TryGet(out _))
                {
                    return true;
                }
                return false;
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
            UITrakcer.GetExistingShips();
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

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        public void SetClientControlsClientRpc(bool enabled)
        {
            InputControl.Singleton.SetFlightEnabled(enabled);
            FindObjectOfType<MouseFlightControllerMP>().isMouseAimFrozen = !enabled;
            LocalSpaceship.Throttle = 0f;
            LocalSpaceship.Kinematic = true;
            LocalSpaceship.Kinematic = false;
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        public void SetClientShipPositionClientRpc(Vector3 position, Vector3 forward)
        {
            if (ShipSpawned)
            {
                
                LocalSpaceship.transform.position = position;
                LocalSpaceship.transform.forward = forward;
                FindObjectOfType<MouseFlightControllerMP>().frozenDirection = forward;
            }
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        public void SetClientCountDownClientRpc(float time )
        {
            UserMenu menu = PasswordLobbyMP.Singleton.menu;
            menu.ShowInfoOverlay(true);
            menu.infoPopUp.UpperLabel = "Starting in...";
            menu.infoPopUp.LowerLabel = ((float)time).ToString("F0");
            if (time <= 3)
            {
                menu.infoPopUp.MakeRed();
            }
            else
            {
                menu.infoPopUp.MakeWhite();
            }
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        public void SetClientLowerLabelClientRpc(string message)
        {
            UserMenu menu = PasswordLobbyMP.Singleton.menu;
            menu.ShowInfoOverlay(true);
            menu.infoPopUp.UpperLabel = "";
            menu.infoPopUp.LowerLabel = message;
            menu.infoPopUp.MakeWhite();
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        public void SetClientLabelsClientRpc(string message1, string message2, bool red = false)
        {
            if(!IsOwner) { return; }
            UserMenu menu = PasswordLobbyMP.Singleton.menu;
            menu.ShowInfoOverlay(true);
            menu.infoPopUp.UpperLabel = message1;
            menu.infoPopUp.LowerLabel = message2;
            if (red)
            {
                menu.infoPopUp.MakeRed();
                return;
            }
            menu.infoPopUp.MakeWhite();
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        public void ResetClientCountDownClientRpc()
        {
            UserMenu menu = PasswordLobbyMP.Singleton.menu;
            menu.ShowInfoOverlay(false);
            menu.infoPopUp.MakeWhite();
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        public void ResetStateClientRpc()
        {
            UserMenu menu = PasswordLobbyMP.Singleton.menu;
            menu.ShowInfoOverlay(false);
            menu.infoPopUp.MakeWhite();

        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        public void AllowRespawnClientRpc(bool respawn)
        {
            AllowRespawn = respawn;

            if (!ShipSpawned && AllowRespawn)
            {
                OnShipLost?.Invoke();
            }
            else if(ShipSpawned && AllowRespawn && IsOwner)
            {
                LocalSpaceship.shipHealthManagerMP.DestroyShipServerRpc();
            }
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
                    return points[UnityEngine.Random.Range(0, points.Count)].Position;
                }
                else
                {
                    return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].Position;
                }
            }
            return Vector3.zero;
        }
    }
}