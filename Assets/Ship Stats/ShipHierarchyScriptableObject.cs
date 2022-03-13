using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MultiplayerRunTime
{
    [CreateAssetMenu(menuName = "Ships/Ship Hierarchy")]
    public class ShipHierarchyScriptableObject : ScriptableObject
    {
        public byte ShipID;
        public string label;
        public ShipPartScriptableObject[] parts;
        public ShipPartID root;
    }
}