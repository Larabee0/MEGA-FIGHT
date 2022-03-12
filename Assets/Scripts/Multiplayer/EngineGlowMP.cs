using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerRunTime
{
    public class EngineGlowMP : MonoBehaviour
    {
        private SpaceshipMP spaceshipMP;

        [Header("Editor Only (DrawGizmos)")]
        [SerializeField][Range(0f, 1f)] float throttleSim = 0f;
        [Space]
        [Header("Settings")]
        [SerializeField] private MeshRenderer[] engineGlows;
        [SerializeField] private Color baseColour;
        [SerializeField] private Color emissionColour;

        [SerializeField] private float intesntiyMin = 0f;
        [SerializeField] private float intensityMax = 3.6f;

        private float Intensity { get => Mathf.Lerp(intesntiyMin, intensityMax, ABSThrottle); }

        private float ABSThrottle
        {
            get
            {
                return Mathf.Abs(spaceshipMP == null ? throttleSim : spaceshipMP.Throttle);
            }
        }

        private void Awake()
        {
            spaceshipMP = GetComponent<SpaceshipMP>();
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