using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using MultiplayerRunTime;

public class ShipPainter : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private ShipProp[] ships;
    [SerializeField] ShipPainterCamera painterCamera;
    [HideInInspector] public ShipProp active;
    public int set = 0;

    private ColourDisplay ShipColourDisplay;
    private Button DiscardButton;
    private Button ApplyButton;

    private Color colour;
    public Color Colour
    {
        get => colour;
        private set
        {
            if(value != colour)
            {
                colour = value;
                active.SetColour(value);
            }
        }
    }

    private IEnumerator Query()
    {
        yield return null;
        ShipColourDisplay = new ColourDisplay(document.rootVisualElement.Q("ColourDisplay"));
        DiscardButton = document.rootVisualElement.Q<Button>("Discard");
        ApplyButton = document.rootVisualElement.Q<Button>("Apply");
        DiscardButton.RegisterCallback<ClickEvent>(ev => OnDiscardClick());
        ApplyButton.RegisterCallback<ClickEvent>(ev => OnApplyClick());
        ShipColourDisplay.Colour = active.healthManagerMP.DefaultColour;
        Debug.Log("ShipPainterStart");
    }

    private void Update()
    {
        if(ShipColourDisplay != null)
        {
            Colour = ShipColourDisplay.Colour;
        }
    }

    public void Open(int ship)
    {
        set = ship;
        gameObject.SetActive(true);

        for (int i = 0; i < ships.Length; i++)
        {
            ships[i].gameObject.SetActive(false);
        }
        ships[set].gameObject.SetActive(true);
        active = ships[set];
        painterCamera.SetCamera();
        StartCoroutine(Query());
    }

    public void Close()
    {
        gameObject.SetActive(false);
        PasswordLobbyMP.Singleton.menu.ShowSpawnOverlay(true);
        ShipColourDisplay = null;
    }

    private void OnApplyClick()
    {
        if (ColourPicker.Instance.IsOpen)
        {
            return;
        }
        Close();
    }

    private void OnDiscardClick()
    {
        if (ColourPicker.Instance.IsOpen)
        {
            return;
        }
        colour = active.healthManagerMP.DefaultColour;
        Close();
    }

}
