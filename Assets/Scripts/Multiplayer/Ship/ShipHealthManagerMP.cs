using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Scripting;

namespace MultiplayerRunTime
{
    public class ShipHealthManagerMP : NetworkBehaviour
    {
        public delegate void ShipHit();
        public ShipHit OnShipHit;
        public static bool PVPEnabled = false;

        public ShipHierarchyScriptableObject stats;
        public ShipHierarchy shipHierarchy;
        [SerializeField] private ExplosionData explosionData;
        private Bounds modelBounds;
        private List<ShipPartMP> parts;

        private NetworkList<float> partHealths;

        public Dictionary<Functionality, float> functionalityEfficiencies = new();

        public float WeaponEfficiency
        {
            get
            {
                if (functionalityEfficiencies.ContainsKey(Functionality.Weapon))
                {
                    return functionalityEfficiencies[Functionality.Weapon];
                }
                else
                {
                    return CalculateTagEfficiency(Functionality.Weapon);
                }
            }
        }

        public float ManeourveEfficiency
        {
            get
            {
                float efficiency = 0f;

                if (functionalityEfficiencies.ContainsKey(Functionality.Piloting))
                {
                    efficiency += functionalityEfficiencies[Functionality.Piloting];
                }
                else
                {
                    efficiency += CalculateTagEfficiency(Functionality.Piloting);
                }

                if (functionalityEfficiencies.ContainsKey(Functionality.Control))
                {
                    efficiency += functionalityEfficiencies[Functionality.Control];
                }
                else
                {
                    efficiency += CalculateTagEfficiency(Functionality.Control);
                }

                return efficiency /= 2f;
            }
        }

        public float ThrustEfficiency
        {
            get
            {
                if (functionalityEfficiencies.ContainsKey(Functionality.Thrust))
                {
                    return functionalityEfficiencies[Functionality.Thrust];
                }
                else
                {
                    return CalculateTagEfficiency(Functionality.Thrust);
                }
            }
        }

        public Bounds ModelBounds
        {
            get
            {
                if (parts.Count > 2)
                {
                    ShipPartRecord part = shipHierarchy.parts[parts[0].HierarchyID];
                    try
                    {
                        modelBounds = parts[0].RendererBounds;
                        for (int i = 1; i < parts.Count; i++)
                        {
                            part = shipHierarchy.parts[parts[i].HierarchyID];
                            if (part.Destroyed)
                            {
                                continue;
                            }
                            try
                            {
                                modelBounds.Encapsulate(parts[i].RendererBounds);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogErrorFormat("Part: {0} \nCaused Exception {1} \nIs Destroyed? {2}\nFirst Destroyed Parent: {3}", part.label, ex.Message, part.Destroyed,
                                LogDestroyedParentOrRoot(part).label);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogErrorFormat("Part: {0} \nCaused Exception {1} \nIs Destroyed? {2}\nFirst Destroyed Parent: {3}", part.label, ex.Message, part.Destroyed,
                        LogDestroyedParentOrRoot(part).label);
                    }
                }
                return modelBounds;
            }
        }

        private ShipPartRecord LogDestroyedParentOrRoot(ShipPartRecord source)
        {
            if(source == null)
            {
                return source;
            }
            if(source.Parent == null)
            {
                return source;
            }
            ShipPartRecord parent = source.Parent;
            if(!parent.Destroyed && parent != shipHierarchy.root)
            {
                return LogDestroyedParentOrRoot(source);
            }
            return parent;
        }

        private void Awake()
        {
            partHealths = new();

            parts = new(GetComponentsInChildren<ShipPartMP>());
            for (int i = 0; i < parts.Count; i++)
            {
                if(parts[i].parent == null)
                {
                    shipHierarchy = new(stats,parts[i]);
                    break;
                }
            }
            
            if (IsServer)
            {
                for (int i = 0; i < shipHierarchy.parts.Count; i++)
                {
                    partHealths.Add(shipHierarchy.parts[i].maxHitPoints);
                }
            }
        }

        private void Start()
        {
            parts.Sort();
            for (int i = 0; i < shipHierarchy.tags.Count; i++)
            {
                functionalityEfficiencies.Add(shipHierarchy.tags[i], CalculateTagEfficiency(shipHierarchy.tags[i]));
                Debug.LogFormat("Tag: {0} Effiency: {1}", shipHierarchy.tags[i].ToString(), functionalityEfficiencies[shipHierarchy.tags[i]]);
            }

            partHealths.OnListChanged += RecalculateEffiencies;
        }

        private void RecalculateEffiencies(NetworkListEvent<float> changedValue)
        {
            Debug.Log("Recaulating Effiencies");
            byte hierachyID = (byte)changedValue.Index;
            Functionality[] effectedFunctions = shipHierarchy.parts[hierachyID].tags;

            if (changedValue.Value <= 0 && shipHierarchy.parts[hierachyID].Destroyed == false)
            {
                shipHierarchy.parts[hierachyID].Destroyed = true;
            }

            for (int i = 0; i < effectedFunctions.Length; i++)
            {
                if (functionalityEfficiencies.ContainsKey(effectedFunctions[i]))
                {
                    functionalityEfficiencies[effectedFunctions[i]] = CalculateTagEfficiency(effectedFunctions[i]);
                    Debug.LogFormat("Tag: {0} Effiency: {1}", effectedFunctions[i].ToString(), functionalityEfficiencies[effectedFunctions[i]]);
                }
                else
                {
                    Debug.LogWarningFormat("Hiercarchy Missing function {0}", effectedFunctions[i].ToString());
                }
            }
            if (IsServer)
            {
                if (!shipHierarchy.parts[hierachyID].Destroyed && partHealths[hierachyID] <= 0f)
                {
                    DestroyChildren(hierachyID);
                    DetachPartClientRPC(hierachyID);
                }
                if (ShouldBeDead())
                {
                    DestroyShipServerRpc();
                }
            }
        }

        public DamageInfo GetDamageInfo(byte hierarchyID, float damage)
        {
            return new DamageInfo
            {
                HierarchyID = hierarchyID,
                ammount = damage,
                PartLabel = shipHierarchy.parts[hierarchyID].label
            };
        }

        private float CalculateTagEfficiency(Functionality tag)
        {
            float num = 0f;
            int num2 = 0;
            float num3 = 0f;
            foreach (ShipPartRecord part in shipHierarchy.GetPartsWithTag(tag))
            {
                float num4 = CalculatePartEfficiency(part);
                num += num4;
                num3 = math.max(num3, num4);
                num2++;
            }
            if (num2 == 0)
            {
                return 1f;
            }
            return math.min(num / num2, 3.4028235E+38f);
        }

        public float CalculatePartEfficiency(byte hierarchyIndex)
        {
            return CalculatePartEfficiency(shipHierarchy.parts[hierarchyIndex]);
        }

        private float CalculatePartEfficiency(ShipPartRecord part)
        {
            float num = 1f;
            float num3 = partHealths[part.HierarchyIndex] / part.maxHitPoints;
            num *= num3;
            return part.Destroyed ? 0f : math.max(num, 0f);
        }

        private bool ShouldBeDead()
        {
            List<RequiredFunctionality> requiredFunctions = ShipHierarchy.RequiredFunctionality;
            for (int i = 0; i < requiredFunctions.Count; i++)
            {
                if (CalculateTagEfficiency(requiredFunctions[i].function) < requiredFunctions[i].minEfficiency)
                {
                    return true;
                }
            }
            return false;
        }

        private void DestroyChildren(byte hierachyID)
        {
            ShipPartRecord part = shipHierarchy.parts[hierachyID];
            Debug.LogFormat("Destroy? {0} Children: {1}", part.Destroyed, part.Children == null ? 0 : part.Children.Count);
            if (part.Destroyed)
            {
                return;
            }
            part.Destroyed = true;
            //partHealths[hierachyID] = 0;
            if (part.Children != null && part.Children.Count > 0)
            {
                for (int i = 0; i < part.Children.Count; i++)
                {
                    DestroyChildren(part.Children[i].HierarchyIndex);
                }
            }
        }

        #region RPCS

        [ServerRpc(Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
        public void HitServerRpc(byte hierachyID, ulong instigatorClientID, float damage)
        {
            if (!PVPEnabled)
            {
                return;
            }
            ClientRpcParams clientRpcParams = new() { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } } };
            if (partHealths[hierachyID] > 0)
            {
                partHealths[hierachyID] -= damage;
                parts[hierachyID].FlashPartClientRpc();

            }
            if (partHealths[hierachyID] <= 0)
            {
                damage += partHealths[hierachyID];
                partHealths[hierachyID] = 0;
            }
            else
            {
                float proportion = partHealths[hierachyID] / shipHierarchy.parts[hierachyID].maxHitPoints;
                parts[hierachyID].SetObjectTintServerRpc((byte)math.lerp(0, 255, proportion));
            }

            if (ShouldBeDead())
            {
                DestroyShipServerRpc();
            }
            InvokeHitEventClientRPC();
            AlertHitClientRPC(NetworkManager.SpawnManager.GetPlayerNetworkObject(instigatorClientID).GetComponent<PlayerManagerMP>(), hierachyID, damage, clientRpcParams);
            clientRpcParams.Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { instigatorClientID } };
            AlertInstigatorClientRPC(NetworkManager.SpawnManager.GetPlayerNetworkObject(OwnerClientId).GetComponent<PlayerManagerMP>(), hierachyID, damage, clientRpcParams);
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void InvokeHitEventClientRPC()
        {
            OnShipHit?.Invoke();
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void AlertHitClientRPC(NetworkBehaviourReference instigatorClient, byte hierachyID, float damage, ClientRpcParams clientRpcParams = default)
        {
            DamageInfo damageInfo = GetDamageInfo(hierachyID, damage);
            if (instigatorClient.TryGet(out PlayerManagerMP targetObject))
            {
                string instigator = targetObject.DisplayedName;
                damageInfo.Instigator = instigator;
            }

            damageInfo.hitPlayer = "you";
            damageInfo.HitPlayerGrammar = "'re";
            Debug.Log(damageInfo.ToString());
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void AlertInstigatorClientRPC(NetworkBehaviourReference target, byte hierachyID, float damage, ClientRpcParams clientRpcParams = default)
        {
            if (target.TryGet(out PlayerManagerMP targetObject))
            {
                DamageInfo damageInfo = targetObject.LocalSpaceship.shipHealthManagerMP.GetDamageInfo(hierachyID, damage);
                damageInfo.Instigator = "You";
                damageInfo.hitPlayer = targetObject.DisplayedName;
                damageInfo.HitPlayerGrammar = "'s";
                Debug.Log(damageInfo.ToString());
            }
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void DetachPartClientRPC(byte hierachyID)
        {
            Rigidbody body = parts[hierachyID].gameObject.AddComponent<Rigidbody>();
            body.mass = 10000f;
            body.drag = 1;
            body.angularDrag = 3;
            Vector3 point = parts[hierachyID].gameObject.GetComponent<Collider>().bounds.center;
            PartExplosion explosion = parts[hierachyID].gameObject.AddComponent<PartExplosion>();
            explosion.StartCoroutine(explosion.Explode(point, explosionData));

            Collider[] collidables = parts[hierachyID].gameObject.GetComponentsInChildren<Collider>();
            for (int i = 0; i < collidables.Length; i++)
            {
                collidables[i].gameObject.layer = 6;
            }
            parts[hierachyID].transform.parent = null;
            ShipPartMP[] childParts = parts[hierachyID].gameObject.GetComponentsInChildren<ShipPartMP>();
            for (int i = 0; i < childParts.Length; i++)
            {
                Destroy(childParts[i]);
            }
            Destroy(parts[hierachyID].gameObject, 30f);
            Destroy(parts[hierachyID]);
        }

        [ServerRpc(Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
        public void DestroyShipServerRpc()
        {
            Destroy(gameObject);
        }

        #endregion
    }


    public struct DamageInfo
    {
        public float ammount;
        public string hitPlayer;
        public byte partID;
        public byte HierarchyID;
        public string PartLabel;
        public string HitPlayerGrammar;
        public string Instigator;

        public override string ToString()
        {
            return string.Format("{0} damaged {1}{2} {3} for {4} damage", Instigator, hitPlayer, HitPlayerGrammar, PartLabel, ammount);
        }
    }

    [Serializable]
    public class ShipHierarchy
    {
        public static List<RequiredFunctionality> RequiredFunctionality = new()
        {
            new RequiredFunctionality
            {
                function = Functionality.Structural,
                minEfficiency = 0.1f
            },
            new RequiredFunctionality
            {
                function = Functionality.Piloting,
                minEfficiency = 0.25f
            },
            new RequiredFunctionality
            {
                function = Functionality.Thrust,
                minEfficiency = 0.0f
            },
            new RequiredFunctionality
            {
                function = Functionality.Control,
                minEfficiency = 0.05f
            }
        };


        public byte ShipID;
        public string Label;
        public List<ShipPartRecord> parts;
        public List<Functionality> tags;
        public ShipPartRecord root;

        public ShipHierarchy(ShipHierarchyScriptableObject hierarchy, ShipPartMP rootPart)
        {
            ShipID = hierarchy.ShipID;
            Label = hierarchy.label;
            _ = new HierarchyBuilder(this, hierarchy,rootPart);
            tags = new List<Functionality>();
            CacheDataRecursive(root);
            parts.Sort();
            for (int i = 0; i < RequiredFunctionality.Count; i++)
            {
                if (!tags.Contains(RequiredFunctionality[i].function))
                {
                    throw new MissingReferenceException(string.Format("Ship Hierarchy {0} is missing required functionality: {1}!", Label, RequiredFunctionality[i].ToString()));
                }
            }
        }

        private void CacheDataRecursive(ShipPartRecord node)
        {
            node.Ship = this;
            for (int i = 0; i < node.tags.Length; i++)
            {
                if (!tags.Contains(node.tags[i]))
                {
                    tags.Add(node.tags[i]);
                }
            }
            if (node.Children == null) return;
            for (int l = 0; l < node.Children.Count; l++)
            {
                CacheDataRecursive(node.Children[l]);
            }
        }


        public IEnumerable<ShipPartRecord> GetPartsWithTag(Functionality tag)
        {
            int num;
            for (int i = 0; i < parts.Count; i = num + 1)
            {
                ShipPartRecord shipPartRecord = parts[i];
                if (shipPartRecord.tags.Contains(tag))
                {
                    yield return shipPartRecord;
                }
                num = i;
            }
            yield break;
        }

        private class HierarchyBuilder
        {
            private readonly ShipHierarchy Target;
            private readonly Dictionary<byte, ShipPartRecord> partsDict;
            private readonly Dictionary<string, ShipPartRecord> stringPartsDict;
            private byte index = 0;
            public HierarchyBuilder(ShipHierarchy target, ShipHierarchyScriptableObject source, ShipPartMP root)
            {
                Target=target;
                stringPartsDict = new();
                for (int i = 0; i < source.parts.Length; i++)
                {
                    stringPartsDict.TryAdd(source.parts[i].label, new(source.parts[i]));
                }
                Target.parts = new List<ShipPartRecord>(stringPartsDict.Count);
                Target.root = new(stringPartsDict[root.PartName]);
                if (!string.IsNullOrEmpty(root.CustomLabel))
                {
                    Target.root.label = root.CustomLabel;
                }
                Target.parts.Add(Target.root);

                RecusiveBodyPartCreator(Target.root,root.children);
            }

            private void RecusiveBodyPartCreator(ShipPartRecord parent, List<ShipPartMP> childParts)
            {
                for (int i = 0; i < childParts.Count; i++)
                {
                    if (!stringPartsDict.ContainsKey(childParts[i].PartName))
                    {
                        Debug.LogError("Missing Part with Name: " + childParts[i].PartName);
                        return;
                    }
                    ShipPartRecord childPart = new(stringPartsDict[childParts[i].PartName]);
                    if (childParts[i].children != null || childParts[i].children.Count > 0)
                    {
                        RecusiveBodyPartCreator(childPart, childParts[i].children);
                    }
                    if (parent.Children == null)
                    {
                        parent.Children = new List<ShipPartRecord>();
                    }
                    
                    childPart.HierarchyIndex = index += 1;
                    if (!string.IsNullOrEmpty(childParts[i].CustomLabel))
                    {
                        childPart.label = childParts[i].CustomLabel;
                    }
                    childPart.Parent = parent;
                    parent.Children.Add(childPart);
                    Target.parts.Add(childPart);
                }
            }
        }
    }

    public class ShipPartRecord : IComparable<ShipPartRecord>
    {
        public byte HierarchyIndex;
        public string label;
        public float maxHitPoints;
        public Functionality[] tags;
        public ShipHierarchy Ship;
        public ShipPartRecord Parent;
        public List<ShipPartRecord> Children;
        public bool Destroyed = false;

        public ShipPartRecord(ShipPartScriptableObject part)
        {
            label = part.label;
            maxHitPoints = part.hitPoints;
            tags = part.tags;
        }


        public ShipPartRecord(ShipPartRecord part)
        {
            label = part.label;
            maxHitPoints = part.maxHitPoints;
            tags = part.tags;
        }

        public int CompareTo(ShipPartRecord other)
        {
            if (other == null)
            {
                return 1;
            }
            else
            {
                return HierarchyIndex.CompareTo(other.HierarchyIndex);
            }
        }
    }

    public enum Functionality : byte
    {
        Structural,
        Thrust,
        Weapon,
        Piloting,
        RepairDroid,
        Control
    }

    public struct RequiredFunctionality
    {
        public Functionality function;
        public float minEfficiency;
    }
}