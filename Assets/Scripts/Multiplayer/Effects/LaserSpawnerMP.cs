using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace MultiplayerRunTime
{
    public class LaserSpawnerMP : NetworkBehaviour
    {
        [SerializeField] private LaserCompMP laserPrefab;

        public void ClientLaserSpawnCall(float3x2 points)
        {
            SpawnLaserServerRPC(points);
            Instantiate(laserPrefab, transform).Show(points);
        }

        [ServerRpc(Delivery = RpcDelivery.Unreliable)]
        private void SpawnLaserServerRPC(float3x2 points)
        {
            SpawnLaserClientRPC(points);
        }

        [ClientRpc(Delivery = RpcDelivery.Unreliable)]
        private void SpawnLaserClientRPC(float3x2 points)
        {
            if (IsOwner) { return; }
            Instantiate(laserPrefab, transform).Show(points);
        }
    }
}