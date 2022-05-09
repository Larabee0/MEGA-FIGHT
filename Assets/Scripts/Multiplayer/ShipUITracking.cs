using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace MultiplayerRunTime
{
    public class ShipUITracking : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private UITrackingElement trackingElementPrefab;
        private  readonly Dictionary<ulong, UITrackingElement> elements = new();

        public void GetExistingShips()
        {
            Debug.Log("Getting Existing Ships");
            PlayerManagerMP[] existingShips = FindObjectsOfType<PlayerManagerMP>();
            for (int i = 0; i < existingShips.Length; i++)
            {
                if (existingShips[i].ShipSpawned)
                {
                    AddName(existingShips[i].OwnerClientId, existingShips[i].DisplayedName, existingShips[i].LocalSpaceship.shipHealthManagerMP);
                }
            }
        }

        public void AddName(ulong clientID, string displayedName, ShipHealthManagerMP hMMP)
        {
            if (clientID == NetworkManager.Singleton.LocalClientId)
            {
                return;
            }

            Debug.LogFormat("Local ID: {0} Provided ID: {1}", NetworkManager.Singleton.LocalClientId, clientID);

            if (!elements.ContainsKey(clientID))
            {
                elements.Add(clientID, Instantiate(trackingElementPrefab, transform));
            }
            elements[clientID].DisplayedName = displayedName;
            elements[clientID].HealthManagerMP = hMMP;
        }

        public void RemoveName(ulong clientID)
        {
            if (elements.ContainsKey(clientID))
            {
                Destroy(elements[clientID].gameObject);
                elements.Remove(clientID);
            }
        }
    }
}