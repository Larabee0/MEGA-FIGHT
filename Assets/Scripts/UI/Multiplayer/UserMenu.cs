using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerRunTime
{
    public class UserMenu : MonoBehaviour
    {
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private UNetTransport uNetTransport;
        [SerializeField] private PasswordLobbyMP lobby;
        [SerializeField] private LocalPlayerManager localPlayerManager;
        private UIDocument document;
        private VisualElement rootVisualElement;
        private VisualElement overlay;

        public ConnectionPopUp connectionPopUp;
        public PausePopUp pausePopUp;
        public SpawnPopUp spawnPopUp;
        private void Awake()
        {
            document=GetComponent<UIDocument>();
            if (document == null)
            {
                Debug.LogError("No UI Document assgined;");
                return;
            }
            lobby.menu = this;
            rootVisualElement = document.rootVisualElement;
            overlay = rootVisualElement.Q("overlay");
            connectionPopUp = new ConnectionPopUp(this, rootVisualElement.Q("ConnectionPopUp"));
            pausePopUp = new PausePopUp(this, rootVisualElement.Q("PausePopUp"));
            spawnPopUp = new SpawnPopUp(this, rootVisualElement.Q("SpawnPopUp"));
            ShowConnectionOverlay(true);
        }

        public InGameInfo GetInGameInfo(MouseFlightControllerMP mFCMP)
        {
            return new InGameInfo(mFCMP, rootVisualElement.Q("InGameInfo"));
        }

        private void ShowOverlay(bool shown)
        {
            switch (shown)
            {
                case true:
                    overlay.style.display = DisplayStyle.Flex;
                    break;
                case false:
                    overlay.style.display = DisplayStyle.None;
                    break;
            }
        }

        public void ShowConnectionOverlay(bool shown, bool BackgroundTo = true)
        {
            switch (shown)
            {
                case true:
                    connectionPopUp.rootVisualElement.style.display = DisplayStyle.Flex;
                    ShowPauseOverlay(false, false);
                    ShowSpawnOverlay(false, false);
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
                    pausePopUp.rootVisualElement.style.display = DisplayStyle.Flex;
                    ShowConnectionOverlay(false);
                    ShowSpawnOverlay(false);
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
                    spawnPopUp.rootVisualElement.style.display = DisplayStyle.Flex;
                    ShowConnectionOverlay(false, false);
                    ShowPauseOverlay(false, false);
                    break;
                case false:
                    spawnPopUp.rootVisualElement.style.display = DisplayStyle.None;
                    break;
            }
            if(BackgroundTo) ShowOverlay(shown);
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

        public class ConnectionPopUp
        {
            private readonly UserMenu menu;
            public readonly VisualElement rootVisualElement;

            private readonly Button QuitButton;
            private readonly Button ConnectButton;
            private readonly Button HostButton;

            private readonly TextField JoinCodeTextField;

            public ConnectionPopUp(UserMenu Menu, VisualElement RootVisualElement)
            {
                menu = Menu;
                rootVisualElement = RootVisualElement;
                QuitButton = rootVisualElement.Q<Button>("QuitGameButton");
                ConnectButton = rootVisualElement.Q<Button>("ClientConnectButton");
                HostButton = rootVisualElement.Q<Button>("HostStartButton");
                JoinCodeTextField = rootVisualElement.Q<TextField>("JoinCode");

                QuitButton.RegisterCallback<ClickEvent>(ev => OnQuitCallback());
                HostButton.RegisterCallback<ClickEvent>(ev => OnHostCallback());
                ConnectButton.RegisterCallback<ClickEvent>(ev => OnConnectCallback());

                JoinCodeTextField.RegisterValueChangedCallback(ev => OnIPChanged(ev.newValue));
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

            private void OnIPChanged(string newValue)
            {
                if (ValidateIP(newValue) != null)
                {
                    menu.MakeTextFieldWhite(JoinCodeTextField);
                }
                else
                {
                    menu.MakeTextFieldRed(JoinCodeTextField);
                }
            }

            private IPAddress ValidateIP(string ip)
            {
                if (IPAddress.TryParse(ip, out IPAddress validIp)) return validIp;
                return null;
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

            public PausePopUp(UserMenu Menu, VisualElement RootVisualElement)
            {
                menu = Menu;
                rootVisualElement = RootVisualElement;

                ResumeButton = rootVisualElement.Q<Button>("ResumeGameButton");
                RespawnButton = rootVisualElement.Q<Button>("RespawnButton");
                MainMenuButton = rootVisualElement.Q<Button>("MainMenuButton");
                CloseGameButton = rootVisualElement.Q<Button>("QuitGameButton");

                ResumeButton.RegisterCallback<ClickEvent>(ev => ResumeButtonCallback());
                RespawnButton.RegisterCallback<ClickEvent>(ev => RespawnButtonCallback());
                MainMenuButton.RegisterCallback<ClickEvent>(ev => MainMenuButtonCallback());
                CloseGameButton.RegisterCallback<ClickEvent>(ev => CloseGameButtonCallback());
            }

            private void ResumeButtonCallback()
            {
                menu.localPlayerManager.Pause();
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
        }

        public class SpawnPopUp
        {
            private readonly UserMenu menu;
            public readonly VisualElement rootVisualElement;

            private readonly Button SpawnButton;
            private readonly Button LeaveButton;
            private readonly Button QuitGameButton;

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

                JoinCodeLabel = rootVisualElement.Q<Label>("JoinCodeDisplay");

                DisplayedNameTextField = rootVisualElement.Q<TextField>("DisplayedName");

                SpawnButton.RegisterCallback<ClickEvent>(ev => SpawnButtonCallback());
                LeaveButton.RegisterCallback<ClickEvent>(ev => LeaveButtonCallback());
                QuitGameButton.RegisterCallback<ClickEvent>(ev => QuitGameButtonCallback());
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
    }
}