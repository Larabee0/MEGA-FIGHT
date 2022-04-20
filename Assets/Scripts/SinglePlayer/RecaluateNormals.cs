using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecaluateNormals : MonoBehaviour
{
    [SerializeField] private MeshFilter meshFilter;

    private void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        //Mesh mesh = new() { subMeshCount = 3 };
        Mesh oldMesh = meshFilter.sharedMesh;
        //mesh.vertices = oldMesh.vertices;
        //mesh.SetTriangles(oldMesh.GetTriangles(0), 0);
        //mesh.SetTriangles(oldMesh.GetTriangles(1), 1);
        //mesh.SetTriangles(oldMesh.GetTriangles(2), 2);
        //mesh.uv = oldMesh.uv;
        //mesh.uv2 = oldMesh.uv2;
        //mesh.uv3 = oldMesh.uv3;
        oldMesh.RecalculateNormals();
        oldMesh.RecalculateTangents();
        oldMesh.RecalculateBounds();
        //meshFilter.mesh = mesh;
    }
}
