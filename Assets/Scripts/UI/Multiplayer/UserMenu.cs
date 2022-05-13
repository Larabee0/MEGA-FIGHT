using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using System.Linq;

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

        [SerializeField] private PasswordLobbyMP lobby;
        [SerializeField] private LocalPlayerManager localPlayerManager;
        private UIDocument document;
        private VisualElement rootVisualElement;
        private VisualElement overlay;
        [HideInInspector] public InputControl inputControl;

        public ConnectionPopUp connectionPopUp;
        public PausePopUp pausePopUp;
        public SpawnPopUp spawnPopUp;
        public SettingsPopUp settingsPopUp;
        public InfoPopUp infoPopUp;

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
            infoPopUp = new InfoPopUp(rootVisualElement.Q("InfoPopUp"));
            ShowConnectionOverlay(true);
            SetSettings();
            UserCustomisableSettings.instance.OnUserSettingsChanged += SetSettings;
        }

        public InGameInfo GetInGameInfo()
        {
            return new InGameInfo(rootVisualElement.Q("InGameInfo"));
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
                    connectionPopUp.EnableButtons = true;
                    ShowPauseOverlay(false, false);
                    ShowSpawnOverlay(false, false);
                    ShowSettingsOverlay(false, false);
                    ShowInfoOverlay(false, false);
                    connectionPopUp.HostButton.Focus();
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
                    ShowInfoOverlay(false, false);
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
                    ShowInfoOverlay(false, false);
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
                    ShowInfoOverlay(false, false);
                    break;
                case false:
                    settingsPopUp.rootVisualElement.style.display = DisplayStyle.None;
                    break;
            }
            if (BackgroundTo) ShowOverlay(shown);
        }

        public void ShowInfoOverlay(bool shown, bool BackgroundTo = true)
        {
            switch (shown)
            {
                case true:
                    SetMenuSelection();
                    infoPopUp.rootVisualElement.style.display = DisplayStyle.Flex;
                    ShowConnectionOverlay(false, false);
                    ShowPauseOverlay(false, false);
                    ShowSpawnOverlay(false, false);
                    ShowSettingsOverlay(false, false);
                    break;
                case false:
                    infoPopUp.rootVisualElement.style.display = DisplayStyle.None;
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
            if (camCart == null) return;
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
            public readonly Button HostButton;
            private readonly Button SettingsButton;
            private readonly Button SwitchNetworkModeButton;
            private readonly Label JoinCodeDisplay;
            private readonly TextField JoinCodeTextField;
            private readonly TextField ConnectPortTextField;
            private readonly TextField ServerPortTextField;
            private readonly string relayLabel = "Join Code for Connecting Players\nProvided in Respawn Lobby";
            private readonly string relayTextField = "Join Code";
            private readonly string PTPTextField = "IP";
            
            public string JoinCodeText { set { JoinCodeTextField.value = value; } }

            public bool EnableButtons
            {
                set
                {
                    ConnectButton.SetEnabled(value);
                    HostButton.SetEnabled(value);
                    SettingsButton.SetEnabled(value);
                    //JoinCodeTextField.SetEnabled(value);
                    ConnectPortTextField.SetEnabled(value);
                    ServerPortTextField.SetEnabled(value);
                }
            }

            public ConnectionPopUp(UserMenu Menu, VisualElement RootVisualElement)
            {
                menu = Menu;
                rootVisualElement = RootVisualElement;
                QuitButton = rootVisualElement.Q<Button>("QuitGameButton");
                ConnectButton = rootVisualElement.Q<Button>("ClientConnectButton");
                HostButton = rootVisualElement.Q<Button>("HostStartButton");
                SettingsButton = rootVisualElement.Q<Button>("SettingsButton");
                SwitchNetworkModeButton = rootVisualElement.Q<Button>("NetworkModeButton");
                JoinCodeTextField = rootVisualElement.Q<TextField>("JoinCode");
                ConnectPortTextField = rootVisualElement.Q<TextField>("ConnectPort");
                ServerPortTextField = rootVisualElement.Q<TextField>("ServerPort");
                JoinCodeDisplay = rootVisualElement.Q<Label>("JoinCodeDisplay");

                QuitButton.RegisterCallback<ClickEvent>(ev => OnQuitCallback());
                HostButton.RegisterCallback<ClickEvent>(ev => OnHostCallback());
                ConnectButton.RegisterCallback<ClickEvent>(ev => OnConnectCallback());
                SettingsButton.RegisterCallback<ClickEvent>(ev => ShowSettings());
                SwitchNetworkModeButton.RegisterCallback<ClickEvent>(ev => SwitchNetworkModeCallback());

                QuitButton.RegisterCallback<NavigationSubmitEvent>(ev => OnQuitCallback());
                HostButton.RegisterCallback<NavigationSubmitEvent>(ev => OnHostCallback());
                ConnectButton.RegisterCallback<NavigationSubmitEvent>(ev => OnConnectCallback());
                SettingsButton.RegisterCallback<NavigationSubmitEvent>(ev => ShowSettings());
                SwitchNetworkModeButton.RegisterCallback<NavigationSubmitEvent>(ev => SwitchNetworkModeCallback());

                if (UserCustomisableSettings.UseLocal)
                {
                    JoinCodeTextField.label = PTPTextField;
                    SetIPText();
                    ConnectPortTextField.style.display = DisplayStyle.Flex;
                    ServerPortTextField.style.display = DisplayStyle.Flex;
                    UNetTransport transport = FindObjectOfType<UNetTransport>();
                    ConnectPortTextField.value = ServerPortTextField.value = transport.ConnectPort.ToString();

                    JoinCodeTextField.RegisterValueChangedCallback(ev => OnIPChanged(ev.newValue));
                    ConnectPortTextField.RegisterValueChangedCallback(ev => OnPortChanged(ev.newValue, ConnectPortTextField));
                    ServerPortTextField.RegisterValueChangedCallback(ev => OnPortChanged(ev.newValue, ServerPortTextField));
                }
                else
                {
                    JoinCodeTextField.label = relayTextField;
                    JoinCodeDisplay.text = relayLabel;
                    ConnectPortTextField.style.display = DisplayStyle.None;
                    ServerPortTextField.style.display = DisplayStyle.None;
                }
            }

            private void OnQuitCallback()
            {
                Application.Quit();
            }

            private void SwitchNetworkModeCallback()
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            }

            private void OnHostCallback()
            {
                if (UserCustomisableSettings.UseLocal)
                {
                    if (int.TryParse(ConnectPortTextField.value, out int port))
                    {
                        menu.lobby.Host(port);
                    }
                    else
                    {
                        ConnectPortTextField.value = "Invalid Port!";
                        return;
                    }
                }
                else
                {
                    menu.lobby.Host();
                }
            }

            private void OnConnectCallback()
            {
                if (UserCustomisableSettings.UseLocal)
                {
                    if (ValidateIP(JoinCodeTextField.value, out IPAddress address))
                    {
                        if(int.TryParse(ConnectPortTextField.value, out _))
                        {
                            EnableButtons = false;
                            menu.lobby.Client(address.ToString()+':'+ConnectPortTextField.value);
                        }
                        else
                        {
                            ConnectPortTextField.value = "Invalid Port!";
                            return;
                        }
                    }
                    else
                    {
                        JoinCodeTextField.value = "Invalid IP!";
                        return;
                    }
                }
                else
                {
                    EnableButtons = false;
                    menu.lobby.Client(JoinCodeTextField.value);
                }
            }

            private void ShowSettings()
            {
                menu.settingsPopUp.onCloseOpenWindow = OnCloseOpenWindow.ConnectionPopUp;
                menu.ShowSettingsOverlay(true);
            }

            private void SetIPText()
            {
                
                string internalIP = Dns.GetHostAddresses(Dns.GetHostName())[1].ToString();
                
                string externalIP = GetPublicIPAddress();

                JoinCodeDisplay.text = string.Format("Local IP: {0}\nExternal IP: {1}", internalIP, externalIP);
            }
            private static string GetPublicIPAddress()
            {
                try
                {
                    string url = "http://checkip.dyndns.org";
                    WebRequest req = WebRequest.Create(url);
                    WebResponse resp = req.GetResponse();
                    StreamReader sr = new(resp.GetResponseStream());
                    string response = sr.ReadToEnd().Trim();
                    string[] ipAddressWithText = response.Split(':');
                    string ipAddressWithHTMLEnd = ipAddressWithText[1][1..];
                    string[] ipAddress = ipAddressWithHTMLEnd.Split('<');
                    string mainIP = ipAddress[0];
                    return mainIP;
                }
                catch { return "Uknown"; }
            }

            private bool ValidateIP(string IP, out IPAddress iPAddress)
            {
                return IPAddress.TryParse(IP, out iPAddress);
            }

            private void OnIPChanged(string newValue)
            {
                if (ValidateIP(newValue, out _))
                {
                    menu.MakeTextFieldWhite(JoinCodeTextField);
                }
                else
                {
                    menu.MakeTextFieldRed(JoinCodeTextField);
                }
            }

            private void OnPortChanged(string newValue,TextField textField)
            {
                if(int.TryParse(newValue,out _))
                {
                    menu.MakeTextFieldWhite(textField);
                }
                else
                {
                    menu.MakeTextFieldRed(textField);
                }
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

            private readonly RadioButton LightShipRadioButton;
            private readonly RadioButton TankShipRadioButton;
            private readonly RadioButton XWingRadioButton;
            private readonly RadioButton FalconRadioButton;

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

                LightShipRadioButton = rootVisualElement.Q<RadioButton>("LightShip");
                TankShipRadioButton = rootVisualElement.Q<RadioButton>("TankShip");
                XWingRadioButton = rootVisualElement.Q<RadioButton>("XWing");
                FalconRadioButton = rootVisualElement.Q<RadioButton>("Falcon");


                LightShipRadioButton.RegisterCallback<NavigationSubmitEvent>(ev => RadioButtonPressedByXbox());
                TankShipRadioButton.RegisterCallback<NavigationSubmitEvent>(ev => RadioButtonPressedByXbox());
                XWingRadioButton.RegisterCallback<NavigationSubmitEvent>(ev => RadioButtonPressedByXbox());
                FalconRadioButton.RegisterCallback<NavigationSubmitEvent>(ev => RadioButtonPressedByXbox());

                SpawnButton.RegisterCallback<ClickEvent>(ev => SpawnButtonCallback());
                LeaveButton.RegisterCallback<ClickEvent>(ev => LeaveButtonCallback());
                QuitGameButton.RegisterCallback<ClickEvent>(ev => QuitGameButtonCallback());
                SettingsButton.RegisterCallback<ClickEvent>(ev => ShowSettings());

                SpawnButton.RegisterCallback<NavigationSubmitEvent>(ev => SpawnButtonCallback());
                LeaveButton.RegisterCallback<NavigationSubmitEvent>(ev => LeaveButtonCallback());
                QuitGameButton.RegisterCallback<NavigationSubmitEvent>(ev => QuitGameButtonCallback());
                SettingsButton.RegisterCallback<NavigationSubmitEvent>(ev => ShowSettings());
            }

            private void RadioButtonPressedByXbox()
            {
                Debug.Log("button pressed");
            }

            private void SpawnButtonCallback()
            {
                Debug.LogFormat("Ship Index: {0}", GetIndexFromButton());
                menu.localPlayerManager.ShipPrefabIndex = GetIndexFromButton();
                menu.localPlayerManager.DisplayedName = DisplayedNameTextField.value;
                menu.localPlayerManager.Spawn();
                menu.ShowSpawnOverlay(false);
            }

            private byte GetIndexFromButton()
            {
                if (LightShipRadioButton.value)
                {
                    return 0;
                }
                else if (TankShipRadioButton.value)
                {
                    return 1;
                }
                else if (XWingRadioButton.value)
                {
                    return 2;
                }
                else if(FalconRadioButton.value)
                {
                    return 3;
                }
                else
                {
                    return 0;
                }
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
                    SpeedLabel.text = (value * 3.6f).ToString("F0");
                }
            }

            public float Altitude
            {
                set
                {
                    AltitudeLabel.text = (value).ToString("F0");
                }
            }

            public InGameInfo(VisualElement RootVisualElement)
            {
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
            private readonly Label DisplayNum;

            private readonly DropdownField DisplayChoices;
            private readonly DropdownField FullScreenMode;
            private readonly DropdownField Resolution;

            private readonly Button SaveAndCloseButton;
            private readonly Button CloseButton;
            private readonly Button ResetButton;

            private readonly Button MinusThirdPersonCamSpeedButton;
            private readonly Button MinusMouseFlightTargetSensButton;
            private readonly Button MinusDefaultAimDistanceButton;
            private readonly Button MinusAimDistanceSensButton;

            private readonly Button PlusThirdPersonCamSpeedButton;
            private readonly Button PlusMouseFlightTargetSensButton;
            private readonly Button PlusDefaultAimDistanceButton;
            private readonly Button PlusAimDistanceSensButton;

            public OnCloseOpenWindow onCloseOpenWindow = OnCloseOpenWindow.SettingsPopUp;
            private readonly List<DisplayInfo> displays = new();

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
                DisplayNum = rootVisualElement.Q<Label>("DisplayNum");

                DisplayChoices = rootVisualElement.Q<DropdownField>("DisplayChoices");
                FullScreenMode = rootVisualElement.Q<DropdownField>("FullScreenMode");
                Resolution = rootVisualElement.Q<DropdownField>("Resolution");

                SaveAndCloseButton = rootVisualElement.Q<Button>("SaveAndCloseButton");
                CloseButton = rootVisualElement.Q<Button>("CloseButton");
                ResetButton = rootVisualElement.Q<Button>("ResetButton");

                MinusThirdPersonCamSpeedButton = rootVisualElement.Q<Button>("TPCSMinus");
                MinusMouseFlightTargetSensButton = rootVisualElement.Q<Button>("MFTSMinus");
                MinusDefaultAimDistanceButton = rootVisualElement.Q<Button>("DADMinus");
                MinusAimDistanceSensButton = rootVisualElement.Q<Button>("ADSMinus");

                PlusThirdPersonCamSpeedButton = rootVisualElement.Q<Button>("TPCSPlus");
                PlusMouseFlightTargetSensButton = rootVisualElement.Q<Button>("MFTSPlus");
                PlusDefaultAimDistanceButton = rootVisualElement.Q<Button>("DADPlus");
                PlusAimDistanceSensButton = rootVisualElement.Q<Button>("ADSPlus");

                SaveAndCloseButton.RegisterCallback<ClickEvent>(ev => SaveAndClose());
                CloseButton.RegisterCallback<ClickEvent>(ev => Close());
                ResetButton.RegisterCallback<ClickEvent>(ev => Reset(true));

                SaveAndCloseButton.RegisterCallback<NavigationSubmitEvent>(ev => SaveAndClose());
                CloseButton.RegisterCallback<NavigationSubmitEvent>(ev => Close());
                ResetButton.RegisterCallback<NavigationSubmitEvent>(ev => Reset(true));


                MinusThirdPersonCamSpeedButton.RegisterCallback<ClickEvent>(ev=> PlusMinusButton(ButtonName.ThirdPersonCamSpeed, true));
                MinusMouseFlightTargetSensButton.RegisterCallback<ClickEvent>(ev => PlusMinusButton(ButtonName.MouseFlightTargetSens, true));
                MinusDefaultAimDistanceButton.RegisterCallback<ClickEvent>(ev => PlusMinusButton(ButtonName.DefaultAimDistance, true));
                MinusAimDistanceSensButton.RegisterCallback<ClickEvent>(ev => PlusMinusButton(ButtonName.AimDistanceSens, true));

                PlusThirdPersonCamSpeedButton.RegisterCallback<ClickEvent>(ev => PlusMinusButton(ButtonName.ThirdPersonCamSpeed));
                PlusMouseFlightTargetSensButton.RegisterCallback<ClickEvent>(ev => PlusMinusButton(ButtonName.MouseFlightTargetSens));
                PlusDefaultAimDistanceButton.RegisterCallback<ClickEvent>(ev => PlusMinusButton(ButtonName.DefaultAimDistance));
                PlusAimDistanceSensButton.RegisterCallback<ClickEvent>(ev => PlusMinusButton(ButtonName.AimDistanceSens));

                MinusThirdPersonCamSpeedButton.RegisterCallback<NavigationSubmitEvent>(ev => PlusMinusButton(ButtonName.ThirdPersonCamSpeed, true));
                MinusMouseFlightTargetSensButton.RegisterCallback<NavigationSubmitEvent>(ev => PlusMinusButton(ButtonName.MouseFlightTargetSens, true));
                MinusDefaultAimDistanceButton.RegisterCallback<NavigationSubmitEvent>(ev => PlusMinusButton(ButtonName.DefaultAimDistance, true));
                MinusAimDistanceSensButton.RegisterCallback<NavigationSubmitEvent>(ev => PlusMinusButton(ButtonName.AimDistanceSens, true));

                PlusThirdPersonCamSpeedButton.RegisterCallback<NavigationSubmitEvent>(ev => PlusMinusButton(ButtonName.ThirdPersonCamSpeed));
                PlusMouseFlightTargetSensButton.RegisterCallback<NavigationSubmitEvent>(ev => PlusMinusButton(ButtonName.MouseFlightTargetSens));
                PlusDefaultAimDistanceButton.RegisterCallback<NavigationSubmitEvent>(ev => PlusMinusButton(ButtonName.DefaultAimDistance));
                PlusAimDistanceSensButton.RegisterCallback<NavigationSubmitEvent>(ev => PlusMinusButton(ButtonName.AimDistanceSens));

                ThirdPersonCamSpeed.RegisterValueChangedCallback(ev => OnTPSCamSpeedValueChange(ev.newValue));
                MouseFlightTargetSens.RegisterValueChangedCallback(ev => MouseFlightTargetSensValueChange(ev.newValue));
                DefaultAimDistance.RegisterValueChangedCallback(ev => DefaultAimDistanceValueChange(ev.newValue));
                AimDistanceSens.RegisterValueChangedCallback(ev => AimDistanceSenstivityValueChange(ev.newValue));
                if (Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    DisplayChoices.choices = new();
                    Screen.GetDisplayLayout(displays);
                    for (int i = 0; i < displays.Count; i++)
                    {
                        DisplayInfo display = displays[i];
                        DisplayChoices.choices.Add(string.Format("Display {0}", i + 1));

                    }
                    int StartDisplayIndex = displays.IndexOf(Screen.mainWindowDisplayInfo);
                    DisplayChoices.index = StartDisplayIndex;
                    DisplayNum.text = (StartDisplayIndex + 1).ToString();

                    DisplayChoices.RegisterValueChangedCallback(ev => OnDisplayChoiceChange());

                    FullScreenMode.index = (int)Screen.fullScreenMode;
                    FullScreenMode.RegisterValueChangedCallback(ev => OnFullScreenChange());

                    RefreshResolutionChoices();
                    Resolution.RegisterValueChangedCallback(ev => OnResolutionChange());
                }
                Reset();
            }

            private enum ButtonName
            {
                ThirdPersonCamSpeed,
                MouseFlightTargetSens,
                DefaultAimDistance,
                AimDistanceSens
            }

            private void RefreshResolutionChoices()
            {
                Resolution.choices.Clear();
                List<Resolution> resolutions = new(Screen.resolutions);
                for (int i = 0; i < resolutions.Count; i++)
                {
                    Resolution resolution = resolutions[i];
                    Resolution.choices.Add(string.Format("{0}x{1}", resolution.width, resolution.height));
                }
                Resolution.index = resolutions.IndexOf(Screen.currentResolution);
            }

            private void OnDisplayChoiceChange()
            {

                Screen.MoveMainWindowTo(displays[DisplayChoices.index], Vector2Int.zero);
                DisplayNum.text = (DisplayChoices.index + 1).ToString();
                RefreshResolutionChoices();
            }

            private void OnFullScreenChange()
            {
                Screen.fullScreenMode = (FullScreenMode)FullScreenMode.index;
            }

            private void OnResolutionChange()
            {
                Resolution newResolution = Screen.resolutions[Resolution.index];
                Screen.SetResolution(newResolution.width, newResolution.height, (FullScreenMode)FullScreenMode.index);
            }

            private void PlusMinusButton(ButtonName button, bool minus = false)
            {
                float Value = GetModifier(button);
                if (minus)
                {
                    Value = -Value;
                }
                switch (button)
                {
                    case ButtonName.ThirdPersonCamSpeed:
                        ThirdPersonCamSpeed.value += Value;
                        break;
                    case ButtonName.MouseFlightTargetSens:
                        MouseFlightTargetSens.value += Value;
                        break;
                    case ButtonName.DefaultAimDistance:
                        DefaultAimDistance.value += Value;
                        break;
                    case ButtonName.AimDistanceSens:
                        AimDistanceSens.value += Value;
                        break;
                }
            }

            private float GetModifier(ButtonName button)
            {
                return button switch
                {
                    ButtonName.ThirdPersonCamSpeed => 0.5f,
                    ButtonName.MouseFlightTargetSens => 0.01f,
                    ButtonName.DefaultAimDistance => 10f,
                    ButtonName.AimDistanceSens => 1f,
                    _ => 1f,
                };
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
                ThirdPersonCamSpeedValue.text = newValue.ToString("N1");
            }

            public void MouseFlightTargetSensValueChange(float newValue)
            {
                MouseFlightTargetSensValue.text = newValue.ToString("N2");
            }

            public void DefaultAimDistanceValueChange(float newValue)
            {
                DefaultAimDst.text = newValue.ToString("N0");
            }

            public void AimDistanceSenstivityValueChange(float newValue)
            {
                AimDstValue.text = newValue.ToString("N0");
            }
        }

        public class InfoPopUp
        {
            public readonly VisualElement rootVisualElement;

            private readonly Label InfoLabelA;
            private readonly Label InfoLabelB;

            public string UpperLabel { set => InfoLabelA.text = value; }
            public string LowerLabel { set => InfoLabelB.text = value; }

            public InfoPopUp(VisualElement RootVisualElement)
            {
                rootVisualElement = RootVisualElement;

                InfoLabelA = rootVisualElement.Q<Label>("InfoDisplayA");
                InfoLabelB = rootVisualElement.Q<Label>("InfoDisplayB");
            }

            public void MakeRed()
            {
                StyleColor textColor = InfoLabelA.style.color;
                textColor.value = Color.red;
                InfoLabelA.style.color = InfoLabelB.style.color = textColor;
            }

            public void MakeWhite()
            {
                StyleColor textColor = InfoLabelA.style.color;
                textColor.value = Color.white;
                InfoLabelA.style.color = InfoLabelB.style.color = textColor;
            }
        }
    }
}
