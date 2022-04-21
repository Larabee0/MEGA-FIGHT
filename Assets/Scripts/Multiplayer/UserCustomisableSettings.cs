using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using UnityEngine;

namespace MultiplayerRunTime
{
    public class UserCustomisableSettings : MonoBehaviour
    {
        public UserSettingsSaveData userSettings;
        
        public static bool UseLocal = false;
        public static UserCustomisableSettings instance;

        private string userSettingsPath;

        public delegate void SettingsChanged();
        public SettingsChanged OnUserSettingsChanged;

        [SerializeField] private GameObject RelayNetworkManager;
        [SerializeField] private GameObject PTPNetworkManager;
        
        private void Awake()
        {
            PrepareScene();
            userSettingsPath = Path.Combine(Application.persistentDataPath, "userSettings.xml");
            instance = this;
            userSettings = new UserSettingsSaveData();
        }

        private void Start()
        {
            if (File.Exists(userSettingsPath))
            {
                XmlSerializer reader = new(typeof(UserSettingsSaveData));
                StreamReader file = new(userSettingsPath);
                userSettings = (UserSettingsSaveData)reader.Deserialize(file);
                file.Close();
                if(userSettings == null)
                {
                    Debug.LogWarning("Read User Settings file, but failed to deserialize it!");
                    userSettings = new UserSettingsSaveData();
                }
            }
        }

        private void OnDestroy()
        {
            if(userSettings == null)
            {
                userSettings = new UserSettingsSaveData();
            }
            XmlSerializer writer = new(typeof(UserSettingsSaveData));
            FileStream file = File.Create(userSettingsPath);
            writer.Serialize(file, userSettings);
            file.Close();
        }

        private void PrepareScene()
        {
            if (UseLocal)
            {
                Instantiate(PTPNetworkManager);
            }
            else
            {
                Instantiate(RelayNetworkManager);
            }
        }
    }

    public class UserSettingsSaveData
    {
        public float ThirdPersonCameraSensitivity = 5f;
        public float FlightTargetSensitivity = 0.3f;
        public float ThrottleSenstivity = 1f;
        public bool ThirdPersonIsDefaultCamera = true;

        public float DefaultAimDistance = 500f;
        public float AimDistanceSenstivity = 25f;

        public bool OverrideEngineColour = false;
        public Color32 PlayerEngineColour = Color.red;

        public bool OverrideLaserColour = false;
        public Color32 PlayerLaserColour = Color.red;

        public bool DisableFlyAroundCamera = false;

        public string PlayerDisplayedName = "Player";
    }
}