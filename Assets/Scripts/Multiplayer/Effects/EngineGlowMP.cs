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

        private float Intensity { get => Mathf.Lerp(intesntiyMin, intensityMax, ABSThrottle); }

        private float ABSThrottle
        {
            get
            {
                return Mathf.Abs(throttleSim);
            }
        }

        private void Awake()
        {
            spaceship = GetComponent<SpaceshipMP>();
        }

        private void Update()
        {
            Color calBase = baseColour;
            calBase.a = ABSThrottle != 0f ? baseColour.a : 0;
            for (int i = 0; i < engineGlows.Length; i++)
            {
                Material material = engineGlows[i].material;
                material.SetColor("_BaseColor", calBase);
                material.SetColor("_EmissionColor", emissionColour * Intensity);
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
        //private void OnDrawGizmos()
        //{
        //
        //    Color calBase = baseColour;
        //    calBase.a = ABSThrottle != 0f ? baseColour.a : 0;
        //    for (int i = 0; i < engineGlows.Length; i++)
        //    {
        //        Material sharedmaterial = engineGlows[i].sharedMaterial;
        //        sharedmaterial.SetColor("_BaseColor", calBase);
        //        sharedmaterial.SetColor("_EmissionColor", emissionColour * Intensity);
        //    }
        //}
    }
}