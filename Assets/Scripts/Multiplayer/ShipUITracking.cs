using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class ShipUITracking : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private UITrackingElement trackingElementPrefab;
    private Dictionary<ulong, UITrackingElement> elements = new();

    private void Start()
    {
        
    }

    private void Update()
    {
    }

    public void AddName(ulong clientID, string displayedName, Transform ship, Bounds bounds)
    {
        if (clientID == NetworkManager.Singleton.LocalClientId)
        { 
            return; 
        }

        Debug.LogFormat("Local ID: {0} Provided ID: {1}", NetworkManager.Singleton.LocalClientId, clientID);

        if (!elements.ContainsKey(clientID))
        {
            elements.Add(clientID, Instantiate(trackingElementPrefab,transform));
        }
        elements[clientID].DisplayedName = displayedName;
        elements[clientID].ship = ship;
        elements[clientID].bounds = bounds;
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
