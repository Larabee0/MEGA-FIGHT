using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MultiplayerRunTime
{
    public class LaserBolt : NetworkBehaviour
    {
        [SerializeField] private Rigidbody rigid;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;

        public Color32 laserColour;
        private Vector3 startPos;
        private float range;
        private float maxDmg;
        private bool gotStartPos = false;
        private void Update()
        {
            if (IsOwner)
            {
                if (!gotStartPos)
                {
                    startPos = transform.position;
                    gotStartPos = true;
                    return;
                }
                float dst = Vector3.Distance(startPos, transform.position);
                if (dst >= range)
                {
                    DestroyBoltServerRpc();
                }
            }
        }

        [ClientRpc]
        public void InitiliseMeshClientRpc(Color32 laserColour)
        {
            meshFilter.mesh = new Mesh() { subMeshCount = 1 };
            meshFilter.mesh.SetVertices(new Vector3[] { new Vector3(0, 0, -0.5f), new Vector3(0, 0, 0.5f) });
            meshFilter.mesh.SetIndices(new int[] { 0, 1 }, MeshTopology.Lines, 0);
            meshRenderer.material.SetColor("_BaseColor", laserColour);
            meshRenderer.material.SetColor("_EmissionColor", (Color)laserColour * 20f);
        }


        [ClientRpc]
        public void SetPhysicsClientRpc(Vector3 velocity, float range, float maxDmg)
        {
            if (IsOwner)
            {
                this.maxDmg = maxDmg;
                this.range = range;
                rigid.velocity = velocity;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (IsOwner)
            {

                switch (collision.collider.gameObject.TryGetComponent(out ShipPartMP part))
                {
                    case true:
                        Debug.Log("Hit Enemy");
                        float angleWeight = Mathf.InverseLerp(90f, 0f, Mathf.Abs(Mathf.DeltaAngle(Vector3.Angle(collision.GetContact(0).normal, transform.forward), 90f)));
                        part.owner.shipHealthManagerMP.HitServerRpc(part.HierarchyID, OwnerClientId, maxDmg * angleWeight);
                        break;
                }
                DestroyBoltServerRpc();
                // do damage to target
            }
        }

        [ServerRpc]
        private void DestroyBoltServerRpc()
        {
            Destroy(gameObject);
        }
    }
}