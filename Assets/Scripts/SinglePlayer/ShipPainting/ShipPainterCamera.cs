using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using MultiplayerRunTime;
public class ShipPainterCamera : MonoBehaviour
{
    [SerializeField] private ShipPainter controller;
    public MouseFlightControllerMP mFCMP;
    [SerializeField] private Transform cameraRig;
    [SerializeField] private Transform cameraPos;
    [SerializeField] private Transform mouseAim;
    private void Awake()
    {
        mFCMP = FindObjectOfType<MouseFlightControllerMP>();
    }
    public void SetCamera()
    {
        transform.position = controller.active.transform.position;
        Vector3 camPos = controller.active.healthManagerMP.gameObject.GetComponent<SpaceshipMP>().TPSCameraPosition;
        camPos.y = 0;
        cameraPos.transform.localPosition = camPos;
    }

    // Update is called once per frame
    private void Update()
    {
        if (!ColourPicker.Instance.IsOpen)
        {
            RotateRig();
        }
    }

    private void RotateRig()
    {
        if (mouseAim == null || cameraPos == null || cameraRig == null)
            return;

        // Mouse input.
        Vector2 rawAxis = mFCMP.inputControl.AlwaysOn.Mouse.ReadValue<Vector2>();
        //Vector2 rawAxis = InputControl.Singleton.AlwaysOn.Mouse.ReadValue<Vector2>();
        float mouseX = rawAxis.x * mFCMP.MouseSensitivity;
        float mouseY = -rawAxis.y * mFCMP.MouseSensitivity;
        // Rotate the aim target that the plane is meant to fly towards.
        // Use the camera's axes in world space so that mouse motion is intuitive.

        mouseAim.Rotate(cameraPos.right, mouseY, Space.World);
        mouseAim.Rotate(cameraPos.up, mouseX, Space.World);
        // The up vector of the camera normally is aligned to the horizon. However, when
        // looking straight up/down this can feel a bit weird. At those extremes, the camera
        // stops aligning to the horizon and instead aligns to itself.
        Vector3 upVec = (math.abs(mouseAim.forward.y) > 0.9f) ? cameraRig.up : Vector3.up;

        // Smoothly rotate the camera to face the mouse aim.
        cameraRig.rotation = DampCamera(cameraRig.rotation, Quaternion.LookRotation(mouseAim.forward, upVec), mFCMP.TPScamSmoothSpeed, Time.deltaTime);
        //cameraRig.rotation = DampCamera(cameraRig.rotation, Quaternion.LookRotation(mouseAim.forward, upVec), 5f, Time.deltaTime);
    }

    private Quaternion DampCamera(Quaternion a, Quaternion b, float lambda, float dt)
    {
        return Quaternion.Slerp(a, b, 1 - math.exp(-lambda * dt));
    }

}
