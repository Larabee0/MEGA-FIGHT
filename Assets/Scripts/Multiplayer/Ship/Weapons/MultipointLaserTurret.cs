using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MultiplayerRunTime
{
    public class MultipointLaserTurret : TurretBase
    {
        private readonly List<Transform> possibleTargets = new();
        private WeaponOutputPoint[] WeaponOutputPoints;
        [Header("Firing Settings")]
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
            base.Start();
            WeaponOutputPoints=GetWeaponOutputPoints();
            if (!Owner)
            {
                enabled = false;
                return;
            }
            InvokeRepeating(nameof(UpdateTargets), 0f, 5f);
            InvokeRepeating(nameof(SetTarget), 5f, 2.5f);
        }

        public override void Update()
        {
            if (!Owner)
            {
                return;
            }
            base.Update();
            if (!IsAimed)
            {
                return;
            }
            fireInterval -= Time.deltaTime;

            switch (fireInterval)
            {
                case <= 0f:
                    fireInterval = UnityEngine.Random.Range(fireIntervalMin, fireIntervalMax);
                    MultiFire();
                    break;
            }
        }

        private void UpdateTargets()
        {
            List<ShipPartMP> parts = new(FindObjectsOfType<ShipPartMP>());
            possibleTargets.Clear();
            for (int i = parts.Count - 1; i >= 0; i--)
            {
                if (parts[i].owner != Spaceship)
                {
                    possibleTargets.Add(parts[i].transform);
                }
            }
        }

        private void SetTarget()
        {
            if(possibleTargets == null || possibleTargets.Count == 0)
            {
                target = null;
                return;
            }
            if (TargetPoint != null && possibleTargets.Contains(TargetPoint))
            {
                if (CanHitTarget(TargetPointPos,laserRange))
                {
                    return;
                }
            }
            for (int i = 0; i < possibleTargets.Count; i++)
            {
                if(possibleTargets[i] == null)
                {
                    continue;
                }
                Transform possibleTarget = possibleTargets[i];
                if (CanHitTarget(possibleTarget.position, laserRange))
                {
                    target = possibleTarget;
                    return;
                }
            }
            target = null;
        }

        private void MultiFire()
        {
            if (TargetPoint == null)
            {
                return;
            }
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

            LaserSpawnerMP.ClientLaserSpawnCall(Spaceship.transform.InverseTransformPoint(WeaponOutputPoints[currentWeaponIndex].point.position), Spaceship.transform.InverseTransformPoint(endPoint));
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
