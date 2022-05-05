using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MultiplayerRunTime
{
    public class PartExplosion : MonoBehaviour
    {
        public IEnumerator Explode(Vector3 origin, ExplosionData data)
        {
            yield return new WaitForFixedUpdate();
            PasswordLobbyMP.Singleton.SpawnExplosion(origin,transform);
            yield return new WaitForEndOfFrame();
            
            Collider[] colliders = Physics.OverlapSphere(origin, data.Radius);
            HashSet<Rigidbody> unquieRigibodies = new();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].attachedRigidbody != null)
                {
                    unquieRigibodies.Add(colliders[i].attachedRigidbody);
                }
                if(colliders[i].gameObject.TryGetComponent(out ShipPartMP part) && NetworkManager.Singleton.IsHost)
                {
                    //part.owner.shipHealthManagerMP.HitServerRpc(part.HierarchyID,  NetworkManager.Singleton.LocalClientId, UnityEngine.Random.Range(0,data.Damage));
                }
            }
            Rigidbody[] bodies = new Rigidbody[unquieRigibodies.Count];
            unquieRigibodies.CopyTo(bodies);
            for (int i = 0; i < bodies.Length; i++)
            {
                bodies[i].AddExplosionForce(data.Force * data.ForceMult, origin, data.Radius);
            }
        }
    }

    [Serializable]
    public struct ExplosionData
    {
        public float Radius;
        public float Force;
        public float ForceMult;
        public float Damage;
    }
}