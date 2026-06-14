using UnityEngine;

public class LevelLoader : MonoBehaviour
{
    public string nextSceneName = "Scene2";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            SceneFader.Instance.LoadScene(nextSceneName);
        }
    }
}