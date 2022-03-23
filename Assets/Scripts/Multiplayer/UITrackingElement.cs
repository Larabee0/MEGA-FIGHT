using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UITrackingElement : MonoBehaviour
{
    [SerializeField] private Text playerName;

    public float rectanglePaddingMultiplier = 0.1f;
    public string DisplayedName { set => playerName.text = value; }
    public Transform ship;
    public Bounds bounds;

    private void Update()
    {
        if (ship == null) {return; }
        Vector3 pos = ship.position;
        //pos.z = 50f;
        transform.position = pos;
        ///Rect visualRect = TargetObjectBoundsToScreenSpace();
        ///
        ///RectTransform rt = GetComponent<RectTransform>();
        ///rt.position = new Vector2(visualRect.xMax, visualRect.yMax);
        ///
        ///
        ///for (int i = 0; i < transform.childCount; i++)
        ///{
        ///    Transform child = transform.GetChild(0);
        ///    child.gameObject.SetActive(true);
        ///
        ///    float xPadding = visualRect.width * rectanglePaddingMultiplier;
        ///    float yPadding = visualRect.height * rectanglePaddingMultiplier;
        ///
        ///    if (i == 0)
        ///    {
        ///        // Resize the tracker sprite rectangle
        ///        rt = child.GetComponent<RectTransform>();
        ///        rt.position = new Vector2(visualRect.xMin - xPadding, visualRect.yMin - yPadding);
        ///        rt.sizeDelta = new Vector2(visualRect.width + xPadding * 2, visualRect.height + yPadding * 2);
        ///    }
        ///    else
        ///    {
        ///        // Reposition the other objects (texts and sprites) beside the tracker rectangle
        ///        rt = child.GetComponent<RectTransform>();
        ///        rt.position = new Vector2(visualRect.xMin + visualRect.width + xPadding, rt.position.y);
        ///    }
        ///}
        //foreach (Transform child in transform)
        //{
        //
        //    // Make all child elements visible
        //    // The parent UI components itself is always active, otherwise it does not receive update events
        //    child.gameObject.SetActive(true);
        //
        //    float xPadding = visualRect.width * rectanglePaddingMultiplier;
        //    float yPadding = visualRect.height * rectanglePaddingMultiplier;
        //
        //    if (child.name == "RectangleImage")
        //    {
        //        // Resize the tracker sprite rectangle
        //        rt = child.GetComponent<RectTransform>();
        //        rt.position = new Vector2(visualRect.xMin - xPadding, visualRect.yMin - yPadding);
        //        rt.sizeDelta = new Vector2(visualRect.width + xPadding * 2, visualRect.height + yPadding * 2);
        //    }
        //    else
        //    {
        //        // Reposition the other objects (texts and sprites) beside the tracker rectangle
        //        rt = child.GetComponent<RectTransform>();
        //        rt.position = new Vector2(visualRect.xMin + visualRect.width + xPadding, rt.position.y);
        //    }
        //}
    }

    private Rect TargetObjectBoundsToScreenSpace()
    {
        // Object visual rectangle in world space
        Bounds b = bounds;

        // Calculate the screen space rectangle surrounding the 3D object
        Camera c = Camera.main;
        Rect rect = Rect.zero;
        Vector3 screenSpacePoint;

        screenSpacePoint = c.WorldToScreenPoint(new Vector3(b.center.x + b.extents.x, b.center.y + b.extents.y, b.center.z + b.extents.z));
        AdjustRect(ref rect, screenSpacePoint, true);
        screenSpacePoint = c.WorldToScreenPoint(new Vector3(b.center.x + b.extents.x, b.center.y + b.extents.y, b.center.z - b.extents.z));
        AdjustRect(ref rect, screenSpacePoint);
        screenSpacePoint = c.WorldToScreenPoint(new Vector3(b.center.x + b.extents.x, b.center.y - b.extents.y, b.center.z + b.extents.z));
        AdjustRect(ref rect, screenSpacePoint);
        screenSpacePoint = c.WorldToScreenPoint(new Vector3(b.center.x + b.extents.x, b.center.y - b.extents.y, b.center.z - b.extents.z));
        AdjustRect(ref rect, screenSpacePoint);
        screenSpacePoint = c.WorldToScreenPoint(new Vector3(b.center.x - b.extents.x, b.center.y + b.extents.y, b.center.z + b.extents.z));
        AdjustRect(ref rect, screenSpacePoint);
        screenSpacePoint = c.WorldToScreenPoint(new Vector3(b.center.x - b.extents.x, b.center.y + b.extents.y, b.center.z - b.extents.z));
        AdjustRect(ref rect, screenSpacePoint);
        screenSpacePoint = c.WorldToScreenPoint(new Vector3(b.center.x - b.extents.x, b.center.y - b.extents.y, b.center.z + b.extents.z));
        AdjustRect(ref rect, screenSpacePoint);
        screenSpacePoint = c.WorldToScreenPoint(new Vector3(b.center.x - b.extents.x, b.center.y - b.extents.y, b.center.z - b.extents.z));
        AdjustRect(ref rect, screenSpacePoint);

        return rect;
    }


    private void AdjustRect(ref Rect rect, Vector3 pnt, bool firstCall = false)
    {
        if (firstCall)
        {
            rect.xMin = pnt.x;
            rect.yMin = pnt.y;
            rect.xMax = pnt.x;
            rect.yMax = pnt.y;
        }
        else
        {
            rect.xMin = Mathf.Min(rect.xMin, pnt.x);
            rect.yMin = Mathf.Min(rect.yMin, pnt.y);
            rect.xMax = Mathf.Max(rect.xMax, pnt.x);
            rect.yMax = Mathf.Max(rect.yMax, pnt.y);
        }
    }

}
