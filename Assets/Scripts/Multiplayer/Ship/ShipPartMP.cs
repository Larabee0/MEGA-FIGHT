using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MultiplayerRunTime
{
    public class ShipPartMP : NetworkBehaviour , IComparable<ShipPartMP>
    {
        public SpaceshipMP owner;
        public byte HierarchyID;
        [SerializeField] private MeshRenderer meshRenderer;
        private Color32 normalFlashColour = new(0, 0, 0, 255);
        private Color32 flashColour = new(187, 191, 41, 255);
        private float Intensity = 10f;
        [SerializeField] private float FlashTime = 0.25f;
        public Transform AnimationPoint;

        public Color32 TintColour
        {
            get => meshRenderer.material.GetColor("_BaseColor");
            set => meshRenderer.material.SetColor("_BaseColor", value);
        }

        public Color32 FlashColour
        {
            get => meshRenderer.material.GetColor("_EmissionColor");
            set => meshRenderer.material.SetColor("_EmissionColor", (Color)value * Intensity);
        }

        private NetworkVariable<bool> ObjectEnabled = new(true);
        private NetworkVariable<Color32> ObjectTint = new(new Color32(255,255,255,255));

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            
        }

        private void OnEnable()
        {
            meshRenderer.enabled =  ObjectEnabled.Value;
            TintColour = ObjectTint.Value;
            ObjectEnabled.OnValueChanged += OnObjectEnabledChanged;
            ObjectTint.OnValueChanged += OnTintColourChanged;
        }

        private void OnObjectEnabledChanged(bool oldValue, bool newValue)
        {
            meshRenderer.enabled = newValue;
        }

        private void OnTintColourChanged(Color32 oldvalue, Color32 newValue)
        {
            TintColour = newValue;
        }

        [ServerRpc(Delivery = RpcDelivery.Reliable)]
        public void SetObjectEnabledServerRpc(bool enabled)
        {
            ObjectEnabled.Value = enabled;
        }

        [ServerRpc(Delivery = RpcDelivery.Reliable)]
        public void SetObjectTintServerRpc(Color32 objectTint)
        {
            ObjectTint.Value = objectTint;
        }

        [ServerRpc(Delivery = RpcDelivery.Reliable,RequireOwnership = false)]
        public void SetObjectTintServerRpc(byte darkness)
        {
            ObjectTint.Value = new Color32(darkness, darkness, darkness, 255);
        }

        [ServerRpc(Delivery = RpcDelivery.Unreliable)]
        public void FlashPartServerRpc()
        {
            FlashPartClientRpc();
        }

        [ClientRpc(Delivery = RpcDelivery.Unreliable)]
        public void FlashPartClientRpc()
        {
            StartCoroutine(Flash());
        }

        private IEnumerator Flash()
        {
            FlashColour = flashColour;
            yield return new WaitForSeconds(FlashTime);
            FlashColour = normalFlashColour;
        }


        public int CompareTo(ShipPartMP other)
        {
            if (other == null)
            {
                return 1;
            }
            else
            {
                return HierarchyID.CompareTo(other.HierarchyID);
            }
        }

        public override void OnDestroy()
        {
            FlashColour = normalFlashColour;
        }
    }
}