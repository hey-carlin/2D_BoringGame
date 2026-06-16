using UnityEngine;
using UnityEngine.SceneManagement;
using DungeonKIT;

/// <summary>
/// 通用场景背景音乐播放器：
///   场景加载时自动播放背景音乐，并确保 GameUI 已加载。
///   挂载到任意场景的 GameObject 上即可。
/// </summary>
public class SceneBGMPlayer : MonoBehaviour
{
    [Header("Background Music")]
    [Tooltip("该场景的背景音乐（留空则使用 AudioManager 上的默认 music）")]
    public AudioClip bgmClip;

    [Tooltip("是否在 Start 时自动播放")]
    public bool playOnStart = true;

    void Start()
    {
        // 1. 确保 GameUI 和核心 Manager 存在
        EnsureGameUI();

        // 2. 播放背景音乐
        if (!playOnStart) return;

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[SceneBGMPlayer] AudioManager.Instance 为空，跳过播放");
            return;
        }

        AudioClip clip = bgmClip != null ? bgmClip : AudioManager.Instance.music;
        AudioManager.Instance.PlayMusic(clip);
    }

    /// <summary>确保核心 Manager 和 GameUI 场景已加载</summary>
    private void EnsureGameUI()
    {
        // 确保 ScenesManager 单例存在
        if (ScenesManager.Instance == null)
        {
            var go = new GameObject("[Bootstrap] ScenesManager");
            DontDestroyOnLoad(go);
            go.AddComponent<ScenesManager>();
        }

        // 确保 AudioManager 单例存在
        if (AudioManager.Instance == null)
        {
            var go = new GameObject("[Bootstrap] AudioManager");
            DontDestroyOnLoad(go);
            go.AddComponent<AudioSource>();
            go.AddComponent<AudioManager>();
        }

        // 确保 GameManager 单例存在
        if (GameManager.Instance == null)
        {
            var go = new GameObject("[Bootstrap] GameManager");
            DontDestroyOnLoad(go);
            go.AddComponent<GameManager>();
        }

        // 加载 GameUI 叠加场景（避免重复）
        if (!IsSceneLoaded("GameUI"))
        {
            ScenesManager.Instance.LoadAdditiveScene("GameUI");
            Debug.Log("[SceneBGMPlayer] GameUI 场景已加载");
        }
    }

    private bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name == sceneName)
                return true;
        }
        return false;
    }

    /// <summary>手动切换背景音乐（可被其他脚本调用）</summary>
    public void PlayBGM(AudioClip clip)
    {
        if (AudioManager.Instance != null && clip != null)
            AudioManager.Instance.PlayMusic(clip);
    }
}
