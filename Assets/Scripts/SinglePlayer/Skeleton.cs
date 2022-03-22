using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skeleton : MonoBehaviour
{
    private List<MeshFilter> meshFilters;
    //private List<MeshRenderer> meshRenderers;
    void Start()
    {
        meshFilters = new(GetComponentsInChildren<MeshFilter>());
        //meshRenderers = new(GetComponentsInChildren<MeshRenderer>());
        Skeletalise();
    }

    private void Skeletalise()
    {
        for (int i = 0; i < meshFilters.Count; i++)
        {
            Mesh mesh = meshFilters[i].mesh;

            mesh.SetIndices(mesh.triangles, MeshTopology.Lines, 0);
        }
    }


}
