using UnityEngine;
using DungeonKIT;

/// <summary>
/// 通用场景背景音乐播放器：
///   场景加载时自动播放指定背景音乐。
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
        if (!playOnStart) return;

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[SceneBGMPlayer] AudioManager.Instance 为空，跳过播放");
            return;
        }

        AudioClip clip = bgmClip != null ? bgmClip : AudioManager.Instance.music;
        AudioManager.Instance.PlayMusic(clip);
    }

    /// <summary>手动切换背景音乐（可被其他脚本调用）</summary>
    public void PlayBGM(AudioClip clip)
    {
        if (AudioManager.Instance != null && clip != null)
            AudioManager.Instance.PlayMusic(clip);
    }
}
