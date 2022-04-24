using System.Collections;
using System.Collections.Generic;
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

    // Start is called before the first frame update
    private void Start()
    {
        document = GetComponent<UIDocument>();
        progressBar = document.rootVisualElement.Q<ProgressBar>("ProgressBar");
        relayButton = document.rootVisualElement.Q<Button>("RelayButton");
        PeerToPeerButton = document.rootVisualElement.Q<Button>("PeerToPeerButton");
        CloseGameButton = document.rootVisualElement.Q<Button>("QuitButton");
        ButtonPanel = document.rootVisualElement.Q<VisualElement>("ButtonPanel");
        LoadingBarPanel = document.rootVisualElement.Q<VisualElement>("LoadingBar");
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
        //yield return new WaitForSeconds(2.5f);
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(1);
        while (!asyncLoad.isDone)
        {
            progressBar.value = asyncLoad.progress;
            yield return null;
        }
    }   
}
