using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    private UIDocument document;
    private ProgressBar progressBar;

    // Start is called before the first frame update
    private void Start()
    {
        document = GetComponent<UIDocument>();
        progressBar = document.rootVisualElement.Q<ProgressBar>("ProgressBar");
        StartCoroutine(LoadMainScene());
    }

    private IEnumerator LoadMainScene()
    {
        yield return new WaitForSeconds(2.5f);
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(1);
        while (!asyncLoad.isDone)
        {
            progressBar.value = asyncLoad.progress;
            yield return null;
        }
    }   
}
