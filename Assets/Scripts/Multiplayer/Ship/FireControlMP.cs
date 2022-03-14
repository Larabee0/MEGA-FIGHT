using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MultiplayerRunTime
{
    public class FireControlMP : MonoBehaviour
    {
        public InputControl inputControl;
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
        private bool fire = false;

        public float TargetDistance
        {
            get => targetDistance;
            set
            {
                targetDistance = math.clamp(value, minDst, maxDst); ;
                spaceship.SetTargetDstServerRPC(targetDistance);
            }
        }

        private void OnEnable()
        {
            inputControl.FlightActions.Shoot.canceled += ResetFireInterval;
            inputControl.FlightActions.Shoot.canceled += ToggleOffFire;
            inputControl.FlightActions.Shoot.started += ToggleOnFire;
            inputControl.FlightActions.ScrollWheel.performed += SetTargetDistance;
        }
        private void OnDisable()
        {
            inputControl.FlightActions.Shoot.canceled -= ResetFireInterval;
            inputControl.FlightActions.Shoot.canceled -= ToggleOffFire;
            inputControl.FlightActions.Shoot.started -= ToggleOnFire;
            inputControl.FlightActions.ScrollWheel.performed -= SetTargetDistance;
            NullOut();
        }

        private void ResetFireInterval(InputAction.CallbackContext context) => fireInterval = 0f;

        private void ToggleOnFire(InputAction.CallbackContext context) => fire = true;
        private void ToggleOffFire(InputAction.CallbackContext context) => fire = false;

        public void GetComponentReferences(SpaceshipMP ship)
        {
            spaceship = ship;
            laserSpawnerMP = spaceship.laserSpawnerMP;
            WeaponOutputPoints = spaceship.WeaponOutputPoints;
            TargetPoint = spaceship.TargetPoint;
            TargetDistance = TargetDistance;
            enabled = true;
        }

        private void SetTargetDistance(InputAction.CallbackContext context)
        {
            TargetDistance += inputControl.FlightActions.ScrollWheel.ReadValue<Vector2>().y * dstSenstivity * Time.deltaTime;
        }

        private void Update()
        {
            if (fire)
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
                if (Physics.SphereCast(ray, 0.005f, out RaycastHit hit, laserRange))
                {
                    endPoint = hit.point;
                    if (hit.collider.gameObject.TryGetComponent(out ShipPartMP part))
                    {
                        byte ID = part.HierarchyID;
                        SpaceshipMP shipHit = part.owner;
                        shipHit.shipHealthManagerMP.HitServerRpc(ID, NetworkManager.Singleton.LocalClientId, 1f);
                    }
                }

                laserSpawnerMP.ClientLaserSpawnCall(new float3x2(spaceship.transform.InverseTransformPoint(WeaponOutputPoints[currentWeaponIndex].position), spaceship.transform.InverseTransformPoint(endPoint)));
                //Debug.DrawLine(WeaponOutputPoints[currentWeaponIndex].position, endPoint, Color.red, 0.25f);
                currentWeaponIndex = (currentWeaponIndex + 1) % WeaponOutputPoints.Length;
            }
        }

        private void NullOut()
        {
            fire = false;
            laserSpawnerMP = null;
            spaceship = null;

            WeaponOutputPoints = null;
            TargetPoint = null;
        }
    }
}