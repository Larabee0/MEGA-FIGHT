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
        public SpaceshipMP spaceship;
        private MouseFlightControllerMP controller;

        //private ShipPartMP[] WeaponOutputPoints;
        private WeaponOutputPoint[] WeaponOutputPoints;
        public Transform TargetPoint;
        public Vector3 lastTargetPointPos;
        public bool locked = false;
        public float lockTime = 1f;
        public float unlockTime = 0.5f;
        public float currentLockTime = 0f;

        [SerializeField] private float dstSenstivity = 1f;
        [SerializeField] private float minDst;
        [SerializeField] private float maxDst;
        [SerializeField] private float targetDistance = 50f;

        [SerializeField] [Range(0f,1f)] float fireIntervalMin = 0.01f;
        [SerializeField] [Range(0f, 1f)] float fireIntervalMax = 0.05f;
        [SerializeField] [Range(500f, 2000f)] float laserRange = 1000f;
        [SerializeField] [Range(1f, 100f)] float damage = 10f;
        private float fireInterval = 0f;
        private int currentWeaponIndex = 0;
        [HideInInspector] public bool fire = false;
        [HideInInspector] public ulong LocalClientId = ulong.MaxValue;

        public float TargetDistance
        {
            get => targetDistance;
            set
            {
                targetDistance = math.clamp(value, minDst, maxDst);
                LocalTargetPointPos = new(TargetPoint.localPosition.x, TargetPoint.localPosition.y, targetDistance);
            }
        }

        private Vector3 LocalTargetPointPos { set => TargetPoint.localPosition = value; get => TargetPoint.localPosition; }

        public void SetUp(MouseFlightControllerMP mouseFlightControllerMP)
        {
            spaceship = GetComponent<SpaceshipMP>();
            controller = mouseFlightControllerMP;
            controller.fireControl = this;
            laserSpawnerMP = GetComponent<LaserSpawnerMP>();
        }

        private void OnEnable()
        {
            currentLockTime = 0f;
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
            lastTargetPointPos = LocalTargetPointPos;
            TargetDistance = TargetDistance;
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

        private void SetTargetDistance(InputAction.CallbackContext context)
        {
            if (!locked)
            {
                TargetDistance += inputControl.FlightActions.ScrollWheel.ReadValue<Vector2>().y * dstSenstivity * Time.deltaTime;
            }
        }

        private void Update()
        {
            AutoTarget();
            switch (fire)
            {
                case true:
                    //Fire();
                    break;
            }
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
                                    float angleWeight = Mathf.InverseLerp(90f, 0f, Mathf.Abs(Mathf.DeltaAngle(Vector3.Angle(hit.normal, ray.direction), 90f)));
                                    part.owner.shipHealthManagerMP.HitServerRpc(part.HierarchyID, LocalClientId, damage * angleWeight);
                                    break;
                            }
                            break;
                        case false:
                            endPoint = TargetPoint.position + (direciton * laserRange);
                            break;
                    }

                    laserSpawnerMP.ClientLaserSpawnCall(spaceship.transform.InverseTransformPoint(WeaponOutputPoints[currentWeaponIndex].point.position), spaceship.transform.InverseTransformPoint(endPoint));
                    currentWeaponIndex = (currentWeaponIndex + 1) % WeaponOutputPoints.Length;
                    break;
            }
        }

        private void AutoTarget()
        {
            Ray mousePosRay = Camera.main.ScreenPointToRay(Camera.main.WorldToScreenPoint(controller.MouseAimPos));
            if (Physics.SphereCast(mousePosRay, 12.5f, out RaycastHit hitInfo))
            {
                if (hitInfo.transform.root.TryGetComponent(out SpaceshipMP targetShip))
                {
                    if (!locked)
                    {
                        currentLockTime += lockTime * Time.deltaTime;
                        if(currentLockTime > lockTime)
                        {
                            Debug.LogFormat("Locked {0}", targetShip.shipHealthManagerMP.shipHierarchy.Label);
                            locked = true;
                            lastTargetPointPos = LocalTargetPointPos;
                        }
                    }
                    else
                    {
                        TargetPoint.position = targetShip.transform.position;
                    }

                    
                }
            }
            else
            {
                if (locked)
                {
                    currentLockTime -= unlockTime * Time.deltaTime;
                    if(currentLockTime <= 0)
                    {
                        Debug.Log("Lost Target");
                        locked = false;
                        LocalTargetPointPos = lastTargetPointPos;
                    }
                }
            }
        }

        private void NullOut()
        {
            fire = false;
            WeaponOutputPoints = null;
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
    }
}