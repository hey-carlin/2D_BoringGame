using UnityEngine;
using UnityEngine.SceneManagement;
using DungeonKIT;

/// <summary>
/// Scene3 动态音乐控制器：
///   - 场景开始时播放背景音乐
///   - 玩家进入 Boss 区域 → 切换到 Boss 主题
///   - Boss 死亡 → 停止背景（胜利音效由 GameManager 播放）
///   - 玩家死亡 → 停止背景（失败音效由 GameManager 播放）
///   Boss3 由墓碑 Summoner 动态生成，本脚本自动检测并绑定。
/// </summary>
public class Scene3MusicController : MonoBehaviour
{
    [Header("Music Clips")]
    public AudioClip backgroundBGM;     // Scene Theme 5 - Loop
    public AudioClip bossTheme;         // Boss Theme 1 - Loop

    private enum MusicState { None, BGM, Boss, Ended }
    private MusicState currentState = MusicState.None;

    private BossCAI boss;
    private PlayerHealth playerHealth;
    private bool bossFightStarted;
    private bool bossEventsBound;
    private bool ended;

    void Start()
    {
        // 0. 确保 GameUI 和核心 Manager 存在
        EnsureGameUI();

        // 查找 Player
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerHealth = playerObj.GetComponent<PlayerHealth>();

        // 查找 Boss（可能还未生成，稍后在 Update 中持续查找）
        boss = FindObjectOfType<BossCAI>();
        TryBindBossEvents();

        // 使用 AudioManager 上的 clip（如果有的话）
        if (backgroundBGM == null && AudioManager.Instance != null)
            backgroundBGM = AudioManager.Instance.scene3BGM;
        if (bossTheme == null && AudioManager.Instance != null)
            bossTheme = AudioManager.Instance.bossTheme1;

        // 订阅玩家死亡
        if (playerHealth != null)
            playerHealth.OnDeath += OnPlayerDeath;

        // 开始播放场景背景音乐
        SetState(MusicState.BGM);
    }

    void Update()
    {
        if (ended) return;

        // 持续查找 Boss（墓碑打碎后才会生成）
        if (!bossEventsBound)
        {
            boss = FindObjectOfType<BossCAI>();
            TryBindBossEvents();
        }
    }

    void OnDestroy()
    {
        if (boss != null)
        {
            boss.OnBossDefeated -= OnBossDefeated;
            boss.OnBossPhaseTransition -= OnBossPhaseTransition;
        }
        if (playerHealth != null)
            playerHealth.OnDeath -= OnPlayerDeath;
    }

    // ──── 尝试绑定 Boss 事件 ────

    private void TryBindBossEvents()
    {
        if (bossEventsBound || boss == null) return;

        boss.OnBossDefeated += OnBossDefeated;
        boss.OnBossPhaseTransition += OnBossPhaseTransition;
        bossEventsBound = true;
        Debug.Log("[Scene3MusicController] Boss3 已检测到，事件已绑定");
    }

    // ──── 公开方法（由 BossArenaTrigger 调用）────

    /// <summary>玩家进入 Boss 区域时调用，立即切换 Boss 战音乐</summary>
    public void StartBossFight()
    {
        if (!bossFightStarted)
        {
            bossFightStarted = true;
            SetState(MusicState.Boss);
        }
    }

    // ──── 事件回调 ────

    private void OnBossDefeated()
    {
        if (!ended)
        {
            ended = true;
            SetState(MusicState.Ended);
        }
    }

    private void OnBossPhaseTransition()
    {
        // P1→P2 时咆哮声效由 BossCAI 直接播放
    }

    private void OnPlayerDeath()
    {
        if (!ended)
        {
            ended = true;
            SetState(MusicState.Ended);
        }
    }

    // ──── GameUI 加载 ────

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

        // 确保 AudioManager 单例存在（直接 Play Scene3 时 MainMenu 未加载）
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
            Debug.Log("[Scene3MusicController] GameUI 场景已加载");
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

    // ──── 状态切换 ────

    private void SetState(MusicState newState)
    {
        if (currentState == newState) return;
        currentState = newState;

        switch (newState)
        {
            case MusicState.BGM:
                if (AudioManager.Instance != null && backgroundBGM != null)
                    AudioManager.Instance.PlayMusic(backgroundBGM);
                break;

            case MusicState.Boss:
                if (AudioManager.Instance != null && bossTheme != null)
                    AudioManager.Instance.CrossfadeMusic(bossTheme, 2.0f);
                break;

            case MusicState.Ended:
                AudioManager.Instance?.StopMusic();
                break;
        }
    }
}
