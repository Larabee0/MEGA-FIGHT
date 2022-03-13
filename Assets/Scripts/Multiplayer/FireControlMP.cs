using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MultiplayerRunTime
{
    public class FireControlMP : MonoBehaviour
    {
        private InputControl inputControl;
        private LaserSpawnerMP laserSpawnerMP;
        private SpaceshipMP spaceship;

        private Transform[] WeaponOutputPoints;
        private Transform TargetPoint;

        [SerializeField] private float dstSenstivity = 1f;
        [SerializeField] private float minDst;
        [SerializeField] private float maxDst;
        [SerializeField] private float targetDistance = 50f;

        [SerializeField] [Range(0f,1f)] float fireIntervalMin = 0.01f;
        [SerializeField] [Range(0f, 1f)] float fireIntervalMax = 0.05f;
        [SerializeField][Range(500f, 2000f)] float laserRange = 1000f;
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

        private void Awake()
        {
            PasswordLobbyMP.Singleton.OnClientDisconnects += NullOutandStop;
            inputControl = InputControl.Singleton;
            inputControl.FlightActions.Shoot.canceled += (InputAction.CallbackContext context) => { fireInterval = 0f; };
        }

        public void GetComponentReferences(SpaceshipMP ship)
        {
            spaceship = ship;
            laserSpawnerMP = spaceship.laserSpawnerMP;
            WeaponOutputPoints = spaceship.WeaponOutputPoints;
            TargetPoint = spaceship.TargetPoint;
            TargetDistance = TargetDistance;
            enabled = true;
        }

        private void Update()
        {
            TargetDistance += inputControl.FlightActions.ScrollWheel.ReadValue<Vector2>().y * dstSenstivity * Time.deltaTime;

            if (inputControl.FlightActions.Shoot.IsPressed())
            {
                Fire();
            }

            //Debug.DrawRay(WeaponOutputPoints[0].position, TargetPoint.position - WeaponOutputPoints[0].position);
        }

        private void Fire()
        {
            fireInterval -= Time.deltaTime;
            if(fireInterval <= 0f)
            {
                fireInterval = UnityEngine.Random.Range(fireIntervalMin, fireIntervalMax);
                Vector3 direciton = TargetPoint.position - WeaponOutputPoints[currentWeaponIndex].position;
                Ray ray = new(WeaponOutputPoints[currentWeaponIndex].position, direciton);
                Vector3 endPoint = TargetPoint.position + (direciton * laserRange);
                if(Physics.Raycast(ray, out RaycastHit hit, laserRange))
                {
                    endPoint = hit.point;
                    if(hit.collider.gameObject.TryGetComponent(out ShipPartMP part))
                    {
                        byte ID = part.HierarchyID;
                        SpaceshipMP shipHit = part.owner;
                    }
                }

                laserSpawnerMP.ClientLaserSpawnCall(new float3x2(spaceship.transform.InverseTransformPoint(WeaponOutputPoints[currentWeaponIndex].position), spaceship.transform.InverseTransformPoint(endPoint)));
                //Debug.DrawLine(WeaponOutputPoints[currentWeaponIndex].position, endPoint, Color.red, 0.25f);
                currentWeaponIndex = (currentWeaponIndex + 1) % WeaponOutputPoints.Length;
            }
        }

        private void NullOutandStop()
        {
            laserSpawnerMP = null;
            spaceship = null;

            WeaponOutputPoints = null;
            TargetPoint = null;
            enabled = false;
        }
    }
}