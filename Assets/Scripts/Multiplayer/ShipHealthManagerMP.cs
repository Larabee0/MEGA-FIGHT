using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MultiplayerRunTime
{
    public class ShipHealthManagerMP : NetworkBehaviour
    {
        public ShipHierarchyScriptableObject stats;
        public ShipHierarchy shipHierarchy;

        private NetworkList<float> partHealths;

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
        }

        private void Start()
        {
            partHealths.OnListChanged += HealthChanged;
        }

        private void HealthChanged(NetworkListEvent<float> changeEvent)
        {
            if (IsOwner)
            {
                Debug.LogFormat("Your {0} is now on {1}/{2}hp.", shipHierarchy.parts[changeEvent.Index].label, changeEvent.Value, shipHierarchy.parts[changeEvent.Index].maxHitPoints);
            }
            else
            {
                Debug.LogFormat("{0}'s {1} is now on {2}/{3}hp.", OwnerClientId, shipHierarchy.parts[changeEvent.Index].label, changeEvent.Value, shipHierarchy.parts[changeEvent.Index].maxHitPoints);
            }
            
        }

        [ServerRpc(Delivery = RpcDelivery.Reliable ,RequireOwnership = false)]
        public void HitServerRpc(byte hierachyID,ulong callID, float damage)
        {
            if(partHealths[hierachyID] > 0)
            {
                partHealths[hierachyID] -= damage;
            }
            else
            {

            }
            if (partHealths[hierachyID] <= 0)
            {
                partHealths[hierachyID] = 0;
                // destroy part
            }

            AlertHitClientRPC(hierachyID);

            AlertInstigatorClientRPC(callID, hierachyID);
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void AlertHitClientRPC(byte hierachyID)
        {
            if (!IsOwner) return;
            //Debug.Log("You where damaged");
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void AlertInstigatorClientRPC(ulong clientID, byte hierachyID)
        {
            if (!IsClient) return;
            if(NetworkManager.Singleton.LocalClientId == clientID)
            {
                // inform instigator client of result
                //Debug.Log("You damaged someone");
            }
        }
    }


    [Serializable]
    public class ShipHierarchy
    {
        public byte ShipID;
        public string Label;
        public List<ShipPartRecord> parts;
        public ShipPartRecord root;

        public ShipHierarchy(ShipHierarchyScriptableObject hierarchy)
        {
            ShipID = hierarchy.ShipID;
            Label = hierarchy.label;
            _ = new HierarchyBuilder(this, hierarchy);
            parts.Sort();
            CacheDataRecursive(root);
        }

        private void CacheDataRecursive(ShipPartRecord node)
        {
            node.Ship = this;
            if (node.Children == null) return;
            for (int l = 0; l < node.Children.Count; l++)
            {
                CacheDataRecursive(node.Children[l]);
            }
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