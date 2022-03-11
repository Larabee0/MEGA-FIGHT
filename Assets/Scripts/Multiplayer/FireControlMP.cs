using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MultiplayerRunTime
{
    public class FireControlMP : MonoBehaviour
    {
        public SpaceshipMP spaceship;
        public MouseFlightControllerMP controller;

        private Transform[] WeaponOutputPoints;
        private Transform TargetPoint;

        [SerializeField] private float dstSenstivity = 1f;
        [SerializeField] private float minDst;
        [SerializeField] private float maxDst;
        [SerializeField] private float targetDistance = 50f;

        public float TargetDistance
        {
            get => targetDistance;
            set
            {
                targetDistance = math.clamp(value, minDst, maxDst); ;
                spaceship.SetTargetDstServerRPC(targetDistance);
            }
        }

        private void Start()
        {
            spaceship = controller.spaceshipController;
            WeaponOutputPoints = spaceship.WeaponOutputPoints;
            TargetPoint = spaceship.TargetPoint;
            TargetDistance = TargetDistance;
        }

        private void Update()
        {
            for (int i = 0; i < WeaponOutputPoints.Length; i++)
            {
                Debug.DrawLine(WeaponOutputPoints[i].position, TargetPoint.position, Color.red);
            }
            TargetDistance += Input.GetAxis("Mouse ScrollWheel") * dstSenstivity * Time.deltaTime;
        }
    }
}