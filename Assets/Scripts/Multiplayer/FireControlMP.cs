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

        [SerializeField] [Range(0f,1f)] float fireIntervalMin = 0.01f;
        [SerializeField] [Range(0f, 1f)] float fireIntervalMax = 0.05f;
        private float fireInterval = 0f;
        private int currentWeaponIndex = 0;


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
            TargetDistance += Input.GetAxis("Mouse ScrollWheel") * dstSenstivity * Time.deltaTime;

            if (Input.GetMouseButton(0))
            {
                Fire();
            }
            if (Input.GetMouseButtonUp(0))
            {
                fireInterval = 0f;
            }
        }

        private void Fire()
        {
            fireInterval -= Time.deltaTime;
            if(fireInterval <= 0f)
            {
                fireInterval = UnityEngine.Random.Range(fireIntervalMin, fireIntervalMax);
                Debug.DrawLine(WeaponOutputPoints[currentWeaponIndex].position, TargetPoint.position, Color.red, 0.25f);
                currentWeaponIndex = (currentWeaponIndex + 1) % WeaponOutputPoints.Length;
            }
        }
    }
}