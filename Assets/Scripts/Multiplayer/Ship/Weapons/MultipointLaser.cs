using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MultiplayerRunTime
{
    public class MultipointLaser : WeaponBase
    {
        private WeaponOutputPoint[] WeaponOutputPoints;
        [SerializeField] bool Syncronous = false;
        [SerializeField] SyncronousGroup[] syncronousGroups;
        [SerializeField][Range(0f, 1f)] float fireIntervalMin = 0.01f;
        [SerializeField][Range(0f, 1f)] float fireIntervalMax = 0.05f;
        [SerializeField][Range(500f, 2000f)] float laserRange = 1000f;
        private float fireInterval = 0f;
        private int currentWeaponIndex = 0;
        private int currentGroupIndex = 0;

        public override void Start()
        {
            WeaponOutputPoints=GetWeaponOutputPoints();
        }


        public override void GroupFire()
        {
            MultiFire();
        }

        public override void Fire()
        {
            fireInterval -= Time.deltaTime;

            switch (fireInterval)
            {
                case <= 0f:
                    fireInterval = UnityEngine.Random.Range(fireIntervalMin, fireIntervalMax);
                    MultiFire();
                    break;
            }
        }

        private void MultiFire()
        {
            if (Syncronous)
            {
                for (int i = 0; i < syncronousGroups[currentGroupIndex].indicies.Length; i++)
                {
                    currentWeaponIndex = syncronousGroups[currentGroupIndex].indicies[i];
                    LaserFire();
                }
                currentGroupIndex = (currentGroupIndex + 1) % syncronousGroups.Length;
            }
            else
            {
                LaserFire();
                currentWeaponIndex = (currentWeaponIndex + 1) % WeaponOutputPoints.Length;
            }
        }

        private void LaserFire()
        {

            if (WeaponOutputPoints[currentWeaponIndex].weaponSource == null)
            {
                //currentWeaponIndex = (currentWeaponIndex + 1) % WeaponOutputPoints.Length;
                return;
            }
            Vector3 direciton = TargetPointPos - WeaponOutputPoints[currentWeaponIndex].point.position;

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
                    endPoint = TargetPointPos + (direciton * laserRange);
                    break;
            }

            LaserSpawnerMP.ClientLaserSpawnCall(new float3x2(Spaceship.transform.InverseTransformPoint(WeaponOutputPoints[currentWeaponIndex].point.position), Spaceship.transform.InverseTransformPoint(endPoint)));
        }


        private WeaponOutputPoint[] GetWeaponOutputPoints()
        {
            ShipPartMP weapons = weaponPart;
            int pointCount = 1;
            if (!weapons.MultiPoint)
            {
                Debug.LogError("Multi point weapon source only has 1 output point!");
                return null;
            }

            pointCount += weapons.AnimationPoints.Length - 1;
            WeaponOutputPoint[] points = new WeaponOutputPoint[pointCount];
            for (int j = 0; j < weapons.AnimationPoints.Length; j++)
            {
                points[j] = new WeaponOutputPoint
                {
                    weaponSource = weapons,
                    point = weapons.AnimationPoints[j]
                };
            }
            return points;
        }

        [System.Serializable]
        public struct SyncronousGroup
        {
            public int[] indicies;
        }
    }
}
