//
// Copyright (c) Brian Hernandez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Mathematics;

namespace MultiplayerRunTime
{
    /// <summary>
    /// Combination of camera rig and controller for aircraft. Requires a properly set
    /// up rig. I highly recommend either using or referencing the included prefab.
    /// </summary>
    public class MouseFlightControllerMP : MonoBehaviour
    {
        [Header("Components")]
        [HideInInspector] public SpaceshipMP spaceshipController;
        [HideInInspector] public InputControl inputControl;
        private Transform spaceshipTransform = null;

        public FireControlMP fireControl;

        private UserMenu.InGameInfo inGameInfo;

        [SerializeField]
        [Tooltip("Transform of the object the mouse rotates to generate MouseAim position")]
        private Transform mouseAim = null;
        [SerializeField]
        [Tooltip("Transform of the object on the rig which the camera is attached to")]
        private Transform cameraRig = null;
        [SerializeField]
        [Tooltip("Transform of the third person camera")]
        private Transform TPSCamPos = null;
        private Transform FPSCamPos = null;

        [SerializeField]
        [Tooltip("Camera of the third person camera")]
        private CinemachineVirtualCamera TPSVirtualCamera = null;
        [SerializeField]
        [Tooltip("Camera of the first person camera")]
        private CinemachineVirtualCamera FPSVirtualCamera = null;
        [SerializeField]
        [Tooltip("Camera of the despawned player")]
        private CinemachineVirtualCamera DeSpawnedVirtualCamera = null;

        [Header("Options")]
        [SerializeField]
        [Tooltip("Starting Camera Perspective")]
        private Perspective perspective = Perspective.ThridPerson;
        private bool EnableFPS = true;

        [SerializeField]
        [Tooltip("How quickly the camera tracks the mouse aim point.")]
        private float tPScamSmoothSpeed = 5f;
        public float TPScamSmoothSpeed => tPScamSmoothSpeed;
        [SerializeField]
        [Tooltip("How quickly the camera tracks the mouse aim point.")]
        private float FPSCamSmoothSpeed = 15f;

        [SerializeField]
        [Tooltip("Mouse sensitivity for the mouse flight target")]
        private float mouseSensitivity = 3f;
        public float MouseSensitivity => mouseSensitivity;

        private float AimDistance => fireControl.TargetDistance;

        [Space]
        [SerializeField]
        [Tooltip("How far the boresight and mouse flight are from the aircraft")]
        private bool showDebugInfo = false;

        public Vector3 frozenDirection = Vector3.forward;
        public bool isMouseAimFrozen = false;

        public bool FreezeMouseAim { get => isMouseAimFrozen; set => isMouseAimFrozen = value; }

        [Space]
        [Header("Player Input")]

        [SerializeField] private float throttleSenstivity = 1f;

        private bool rollOverride = false;
        private bool yawOverride = false;
        private bool pitchOverride = false;

        /// <summary>
        /// Get a point along the aircraft's boresight projected out to aimDistance meters.
        /// Useful for drawing a crosshair to aim fixed forward guns with, or to indicate what
        /// direction the aircraft is pointed.
        /// </summary>
        public Vector3 BoresightPos => spaceshipTransform == null
                     ? transform.forward * AimDistance
                     : (spaceshipTransform.transform.forward * AimDistance) + (spaceshipTransform.transform.position + spaceshipController.AimOffset);

        /// <summary>
        /// Get the position that the mouse is indicating the aircraft should fly, projected
        /// out to aimDistance meters. Also meant to be used to draw a mouse cursor.
        /// </summary>
        public Vector3 MouseAimPos
        {
            get
            {
                if (mouseAim != null && spaceshipController != null)
                {
                    return isMouseAimFrozen
                        ? GetFrozenMouseAimPos()
                        : mouseAim.position + spaceshipController.AimOffset + (mouseAim.forward * AimDistance);
                }
                else
                {
                    return transform.forward * AimDistance;
                }
            }
        }
        private void OnEnable()
        {
            fireControl.inputControl = inputControl;
            fireControl.enabled = true;
            if (mouseAim == null)
                Debug.LogError(name + "MouseFlightController - No mouse aim transform assigned!");
            if (cameraRig == null)
                Debug.LogError(name + "MouseFlightController - No camera rig transform assigned!");
            UserCustomisableSettings.instance.OnUserSettingsChanged += SetSettings;
            // To work correctly, the entire rig must not be parented to anything.
            // When parented to something (such as an aircraft) it will inherit those
            // rotations causing unintended rotations as it gets dragged around.
            transform.parent = null;
            inputControl.FlightActions.CameraSwitch.canceled += OnPerspectiveButtonPressed;
            inputControl.FlightActions.FreeCam.started += OnFreeCamButton;
            inputControl.FlightActions.FreeCam.canceled += OnFreeCamButton;
            inGameInfo = PasswordLobbyMP.Singleton.menu.GetInGameInfo();

            if (DeSpawnedVirtualCamera != null) DeSpawnedVirtualCamera.Priority = 8;
            SetSettings();
        }

        private void Update()
        {
            if (spaceshipTransform == null)
            {
                return;
            }
            UpdateCameraPos();
            if (spaceshipController != null)
            {
                ThrottleInput();
                Vector3 playerOverride = inputControl.FlightActions.JoyStick.ReadValue<Vector3>();
                rollOverride = false;
                pitchOverride = false;
                yawOverride = false;

                // roll (x)
                switch (math.abs(playerOverride.x))
                {
                    case > 0.25f:
                        rollOverride = true;
                        break;
                }

                // pitch (y)
                switch (math.abs(playerOverride.y))
                {
                    case > 0.25f:
                        yawOverride = true;
                        pitchOverride = true;
                        rollOverride = true;
                        break;
                }
                // yaw (z)
                switch (math.abs(playerOverride.z))
                {
                    case > 0.25f:
                        yawOverride = true;
                        pitchOverride = true;
                        rollOverride = true;
                        break;
                }

                RunAutopilot(MouseAimPos, out float autoYaw, out float autoPitch, out float autoRoll);
                playerOverride.x = rollOverride ? playerOverride.x : autoRoll;
                playerOverride.y = pitchOverride ? playerOverride.y : autoPitch;
                playerOverride.z = yawOverride ? playerOverride.z : autoYaw;

                spaceshipController.controlInput = playerOverride;
            }
            RotateRig();
        }

        private void FixedUpdate()
        {
            inGameInfo.Thrust = spaceshipController.Throttle * 100f;
            inGameInfo.Speed = spaceshipController.Velocity;
            inGameInfo.Altitude = spaceshipTransform.position.y;
        }

        private void OnDisable()
        {
            if(fireControl != null)
            {
                fireControl.enabled = false;
            }
            UserCustomisableSettings.instance.OnUserSettingsChanged -= SetSettings;
            inputControl.FlightActions.CameraSwitch.canceled -= OnPerspectiveButtonPressed;
            inputControl.FlightActions.FreeCam.started -= OnFreeCamButton;
            inputControl.FlightActions.FreeCam.canceled -= OnFreeCamButton;
            spaceshipTransform = null;
            spaceshipController = null;
            if (DeSpawnedVirtualCamera != null) DeSpawnedVirtualCamera.Priority = 11;
        }

        private void SetSettings()
        {
            UserCustomisableSettings userSettings = UserCustomisableSettings.instance;
            throttleSenstivity = userSettings.userSettings.ThrottleSenstivity;
            mouseSensitivity = userSettings.userSettings.FlightTargetSensitivity;
            tPScamSmoothSpeed = userSettings.userSettings.ThirdPersonCameraSensitivity;
            if (!userSettings.userSettings.ThirdPersonIsDefaultCamera)
            {
                perspective = Perspective.FirstPerson;
            }

            SetVirtualCameraTarget();
        }

        public void SetShip(SpaceshipMP ship)
        {
            spaceshipTransform = ship.transform;
            spaceshipController = ship;
            TPSCamPos.localPosition = ship.TPSCameraPosition;
            TPSVirtualCamera.Follow = TPSCamPos;
            if (spaceshipController.FPSCamPos == null)
            {
                EnableFPS = false;
                perspective = Perspective.ThridPerson;
            }
            else
            {
                EnableFPS = true;
                FPSVirtualCamera.Follow = FPSCamPos = spaceshipController.FPSCamPos;
            }
            SetVirtualCameraTarget();
        }

        public void SetDeathCamera(SpaceshipMP target)
        {
            TPSCamPos.localPosition = target.TPSCameraPosition;
            TPSVirtualCamera.Follow = TPSCamPos;
            perspective = Perspective.ThridPerson;

        }

        private void ThrottleInput()
        {
            float axisValue = inputControl.FlightActions.Throttle.ReadValue<float>();
            bool reverse = inputControl.FlightActions.ReverseThrottle.IsPressed();
            axisValue = axisValue != 0 ? math.clamp(spaceshipController.Throttle + (axisValue * (throttleSenstivity * Time.deltaTime)), 0f, 1f) : spaceshipController.Throttle;
            axisValue = axisValue > 0 && reverse ? 0 : axisValue;
            axisValue -= reverse ? throttleSenstivity * Time.deltaTime : 0;
            axisValue = math.clamp(axisValue, -0.25f, 1f);
            spaceshipController.Throttle = axisValue;
        }

        private void OnPerspectiveButtonPressed(InputAction.CallbackContext context)
        {
            perspective = EnableFPS ? perspective.Next() : Perspective.ThridPerson;
            SetVirtualCameraTarget();
        }

        private void OnFreeCamButton(InputAction.CallbackContext context)
        {
            if (isMouseAimFrozen)
            {
                isMouseAimFrozen = false;
                mouseAim.forward = frozenDirection;
            }
            else
            {
                isMouseAimFrozen = true;
                frozenDirection = mouseAim.forward;
            }
        }


        private void RunAutopilot(Vector3 flyTarget, out float yaw, out float pitch, out float roll)
        {
            Vector3 localFlyTarget = spaceshipTransform.InverseTransformPoint(flyTarget).normalized * spaceshipController.sensitivity;
            float angleOffTarget = Vector3.Angle(spaceshipTransform.forward, flyTarget - spaceshipTransform.position);

            yaw = math.clamp(localFlyTarget.x, -1f, 1f);
            pitch = -math.clamp(localFlyTarget.y, -1f, 1f);

            float agressiveRoll = math.clamp(localFlyTarget.x, -1f, 1f);

            float wingsLevelRoll = spaceshipTransform.right.y;

            float wingsLevelInfluence = Mathf.InverseLerp(0f, spaceshipController.aggressiveTurnAngle, angleOffTarget);
            roll = math.lerp(wingsLevelRoll, agressiveRoll, wingsLevelInfluence);
        }

        private void RotateRig()
        {
            if (mouseAim == null || TPSCamPos == null || cameraRig == null)
                return;

            // Mouse input.
            Vector2 rawAxis = inputControl.AlwaysOn.Mouse.ReadValue<Vector2>();
            float mouseX = rawAxis.x * mouseSensitivity;
            float mouseY = -rawAxis.y * mouseSensitivity;

            // Rotate the aim target that the plane is meant to fly towards.
            // Use the camera's axes in world space so that mouse motion is intuitive.
            switch (perspective)
            {
                case Perspective.ThridPerson:
                    mouseAim.Rotate(TPSCamPos.right, mouseY, Space.World);
                    mouseAim.Rotate(TPSCamPos.up, mouseX, Space.World);
                    break;
                case Perspective.FirstPerson:
                    mouseAim.Rotate(FPSCamPos.right, mouseY, Space.World);
                    mouseAim.Rotate(FPSCamPos.up, mouseX, Space.World);
                    if (isMouseAimFrozen)
                    {
                        Vector3 FpsUpVec = (math.abs(mouseAim.forward.y) > 0.9f) ? FPSCamPos.up : Vector3.up;
                        FPSCamPos.rotation = DampCamera(FPSCamPos.rotation, Quaternion.LookRotation(mouseAim.forward, FpsUpVec), FPSCamSmoothSpeed, Time.deltaTime);
                    }
                    else
                    {
                        FPSCamPos.localEulerAngles = Vector3.zero;
                    }
                    break;
            }
            // The up vector of the camera normally is aligned to the horizon. However, when
            // looking straight up/down this can feel a bit weird. At those extremes, the camera
            // stops aligning to the horizon and instead aligns to itself.
            Vector3 upVec = (math.abs(mouseAim.forward.y) > 0.9f) ? cameraRig.up : Vector3.up;

            // Smoothly rotate the camera to face the mouse aim.
            cameraRig.rotation = DampCamera(cameraRig.rotation, Quaternion.LookRotation(mouseAim.forward, upVec), tPScamSmoothSpeed, Time.deltaTime);
            
        }

        private Vector3 GetFrozenMouseAimPos()
        {
            if (mouseAim != null)
                return mouseAim.position + spaceshipController.AimOffset + (frozenDirection * AimDistance);
            else
                return transform.forward * AimDistance;
        }

        private void UpdateCameraPos()
        {
            if (spaceshipTransform != null)
            {
                // Move the whole rig to follow the aircraft.
                transform.position = spaceshipTransform.position;
            }
        }

        private void SetVirtualCameraTarget()
        {
            switch (perspective)
            {
                case Perspective.ThridPerson:
                    TPSVirtualCamera.Priority = 10;
                    FPSVirtualCamera.Priority = 9;
                    break;
                case Perspective.FirstPerson:
                    TPSVirtualCamera.Priority = 9;
                    FPSVirtualCamera.Priority = 10;
                    break;
            }
        }

        private Quaternion DampCamera(Quaternion a, Quaternion b, float lambda, float dt)
        {
            return Quaternion.Slerp(a, b, 1 - math.exp(-lambda * dt));
        }

        private void OnDrawGizmos()
        {
            if (showDebugInfo == true)
            {
                Color oldColor = Gizmos.color;

                // Draw the boresight position.
                if (spaceshipTransform != null)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireSphere(BoresightPos, 10f);
                }

                if (mouseAim != null)
                {
                    // Draw the position of the mouse aim position.
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(MouseAimPos, 10f);

                    // Draw axes for the mouse aim transform.
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(mouseAim.position, mouseAim.forward * 50f);
                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(mouseAim.position, mouseAim.up * 50f);
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(mouseAim.position, mouseAim.right * 50f);
                }

                Gizmos.color = oldColor;
            }
        }
    }
}
