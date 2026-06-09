using UnityEngine;
using Enemy;   // IDamageable 接口所在命名空间

/// <summary>
/// 通用陷阱脚本：通过标签检测玩家触碰，触发伤害和 Hit 动画。
/// 挂载到带有 Collider2D（勾选 IsTrigger）的 GameObject 上即可使用。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Trap : MonoBehaviour
{
    [Header("伤害")]
    public int damage = 10;                              // 每次触发造成的伤害

    [Header("冷却")]
    public float cooldown = 1f;                          // 两次触发的最小间隔（秒），防止连续触发

    [Header("一次性")]
    public bool triggerOnce = false;                     // true=仅触发一次后禁用
    public float disableDelay = 0f;                      // 触发后延迟禁用（秒），0=立即

    [Header("检测")]
    public string targetTag = "Player";                  // 目标标签，默认检测 Player

    [Header("碰撞体同步（精灵帧动画用）")]
    public bool syncColliderToSprite = true;             // 每帧根据当前精灵图自动调整碰撞体大小/偏移
    public float colliderPadding = 0f;                   // 碰撞体额外扩展（正值=比精灵略大）

    [Header("碰撞体动画偏移（让碰撞体跟着动画上下左右移动）")]
    public bool animateColliderOffset = true;            // 是否启用碰撞体动画偏移
    public AnimationCurve offsetXCurve = AnimationCurve.Constant(0, 1, 0);  // X偏移曲线（横轴=动画归一化时间 0~1）
    public AnimationCurve offsetYCurve = AnimationCurve.Constant(0, 1, 0);  // Y偏移曲线
    public float offsetMultiplier = 1f;                  // 曲线值倍率（调大=移动幅度更大）

    [Header("调试")]
    public bool showDebugLog = true;                     // 在 Console 输出调试信息

    // 内部状态
    private float cooldownTimer = 0f;                    // 当前冷却倒计时
    private bool hasTriggered = false;                   // 是否已触发（一次性陷阱用）
    private Collider2D trapCollider;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Vector2 baseColliderOffset;                  // 记录初始/精灵同步的基础偏移

    private void Awake()
    {
        trapCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        // 记录碰撞体初始偏移（后续动画偏移叠加在此之上）
        if (trapCollider is BoxCollider2D box)
            baseColliderOffset = box.offset;
        else if (trapCollider is CircleCollider2D circle)
            baseColliderOffset = circle.offset;

        // ──── 配置自检 ────
        if (trapCollider == null)
        {
            Debug.LogError($"[Trap] {name}: 未找到 Collider2D 组件！请添加 Collider2D。", this);
            return;
        }

        // ── TilemapCollider2D 特殊处理（运行时类型检测，避免编译依赖 Tilemap 包）──
        Component tilemapCol = GetComponent("TilemapCollider2D");
        Component compositeCol = GetComponent("CompositeCollider2D");

        if (tilemapCol != null && compositeCol != null)
        {
            // 使用了 Composite 模式：实际物理形状由 CompositeCollider2D 生成
            bool compositeIsTrigger = GetCollider2DIsTrigger(compositeCol);
            if (!compositeIsTrigger)
            {
                Debug.LogWarning(
                    $"[Trap] {name}: 检测到 TilemapCollider2D 配合 CompositeCollider2D 使用。\n" +
                    "  → 请在 CompositeCollider2D 上勾选 IsTrigger（而不是 TilemapCollider2D）！", this);
            }
            else
            {
                if (showDebugLog) Debug.Log($"[Trap] {name}: CompositeCollider2D IsTrigger 已勾选 ✓");
            }
            // 使用 Composite 作为实际工作的碰撞体
            if (compositeCol is Collider2D compCol2D)
                trapCollider = compCol2D;
        }
        else if (tilemapCol != null)
        {
            // 纯 TilemapCollider2D（无 Composite）
            if (!trapCollider.isTrigger)
            {
                Debug.LogWarning(
                    $"[Trap] {name}: TilemapCollider2D 的 IsTrigger 未勾选！\n" +
                    "  → 请在 TilemapCollider2D 上勾选 IsTrigger。", this);
            }
        }
        else if (!trapCollider.isTrigger)
        {
            // 普通 Collider2D
            Debug.LogWarning(
                $"[Trap] {name}: Collider2D 的 IsTrigger 未勾选！\n" +
                    "  → 请在 Inspector 中勾选 IsTrigger。", this);
        }

        // 检查 Physics2D 层级交互设置
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            if (showDebugLog) Debug.Log($"[Trap] {name}: 陷阱自身无 Rigidbody2D。只要目标（玩家）有 Rigidbody2D 即可正常触发。");
        }
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        // ──── 每帧同步碰撞体 ────
        if (syncColliderToSprite)
            SyncColliderToSprite();

        if (animateColliderOffset)
            ApplyAnimOffset();
    }

    /// <summary>
    /// 根据当前 SpriteRenderer 的精灵图自动调整碰撞体大小和基础偏移。
    /// 支持 BoxCollider2D 和 CircleCollider2D。
    /// </summary>
    private void SyncColliderToSprite()
    {
        if (spriteRenderer == null || trapCollider == null)
            return;

        Sprite currentSprite = spriteRenderer.sprite;
        if (currentSprite == null)
            return;

        Bounds bounds = currentSprite.bounds;

        if (trapCollider is BoxCollider2D box)
        {
            baseColliderOffset = bounds.center;
            box.size = bounds.size + Vector3.one * colliderPadding * 2f;
        }
        else if (trapCollider is CircleCollider2D circle)
        {
            baseColliderOffset = bounds.center;
            float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y);
            circle.radius = maxExtent + colliderPadding;
        }

        // 立即应用基础偏移（动画偏移由 ApplyAnimOffset 在下文叠加）
        ApplyBaseOffset();
    }

    /// <summary>
    /// 从 Animator 获取当前动画归一化时间，根据曲线计算偏移，叠加到碰撞体上。
    /// </summary>
    private void ApplyAnimOffset()
    {
        if (trapCollider == null)
            return;

        float normalizedTime = 0f;
        if (animator != null)
        {
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            normalizedTime = stateInfo.normalizedTime % 1f;  // 循环取 0~1
        }

        float extraX = offsetXCurve.Evaluate(normalizedTime) * offsetMultiplier;
        float extraY = offsetYCurve.Evaluate(normalizedTime) * offsetMultiplier;

        Vector2 finalOffset = baseColliderOffset + new Vector2(extraX, extraY);

        if (trapCollider is BoxCollider2D box)
            box.offset = finalOffset;
        else if (trapCollider is CircleCollider2D circle)
            circle.offset = finalOffset;
    }

    /// <summary>
    /// 只应用基础偏移（不含动画偏移）。
    /// </summary>
    private void ApplyBaseOffset()
    {
        if (trapCollider is BoxCollider2D box)
            box.offset = baseColliderOffset;
        else if (trapCollider is CircleCollider2D circle)
            circle.offset = baseColliderOffset;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // ──── 冷却检查 ────
        if (cooldownTimer > 0f)
        {
            if (showDebugLog) Debug.Log($"[Trap] {name}: 冷却中（{cooldownTimer:F1}s 剩余），忽略触发。");
            return;
        }

        // ──── 一次性检查 ────
        if (hasTriggered)
        {
            if (showDebugLog) Debug.Log($"[Trap] {name}: 一次性陷阱已触发过，忽略。");
            return;
        }

        // ──── 标签检测 ────
        if (!other.CompareTag(targetTag))
        {
            if (showDebugLog) Debug.Log($"[Trap] {name}: 标签不匹配。目标标签={other.tag}，期望={targetTag}");
            return;
        }

        if (showDebugLog) Debug.Log($"[Trap] {name}: 检测到玩家触碰 → {other.name} (Tag: {other.tag})");

        // ──── 获取玩家组件 ────
        Player.PlayerStateMachine psm = other.GetComponent<Player.PlayerStateMachine>();
        IDamageable damageable = other.GetComponent<IDamageable>();

        if (psm == null)
        {
            Debug.LogWarning($"[Trap] {name}: 目标 {other.name} 缺少 PlayerStateMachine 组件！");
            return;
        }
        if (damageable == null)
        {
            Debug.LogWarning($"[Trap] {name}: 目标 {other.name} 缺少 IDamageable 组件（需要 PlayerHealthAdapter）！");
            return;
        }

        // ──── 造成伤害 ────
        damageable.TakeDamage(damage);

        if (showDebugLog) Debug.Log($"[Trap] {name}: 对 {other.name} 造成 {damage} 点伤害");

        // ──── 冷却与销毁逻辑 ────
        cooldownTimer = cooldown;

        if (triggerOnce)
        {
            hasTriggered = true;
            if (disableDelay > 0f)
                Invoke(nameof(DisableTrap), disableDelay);
            else
                DisableTrap();
        }
    }

    /// <summary>
    /// 禁用陷阱（一次性陷阱触发后调用）
    /// </summary>
    private void DisableTrap()
    {
        if (trapCollider != null)
            trapCollider.enabled = false;
    }

    /// <summary>
    /// 重置陷阱状态（外部调用，用于陷阱复用）
    /// </summary>
    public void ResetTrap()
    {
        cooldownTimer = 0f;
        hasTriggered = false;
        if (trapCollider != null)
            trapCollider.enabled = true;
    }

    /// <summary>
    /// 安全获取 Collider2D 的 isTrigger 属性（兼容 Tilemap 包未安装的情况）
    /// </summary>
    private static bool GetCollider2DIsTrigger(Component col)
    {
        if (col is Collider2D c2d)
            return c2d.isTrigger;
        return false;
    }

#if UNITY_EDITOR
    // 编辑器中绘制触发范围辅助线
    private void OnDrawGizmosSelected()
    {
        if (trapCollider == null)
            trapCollider = GetComponent<Collider2D>();
        if (trapCollider == null) return;

        // 未勾选 IsTrigger 时用红色警示
        Gizmos.color = trapCollider.isTrigger
            ? new Color(1f, 0.3f, 0f, 0.5f)
            : new Color(1f, 0f, 0f, 0.7f);

        Gizmos.matrix = transform.localToWorldMatrix;

        if (trapCollider is BoxCollider2D box)
            Gizmos.DrawWireCube(box.offset, box.size);
        else if (trapCollider is CircleCollider2D circle)
            Gizmos.DrawWireSphere(circle.offset, circle.radius);
    }
#endif
}
