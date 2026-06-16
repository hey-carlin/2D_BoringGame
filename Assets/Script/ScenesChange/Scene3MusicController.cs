using UnityEngine;
using DungeonKIT;

/// <summary>
/// Scene3 动态音乐控制器：
///   - 场景开始时播放背景音乐（Scene Theme 5 - Loop）
///   - Boss 战触发时淡入淡出切换到 Boss 主题（Boss Theme 1 - Loop）
///   - Boss 死亡 / 玩家死亡时停止背景音乐
///   （胜利/失败音效由 GameManager 统一处理）
/// </summary>
public class Scene3MusicController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Boss GameObject（挂有 BossCAI 组件）")]
    public BossCAI boss;
    [Tooltip("玩家 PlayerHealth 组件")]
    public PlayerHealth playerHealth;

    [Header("Music Clips")]
    public AudioClip backgroundBGM;     // Scene Theme 5 - Loop
    public AudioClip bossTheme;         // Boss Theme 1 - Loop

    private enum MusicState { None, BGM, Boss, Ended }
    private MusicState currentState = MusicState.None;

    private bool bossFightStarted;
    private bool ended;

    void Start()
    {
        // 自动查找引用
        if (boss == null)
            boss = FindObjectOfType<BossCAI>();
        if (playerHealth == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerHealth = playerObj.GetComponent<PlayerHealth>();
        }

        // 使用 AudioManager 上的 clip（如果有的话），否则使用本地引用
        if (backgroundBGM == null && AudioManager.Instance != null)
            backgroundBGM = AudioManager.Instance.scene3BGM;
        if (bossTheme == null && AudioManager.Instance != null)
            bossTheme = AudioManager.Instance.bossTheme1;

        // 订阅事件
        if (boss != null)
            boss.OnBossDefeated += OnBossDefeated;
        if (playerHealth != null)
            playerHealth.OnDeath += OnPlayerDeath;

        // 开始播放场景背景音乐
        SetState(MusicState.BGM);
    }

    void OnDestroy()
    {
        if (boss != null)
            boss.OnBossDefeated -= OnBossDefeated;
        if (playerHealth != null)
            playerHealth.OnDeath -= OnPlayerDeath;
    }

    // ──── 公开方法（由 BossArenaTrigger 调用）────

    /// <summary>由 Boss 区域触发器调用，开始 Boss 战音乐</summary>
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

    private void OnPlayerDeath()
    {
        if (!ended)
        {
            ended = true;
            SetState(MusicState.Ended);
        }
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
                // 停止背景音乐，胜利/失败音效由 GameManager 播放
                AudioManager.Instance?.StopMusic();
                break;
        }
    }
}
