using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class LaserCompMP : MonoBehaviour
{
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;

    public void Show(float3x2 point)
    {
        meshFilter.mesh = new Mesh() { subMeshCount = 1 };
        meshFilter.mesh.SetVertices(new Vector3[]{ point.c0, point.c1 });
        meshFilter.mesh.SetIndices(new int[] { 0, 1 }, MeshTopology.Lines, 0);
        Destroy(this.gameObject, 0.125f);
    }
}
