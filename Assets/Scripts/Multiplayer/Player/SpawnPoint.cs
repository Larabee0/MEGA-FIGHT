using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SpawnPoint : MonoBehaviour
{
    private bool spawnEmpty = true;
    public bool IsServer = false;
    public bool SpawnEmpty
    {
        get { return spawnEmpty; }
        private set { spawnEmpty = value; }
    }

    public Vector3 Position => transform.position;
    public Vector3 Forward => transform.forward;

    private void OnTriggerEnter(Collider other)
    {
        if (IsServer && other.TryGetComponent<SpawnDetector>(out _))
        {
            SpawnEmpty = false;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (IsServer && other.TryGetComponent<SpawnDetector>(out _))
        {
            SpawnEmpty = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsServer && other.TryGetComponent<SpawnDetector>(out _))
        {
            SpawnEmpty = true;
        }
    }
}
