using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MultiplayerRunTime
{
    public class FixedLaser : WeaponBase
    {
        public WeaponOutputPoint weaponOutputPoint;
        [SerializeField][Range(0f, 1f)] float fireIntervalMin = 0.01f;
        [SerializeField][Range(0f, 1f)] float fireIntervalMax = 0.05f;
        [SerializeField][Range(500f, 2000f)] float laserRange = 1000f;
        private float fireInterval = 0f;

        public override void Start()
        {
            weaponOutputPoint = new WeaponOutputPoint { point = weaponPart.AnimationPoint, weaponSource = weaponPart };
        }

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

            Ray ray = new(weaponOutputPoint.point.position, direciton);

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
                    endPoint = TargetPointPos + (direciton * laserRange);
                    break;
            }

            LaserSpawnerMP.ClientLaserSpawnCall(new float3x2(Spaceship.transform.InverseTransformPoint(weaponOutputPoint.point.position), Spaceship.transform.InverseTransformPoint(endPoint)));
        }
    }
}