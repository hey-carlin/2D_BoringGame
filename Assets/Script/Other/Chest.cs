using UnityEngine;
using System.Collections;

/// <summary>
/// 宝箱交互脚本：
/// - 玩家进入触发范围后，连续按 J 攻击宝箱 N 次即可打开
/// - 打开后播放宝箱动画，停在最后一帧
/// - 在指定位置生成道具
///
/// 挂载要求：Collider2D（IsTrigger=true）、Animator
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Animator))]
public class Chest : MonoBehaviour
{
    [Header("攻击次数")]
    public int requiredHits = 3;                         // 需要按 J 的次数
    public float comboTimeout = 2f;                      // 连击超时（秒），超时后计数重置

    [Header("开箱延迟")]
    public float openDelay = 0.3f;                       // 第三次攻击后延迟多久播放开箱动画

    [Header("道具生成")]
    public GameObject itemPrefab;                        // 道具预制体（需要有 CollectibleItem 组件）
    public Transform itemSpawnPoint;                     // 道具生成位置（留空则自动在宝箱上方生成）

    [Header("视觉反馈")]
    public GameObject pressJPrompt;                      // "按 J 打开"提示 UI（可选）
    public GameObject hitFeedbackPrefab;                 // 每次受击的反馈特效（可选）

    [Header("调试")]
    public bool showDebugLog = true;

    // ── 内部状态 ──
    private Animator animator;
    private int hitCount = 0;
    private float comboTimer = 0f;
    private bool isOpened = false;
    private bool itemSpawned = false;
    private bool isPlayerInRange = false;
    private Transform playerTransform;

    private static readonly int ParamOpen = Animator.StringToHash("Open");

    void Awake()
    {
        animator = GetComponent<Animator>();

        // 确保碰撞体是 Trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"[Chest] {name}: Collider2D 未勾选 IsTrigger，自动设置为 Trigger。", this);
            col.isTrigger = true;
        }
    }

    void Start()
    {
        // 查找玩家（用于道具追踪）
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

    void Update()
    {
        if (isOpened) return;

        // ──── 连击超时 ────
        if (comboTimer > 0f)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                if (hitCount > 0 && showDebugLog)
                    Debug.Log($"[Chest] {name}: 连击超时，计数归零。");
                hitCount = 0;
            }
        }

        // ──── 检测 J 键攻击 ────
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.J))
        {
            hitCount++;
            comboTimer = comboTimeout;  // 重置超时

            if (showDebugLog)
                Debug.Log($"[Chest] {name}: 受到攻击！{hitCount}/{requiredHits}");

            // 受击反馈
            if (hitFeedbackPrefab != null)
                Instantiate(hitFeedbackPrefab, transform.position, Quaternion.identity);

            // 连击计数特效（藏宝箱轻微抖动）
            StopAllCoroutines();
            StartCoroutine(HitShakeRoutine());

            if (hitCount >= requiredHits)
            {
                OpenChest();
            }
        }
    }

    // ──────────────────────── 触发器检测 ────────────────────────

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            playerTransform = other.transform;

            if (pressJPrompt != null)
                pressJPrompt.SetActive(true);

            if (showDebugLog)
                Debug.Log($"[Chest] {name}: 玩家进入范围，按 J 攻击宝箱。");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            hitCount = 0;        // 离开范围重置计数
            comboTimer = 0f;

            if (pressJPrompt != null)
                pressJPrompt.SetActive(false);

            if (showDebugLog)
                Debug.Log($"[Chest] {name}: 玩家离开范围，计数重置。");
        }
    }

    // ──────────────────────── 开宝箱 ────────────────────────

    void OpenChest()
    {
        isOpened = true;
        hitCount = 0;
        comboTimer = 0f;

        if (pressJPrompt != null)
            pressJPrompt.SetActive(false);

        // ── 延迟 openDelay 秒后再播放动画 ──
        StartCoroutine(OpenChestDelayedRoutine());
    }

    IEnumerator OpenChestDelayedRoutine()
    {
        // 等待开箱延迟
        yield return new WaitForSeconds(openDelay);

        // 播放开箱动画
        if (animator != null)
        {
            animator.SetTrigger(ParamOpen);
            if (showDebugLog)
                Debug.Log($"[Chest] {name}: 宝箱打开！播放动画（延迟 {openDelay}s）。");
        }

        // 等待动画播放完毕后生成道具、冻结动画
        yield return StartCoroutine(OnOpenAnimationComplete());
    }

    IEnumerator OnOpenAnimationComplete()
    {
        // 等一帧让 Animator 切换到 Open 状态
        yield return null;

        float animLength = 1f;  // 默认 1 秒兜底
        if (animator != null)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            animLength = state.length > 0f ? state.length : 1f;
        }

        // 等待动画播完
        yield return new WaitForSeconds(animLength);

        // ── 停在最后一帧 ──
        if (animator != null)
        {
            animator.enabled = false;   // 禁用 Animator，精灵保持在最后一帧
        }

        // ── 生成道具 ──
        SpawnItem();
    }

    void SpawnItem()
    {
        if (itemSpawned) return;
        if (itemPrefab == null)
        {
            Debug.LogWarning($"[Chest] {name}: itemPrefab 未设置，跳过道具生成。", this);
            return;
        }

        itemSpawned = true;

        // 道具起始位置：宝箱内部（当前 position）
        Vector3 startPos = transform.position;

        // 道具目标位置
        Vector3 targetPos = itemSpawnPoint != null
            ? itemSpawnPoint.position
            : transform.position + Vector3.up * 1.5f;

        // 生成在宝箱位置（内部），后续由 CollectibleItem 飞到目标位置
        GameObject item = Instantiate(itemPrefab, startPos, Quaternion.identity);

        // 将目标位置传给道具
        CollectibleItem collector = item.GetComponent<CollectibleItem>();
        if (collector != null)
        {
            collector.SetTargetPosition(targetPos);
        }

        if (showDebugLog)
            Debug.Log($"[Chest] {name}: 道具已生成 → {item.name}（从 {startPos} 飞往 {targetPos}）");
    }

    // ──────────────────────── 视觉反馈 ────────────────────────

    /// <summary>受击时宝箱轻微抖动</summary>
    IEnumerator HitShakeRoutine()
    {
        Vector3 originalPos = transform.position;
        float duration = 0.15f;
        float intensity = 0.08f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = originalPos.x + Random.Range(-intensity, intensity);
            float y = originalPos.y + Random.Range(-intensity, intensity);
            transform.position = new Vector3(x, y, originalPos.z);
            yield return null;
        }
        transform.position = originalPos;
    }

    // ──────────────────────── 编辑器辅助 ────────────────────────

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // 绘制道具生成点
        Gizmos.color = Color.yellow;
        Vector3 spawnPos;
        if (itemSpawnPoint != null)
            spawnPos = itemSpawnPoint.position;
        else
            spawnPos = transform.position + Vector3.up * 1.5f;

        Gizmos.DrawWireSphere(spawnPos, 0.3f);
        Gizmos.DrawLine(transform.position, spawnPos);

        // 显示"道具生成点"标签
        UnityEditor.Handles.Label(spawnPos + Vector3.up * 0.3f, "道具生成点");
    }
#endif
}
