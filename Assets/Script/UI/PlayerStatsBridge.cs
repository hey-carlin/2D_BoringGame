using UnityEngine;
using UnityEngine.SceneManagement;
using DungeonKIT;

/// <summary>
/// 游戏启动桥接脚本：初始化所有旧系统 Manager 单例，
/// 同步新旧血量系统，加载 GameUI。
/// 挂到 SampleScene 中任意 GameObject 上。
/// </summary>
public class PlayerStatsBridge : MonoBehaviour
{
    private PlayerHealth newHealth;

    private void Awake()
    {
        Debug.Log("[Bridge] Awake 开始...");

        // 1. 确保旧系统核心单例存在
        EnsureScenesManager();
        Debug.Log($"[Bridge] ScenesManager.Instance = {ScenesManager.Instance != null}");

        EnsureAudioManager();
        Debug.Log($"[Bridge] AudioManager.Instance = {AudioManager.Instance != null}");

        EnsureSingleton<GameManager>();
        Debug.Log($"[Bridge] GameManager.Instance = {GameManager.Instance != null}");

        EnsureSingleton<PlayerStats>();
        Debug.Log($"[Bridge] PlayerStats.Instance = {PlayerStats.Instance != null}");

        // 2. 加载 GameUI 叠加场景（先检查避免重复加载）
        if (!IsSceneLoaded("GameUI"))
        {
            Debug.Log("[Bridge] 正在加载 GameUI 场景...");
            ScenesManager.Instance.LoadAdditiveScene("GameUI");
            Debug.Log("[Bridge] LoadAdditiveScene(\"GameUI\") 调用完成");
        }
        else
        {
            Debug.Log("[Bridge] GameUI 场景已加载，跳过重复加载。");
        }
    }

    /// <summary>检查场景是否已加载</summary>
    private bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).name == sceneName)
                return true;
        }
        return false;
    }

    private void Start()
    {
        Debug.Log("[Bridge] Start 开始...");

        // 3. 找到新系统的玩家组件
        newHealth = FindObjectOfType<PlayerHealth>();

        if (newHealth == null)
        {
            Debug.LogError("[Bridge] 找不到 PlayerHealth 组件！请在玩家 GameObject 上挂 PlayerHealth。");
            return;
        }
        Debug.Log($"[Bridge] 找到 PlayerHealth: {newHealth.name}, HP={newHealth.currentHealth}/{newHealth.maxHealth}");

        // 4. 订阅死亡事件
        newHealth.OnDeath += HandlePlayerDeath;

        // 5. 初始化 PlayerStats
        PlayerStats.Instance.HP.max = newHealth.maxHealth;
        PlayerStats.Instance.HP.current = newHealth.currentHealth;
        Debug.Log($"[Bridge] PlayerStats HP 已初始化: {PlayerStats.Instance.HP.current}/{PlayerStats.Instance.HP.max}");

        // 6. 初始化 GameManager
        GameManager.Instance.isGame = true;

        // 延迟检查 UIManager 是否加载成功
        Invoke(nameof(CheckUILoaded), 0.5f);
    }

    private void CheckUILoaded()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("[Bridge] UIManager.Instance 为 null！GameUI 场景可能没有加载成功。请确认：\n"
                + "1. GameUI.unity 已添加到 Build Settings（File → Build Settings → Add Open Scenes）\n"
                + "2. GameUI.unity 场景文件名就是 'GameUI'\n"
                + "3. GameUI 场景中 [Managers] 下的 Managers GameObject 挂有 UIManager 组件");
        }
        else
        {
            Debug.Log("[Bridge] UIManager.Instance 加载成功！UI 应该可见了。");
        }
    }

    private void Update()
    {
        if (newHealth == null) return;

        // 实时同步血量
        if (PlayerStats.Instance.HP.current != newHealth.currentHealth)
        {
            PlayerStats.Instance.HP.current = newHealth.currentHealth;
            UIManager.Instance?.UpdateUI();
        }
    }

    private void OnDestroy()
    {
        if (newHealth != null)
            newHealth.OnDeath -= HandlePlayerDeath;
    }

    private void HandlePlayerDeath()
    {
        Debug.Log("[Bridge] 玩家死亡，触发 GameOver");
        GameManager.Instance?.GameOver();
    }

    // ── 单例创建辅助 ──────────────────────────────

    private T EnsureSingleton<T>() where T : MonoBehaviour
    {
        // 先尝试 property，再尝试 field（旧代码混用了两种写法）
        var existing = GetStaticInstance<T>();
        if (existing != null)
        {
            Debug.Log($"[Bridge] {typeof(T).Name} 单例已存在，跳过创建");
            return existing;
        }

        Debug.Log($"[Bridge] 创建 {typeof(T).Name} 单例...");
        var go = new GameObject($"[Bridge] {typeof(T).Name}");
        go.transform.SetParent(transform);
        return go.AddComponent<T>();
    }

    /// <summary>通过反射获取静态 Instance（兼容 field 和 property）</summary>
    private static T GetStaticInstance<T>() where T : MonoBehaviour
    {
        var flags = System.Reflection.BindingFlags.Static
                  | System.Reflection.BindingFlags.Public
                  | System.Reflection.BindingFlags.NonPublic;

        // 尝试 property
        var prop = typeof(T).GetProperty("Instance", flags);
        if (prop != null)
            return prop.GetValue(null) as T;

        // 尝试 field
        var field = typeof(T).GetField("Instance", flags);
        if (field != null)
            return field.GetValue(null) as T;

        return null;
    }

    private void EnsureScenesManager()
    {
        if (ScenesManager.Instance != null)
        {
            Debug.Log("[Bridge] ScenesManager 已存在（来自 MainMenu 的 DontDestroyOnLoad）");
            return;
        }

        Debug.Log("[Bridge] 创建 ScenesManager（DontDestroyOnLoad）...");
        var go = new GameObject("[Bridge] ScenesManager");
        DontDestroyOnLoad(go);
        go.AddComponent<ScenesManager>();
    }

    private void EnsureAudioManager()
    {
        if (AudioManager.Instance != null)
        {
            Debug.Log("[Bridge] AudioManager 已存在（来自 MainMenu 的 DontDestroyOnLoad）");
            return;
        }

        Debug.Log("[Bridge] 创建 AudioManager（DontDestroyOnLoad）...");
        var go = new GameObject("[Bridge] AudioManager");
        DontDestroyOnLoad(go);
        go.AddComponent<AudioManager>();
        go.AddComponent<AudioSource>();
    }
}
