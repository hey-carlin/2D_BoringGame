using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    private CanvasGroup fadeGroup;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        fadeGroup = GetComponentInChildren<CanvasGroup>();
        fadeGroup.alpha = 1f;
    }

    private void Start()
    {
        SceneManager.sceneLoaded += (_, __) =>
        {
            StartCoroutine(FadeIn());
        };

        StartCoroutine(FadeIn());
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(FadeOutAndLoad(sceneName));
    }

    IEnumerator FadeIn()
    {
        float t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime * 2f;
            fadeGroup.alpha = t;
            yield return null;
        }
        fadeGroup.alpha = 0f;
    }

    IEnumerator FadeOutAndLoad(string sceneName)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            fadeGroup.alpha = t;
            yield return null;
        }

        SceneManager.LoadScene(sceneName);
    }
}