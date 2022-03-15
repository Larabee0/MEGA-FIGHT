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
        [SerializeField] private FireControlMP fireControl;
        [SerializeField] private HudMP hud;
        [SerializeField] private PlayerManagerMP PlayerManagerMP;
        [SerializeField] private string displayedName;
        private bool Paused = true;

        public string DisplayedName
        {
            get => displayedName;
            set
            {
                displayedName = value;
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
            fireControl.inputControl = inputControl;
            mouseFlightController.inputControl = inputControl;
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
            Pause();
        }

        public void Pause()
        {
            Paused = !Paused;
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
            Pause();
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
            Debug.Log("Ship lost");
            hud.enabled = false;
            mouseFlightController.enabled = false;
            fireControl.enabled = false;
            enabled = false;
            lobby.menu.ShowSpawnOverlay(true);
        }


        private void SetAndEnableLocalScripts(SpaceshipMP ship)
        {
            mouseFlightController.SetShip(ship);
            fireControl.GetComponentReferences(ship);
            hud.enabled = true;
            mouseFlightController.enabled = true;
            fireControl.enabled = true;
            enabled = true;
        }

        public void Spawn()
        {
            if (PlayerManagerMP != null)
            {
                PlayerManagerMP.SetDisplayedName(displayedName);
                PlayerManagerMP.SpawnShipServerRpc(PlayerManagerMP.transform.position, 0);
            }
        }

        public void Respawn()
        {
            if (PlayerManagerMP != null)
            {
                //Pause();
                PlayerManagerMP.LocalSpaceship.shipHealthManagerMP.DestroyShip();
            }
        }
    }
}