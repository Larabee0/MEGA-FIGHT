using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace MultiplayerRunTime
{
    public class ShipHealthManagerMP : NetworkBehaviour
    {
        public ShipHierarchyScriptableObject stats;
        public ShipHierarchy shipHierarchy;

        private List<ShipPartMP> parts;

        private NetworkList<float> partHealths;

        public Dictionary<Functionality, float> functionalityEfficiencies = new();

        private void Awake()
        {
            partHealths = new();
            shipHierarchy = new(stats);
            if (IsServer)
            {
                for (int i = 0; i < shipHierarchy.parts.Count; i++)
                {
                    partHealths.Add(shipHierarchy.parts[i].maxHitPoints);
                }
            }
            parts = new(GetComponentsInChildren<ShipPartMP>());
            parts.Sort();
            for (int i = 0; i < shipHierarchy.tags.Count; i++)
            {
                functionalityEfficiencies.Add(shipHierarchy.tags[i], CalculateTagEfficiency(shipHierarchy.tags[i]));
            }
        }

        private void Start()
        {
            
        }

        public DamageInfo GetDamageInfo(byte hierarchyID, float damage)
        {
            return new DamageInfo
            {
                HierarchyID = hierarchyID,
                partID = shipHierarchy.parts[hierarchyID].PartID,
                ammount = damage,
                PartLabel = shipHierarchy.parts[hierarchyID].label
            };
            
        }

        [ServerRpc(Delivery = RpcDelivery.Reliable ,RequireOwnership = false)]
        public void HitServerRpc(byte hierachyID,ulong instigatorClientID, float damage)
        {
            if(partHealths[hierachyID] > 0)
            {
                partHealths[hierachyID] -= damage;
                parts[hierachyID].FlashPartClientRpc();

            }
            if (partHealths[hierachyID] <= 0)
            {
                damage += partHealths[hierachyID];
                partHealths[hierachyID] = 0;
                // destroy part OwnerClientId
                if (shipHierarchy.parts[hierachyID].tags.Contains(Functionality.Piloting))
                {
                    DestroyShip();
                }
            }
            else
            {
                // apply any functionality changes
                float proportion = partHealths[hierachyID] / shipHierarchy.parts[hierachyID].maxHitPoints;
                parts[hierachyID].SetObjectTintServerRpc((byte)math.lerp(0, 255, proportion));

                Functionality[] effectedFunctions = shipHierarchy.parts[hierachyID].tags;
                for (int i = 0; i < effectedFunctions.Length; i++)
                {
                    functionalityEfficiencies[effectedFunctions[i]] = CalculateTagEfficiency(effectedFunctions[i]);
                }
            }

            ClientRpcParams clientRpcParams = new() { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } } };
            AlertHitClientRPC(NetworkManager.SpawnManager.GetPlayerNetworkObject(instigatorClientID).GetComponent<PlayerManagerMP>(), hierachyID, damage, clientRpcParams);
            clientRpcParams.Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { instigatorClientID } };
            AlertInstigatorClientRPC(NetworkManager.SpawnManager.GetPlayerNetworkObject(OwnerClientId).GetComponent<PlayerManagerMP>(), hierachyID, damage, clientRpcParams);
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

        public void DestroyShip()
        {
            if (IsServer || IsOwner)
            {
                Destroy(gameObject);
            }
        }


        private float CalculateTagEfficiency(Functionality tag)
        {
            float efficiency = 1f;
            foreach (ShipPartRecord part in shipHierarchy.GetPartsWithTag(tag))
            {
                efficiency *= partHealths[part.HierarchyIndex] / part.maxHitPoints;
            }
            return math.max(efficiency, 0);
        }
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
        public byte ShipID;
        public string Label;
        public List<ShipPartRecord> parts;
        public List<Functionality> tags;
        public ShipPartRecord root;

        public ShipHierarchy(ShipHierarchyScriptableObject hierarchy)
        {
            ShipID = hierarchy.ShipID;
            Label = hierarchy.label;
            _ = new HierarchyBuilder(this, hierarchy);
            tags = new List<Functionality>();
            parts.Sort();
            CacheDataRecursive(root);
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

            public HierarchyBuilder(ShipHierarchy target, ShipHierarchyScriptableObject source)
            {
                Target = target;
                partsDict = new();
                for (int i = 0; i < source.parts.Length; i++)
                {
                    partsDict.TryAdd(source.parts[i].PartID, new(source.parts[i]));
                }
                Target.parts = new List<ShipPartRecord>(partsDict.Count);
                Target.root = new(partsDict[source.root.PartID]);
                Target.root.HierarchyIndex = source.root.HierachyID;
                if (!string.IsNullOrEmpty(source.root.customLabel))
                {
                    Target.root.label = source.root.customLabel;
                }
                //Target.root.Ship = Target;
                Target.parts.Add(Target.root);
                RecusiveBodyPartCreator(Target.root, source.root.children);
            }

            private void RecusiveBodyPartCreator(ShipPartRecord parent, ShipPartID[] childIDs)
            {
                for (int i = 0; i < childIDs.Length; i++)
                {
                    if (!partsDict.ContainsKey(childIDs[i].PartID))
                    {
                        Debug.LogError("Missing Part with ID: " + childIDs[i].PartID);
                        return;
                    }
                    ShipPartRecord childPart = new(partsDict[childIDs[i].PartID]);
                    if (childIDs[i].children != null || childIDs[i].children.Length > 0)
                    {
                        RecusiveBodyPartCreator(childPart, childIDs[i].children);
                    }
                    if (parent.Children == null)
                    {
                        parent.Children = new List<ShipPartRecord>();
                    }

                    childPart.HierarchyIndex = childIDs[i].HierachyID;
                    if (!string.IsNullOrEmpty(childIDs[i].customLabel))
                    {
                        childPart.label = childIDs[i].customLabel;
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
        public byte PartID;
        public string label;
        public float maxHitPoints;
        public Functionality[] tags;
        public ShipHierarchy Ship;
        public ShipPartRecord Parent;
        public List<ShipPartRecord> Children;

        public ShipPartRecord(ShipPartScriptableObject part)
        {
            PartID = part.PartID;
            label = part.label;
            maxHitPoints = part.hitPoints;
            tags = part.tags;
        }


        public ShipPartRecord(ShipPartRecord part)
        {
            PartID = part.PartID;
            label = part.label;
            maxHitPoints = part.maxHitPoints;
            tags = part.tags;
        }

        public int CompareTo(ShipPartRecord other)
        {
            if(other == null)
            {
                return 1;
            }
            else
            {
                return HierarchyIndex.CompareTo(other.HierarchyIndex);
            }
        }
    }

    [Serializable]
    public class ShipPartID
    {
        public byte PartID;
        public byte HierachyID;
        public string customLabel;
        public ShipPartID[] children;
    }

    public enum Functionality : byte
    {
        Structural,
        Thrust,
        Weapon,
        Piloting,
        RepairDroid
    }
}