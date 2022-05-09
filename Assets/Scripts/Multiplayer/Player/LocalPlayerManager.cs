using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MultiplayerRunTime 
{
    public class LocalPlayerManager : MonoBehaviour
    {
        [SerializeField] private PasswordLobbyMP lobby;
        [SerializeField] private InputControl inputControl;
        [SerializeField] private MouseFlightControllerMP mouseFlightController;
        [SerializeField] private HudMP hud;
        [SerializeField] private string displayedName;
        public byte ShipPrefabIndex;
        private PlayerManagerMP PlayerManagerMP;
        private bool Paused = true;

        public string DisplayedName
        {
            get => displayedName;
            set
            {
                displayedName = value;
            }
        }

        public SpaceshipMP LocalShip
        {
            get
            {
                if(PlayerManagerMP != null)
                {
                    return PlayerManagerMP.LocalSpaceship;
                }
                return null;
            }
        }

        private void Awake()
        {
            if(mouseFlightController == null)
            {
                Debug.LogWarning("Missing mouseFlightController Reference");
            }
            if (hud == null)
            {
                Debug.LogWarning("Missing HUD Reference");
            }
            lobby.OnClientConnects += OnConnect;
            lobby.OnClientDisconnects += OnDisconnect;
            mouseFlightController.inputControl = inputControl;
            FindObjectOfType<UserMenu>().inputControl = inputControl;
        }

        private void OnEnable()
        {
            inputControl.UIActions.Pause.canceled += PauseCallback;
        }

        private void OnDisable()
        {
            inputControl.UIActions.Pause.canceled -= PauseCallback;
        }

        private void PauseCallback(InputAction.CallbackContext context)
        {
            PauseToggle();
        }

        public void Pause()
        {
            Debug.Log("Pausing!");
            Paused = true;
            SetBasedOffPause();
        }

        public void UnPause()
        {
            Debug.Log("Un-pausing!");
            Paused = false;
            SetBasedOffPause();
        }

        public void PauseToggle()
        {
            switch (Paused)
            {
                case true:
                    UnPause();
                    break;
                case false:
                    Pause();
                    break;
            }
        }

        private void SetBasedOffPause()
        {
            Cursor.lockState = Paused ? CursorLockMode.None : CursorLockMode.Locked;

            lobby.menu.ShowPauseOverlay(Paused);
            inputControl.SetFlightEnabled(!Paused);
        }

        private void OnConnect(GameObject playerObject)
        {
            SetPlayerManagerMP(playerObject.GetComponent<PlayerManagerMP>());
            lobby.menu.ShowSpawnOverlay(true);
        }

        private void OnDisconnect()
        {
            Pause();
            PlayerManagerMP.OnShipGained -= OnShipGained;
            PlayerManagerMP.OnShipLost -= OnShipLost;
            hud.enabled = false;
            mouseFlightController.enabled = false;
            enabled = false;
        }

        public void SetPlayerManagerMP(PlayerManagerMP playerMP)
        {
            PlayerManagerMP = playerMP;
            PlayerManagerMP.OnShipGained += OnShipGained;
            PlayerManagerMP.OnShipLost += OnShipLost;
        }

        private void OnShipGained(SpaceshipMP ship)
        {
            Debug.Log("Ship gained");
            SetAndEnableLocalScripts(ship);
            SetLocalShipPhysicsLayer(ship, 2);
            UnPause();
        }

        private void SetLocalShipPhysicsLayer(SpaceshipMP ship, int layer)
        {
            Collider[] collidables = ship.GetComponentsInChildren<Collider>();
            for (int i = 0; i < collidables.Length; i++)
            {
                collidables[i].gameObject.layer = layer;
            }
        }

        private void OnShipLost()
        {
            Pause();
            Debug.Log("Ship lost");
            hud.enabled = false;
            mouseFlightController.enabled = false;
            enabled = false;
            if (PlayerManagerMP.AllowRespawn)
            {
                lobby.menu.ShowSpawnOverlay(true);
            }
        }


        private void SetAndEnableLocalScripts(SpaceshipMP ship)
        {
            ship.GetComponent<FireControlMP>().SetUp(mouseFlightController);
            mouseFlightController.SetShip(ship);
            hud.enabled = true;
            mouseFlightController.enabled = true;
            enabled = true;
        }

        public void Spawn()
        {
            if (PlayerManagerMP != null)
            {
                PlayerManagerMP.DisplayedName = displayedName;
                PlayerManagerMP.SpawnShipServerRpc(ShipPrefabIndex);
            }
        }

        public void Respawn()
        {
            if (PlayerManagerMP != null)
            {
                PlayerManagerMP.LocalSpaceship.shipHealthManagerMP.DestroyShipServerRpc();
            }
        }
    }
}