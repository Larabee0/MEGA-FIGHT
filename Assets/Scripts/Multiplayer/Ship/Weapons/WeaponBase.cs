using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerRunTime
{
    public class WeaponBase : MonoBehaviour
    {
        [Header("Base Weapon Settings")]
        [SerializeField] protected ShipPartMP weaponPart;
        [Range(1f, 100f)] public float damage = 10f;
        [HideInInspector] public FireGroup fireGroup;
        [HideInInspector] public FireControlMP controller;
        private SpaceshipMP spaceship;

        public virtual Transform TargetPoint { get { return controller.TargetPoint; } } 
        public virtual Vector3 TargetPointPos => TargetPoint.position;

        public SpaceshipMP Spaceship => spaceship;
        public LaserSpawnerMP LaserSpawnerMP => Spaceship.laserSpawnerMP;

        public ulong LocalClientId => controller.LocalClientId;
        public bool Grouped => fireGroup != null;
        public bool Owner;
        public virtual void Awake()
        {
            spaceship = GetComponentInParent<SpaceshipMP>();
            controller = GetComponentInParent<FireControlMP>();
        }
        public virtual void Start()
        {
            Owner = Spaceship.IsOwner;
        }

        public virtual void Update()
        {
            if (!Owner)
            {
                return;
            }
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