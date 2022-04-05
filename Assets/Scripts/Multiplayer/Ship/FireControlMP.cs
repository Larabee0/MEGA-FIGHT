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

        //private ShipPartMP[] WeaponOutputPoints;
        private WeaponOutputPoint[] WeaponOutputPoints;
        private Transform TargetPoint;

        [SerializeField] private float dstSenstivity = 1f;
        [SerializeField] private float minDst;
        [SerializeField] private float maxDst;
        [SerializeField] private float targetDistance = 50f;

        [SerializeField] [Range(0f,1f)] float fireIntervalMin = 0.01f;
        [SerializeField] [Range(0f, 1f)] float fireIntervalMax = 0.05f;
        [SerializeField] [Range(500f, 2000f)] float laserRange = 1000f;
        [SerializeField][Range(1f, 100f)] float damage = 10f;
        private float fireInterval = 0f;
        private int currentWeaponIndex = 0;
        private bool fire = false;
        private ulong LocalClientId = ulong.MaxValue;

        public float TargetDistance
        {
            get => targetDistance;
            set
            {
                spaceship.TargetDistance = targetDistance = math.clamp(value, minDst, maxDst);
            }
        }



        private void OnEnable()
        {
            UserCustomisableSettings userSettings = UserCustomisableSettings.instance;
            if (userSettings == null)
            {
                Debug.LogError(name + "MouseFlightController - No User Customisable Settings Instance");
                enabled = false;
                return;
            }

            targetDistance = userSettings.userSettings.DefaultAimDistance;
            dstSenstivity = userSettings.userSettings.AimDistanceSenstivity;

            inputControl.FlightActions.Shoot.canceled += ResetFireInterval;
            inputControl.FlightActions.Shoot.canceled += ToggleOffFire;
            inputControl.FlightActions.Shoot.started += ToggleOnFire;
            inputControl.FlightActions.ScrollWheel.performed += SetTargetDistance;
            LocalClientId = NetworkManager.Singleton.LocalClientId;
        }
        private void OnDisable()
        {
            inputControl.FlightActions.Shoot.canceled -= ResetFireInterval;
            inputControl.FlightActions.Shoot.canceled -= ToggleOffFire;
            inputControl.FlightActions.Shoot.started -= ToggleOnFire;
            inputControl.FlightActions.ScrollWheel.performed -= SetTargetDistance;
            LocalClientId = ulong.MaxValue;
            NullOut();
        }

        private void ResetFireInterval(InputAction.CallbackContext context) => fireInterval = 0f;

        private void ToggleOnFire(InputAction.CallbackContext context) => fire = true;
        private void ToggleOffFire(InputAction.CallbackContext context) => fire = false;

        public void GetComponentReferences(SpaceshipMP ship)
        {
            spaceship = ship;
            laserSpawnerMP = spaceship.laserSpawnerMP;
            WeaponOutputPoints = GetWeaponOutputPoints(spaceship.WeaponOutputPoints);
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
            switch (fire)
            {
                case true:
                    Fire();
                    break;
            }

            //Debug.DrawRay(WeaponOutputPoints[0].position, TargetPoint.position - WeaponOutputPoints[0].position);
        }

        private void Fire()
        {
            fireInterval -= Time.deltaTime;

            switch (fireInterval)
            {
                case <= 0f:
                    fireInterval = UnityEngine.Random.Range(fireIntervalMin, fireIntervalMax);
                    if (WeaponOutputPoints[currentWeaponIndex].weaponSource == null)
                    {
                        currentWeaponIndex = (currentWeaponIndex + 1) % WeaponOutputPoints.Length;
                        break;
                    }
                    Vector3 direciton = TargetPoint.position - WeaponOutputPoints[currentWeaponIndex].point.position;

                    Ray ray = new(WeaponOutputPoints[currentWeaponIndex].point.position, direciton);

                    Vector3 endPoint;

                    switch (Physics.SphereCast(ray, 0.005f, out RaycastHit hit, laserRange))
                    {
                        case true:
                            endPoint = hit.point;
                            switch (hit.collider.gameObject.TryGetComponent(out ShipPartMP part))
                            {
                                case true:
                                    Debug.Log(part);
                                    Debug.Log(part.owner);
                                    Debug.Log(part.owner.shipHealthManagerMP);
                                    float angleWeight = Mathf.InverseLerp(90f, 0f, Mathf.Abs(Mathf.DeltaAngle(Vector3.Angle(hit.normal, ray.direction), 90f)));
                                    //Debug.LogFormat("Hit Daamge: {0}", damage * angleWeight);
                                    part.owner.shipHealthManagerMP.HitServerRpc(part.HierarchyID, LocalClientId, damage * angleWeight);
                                    break;
                            }
                            break;
                        case false:
                            endPoint = TargetPoint.position + (direciton * laserRange);
                            break;
                    }

                    laserSpawnerMP.ClientLaserSpawnCall(new float3x2(spaceship.transform.InverseTransformPoint(WeaponOutputPoints[currentWeaponIndex].point.position), spaceship.transform.InverseTransformPoint(endPoint)));
                    //Debug.DrawLine(WeaponOutputPoints[currentWeaponIndex].position, endPoint, Color.red, 0.25f);
                    currentWeaponIndex = (currentWeaponIndex + 1) % WeaponOutputPoints.Length;
                    break;
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

        private WeaponOutputPoint[] GetWeaponOutputPoints(ShipPartMP[] weapons)
        {
            int pointCount = weapons.Length;
            for (int i = 0; i < weapons.Length; i++)
            {
                ShipPartMP weapon = weapons[i];
                if (weapon.MultiPoint)
                {
                    pointCount += weapon.AnimationPoints.Length - 1;
                }
            }

            WeaponOutputPoint[] points = new WeaponOutputPoint[pointCount];

            for (int i = 0,p = 0; i < weapons.Length; i++,p++)
            {
                ShipPartMP weapon = weapons[i];
                if (weapon.MultiPoint)
                {
                    for (int j = 0; j < weapon.AnimationPoints.Length; j++, p++)
                    {
                        points[p] = new WeaponOutputPoint
                        {
                            weaponSource = weapon,
                            point = weapon.AnimationPoints[j]
                        };
                    }
                    p--;
                }
                else
                {
                    points[p] = new WeaponOutputPoint
                    {
                        weaponSource = weapon,
                        point = weapon.AnimationPoint
                    };
                }
            }
            return points;
        }

        public class WeaponOutputPoint
        {
            public ShipPartMP weaponSource;
            public Transform point;
        }
    }
}