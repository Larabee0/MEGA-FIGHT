using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerRunTime
{
    public class FixedLaserBolt : WeaponBase
    {
        public WeaponOutputPoint weaponOutputPoint;
        [SerializeField][Range(0f, 1f)] float fireIntervalMin = 0.01f;
        [SerializeField][Range(0f, 1f)] float fireIntervalMax = 0.05f;
        [SerializeField][Range(500f, 2000f)] float laserRange = 1000f;
        [SerializeField][Range(100f, 1000f)] float MuzzelVelocity = 250f;
        private float fireInterval = 0f;

        public override void Start()
        {
            base.Start();
            weaponOutputPoint = new WeaponOutputPoint { point = weaponPart.AnimationPoint, weaponSource = weaponPart };
        }

        // Update is called once per frame
        public override void GroupFire()
        {
            LaserFire();
        }

        public override void Fire()
        {
            fireInterval -= Time.deltaTime;

            switch (fireInterval)
            {
                case <= 0f:
                    fireInterval = UnityEngine.Random.Range(fireIntervalMin, fireIntervalMax);
                    LaserFire();
                    break;
            }
        }

        private void LaserFire()
        {

            if (weaponOutputPoint.weaponSource == null)
            {
                return;
            }

            Vector3 direciton = TargetPointPos - weaponOutputPoint.point.position;
            LaserSpawnerMP.SpawnLaserBolt(weaponOutputPoint.point.position, direciton, new Vector2(MuzzelVelocity,laserRange),damage);
        }
    }
}