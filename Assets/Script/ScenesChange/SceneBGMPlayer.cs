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

        // 1.1 确保旧版 PlayerStats 存在并同步新版 PlayerHealth 血量
        SyncPlayerStats();

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

    private PlayerHealth cachedPlayerHealth;

    void Update()
    {
        // 每帧同步新版 PlayerHealth → 旧版 PlayerStats → UI
        if (cachedPlayerHealth == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) cachedPlayerHealth = playerObj.GetComponent<PlayerHealth>();
        }

        if (cachedPlayerHealth != null && PlayerStats.Instance != null)
        {
            if (PlayerStats.Instance.HP.current != cachedPlayerHealth.currentHealth)
            {
                PlayerStats.Instance.HP.current = cachedPlayerHealth.currentHealth;
                PlayerStats.Instance.HP.max = cachedPlayerHealth.maxHealth;
                UIManager.Instance?.UpdateUI();
            }
        }
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

    /// <summary>确保旧版 PlayerStats 存在，并从新版 PlayerHealth 同步血量</summary>
    private void SyncPlayerStats()
    {
        if (PlayerStats.Instance == null)
        {
            var go = new GameObject("[Bootstrap] PlayerStats");
            DontDestroyOnLoad(go);
            go.AddComponent<PlayerStats>();
        }

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            var ph = playerObj.GetComponent<PlayerHealth>();
            if (ph != null && PlayerStats.Instance != null)
            {
                PlayerStats.Instance.HP.max = ph.maxHealth;
                PlayerStats.Instance.HP.current = ph.currentHealth;
            }
        }
    }

    /// <summary>手动切换背景音乐（可被其他脚本调用）</summary>
    public void PlayBGM(AudioClip clip)
    {
        if (AudioManager.Instance != null && clip != null)
            AudioManager.Instance.PlayMusic(clip);
    }
}
