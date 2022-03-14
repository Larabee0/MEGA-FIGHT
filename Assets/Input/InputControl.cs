using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class InputControl : MonoBehaviour
{
    public static InputControl Singleton;

    private Player playerActions;

    public Player.FlightActions FlightActions
    {
        get => playerActions.Flight;
    }

    public Player.UIActions UIActions
    {
        get => playerActions.UI;
    }

    private void Awake()
    {
        Singleton = this;
        playerActions = new Player();
        SetUIEnabled(true);
    }

    //private void FixedUpdate()
    //{
    //    //Debug.LogFormat("MouseX {0}", Input.GetAxis("Mouse X"));
    //    //Debug.LogFormat("MouseY {0}", Input.GetAxis("Mouse Y"));
    //    //Debug.LogFormat("New MouseX {0}", FlightActions.MouseX.ReadValue<float>());
    //    //Debug.LogFormat("New MouseY {0}", FlightActions.MouseY.ReadValue<float>());
    //
    //    Debug.Log(FlightActions.JoyStick.ReadValue<Vector3>());
    //
    //}


    

    public void SetFlightEnabled(bool enabled)
    {
        switch (enabled)
        {
            case true:
                playerActions.Flight.Enable();
                break;
            case false:
                playerActions.Flight.Disable();
                break;
        }
    }

    public void SetUIEnabled(bool enabled)
    {
        switch (enabled)
        {
            case true:
                playerActions.UI.Enable();
                break;
            case false:
                playerActions.UI.Disable();
                break;
        }
    }
}
