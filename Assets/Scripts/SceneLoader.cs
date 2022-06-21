using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using MultiplayerRunTime;
using UnityEngine.EventSystems;

public class SceneLoader : MonoBehaviour
{
    private UIDocument document;
    private ProgressBar progressBar;
    private Button relayButton;
    private Button PeerToPeerButton;
    private Button CloseGameButton;
    private VisualElement ButtonPanel;
    private VisualElement LoadingBarPanel;

    private RadioButton DesertRadioButton;
    private RadioButton CityRadioButton;
    private RadioButton GridRadioButton;

    private void Start()
    {
        document = GetComponent<UIDocument>();
        progressBar = document.rootVisualElement.Q<ProgressBar>("ProgressBar");
        relayButton = document.rootVisualElement.Q<Button>("RelayButton");
        PeerToPeerButton = document.rootVisualElement.Q<Button>("PeerToPeerButton");
        CloseGameButton = document.rootVisualElement.Q<Button>("QuitButton");
        ButtonPanel = document.rootVisualElement.Q<VisualElement>("ButtonPanel");
        LoadingBarPanel = document.rootVisualElement.Q<VisualElement>("LoadingBar");
        DesertRadioButton = document.rootVisualElement.Q<RadioButton>("DesertButton");
        CityRadioButton = document.rootVisualElement.Q<RadioButton>("CityButton");
        GridRadioButton = document.rootVisualElement.Q<RadioButton>("GridButton");
        relayButton.RegisterCallback<ClickEvent>(ev => OnRelayClicked());
        PeerToPeerButton.RegisterCallback<ClickEvent>(ev => OnPeerToPeerClicked());
        CloseGameButton.RegisterCallback<ClickEvent>(ev => Closegame());
        relayButton.RegisterCallback<NavigationSubmitEvent>(ev => OnRelayClicked());
        PeerToPeerButton.RegisterCallback<NavigationSubmitEvent>(ev => OnPeerToPeerClicked());
        CloseGameButton.RegisterCallback<NavigationSubmitEvent>(ev => Closegame());
        LoadingBarPanel.style.display = DisplayStyle.None;
        ButtonPanel.style.display = DisplayStyle.Flex;

        FindObjectOfType<EventSystem>().SetSelectedGameObject(FindObjectOfType<PanelEventHandler>().gameObject);
    }

    private void OnRelayClicked()
    {
        UserCustomisableSettings.UseLocal = false;
        StartCoroutine(LoadMainScene());
    }

    private void OnPeerToPeerClicked()
    {
        UserCustomisableSettings.UseLocal = true;
        StartCoroutine(LoadMainScene());
    }

    private void Closegame()
    {
        Application.Quit();
    }

    private IEnumerator LoadMainScene()
    {
        LoadingBarPanel.style.display = DisplayStyle.Flex;
        ButtonPanel.style.display = DisplayStyle.None;
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(GetMapIndex());
        while (!asyncLoad.isDone)
        {
            progressBar.value = asyncLoad.progress;
            yield return null;
        }
    }   

    private int GetMapIndex()
    {
        if (DesertRadioButton.value)
        {
            return 3;
        }
        else if (CityRadioButton.value)
        {
            return 2;
        }
        else
        {
            return 1;
        }
    }
}
