using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MultiplayerRunTime
{
    public class ShipPartMP : NetworkBehaviour , IComparable<ShipPartMP>
    {
        public string PartName;
        public string CustomLabel;
        public ShipPartMP parent;
        public List<ShipPartMP> children = new();

        public SpaceshipMP owner;
        public byte HierarchyID;
        [SerializeField] private MeshRenderer meshRenderer;
        private Color32 normalFlashColour = new(0, 0, 0, 255);
        private Color32 flashColour = new(187, 191, 41, 255);
        [SerializeField] private float Intensity = 10f;
        [SerializeField] private float FlashTime = 0.25f;
        public Transform AnimationPoint;
        public Transform[] AnimationPoints;
        public bool MultiPoint = false;

        public Color32 ObjectColour
        {
            get => ObjectReColour.Value;
            set
            {
                if (meshRenderer == null)
                {
                    return;
                }
                List<Material> mats = new();
                meshRenderer.GetMaterials(mats);
                mats.ForEach(mat =>
                {
                    switch (mat.shader.FindPropertyIndex("_Colour"))
                    {
                        case -1:
                            break;
                        default:
                            mat.SetColor("_Colour", value);
                            break;
                    }
                });
            }
        }

        public bool Recolourable
        {
            get
            {
                if (meshRenderer == null)
                {
                    return false;
                }
                List<Material> mats = new();
                meshRenderer.GetMaterials(mats);
                for (int i = 0; i < mats.Count; i++)
                {
                    switch (mats[i].shader.FindPropertyIndex("_Colour"))
                    {
                        case -1:
                            break;
                        default:
                            return true;
                    }
                }
                return false;
            }
        }

        public Color32 TintColour
        {
            get => meshRenderer.material.GetColor("_BaseColor");
            set
            {

                if (meshRenderer == null)
                {
                    return;
                }
                List<Material> mats = new();
                meshRenderer.GetMaterials(mats);
                mats.ForEach(mat => mat.SetColor("_BaseColor", value));
            }
        }

        public Color32 FlashColour
        {
            get => meshRenderer.material.GetColor("_EmissionColor");
            set
            {

                if (meshRenderer == null)
                {
                    return;
                }
                meshRenderer.material.SetColor("_EmissionColor", (Color)value * Intensity);
            }
        }

        public Bounds RendererBounds
        {
            get => meshRenderer.bounds;
        }

        private NetworkVariable<bool> ObjectEnabled = new(true);
        private NetworkVariable<Color32> ObjectTint = new(new Color32(255,255,255,255));
        private NetworkVariable<Color32> ObjectReColour = new(new Color32(255, 255, 255, 255));

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            parent = transform.parent.GetComponentInParent<ShipPartMP>();
            if(parent != null)
            {
                parent.children.Add(this);
            }
        }

        private void OnEnable()
        {
            if(meshRenderer == null)
            {
                return;
            }
            meshRenderer.enabled =  ObjectEnabled.Value;
            TintColour = ObjectTint.Value;
            ObjectEnabled.OnValueChanged += OnObjectEnabledChanged;
            ObjectTint.OnValueChanged += OnTintColourChanged;
            ObjectReColour.OnValueChanged += OnObjectColourChanged;
        }

        private void OnObjectEnabledChanged(bool oldValue, bool newValue)
        {
            if (meshRenderer == null)
            {
                return;
            }
            meshRenderer.enabled = newValue;
        }

        private void OnTintColourChanged(Color32 oldvalue, Color32 newValue)
        {
            TintColour = newValue;
        }

        private void OnObjectColourChanged(Color32 oldvalue, Color32 newValue)
        {
            ObjectColour = newValue;
        }

        [ServerRpc(Delivery = RpcDelivery.Reliable)]
        public void SetObjectEnabledServerRpc(bool enabled)
        {
            ObjectEnabled.Value = enabled;
        }

        public void SetObjectColour(Color32 objectColour)
        {
            ObjectReColour.Value = objectColour;
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
            if (meshRenderer == null)
            {
                return;
            }
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