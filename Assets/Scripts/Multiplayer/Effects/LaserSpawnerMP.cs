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
        [SerializeField] private Color32 laserColour;

        private void Start()
        {
            if (IsOwner)
            {
                UserCustomisableSettings.instance.OnUserSettingsChanged += SetLaserColour;
            }
        }

        public override void OnDestroy()
        {
            if (IsOwner)
            {
                UserCustomisableSettings.instance.OnUserSettingsChanged-= SetLaserColour;
            }
        }

        private void SetLaserColour()
        {
            UserCustomisableSettings userSettings = UserCustomisableSettings.instance;
            if (userSettings.userSettings.OverrideLaserColour)
            {
                laserColour = userSettings.userSettings.PlayerLaserColour;
            }
        }

        public void ClientLaserSpawnCall(float3x2 points)
        {
            SpawnLaserServerRPC(points, laserColour);
            Instantiate(laserPrefab, transform).Show(points, laserColour);
        }

        [ServerRpc(Delivery = RpcDelivery.Unreliable)]
        private void SpawnLaserServerRPC(float3x2 points, Color32 laserColour)
        {
            SpawnLaserClientRPC(points, laserColour);
        }

        [ClientRpc(Delivery = RpcDelivery.Unreliable)]
        private void SpawnLaserClientRPC(float3x2 points, Color32 laserColour)
        {
            if (IsOwner) { return; }
            Instantiate(laserPrefab, transform).Show(points, laserColour);
        }
    }
}