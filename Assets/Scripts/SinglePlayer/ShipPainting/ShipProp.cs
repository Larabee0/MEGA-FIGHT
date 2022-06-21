using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipProp : MonoBehaviour
{
    public List<Material> mats;
    public MultiplayerRunTime.ShipHealthManagerMP healthManagerMP;
    // Start is called before the first frame update
    void Awake()
    {
        MeshRenderer[] AllRenderers = GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < AllRenderers.Length; i++)
        {
            List<Material> mats = new();
            AllRenderers[i].GetMaterials(mats);
            for (int m = 0; m < mats.Count; m++)
            {

                switch (mats[m].shader.FindPropertyIndex("_Colour"))
                {
                    case -1:
                        continue;
                    default:
                        this.mats.Add(mats[m]);
                        break;
                }
            }
        }
        SetColour(healthManagerMP.DefaultColour);
    }

    public void SetColour(Color colour)
    {
        mats.ForEach(mat => mat.SetColor("_Colour", colour));
    }
}
