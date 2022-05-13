using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace MultiplayerRunTime
{
    public class LaserSpawnerMP : NetworkBehaviour
    {
        [SerializeField] private Rigidbody rigid;
        [SerializeField] private LaserBolt laserBoltPrefab;
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

        public void ClientRaycastLaserSpawnCall(Vector3 c0, Vector3 c1)
        {
            SpawnRaycastLaserServerRPC(c0,c1, laserColour);
            Instantiate(laserPrefab, transform).Show(new float3x2(c0,c1), laserColour);
        }

        [ServerRpc(Delivery = RpcDelivery.Unreliable)]
        private void SpawnRaycastLaserServerRPC(Vector3 c0, Vector3 c1, Color32 laserColour)
        {
            SpawnRaycastLaserClientRPC(c0, c1, laserColour);
        }

        [ClientRpc(Delivery = RpcDelivery.Unreliable)]
        private void SpawnRaycastLaserClientRPC(Vector3 c0, Vector3 c1, Color32 laserColour)
        {
            if (IsOwner) { return; }
            Instantiate(laserPrefab, transform).Show(new float3x2(c0, c1), laserColour);
        }

        public void SpawnLaserBolt(Vector3 pos, Vector3 fwd, Vector2 muzzleVelocity, float damage)
        {
            SpawnLaserBoltServerRpc(pos, fwd, muzzleVelocity,damage, laserColour);
        }

        [ServerRpc]
        private void SpawnLaserBoltServerRpc(Vector3 pos,Vector3 fwd,Vector2 muzzleVelocity, float damage,Color32 laserColour)
        {
            LaserBolt bolt = Instantiate(laserBoltPrefab, pos, Quaternion.identity);
            bolt.transform.forward = fwd;
            bolt.NetworkObject.SpawnWithOwnership(OwnerClientId);
            bolt.InitiliseMeshClientRpc(laserColour);
            Vector3 velocity = rigid.velocity + transform.forward * muzzleVelocity.x;
            bolt.SetPhysicsClientRpc(velocity,muzzleVelocity.y,damage);
        }
    }
}