//
// Copyright (c) Brian Hernandez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using UnityEngine;

namespace MultiplayerRunTime
{
    public class HudMP : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private MouseFlightControllerMP mouseFlight = null;

        [Header("HUD Elements")]
        [SerializeField] private RectTransform boresight = null;
        [SerializeField] private RectTransform mousePos = null;

        private Camera playerCam = null;

        private void Start()
        {
            if (mouseFlight == null)
                Debug.LogError(name + ": Hud - Mouse Flight Controller not assigned!");

            playerCam = Camera.main;

            if (playerCam == null)
                Debug.LogError(name + ": Hud - No camera found on assigned Mouse Flight Controller!");
        }

        private void Update()
        {
            if (mouseFlight == null || playerCam == null)
                return;

            UpdateGraphics(mouseFlight);
        }

        private void UpdateGraphics(MouseFlightControllerMP controller)
        {
            if (boresight != null)
            {
                Vector3 position = playerCam.WorldToScreenPoint(controller.BoresightPos);
                position.z = Mathf.Clamp(position.z, float.MinValue, 2f);
                boresight.position = position;
                boresight.gameObject.SetActive(boresight.position.z > 1f);
            }

            if (mousePos != null)
            {
                Vector3 position = playerCam.WorldToScreenPoint(controller.MouseAimPos);
                position.z = Mathf.Clamp(position.z, float.MinValue, 2f);
                mousePos.position = position;
                mousePos.gameObject.SetActive(mousePos.position.z > 1f);
            }
        }

        public void SetReferenceMouseFlight(MouseFlightControllerMP controller)
        {
            mouseFlight = controller;
        }
    }
}
