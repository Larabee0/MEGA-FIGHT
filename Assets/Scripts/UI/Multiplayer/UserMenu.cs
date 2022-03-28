using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace MultiplayerRunTime
{
    public class UserMenu : MonoBehaviour
    {
        public enum OnCloseOpenWindow : byte
        {
            ConnectionPopUp,
            PausePopUp,
            SpawnPopUp,
            SettingsPopUp
        }

        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private PasswordLobbyMP lobby;
        [SerializeField] private LocalPlayerManager localPlayerManager;
        private UIDocument document;
        private VisualElement rootVisualElement;
        private VisualElement overlay;
        public InputControl inputControl;

        public ConnectionPopUp connectionPopUp;
        public PausePopUp pausePopUp;
        public SpawnPopUp spawnPopUp;
        public SettingsPopUp settingsPopUp;

        private void Awake()
        {
            document = GetComponent<UIDocument>();
            if (document == null)
            {
                Debug.LogError("No UI Document assgined;");
                return;
            }
            lobby.menu = this;
            rootVisualElement = document.rootVisualElement;
            overlay = rootVisualElement.Q("overlay");

        }

        private void Start()
        {
            connectionPopUp = new ConnectionPopUp(this, rootVisualElement.Q("ConnectionPopUp"));
            pausePopUp = new PausePopUp(this, rootVisualElement.Q("PausePopUp"));
            spawnPopUp = new SpawnPopUp(this, rootVisualElement.Q("SpawnPopUp"));
            settingsPopUp = new SettingsPopUp(this, rootVisualElement.Q("SettingsPopUp"));
            ShowConnectionOverlay(true);
            SetSettings();
            UserCustomisableSettings.instance.OnUserSettingsChanged += SetSettings;
        }

        public InGameInfo GetInGameInfo(MouseFlightControllerMP mFCMP)
        {
            return new InGameInfo(mFCMP, rootVisualElement.Q("InGameInfo"));
        }

        public void SetMenuSelection()
        {
            inputControl.SetUIEnabled(true);
            FindObjectOfType<EventSystem>().SetSelectedGameObject(FindObjectOfType<PanelEventHandler>().gameObject);
        }

        private void ShowOverlay(bool shown)
        {
            switch (shown)
            {
                case true:
                    overlay.style.display = DisplayStyle.Flex;
                    break;
                case false:
                    //inputControl.SetUIEnabled(false);
                    overlay.style.display = DisplayStyle.None;
                    break;
            }
        }

        public void ShowConnectionOverlay(bool shown, bool BackgroundTo = true)
        {
            switch (shown)
            {
                case true:
                    SetMenuSelection();
                    connectionPopUp.rootVisualElement.style.display = DisplayStyle.Flex;
                    ShowPauseOverlay(false, false);
                    ShowSpawnOverlay(false, false);
                    ShowSettingsOverlay(false, false);
                    break;
                case false:
                    connectionPopUp.rootVisualElement.style.display = DisplayStyle.None;
                    break;
            }
            if (BackgroundTo) ShowOverlay(shown);
        }

        public void ShowPauseOverlay(bool shown, bool BackgroundTo = true)
        {
            switch (shown)
            {
                case true:
                    SetMenuSelection();
                    pausePopUp.rootVisualElement.style.display = DisplayStyle.Flex;
                    ShowConnectionOverlay(false, false);
                    ShowSpawnOverlay(false, false);
                    ShowSettingsOverlay(false, false);
                    break;
                case false:
                    pausePopUp.rootVisualElement.style.display = DisplayStyle.None;
                    break;
            }
            if (BackgroundTo) ShowOverlay(shown);
        }

        public void ShowSpawnOverlay(bool shown, bool BackgroundTo = true)
        {
            switch (shown)
            {
                case true:
                    SetMenuSelection();
                    spawnPopUp.rootVisualElement.style.display = DisplayStyle.Flex;
                    ShowConnectionOverlay(false, false);
                    ShowPauseOverlay(false, false);
                    ShowSettingsOverlay(false, false);
                    break;
                case false:
                    spawnPopUp.rootVisualElement.style.display = DisplayStyle.None;
                    break;
            }
            if (BackgroundTo) ShowOverlay(shown);
        }

        public void ShowSettingsOverlay(bool shown, bool BackgroundTo = true)
        {
            switch (shown)
            {
                case true:
                    SetMenuSelection();
                    settingsPopUp.rootVisualElement.style.display = DisplayStyle.Flex;
                    ShowConnectionOverlay(false, false);
                    ShowPauseOverlay(false, false);
                    ShowSpawnOverlay(false, false);
                    break;
                case false:
                    settingsPopUp.rootVisualElement.style.display = DisplayStyle.None;
                    break;
            }
            if (BackgroundTo) ShowOverlay(shown);
        }

        public void MakeTextFieldWhite(TextField field)
        {
            VisualElement element = field.Q<VisualElement>("unity-text-input");
            StyleColor textColor = element.style.color;
            textColor.value = Color.white;
            element.style.color = textColor;
        }

        public void MakeTextFieldRed(TextField field)
        {
            VisualElement element = field.Q<VisualElement>("unity-text-input");
            StyleColor textColor = element.style.color;
            textColor.value = Color.red;
            element.style.color = textColor;
        }

        private void SetSettings()
        {
            UserCustomisableSettings userSettings = UserCustomisableSettings.instance;
            Cinemachine.CinemachineDollyCart camCart = FindObjectOfType<Cinemachine.CinemachineDollyCart>();
            camCart.enabled = !userSettings.userSettings.DisableFlyAroundCamera;//21000
            if (userSettings.userSettings.DisableFlyAroundCamera)
            {
                camCart.m_Position = 21000;
            }
        }

        public class ConnectionPopUp
        {
            private readonly UserMenu menu;
            public readonly VisualElement rootVisualElement;

            private readonly Button QuitButton;
            private readonly Button ConnectButton;
            private readonly Button HostButton;
            private readonly Button SettingsButton;

            private readonly TextField JoinCodeTextField;

            public ConnectionPopUp(UserMenu Menu, VisualElement RootVisualElement)
            {
                menu = Menu;
                rootVisualElement = RootVisualElement;
                QuitButton = rootVisualElement.Q<Button>("QuitGameButton");
                ConnectButton = rootVisualElement.Q<Button>("ClientConnectButton");
                HostButton = rootVisualElement.Q<Button>("HostStartButton");
                SettingsButton = rootVisualElement.Q<Button>("SettingsButton");
                JoinCodeTextField = rootVisualElement.Q<TextField>("JoinCode");

                QuitButton.RegisterCallback<ClickEvent>(ev => OnQuitCallback());
                HostButton.RegisterCallback<ClickEvent>(ev => OnHostCallback());
                ConnectButton.RegisterCallback<ClickEvent>(ev => OnConnectCallback());
                SettingsButton.RegisterCallback<ClickEvent>(ev => ShowSettings());

                QuitButton.RegisterCallback<NavigationSubmitEvent>(ev => OnQuitCallback());
                HostButton.RegisterCallback<NavigationSubmitEvent>(ev => OnHostCallback());
                ConnectButton.RegisterCallback<NavigationSubmitEvent>(ev => OnConnectCallback());
                SettingsButton.RegisterCallback<NavigationSubmitEvent>(ev => ShowSettings());
            }

            private void OnQuitCallback()
            {
                Application.Quit();
            }

            private void OnHostCallback()
            {
                menu.lobby.Host();
            }

            private void OnConnectCallback()
            {
                menu.lobby.Client(JoinCodeTextField.value);
            }

            private void ShowSettings()
            {
                menu.settingsPopUp.onCloseOpenWindow = OnCloseOpenWindow.ConnectionPopUp;
                menu.ShowSettingsOverlay(true);
            }
        }

        public class PausePopUp
        {
            private readonly UserMenu menu;
            public readonly VisualElement rootVisualElement;

            private readonly Button ResumeButton;
            private readonly Button RespawnButton;
            private readonly Button MainMenuButton;
            private readonly Button CloseGameButton;
            private readonly Button SettingsButton;

            public PausePopUp(UserMenu Menu, VisualElement RootVisualElement)
            {
                menu = Menu;
                rootVisualElement = RootVisualElement;

                ResumeButton = rootVisualElement.Q<Button>("ResumeGameButton");
                RespawnButton = rootVisualElement.Q<Button>("RespawnButton");
                MainMenuButton = rootVisualElement.Q<Button>("MainMenuButton");
                CloseGameButton = rootVisualElement.Q<Button>("QuitGameButton");
                SettingsButton = rootVisualElement.Q<Button>("SettingsButton");

                ResumeButton.RegisterCallback<ClickEvent>(ev => ResumeButtonCallback());
                RespawnButton.RegisterCallback<ClickEvent>(ev => RespawnButtonCallback());
                MainMenuButton.RegisterCallback<ClickEvent>(ev => MainMenuButtonCallback());
                CloseGameButton.RegisterCallback<ClickEvent>(ev => CloseGameButtonCallback());
                SettingsButton.RegisterCallback<ClickEvent>(ev => ShowSettings());

                ResumeButton.RegisterCallback<NavigationSubmitEvent>(ev => ResumeButtonCallback());
                RespawnButton.RegisterCallback<NavigationSubmitEvent>(ev => RespawnButtonCallback());
                MainMenuButton.RegisterCallback<NavigationSubmitEvent>(ev => MainMenuButtonCallback());
                CloseGameButton.RegisterCallback<NavigationSubmitEvent>(ev => CloseGameButtonCallback());
                SettingsButton.RegisterCallback<NavigationSubmitEvent>(ev => ShowSettings());
            }

            private void ResumeButtonCallback()
            {
                menu.localPlayerManager.UnPause();
            }

            private void RespawnButtonCallback()
            {
                menu.localPlayerManager.Respawn();
            }

            private void MainMenuButtonCallback()
            {
                menu.lobby.Leave();
            }

            private void CloseGameButtonCallback()
            {
                menu.lobby.Leave();
                Application.Quit();
            }

            private void ShowSettings()
            {
                menu.settingsPopUp.onCloseOpenWindow = OnCloseOpenWindow.PausePopUp;
                menu.ShowSettingsOverlay(true);
            }
        }

        public class SpawnPopUp
        {
            private readonly UserMenu menu;
            public readonly VisualElement rootVisualElement;

            private readonly Button SpawnButton;
            private readonly Button LeaveButton;
            private readonly Button QuitGameButton;
            private readonly Button SettingsButton;

            private readonly Label JoinCodeLabel;
            private readonly TextField DisplayedNameTextField;

            public string DisplayedNameOut { set => DisplayedNameTextField.value = value; }

            public string DisplayJoinCode { set => JoinCodeLabel.text = string.Format("Join Code: {0}", value); }

            public SpawnPopUp(UserMenu Menu, VisualElement RootVisualElement)
            {
                menu = Menu;
                rootVisualElement = RootVisualElement;

                SpawnButton = rootVisualElement.Q<Button>("SpawnButton");
                LeaveButton = rootVisualElement.Q<Button>("LeaveButton");
                QuitGameButton = rootVisualElement.Q<Button>("QuitGameButton");
                SettingsButton = rootVisualElement.Q<Button>("SettingsButton");

                JoinCodeLabel = rootVisualElement.Q<Label>("JoinCodeDisplay");

                DisplayedNameTextField = rootVisualElement.Q<TextField>("DisplayedName");

                SpawnButton.RegisterCallback<ClickEvent>(ev => SpawnButtonCallback());
                LeaveButton.RegisterCallback<ClickEvent>(ev => LeaveButtonCallback());
                QuitGameButton.RegisterCallback<ClickEvent>(ev => QuitGameButtonCallback());
                SettingsButton.RegisterCallback<ClickEvent>(ev => ShowSettings());

                SpawnButton.RegisterCallback<NavigationSubmitEvent>(ev => SpawnButtonCallback());
                LeaveButton.RegisterCallback<NavigationSubmitEvent>(ev => LeaveButtonCallback());
                QuitGameButton.RegisterCallback<NavigationSubmitEvent>(ev => QuitGameButtonCallback());
                SettingsButton.RegisterCallback<NavigationSubmitEvent>(ev => ShowSettings());
            }

            private void SpawnButtonCallback()
            {
                menu.localPlayerManager.DisplayedName = DisplayedNameTextField.value;
                menu.localPlayerManager.Spawn();
                menu.ShowSpawnOverlay(false);
            }

            private void LeaveButtonCallback()
            {
                menu.lobby.Leave();
            }

            private void QuitGameButtonCallback()
            {
                menu.lobby.Leave();
                Application.Quit();
            }

            private void ShowSettings()
            {
                menu.settingsPopUp.onCloseOpenWindow = OnCloseOpenWindow.SpawnPopUp;
                menu.ShowSettingsOverlay(true);
            }
        }

        public class InGameInfo
        {
            private readonly MouseFlightControllerMP controller;
            private readonly VisualElement rootVisualElement;

            private readonly Label ThrustLabel;
            private readonly Label SpeedLabel;
            private readonly Label AltitudeLabel;

            public float Thrust
            {
                set
                {
                    ThrustLabel.text = (value).ToString("F0");
                }
            }

            public float Speed
            {
                set
                {
                    SpeedLabel.text = (value * 1.8f).ToString("F0");
                }
            }

            public float Altitude
            {
                set
                {
                    AltitudeLabel.text = (value).ToString("F0");
                }
            }

            public InGameInfo(MouseFlightControllerMP Controller, VisualElement RootVisualElement)
            {
                controller = Controller;
                rootVisualElement = RootVisualElement;

                ThrustLabel = rootVisualElement.Q<Label>("THRValue");
                SpeedLabel = rootVisualElement.Q<Label>("SPDValue");
                AltitudeLabel = rootVisualElement.Q<Label>("ALTValue");
            }
        }

        public class SettingsPopUp
        {
            private readonly UserCustomisableSettings userSettings;
            private readonly UserMenu menu;
            public readonly VisualElement rootVisualElement;

            private readonly TextField PlayerDisplayedName;
            private readonly Toggle DisableFlyAroundCamera;
            private readonly Toggle DefaultFlightCamera;
            private readonly Slider ThirdPersonCamSpeed;
            private readonly Slider MouseFlightTargetSens;
            private readonly Slider DefaultAimDistance;
            private readonly Slider AimDistanceSens;

            private readonly Label ThirdPersonCamSpeedValue;
            private readonly Label MouseFlightTargetSensValue;
            private readonly Label DefaultAimDst;
            private readonly Label AimDstValue;

            private readonly Button SaveAndCloseButton;
            private readonly Button CloseButton;
            private readonly Button ResetButton;

            public OnCloseOpenWindow onCloseOpenWindow = OnCloseOpenWindow.SettingsPopUp;

            public SettingsPopUp(UserMenu Menu, VisualElement RootVisualElement)
            {
                menu = Menu;
                userSettings = UserCustomisableSettings.instance;
                rootVisualElement = RootVisualElement;

                PlayerDisplayedName = rootVisualElement.Q<TextField>("PlayerDisplayedName");

                DisableFlyAroundCamera = rootVisualElement.Q<Toggle>("DisableFlyAroundCamera");
                DefaultFlightCamera = rootVisualElement.Q<Toggle>("DefaultFlightCamera");

                ThirdPersonCamSpeed = rootVisualElement.Q<Slider>("ThirdPersonCamSpeed");
                MouseFlightTargetSens = rootVisualElement.Q<Slider>("MouseFlightTargetSens");
                DefaultAimDistance = rootVisualElement.Q<Slider>("DefaultAimDistance");
                AimDistanceSens = rootVisualElement.Q<Slider>("AimDistanceSens");

                ThirdPersonCamSpeedValue = rootVisualElement.Q<Label>("ThirdPersonCamSpeedValue");
                MouseFlightTargetSensValue = rootVisualElement.Q<Label>("MouseFlightTargetSensValue");
                DefaultAimDst = rootVisualElement.Q<Label>("DefaultAimDst");
                AimDstValue = rootVisualElement.Q<Label>("AimDstValue");

                SaveAndCloseButton = rootVisualElement.Q<Button>("SaveAndCloseButton");
                CloseButton = rootVisualElement.Q<Button>("CloseButton");
                ResetButton = rootVisualElement.Q<Button>("ResetButton");

                SaveAndCloseButton.RegisterCallback<ClickEvent>(ev => SaveAndClose());
                CloseButton.RegisterCallback<ClickEvent>(ev => Close());
                ResetButton.RegisterCallback<ClickEvent>(ev => Reset(true));

                SaveAndCloseButton.RegisterCallback<NavigationSubmitEvent>(ev => SaveAndClose());
                CloseButton.RegisterCallback<NavigationSubmitEvent>(ev => Close());
                ResetButton.RegisterCallback<NavigationSubmitEvent>(ev => Reset(true));

                ThirdPersonCamSpeed.RegisterValueChangedCallback(ev => OnTPSCamSpeedValueChange(ev.newValue));
                MouseFlightTargetSens.RegisterValueChangedCallback(ev => MouseFlightTargetSensValueChange(ev.newValue));
                DefaultAimDistance.RegisterValueChangedCallback(ev => DefaultAimDistanceValueChange(ev.newValue));
                AimDistanceSens.RegisterValueChangedCallback(ev => AimDistanceSenstivityValueChange(ev.newValue));

                Reset();
            }

            public void SaveAndClose()
            {
                userSettings.userSettings.PlayerDisplayedName = PlayerDisplayedName.value;
                userSettings.userSettings.DisableFlyAroundCamera = DisableFlyAroundCamera.value;
                userSettings.userSettings.ThirdPersonIsDefaultCamera = DefaultFlightCamera.value;
                userSettings.userSettings.ThirdPersonCameraSensitivity = ThirdPersonCamSpeed.value;
                userSettings.userSettings.FlightTargetSensitivity = MouseFlightTargetSens.value;
                userSettings.userSettings.DefaultAimDistance = DefaultAimDistance.value;
                userSettings.userSettings.AimDistanceSenstivity = AimDistanceSens.value;

                userSettings.OnUserSettingsChanged?.Invoke();
                Close();
            }

            public void Close()
            {
                switch (onCloseOpenWindow)
                {
                    case OnCloseOpenWindow.ConnectionPopUp:
                        menu.ShowConnectionOverlay(true);
                        break;
                    case OnCloseOpenWindow.PausePopUp:
                        menu.ShowPauseOverlay(true);
                        break;
                    case OnCloseOpenWindow.SpawnPopUp:
                        menu.ShowSpawnOverlay(true);
                        break;
                    default:
                        menu.ShowOverlay(false);
                        break;
                }
            }

            public void Reset(bool resetValues = false)
            {
                if (resetValues)
                {
                    userSettings.userSettings = new UserSettingsSaveData();
                }
                PlayerDisplayedName.value = userSettings.userSettings.PlayerDisplayedName;
                DisableFlyAroundCamera.value = userSettings.userSettings.DisableFlyAroundCamera;
                DefaultFlightCamera.value = userSettings.userSettings.ThirdPersonIsDefaultCamera;
                ThirdPersonCamSpeed.value = userSettings.userSettings.ThirdPersonCameraSensitivity;
                MouseFlightTargetSens.value = userSettings.userSettings.FlightTargetSensitivity;
                DefaultAimDistance.value = userSettings.userSettings.DefaultAimDistance;
                AimDistanceSens.value = userSettings.userSettings.AimDistanceSenstivity;
            }

            public void OnTPSCamSpeedValueChange(float newValue)
            {
                ThirdPersonCamSpeedValue.text = newValue.ToString();
            }

            public void MouseFlightTargetSensValueChange(float newValue)
            {
                MouseFlightTargetSensValue.text = newValue.ToString();
            }

            public void DefaultAimDistanceValueChange(float newValue)
            {
                DefaultAimDst.text = newValue.ToString();
            }

            public void AimDistanceSenstivityValueChange(float newValue)
            {
                AimDstValue.text = newValue.ToString();
            }
        }
    }
}
