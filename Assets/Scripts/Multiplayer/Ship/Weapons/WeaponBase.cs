using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerRunTime
{
    public class WeaponBase : MonoBehaviour
    {
        [SerializeField] protected ShipPartMP weaponPart;
        [Range(1f, 100f)] public float damage = 10f;
        [HideInInspector] public FireGroup fireGroup;
        [HideInInspector] public FireControlMP controller;

        public Transform TargetPoint => controller.TargetPoint;
        public Vector3 TargetPointPos => TargetPoint.position;

        public SpaceshipMP Spaceship => controller.spaceship;
        public LaserSpawnerMP LaserSpawnerMP => Spaceship.laserSpawnerMP;

        public ulong LocalClientId => controller.LocalClientId;
        public bool Grouped => fireGroup != null;
        public virtual void Awake()
        {
            controller = GetComponentInParent<FireControlMP>();
        }
        public virtual void Start() { }

        public virtual void Update()
        {
            switch (controller.fire && !Grouped)
            {
                case true:
                    Fire();
                    break;
            }
        }

        public virtual void Fire() { }

        public virtual void GroupFire() { }

    }

    public class WeaponOutputPoint
    {
        public ShipPartMP weaponSource;
        public Transform point;
    }
}