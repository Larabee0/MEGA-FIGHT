//
// Copyright (c) Brian Hernandez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Cinemachine;
using Unity.Netcode;
using UnityEngine;

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
        private Transform spaceshipTransform = null;

        public FireControlMP fireControl;

        [SerializeField] [Tooltip("Transform of the object the mouse rotates to generate MouseAim position")]
        private Transform mouseAim = null;
        [SerializeField] [Tooltip("Transform of the object on the rig which the camera is attached to")]
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

        [Header("Options")]
        [SerializeField]
        [Tooltip("Starting Camera Perspective")]
        private Perspective perspective = Perspective.ThridPerson;
        private bool EnableFPS = true;

        [SerializeField] [Tooltip("How quickly the camera tracks the mouse aim point.")]
        private float camSmoothSpeed = 5f;

        [SerializeField] [Tooltip("Mouse sensitivity for the mouse flight target")]
        private float mouseSensitivity = 3f;

        private float AimDistance { get => fireControl.TargetDistance; }

        [Space]
        [SerializeField] [Tooltip("How far the boresight and mouse flight are from the aircraft")]
        private bool showDebugInfo = false;

        private Vector3 frozenDirection = Vector3.forward;
        private bool isMouseAimFrozen = false;

        [Space]
        [Header("Player Input")]

        [SerializeField] private float throttleSenstivity = 1f;
        private float throttle;
        private float throttleLastFrame;

        Vector3 playerOverride;

        /// <summary>
        /// Get a point along the aircraft's boresight projected out to aimDistance meters.
        /// Useful for drawing a crosshair to aim fixed forward guns with, or to indicate what
        /// direction the aircraft is pointed.
        /// </summary>
        public Vector3 BoresightPos
        {
            get
            {
                return spaceshipTransform == null
                     ? transform.forward * AimDistance
                     : (spaceshipTransform.transform.forward * AimDistance) + (spaceshipTransform.transform.position + spaceshipController.AimOffset);
            }
        }

        /// <summary>
        /// Get the position that the mouse is indicating the aircraft should fly, projected
        /// out to aimDistance meters. Also meant to be used to draw a mouse cursor.
        /// </summary>
        public Vector3 MouseAimPos
        {
            get
            {
                if (mouseAim != null && spaceshipController!= null)
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

        private void Awake()
        {
            if (mouseAim == null)
                Debug.LogError(name + "MouseFlightController - No mouse aim transform assigned!");
            if (cameraRig == null)
                Debug.LogError(name + "MouseFlightController - No camera rig transform assigned!");

            // To work correctly, the entire rig must not be parented to anything.
            // When parented to something (such as an aircraft) it will inherit those
            // rotations causing unintended rotations as it gets dragged around.
            transform.parent = null;
        }

        private void GetShip()
        {
            if(NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
            {
                if (NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null)
                {
                    GameObject playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().gameObject;
                    spaceshipTransform = playerObject.transform;
                    spaceshipController = playerObject.GetComponent<SpaceshipMP>();

                    TPSVirtualCamera.Follow = TPSCamPos;
                    if(spaceshipController.FPSCamPos == null)
                    {
                        EnableFPS = false;
                        perspective = Perspective.ThridPerson;
                    }
                    else
                    {
                        EnableFPS = true;
                        FPSVirtualCamera.Follow = FPSCamPos= spaceshipController.FPSCamPos;
                    }

                    fireControl.spaceship = spaceshipController;
                    fireControl.enabled = true;
                    SetVirtualCameraTarget();
                }
            }
        }

        private void Update()
        {
            if(spaceshipTransform == null)
            {
                GetShip();
            }
            UpdateCameraPos();
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
            }
            if (Input.GetKeyUp(KeyCode.V))
            {
                OnPerspectiveButtonPressed();
            }
            if (spaceshipController != null && spaceshipController.IsOwner)
            {
                NewUpdate();
                playerOverride = new()
                {
                    x = Input.GetAxis("Vertical"),
                    y = Input.GetAxis("Yaw"),
                    z = Input.GetAxis("Horizontal")
                };
                spaceshipController.SetMouseAimPosServerRPC(MouseAimPos);
                spaceshipController.SetPlayerOverrideServerRPC(playerOverride);
            }
            RotateRig();
        }

        private void NewUpdate()
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                throttle += throttleSenstivity * Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.LeftControl))
            {
                throttle = Mathf.Clamp01(throttle - (throttleSenstivity * Time.deltaTime));
            }

            if (Input.GetKey(KeyCode.B))
            {
                throttle -= throttleSenstivity * Time.deltaTime;
            }

            if (throttle != throttleLastFrame)
            {
                spaceshipController.Throttle = throttle;
            }

            throttleLastFrame = throttle;
        }

        private void RotateRig()
        {
            if (mouseAim == null || TPSCamPos == null || cameraRig == null)
                return;

            // Freeze the mouse aim direction when the free look key is pressed.
            if (Input.GetKeyDown(KeyCode.C))
            {
                isMouseAimFrozen = true;
                frozenDirection = mouseAim.forward;
            }
            else if  (Input.GetKeyUp(KeyCode.C))
            {
                isMouseAimFrozen = false;
                mouseAim.forward = frozenDirection;
            }

            // Mouse input.
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = -Input.GetAxis("Mouse Y") * mouseSensitivity;

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
                    break;
            }

            // The up vector of the camera normally is aligned to the horizon. However, when
            // looking straight up/down this can feel a bit weird. At those extremes, the camera
            // stops aligning to the horizon and instead aligns to itself.
            Vector3 upVec = (Mathf.Abs(mouseAim.forward.y) > 0.9f) ? cameraRig.up : Vector3.up;

            // Smoothly rotate the camera to face the mouse aim.
            cameraRig.rotation = Damp(cameraRig.rotation,
                                      Quaternion.LookRotation(mouseAim.forward, upVec),
                                      camSmoothSpeed,
                                      Time.deltaTime);
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

        public void OnPerspectiveButtonPressed()
        {
            perspective = EnableFPS ? perspective.Next() : Perspective.ThridPerson;
            SetVirtualCameraTarget();
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

        // Thanks to Rory Driscoll
        // http://www.rorydriscoll.com/2016/03/07/frame-rate-independent-damping-using-lerp/
        /// <summary>
        /// Creates dampened motion between a and b that is framerate independent.
        /// </summary>
        /// <param name="a">Initial parameter</param>
        /// <param name="b">Target parameter</param>
        /// <param name="lambda">Smoothing factor</param>
        /// <param name="dt">Time since last damp call</param>
        /// <returns></returns>
        private Quaternion Damp(Quaternion a, Quaternion b, float lambda, float dt)
        {
            return Quaternion.Slerp(a, b, 1 - Mathf.Exp(-lambda * dt));
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
