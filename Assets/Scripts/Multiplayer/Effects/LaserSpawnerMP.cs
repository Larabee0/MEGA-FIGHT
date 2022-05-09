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

        public void ClientLaserSpawnCall(Vector3 c0, Vector3 c1)
        {
            SpawnLaserServerRPC(c0,c1, laserColour);
            Instantiate(laserPrefab, transform).Show(new float3x2(c0,c1), laserColour);
        }

        [ServerRpc(Delivery = RpcDelivery.Unreliable)]
        private void SpawnLaserServerRPC(Vector3 c0, Vector3 c1, Color32 laserColour)
        {
            SpawnLaserClientRPC(c0, c1, laserColour);
        }

        [ClientRpc(Delivery = RpcDelivery.Unreliable)]
        private void SpawnLaserClientRPC(Vector3 c0, Vector3 c1, Color32 laserColour)
        {
            if (IsOwner) { return; }
            Instantiate(laserPrefab, transform).Show(new float3x2(c0, c1), laserColour);
        }
    }
}