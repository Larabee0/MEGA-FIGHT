using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerRunTime
{
    [CreateAssetMenu(menuName = "Ships/Ship Part")]
    public class ShipPartScriptableObject : ScriptableObject
    {
        public string label;
        public float hitPoints;
        public Functionality[] tags;
    }
}