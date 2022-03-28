using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace MultiplayerRunTime
{
    public class EngineGlowMP : NetworkBehaviour
    {
        private SpaceshipMP spaceship;
        [SerializeField] private ShipPartMP[] engineParts;
        [Header("Editor Only (DrawGizmos)")]
        [SerializeField][Range(-0.25f, 1f)] private float throttleSim = 0f;
        [Space]
        [Header("Settings")]
        [SerializeField] private MeshRenderer[] engineGlows;
        [SerializeField] private Color baseColour;
        [SerializeField] private Color emissionColour;

        [SerializeField] private float intesntiyMin = 0f;
        [SerializeField] private float intensityMax = 3.6f;

        private float throttleLastFixedUpdate = float.NegativeInfinity;

        private float GetIntensity(float throttle)
        {
            return Mathf.Lerp(intesntiyMin, intensityMax, throttle);
        }

        private float ABSThrottle => Mathf.Abs(throttleSim);

        private void Awake()
        {
            spaceship = GetComponent<SpaceshipMP>();
        }

        private void Start()
        {
            if (IsOwner)
            {
                SetSettings();
                UserCustomisableSettings.instance.OnUserSettingsChanged += SetSettings;
            }
        }

        private void Update()
        {
            Color calBase = baseColour;
            calBase.a = ABSThrottle != 0f ? baseColour.a : 0;
            for (int i = 0; i < engineGlows.Length; i++)
            {
                if (engineGlows[i] != null && engineParts[i] != null)
                {
                    float maxThrottle = spaceship.shipHealthManagerMP.CalculatePartEfficiency(engineParts[i].HierarchyID);
                    float intensity = GetIntensity(math.clamp(ABSThrottle, 0, maxThrottle));
                    Material material = engineGlows[i].material;
                    material.SetColor("_BaseColor", calBase);
                    material.SetColor("_EmissionColor", emissionColour * intensity);
                }
            }
        }

        private void FixedUpdate()
        {
            switch (IsOwner)
            {
                case true:
                    throttleSim = spaceship.Throttle;
                    if (throttleLastFixedUpdate != throttleSim)
                    {
                        SetThrottleServerRPC(throttleSim);
                        throttleLastFixedUpdate = throttleSim;
                    }
                    break;
            }
        }

        public override void OnDestroy()
        {
            if (IsOwner)
            {
                UserCustomisableSettings.instance.OnUserSettingsChanged -= SetSettings;
            }
        }

        private void SetSettings()
        {
            UserCustomisableSettings userSettings = UserCustomisableSettings.instance;
            if (userSettings.userSettings.OverrideEngineColour)
            {
                Color32 userColour = userSettings.userSettings.PlayerEngineColour;
                SetEngineColourServerRPC(userColour);
                baseColour = userColour;
                baseColour.a = 45;
                emissionColour = userColour;
            }
        }

        [ServerRpc(Delivery = RpcDelivery.Unreliable)]
        private void SetThrottleServerRPC(float value)
        {
            SetThrottleClientRPC(value);
        }

        [ClientRpc(Delivery = RpcDelivery.Unreliable)]
        private void SetThrottleClientRPC(float value)
        {
            switch (IsOwner)
            {
                case false:
                    throttleSim = value;
                    break;
            }
        }

        [ServerRpc(Delivery = RpcDelivery.Reliable)]
        private void SetEngineColourServerRPC(Color32 colour)
        {
            SetEngineColourClientRPC(colour);
            baseColour = colour;
            baseColour.a = 45;
            emissionColour = colour;
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void SetEngineColourClientRPC(Color32 colour)
        {
            if (IsOwner) { return; }
            baseColour = colour;
            baseColour.a = 45;
            emissionColour = colour;
        }
    }
}