using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MultiplayerRunTime
{
    public class UITrackingElement : MonoBehaviour
    {
        [SerializeField] private Text playerName;
        [SerializeField] private Text shipType;
        [SerializeField] private Text distance;
        [SerializeField] private Image image;

        public float rectanglePaddingMultiplier = 0.1f;
        public string DisplayedName { set => playerName.text = value; }
        public string ShipType { set => shipType.text = value; }
        public Color defaultColour = Color.white;
        [SerializeField] private Color32 flashColour = new(187, 191, 41, 255);
        [SerializeField] private float FlashTime = 0.25f;
        public Color ElementColours
        {
            set
            {
                playerName.color = value;
                shipType.color = value;
                distance.color = value;
                image.color = value;
            }
        }

        public float Distance
        {
            set => distance.text = string.Format("{0}km", (value / 1000f).ToString("F2"));
        }

        public ShipHealthManagerMP HealthManagerMP
        {
            set
            {
                healthManagerMP = value;
                shipTransform = healthManagerMP.transform;
                ShipType = healthManagerMP.shipHierarchy.Label;
                healthManagerMP.OnShipHit += FlashTracker;
            }
        }

        private LocalPlayerManager playerManager;
        private ShipHealthManagerMP healthManagerMP;
        public Transform shipTransform;
        public Transform localShipTransform;
        private RectTransform rootTransform;
        private List<RectTransform> childRectTransformList = new();
        private Camera c;

        private void Awake()
        {
            c = Camera.main;
            ElementColours = defaultColour;
            playerManager = c.GetComponent<LocalPlayerManager>();
            rootTransform = GetComponent<RectTransform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                childRectTransformList.Add(child.GetComponent<RectTransform>());
            }
        }

        private void Start()
        {
            ResetState();
        }

        private void Update()
        {
            if (shipTransform == null)
            {
                ResetState();
                return;
            }
            Vector3 screenPoint = c.WorldToViewportPoint(shipTransform.position);
            if (screenPoint.z <= 0 || screenPoint.x <= 0 || screenPoint.x >= 1 || screenPoint.y <= 0 || screenPoint.y >= 1)
            {
                ResetState();
                return;
            }

            if (localShipTransform == null)
            {
                GetLocalPlayerShip();
            }
            else
            {
                Vector3 dst = localShipTransform.position - shipTransform.position;
                Distance = dst.magnitude;
            }

            Rect visualRect = TargetObjectBoundsToScreenSpace();

            rootTransform.position = new Vector2(visualRect.xMax, visualRect.yMin);

            for (int i = 0; i < childRectTransformList.Count; i++)
            {
                RectTransform child = childRectTransformList[i];
                child.gameObject.SetActive(true);

                float xPadding = visualRect.width * rectanglePaddingMultiplier;
                float yPadding = visualRect.height * rectanglePaddingMultiplier;

                if (i == 0)
                {
                    // Resize the tracker sprite rectangle
                    child.position = new Vector2(visualRect.xMin - xPadding, visualRect.yMin - yPadding);
                    child.sizeDelta = new Vector2(visualRect.width + xPadding * 2, visualRect.height + yPadding * 2);
                }
                else if (i == 3)
                {
                    // Reposition the other objects (texts and sprites) beside the tracker rectangle
                    child.position = new Vector2(visualRect.xMin + visualRect.width + xPadding, child.position.y);
                    //child.position = new Vector2(visualRect.xMin + visualRect.width + xPadding, visualRect.yMin + visualRect.height - yPadding);
                }
                else
                {
                    // Reposition the other objects (texts and sprites) beside the tracker rectangle
                    // child.position = new Vector2(visualRect.xMin + visualRect.width + xPadding, child.position.y);
                    child.position = new Vector2(visualRect.xMin + visualRect.width + xPadding, child.position.y);
                }
            }
        }

        private Rect TargetObjectBoundsToScreenSpace()
        {
            // Object visual rectangle in world space
            Bounds b = healthManagerMP.ModelBounds;

            // Calculate the screen space rectangle surrounding the 3D object
            Rect rect = Rect.zero;
            Vector3 screenSpacePoint;

            screenSpacePoint = c.WorldToScreenPoint(new Vector3(b.center.x + b.extents.x, b.center.y + b.extents.y, b.center.z + b.extents.z));

            rect.xMin = screenSpacePoint.x;
            rect.yMin = screenSpacePoint.y;
            rect.xMax = screenSpacePoint.x;
            rect.yMax = screenSpacePoint.y;

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

        private void AdjustRect(ref Rect rect, Vector3 pnt)
        {
            rect.xMin = Mathf.Min(rect.xMin, pnt.x);
            rect.yMin = Mathf.Min(rect.yMin, pnt.y);
            rect.xMax = Mathf.Max(rect.xMax, pnt.x);
            rect.yMax = Mathf.Max(rect.yMax, pnt.y);
        }

        public void ResetState()
        {
            rootTransform.position = Vector2.zero;
            rootTransform.anchorMax = Vector2.zero;
            rootTransform.anchorMin = Vector2.zero;
            rootTransform.pivot = Vector2.zero;

            for (int i = 0; i < childRectTransformList.Count; i++)
            {
                RectTransform child = childRectTransformList[i];
                child.gameObject.SetActive(false);
                child.anchorMax = Vector2.zero;
                child.anchorMin = Vector2.zero;
                child.pivot = Vector2.zero;
            }
        }

        private void GetLocalPlayerShip()
        {
            if (playerManager != null && playerManager.LocalShip != null)
            {
                localShipTransform = playerManager.LocalShip.transform;
            }
        }

        public void FlashTracker()
        {
            StartCoroutine(Flash());
        }

        private IEnumerator Flash()
        {
            ElementColours = flashColour;
            yield return new WaitForSeconds(FlashTime);
            ElementColours = defaultColour;
        }

    }
}