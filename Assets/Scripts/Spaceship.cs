﻿//
// Copyright (c) Brian Hernandez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Unity.Netcode;
using UnityEngine;

namespace MFlight.Demo
{
    /// <summary>
    /// This is a very demo-ey example of how to interpret the input generated by the
    /// MouseFlightController. The plane flies towards the MouseAimPos automatically in
    /// a similar fashion to how War Thunder's Instructor does it. There are also
    /// keyboard overrides for flight control. It's not perfect, but it works well enough
    /// for an example.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Spaceship : NetworkBehaviour
    {
        //[Header("Components")]
        //public MouseFlightController controller = null;
        public NetworkVariable<Vector3> MouseAimPos = new(Vector3.zero);

        [Header("Physics")]
        [Tooltip("Force to push plane forwards with")] public float thrust = 100f;
        [Tooltip("Pitch, Yaw, Roll")] public Vector3 turnTorque = new(90f, 25f, 45f);
        [Tooltip("Multiplier for all forces")] public float forceMult = 1000f;

        [Header("Autopilot")]
        [Tooltip("Sensitivity for autopilot flight.")] public float sensitivity = 5f;
        [Tooltip("Angle at which airplane banks fully into target.")] public float aggressiveTurnAngle = 10f;

        [Header("Input")]
        [SerializeField] [Range(-1f, 1f)] private NetworkVariable<float> pitch = new();
        [SerializeField] [Range(-1f, 1f)] private NetworkVariable<float> yaw = new();
        [SerializeField] [Range(-1f, 1f)] private NetworkVariable<float> roll = new();
        [SerializeField] [Range(-0.25f, 1f)] private NetworkVariable<float> throttle = new();
        [SerializeField] private float throttleSenstivity = 1f;

        public float Pitch { set { pitch.Value = Mathf.Clamp(value, -1f, 1f); } get { return pitch.Value; } }
        public float Yaw { set { yaw.Value = Mathf.Clamp(value, -1f, 1f); } get { return yaw.Value; } }
        public float Roll { set { roll.Value = Mathf.Clamp(value, -1f, 1f); } get { return roll.Value; } }
        public float Throttle { set { throttle.Value = Drag = Mathf.Clamp(value, -0.25f, 1f); } get { return throttle.Value; } }

        private float Drag { set { rigid.drag = Mathf.Clamp(Mathf.Lerp(1f, 5f, Mathf.Abs(value)*1.2f), 1f, 5f); } }

        private Rigidbody rigid;

        private bool rollOverride = false;
        private bool yawOverride = false;
        private bool pitchOverride = false;

        private void Awake()
        {
            rigid = GetComponent<Rigidbody>();

            //if (controller == null)
            //    Debug.LogError(name + ": Plane - Missing reference to MouseFlightController!");
        }

        private void Update()
        {
            //if (!IsOwner) return;
            // When the player commands their own stick input, it should override what the
            // autopilot is trying to do.
            rollOverride = false;
            pitchOverride = false;
            yawOverride = false;

            float keyboardRoll = Input.GetAxis("Horizontal");
            if (Mathf.Abs(keyboardRoll) > .25f)
            {
                rollOverride = true;
            }

            float keyboardPitch = Input.GetAxis("Vertical");
            if (Mathf.Abs(keyboardPitch) > .25f)
            {
                pitchOverride = true;
                rollOverride = true;
            }

            float keyboardYaw = Input.GetAxis("Yaw");
            if(Mathf.Abs(keyboardYaw) > .25f)
            {
                yawOverride = true;
                pitchOverride = true;
                rollOverride = true;
            }
            float throttle = 0f;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                throttle += throttleSenstivity * Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.LeftControl))
            {
                throttle -= throttleSenstivity * Time.deltaTime;
            }

            // Calculate the autopilot stick inputs.
            RunAutopilot(MouseAimPos.Value, out float autoYaw, out float autoPitch, out float autoRoll);

            // Use either keyboard or autopilot input.
            //yaw.Value = yawOverride ? keyboardYaw : autoYaw;
            //pitch.Value = pitchOverride ? keyboardPitch : autoPitch;
            //roll.Value = rollOverride ? keyboardRoll : autoRoll;
            if (!IsOwner) return;
            SetOverridesAndThrottleServerRPC(yawOverride ? keyboardYaw : autoYaw,
                pitchOverride ? keyboardPitch : autoPitch,
                rollOverride ? keyboardRoll : autoRoll, throttle);
        }
        [ServerRpc]
        public void SetMouseAimPosServerRPC(Vector3 aimPos)
        {
            MouseAimPos.Value = aimPos;
        }

        [ServerRpc]
        public void SetOverridesAndThrottleServerRPC(float yaw, float pitch, float roll, float throttle)
        {
            this.yaw.Value = yaw;
            this.pitch.Value = pitch;
            this.roll.Value = roll;
            this.throttle.Value += throttle;
        }

        private void RunAutopilot(Vector3 flyTarget, out float yaw, out float pitch, out float roll)
        {
            // This is my usual trick of converting the fly to position to local space.
            // You can derive a lot of information from where the target is relative to self.
            Vector3 localFlyTarget = transform.InverseTransformPoint(flyTarget).normalized * sensitivity;
            float angleOffTarget = Vector3.Angle(transform.forward, flyTarget - transform.position);

            // IMPORTANT!
            // These inputs are created proportionally. This means it can be prone to
            // overshooting. The physics in this example are tweaked so that it's not a big
            // issue, but in something with different or more realistic physics this might
            // not be the case. Use of a PID controller for each axis is highly recommended.

            // ====================
            // PITCH AND YAW
            // ====================

            // Yaw/Pitch into the target so as to put it directly in front of the aircraft.
            // A target is directly in front the aircraft if the relative X and Y are both
            // zero. Note this does not handle for the case where the target is directly behind.
            yaw = Mathf.Clamp(localFlyTarget.x, -1f, 1f);
            pitch = -Mathf.Clamp(localFlyTarget.y, -1f, 1f);

            // ====================
            // ROLL
            // ====================

            // Roll is a little special because there are two different roll commands depending
            // on the situation. When the target is off axis, then the plane should roll into it.
            // When the target is directly in front, the plane should fly wings level.

            // An "aggressive roll" is input such that the aircraft rolls into the target so
            // that pitching up (handled above) will put the nose onto the target. This is
            // done by rolling such that the X component of the target's position is zeroed.
            float agressiveRoll = Mathf.Clamp(localFlyTarget.x, -1f, 1f);

            // A "wings level roll" is a roll commands the aircraft to fly wings level.
            // This can be done by zeroing out the Y component of the aircraft's right.
            float wingsLevelRoll = transform.right.y;

            // Blend between auto level and banking into the target.
            float wingsLevelInfluence = Mathf.InverseLerp(0f, aggressiveTurnAngle, angleOffTarget);
            roll = Mathf.Lerp(wingsLevelRoll, agressiveRoll, wingsLevelInfluence);
        }

        private void FixedUpdate()
        {
            // Ultra simple flight where the plane just gets pushed forward and manipulated
            // with torques to turn.
            rigid.AddRelativeForce(forceMult * Throttle * thrust * Vector3.forward, ForceMode.Force);
            rigid.AddRelativeTorque(forceMult * new Vector3(turnTorque.x * pitch.Value, turnTorque.y * yaw.Value, -turnTorque.z * roll.Value), ForceMode.Force);
        }
    }
}
