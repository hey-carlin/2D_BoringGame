using System.Collections;
using UnityEngine;

namespace DungeonKIT
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance; //Singleton

        AudioSource backgroundMusic;
        bool isMusicPlay; //for check music status

        // ────────── SFX 音源池 ──────────
        private const int SFX_POOL_SIZE = 8;
        private AudioSource[] sfxSources;
        private int sfxPoolIndex;

        // ═══════════════════════════════════════
        //  Inspector 字段
        // ═══════════════════════════════════════

        [Header("Legacy Clips (keep for backward compat)")]
        public AudioClip aiDamage;
        public AudioClip playerDamage;
        public AudioClip pickUpKey;
        public AudioClip pickUpItems;
        public AudioClip pickUpCoin;
        public AudioClip openDoor;
        public AudioClip drinkBottle;
        public AudioClip music;          // 默认背景音乐（主菜单 + 通用）

        [Header("Music Clips")]
        public AudioClip scene3BGM;      // Scene Theme 5 - Loop
        public AudioClip bossTheme1;     // Boss Theme 1 - Loop
        public AudioClip bossTheme2;     // Boss Theme 2 - Loop（备用）
        public AudioClip victoryMusic;   // win1.wav
        public AudioClip defeatMusic;    // fail.wav

        [Header("Player SFX")]
        public AudioClip playerJump;         // 普通跳跃
        public AudioClip playerDoubleJump;   // 二段跳
        public AudioClip playerAttackLight;  // 轻攻击
        public AudioClip playerAttackHeavy;  // 重攻击
        public AudioClip playerAttackUp;     // 上挑攻击
        public AudioClip playerGlide;        // 滑翔（Loop）
        public AudioClip playerAppear;       // 登场

        [Header("Enemy/Boss SFX")]
        public AudioClip bossRoar;           // Boss 咆哮
        public AudioClip enemyHit;           // 敌人受击
        public AudioClip enemyMeleeAttack;   // 敌人近战
        public AudioClip enemyRangeAttack;   // 敌人远程/AOE

        [Header("Volume Settings")]
        [Range(0f, 1f)] public float sfxVolume = 0.8f;
        [Range(0f, 1f)] public float musicVolume = 0.6f;

        // ═══════════════════════════════════════
        //  生命周期
        // ═══════════════════════════════════════

        private void Awake()
        {
            // Singleton
            if (AudioManager.Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 初始化 SFX 音源池
            sfxSources = new AudioSource[SFX_POOL_SIZE];
            for (int i = 0; i < SFX_POOL_SIZE; i++)
            {
                sfxSources[i] = gameObject.AddComponent<AudioSource>();
                sfxSources[i].playOnAwake = false;
                sfxSources[i].volume = sfxVolume;
            }
            sfxPoolIndex = 0;
        }

        private void Start()
        {
            backgroundMusic = GetComponent<AudioSource>();
            if (backgroundMusic == null)
                backgroundMusic = gameObject.AddComponent<AudioSource>();
            backgroundMusic.volume = musicVolume;
            backgroundMusic.loop = true;
            backgroundMusic.playOnAwake = false;
        }

        // ═══════════════════════════════════════
        //  背景音乐
        // ═══════════════════════════════════════

        /// <summary>播放/切换背景音乐（直接切换）</summary>
        public void PlayMusic(AudioClip musicClip)
        {
            if (musicClip == null) return;

            if (!isMusicPlay)
            {
                isMusicPlay = true;
                Play(backgroundMusic, musicClip, true);
            }
            else
            {
                backgroundMusic.Stop();
                backgroundMusic.clip = musicClip;
                backgroundMusic.loop = true;
                backgroundMusic.volume = musicVolume;
                backgroundMusic.Play();
            }
        }

        /// <summary>淡入淡出切换背景音乐</summary>
        public void CrossfadeMusic(AudioClip newClip, float duration = 1.5f)
        {
            if (musicCrossfadeCoroutine != null)
                StopCoroutine(musicCrossfadeCoroutine);
            musicCrossfadeCoroutine = StartCoroutine(CrossfadeRoutine(newClip, duration));
        }

        private Coroutine musicCrossfadeCoroutine;

        private IEnumerator CrossfadeRoutine(AudioClip newClip, float duration)
        {
            if (newClip == null) yield break;

            float halfDuration = duration * 0.5f;
            float startVol = backgroundMusic.volume;

            // Fade out
            float t = 0f;
            while (t < halfDuration)
            {
                t += Time.unscaledDeltaTime;
                backgroundMusic.volume = Mathf.Lerp(startVol, 0f, t / halfDuration);
                yield return null;
            }

            // 切换 clip
            backgroundMusic.Stop();
            backgroundMusic.clip = newClip;
            backgroundMusic.loop = true;
            backgroundMusic.volume = 0f;
            backgroundMusic.Play();
            isMusicPlay = true;

            // Fade in
            t = 0f;
            while (t < halfDuration)
            {
                t += Time.unscaledDeltaTime;
                backgroundMusic.volume = Mathf.Lerp(0f, musicVolume, t / halfDuration);
                yield return null;
            }
            backgroundMusic.volume = musicVolume;
        }

        /// <summary>停止背景音乐</summary>
        public void StopMusic()
        {
            backgroundMusic.Stop();
            isMusicPlay = false;
        }

        // ═══════════════════════════════════════
        //  SFX（音源池轮询）
        // ═══════════════════════════════════════

        /// <summary>播放一次音效（使用内部池，调用方无需自带 AudioSource）</summary>
        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            AudioSource src = sfxSources[sfxPoolIndex];
            sfxPoolIndex = (sfxPoolIndex + 1) % SFX_POOL_SIZE;
            src.volume = sfxVolume;
            src.PlayOneShot(clip);
        }

        /// <summary>播放一次音效（自定义音量）</summary>
        public void PlaySFX(AudioClip clip, float volume)
        {
            if (clip == null) return;
            AudioSource src = sfxSources[sfxPoolIndex];
            sfxPoolIndex = (sfxPoolIndex + 1) % SFX_POOL_SIZE;
            src.volume = volume;
            src.PlayOneShot(clip);
        }

        /// <summary>播放循环音效（如滑翔）。返回 AudioSource 供 StopLoopingSFX 使用</summary>
        public AudioSource PlayLoopingSFX(AudioClip clip)
        {
            if (clip == null) return null;
            AudioSource src = sfxSources[sfxPoolIndex];
            sfxPoolIndex = (sfxPoolIndex + 1) % SFX_POOL_SIZE;
            src.volume = sfxVolume;
            src.clip = clip;
            src.loop = true;
            src.Play();
            return src;
        }

        /// <summary>停止指定循环音效</summary>
        public void StopLoopingSFX(AudioSource src)
        {
            if (src != null) src.Stop();
        }

        // ═══════════════════════════════════════
        //  通用（向后兼容）
        // ═══════════════════════════════════════

        /// <summary>用指定的 AudioSource 播放 AudioClip（旧版兼容接口）</summary>
        public void Play(AudioSource audioSource, AudioClip audioClip, bool loop)
        {
            if (audioClip == null || audioSource == null) return;
            audioSource.clip = audioClip;
            audioSource.loop = loop;
            audioSource.Play();
        }

        /// <summary>更新 SFX 音量（运行时修改）</summary>
        public void SetSFXVolume(float vol)
        {
            sfxVolume = Mathf.Clamp01(vol);
            for (int i = 0; i < SFX_POOL_SIZE; i++)
                sfxSources[i].volume = sfxVolume;
        }

        /// <summary>更新音乐音量（运行时修改）</summary>
        public void SetMusicVolume(float vol)
        {
            musicVolume = Mathf.Clamp01(vol);
            backgroundMusic.volume = musicVolume;
        }
    }
}
