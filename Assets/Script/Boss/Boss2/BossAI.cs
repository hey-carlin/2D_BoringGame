using UnityEngine;
using Game.Boss;

/// <summary>
/// Boss 自主 AI：检测玩家 → 根据距离 + 阶段 + 冷却 → 选择行为。
/// 挂在 Boss 对象上，依赖 BossStateMachine / BossHealth / BossAnimationEvents。
/// </summary>
public class BossAI : MonoBehaviour
{
    [Header("Player Detection")]
    [Tooltip("超过此距离 Boss 不会行动（脱战）")]
    public float loseAggroRange = 20f;
    [Tooltip("玩家进入此范围后 Boss 开始行动")]
    public float aggroRange = 12f;
    [Tooltip("近战攻击范围")]
    public float meleeRange = 3f;
    [Tooltip("施法/远程技能范围")]
    public float castRange = 8f;

    [Header("Phase 1 — Timings")]
    public float p1ActionInterval = 1.5f;           // 每次行动后的最小间隔
    public float p1MeleeCooldown = 2f;
    public float p1CastCooldown = 4f;
    public float p1ShieldCooldown = 10f;
    public float p1SpellCooldown = 5f;
    [Tooltip("被攻击 N 次后优先开盾")]
    public int p1HitsToShield = 3;

    [Header("Phase 2 — Timings")]
    public float p2ActionInterval = 1f;
    public float p2ExplodeCooldown = 6f;
    public float p2CastCooldown = 3f;
    public float p2SummonCooldown = 12f;
    public float p2TeleportCooldown = 8f;
    public float p2SpellCooldown = 4f;

    [Header("Teleport")]
    public float teleportMinDist = 5f;
    public float teleportMaxDist = 10f;

    [Header("Summon")]
    public GameObject summonPrefab;                   // 召唤物预制体
    public Transform[] summonSpawnPoints;             // 召唤出生点

    [Header("Shield")]
    public float shieldDuration = 4f;                 // 护盾持续时间
    public float shieldDamageReduction = 0.5f;        // 护盾减伤比例

    [Header("Debug")]
    public bool showDebugGizmos = true;

    // ── 组件引用 ──
    private BossStateMachine sm;
    private BossHealth health;
    private BossAnimationEvents animEvents;
    private Transform player;

    // ── 冷却计时器 ──
    private float actionIntervalTimer;
    private float meleeTimer;
    private float castTimer;
    private float shieldTimer;
    private float spellTimer;
    private float explodeTimer;
    private float summonTimer;
    private float teleportTimer;
    private float actionDurationTimer;      // 当前动作已持续多久
    private float actionMaxDuration;        // 当前动作最大时长（超时强制结束）

    // ── 状态标记 ──
    private bool isActing;                  // 正在执行动作中
    private BossState currentAction;        // 当前执行的动作类型
    private int hitCounter;                 // P1 被击中计数（用于触发开盾）
    private bool shieldActive;              // 护盾是否激活
    private float shieldTimer_remaining;    // 护盾剩余时间

    // ── 属性 ──
    private BossPhase CurrentPhase => health != null ? health.CurrentPhase : BossPhase.Phase1;
    private float DistToPlayer => player != null
        ? Vector2.Distance(transform.position, player.position)
        : float.MaxValue;

    // ═══════════════════════════════════════════════════════
    // 初始化
    // ═══════════════════════════════════════════════════════

    private void Awake()
    {
        sm = GetComponent<BossStateMachine>();
        health = GetComponent<BossHealth>();
        animEvents = GetComponent<BossAnimationEvents>();
    }

    private void Start()
    {
        FindPlayer();

        // 订阅动画事件
        if (animEvents != null)
        {
            animEvents.OnMeleeAttackEnd += OnMeleeAttackEnd;
            animEvents.OnCastingEnd += OnActionEnd;
            animEvents.OnCreateShieldEnd += OnActionEnd;
            animEvents.OnExplodeEnd += OnActionEnd;
            animEvents.OnSummonEnd += OnActionEnd;
            animEvents.OnSpellEnd += OnActionEnd;
            animEvents.OnCastSpellEnd += OnActionEnd;
            animEvents.OnTeleportationEnd += OnActionEnd;
            animEvents.OnHurtEnd += OnActionEnd;
            animEvents.OnShieldHitEnd += OnActionEnd;
            animEvents.OnAnySkillEnd += OnActionEnd;    // 兼容通用回调
        }

        // 订阅受伤事件（用于计数开盾）
        if (health != null)
            health.OnDamaged += OnBossDamaged;
    }

    /// <summary>通过 Tag 查找玩家（找不到会持续重试）</summary>
    private void FindPlayer()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    private void OnDestroy()
    {
        if (animEvents != null)
        {
            animEvents.OnMeleeAttackEnd -= OnMeleeAttackEnd;
            animEvents.OnCastingEnd -= OnActionEnd;
            animEvents.OnCreateShieldEnd -= OnActionEnd;
            animEvents.OnExplodeEnd -= OnActionEnd;
            animEvents.OnSummonEnd -= OnActionEnd;
            animEvents.OnSpellEnd -= OnActionEnd;
            animEvents.OnCastSpellEnd -= OnActionEnd;
            animEvents.OnTeleportationEnd -= OnActionEnd;
            animEvents.OnHurtEnd -= OnActionEnd;
            animEvents.OnShieldHitEnd -= OnActionEnd;
            animEvents.OnAnySkillEnd -= OnActionEnd;
        }
        if (health != null)
            health.OnDamaged -= OnBossDamaged;
    }

    // ═══════════════════════════════════════════════════════
    // 主循环
    // ═══════════════════════════════════════════════════════

    private void Update()
    {
        if (sm == null || health == null) return;

        // 死亡不动作
        if (sm.CurrentState == BossState.Death) return;

        // 持续尝试查找玩家（防止玩家后生成）
        if (player == null)
        {
            FindPlayer();
            sm.SetState(BossState.Idle);
            return;
        }

        TickCooldowns();
        TickShield();

        // 正在执行动作中 — 等待完成
        if (isActing)
        {
            actionDurationTimer += Time.deltaTime;
            // 超时强制结束
            if (actionDurationTimer >= actionMaxDuration)
                ForceEndAction();
            return;
        }

        // 受伤 / 硬直中 — 不打断
        if (sm.CurrentState == BossState.Hurt || sm.CurrentState == BossState.ShieldHit)
            return;

        float dist = DistToPlayer;

        // 超出脱战范围 → 回到待机
        if (dist > loseAggroRange)
        {
            sm.SetState(BossState.Idle);
            return;
        }

        // 超出索敌范围但不在脱战 → 追击玩家
        if (dist > aggroRange)
        {
            sm.SetState(BossState.Move);
            return;
        }

        // 动作间隔
        actionIntervalTimer += Time.deltaTime;
        float interval = CurrentPhase == BossPhase.Phase1 ? p1ActionInterval : p2ActionInterval;
        if (actionIntervalTimer < interval) return;

        // 根据阶段决策
        if (CurrentPhase == BossPhase.Phase1)
            Phase1Decision(dist);
        else
            Phase2Decision(dist);
    }

    private void FixedUpdate()
    {
        // 移动状态在 FixedUpdate 中驱动物理
        if (sm.CurrentState == BossState.Move && player != null)
        {
            Vector2 dir = player.position - transform.position;
            sm.MoveToward(new Vector2(Mathf.Sign(dir.x), 0f));
            sm.FaceDirection(dir.x);
        }
    }

    // ═══════════════════════════════════════════════════════
    // Phase 1 决策
    // ═══════════════════════════════════════════════════════

    private void Phase1Decision(float dist)
    {
        // 近战范围 → 优先近战，有概率开盾
        if (dist <= meleeRange)
        {
            if (hitCounter >= p1HitsToShield && shieldTimer <= 0f)
            {
                ExecuteAction(BossState.CreateShield, 1.5f, ref shieldTimer, p1ShieldCooldown);
                ActivateShield();
                hitCounter = 0;
                return;
            }

            if (meleeTimer <= 0f)
            {
                float r = Random.value;
                if (r < 0.65f)
                    ExecuteAction(BossState.MeleeAttack, 1.2f, ref meleeTimer, p1MeleeCooldown);
                else if (r < 0.85f && spellTimer <= 0f)
                    ExecuteAction(BossState.Spell, 1.8f, ref spellTimer, p1SpellCooldown);
                else
                    ExecuteAction(BossState.Casting, 1.6f, ref castTimer, p1CastCooldown);
                return;
            }
        }

        // 施法范围 → 法术 / 接近
        if (dist <= castRange)
        {
            if (castTimer <= 0f && Random.value < 0.55f)
            {
                ExecuteAction(BossState.Casting, 1.6f, ref castTimer, p1CastCooldown);
                return;
            }
            if (spellTimer <= 0f && Random.value < 0.5f)
            {
                ExecuteAction(BossState.Spell, 1.8f, ref spellTimer, p1SpellCooldown);
                return;
            }
            if (Random.value < 0.4f)
            {
                ExecuteAction(BossState.CastSpell, 2f, ref castTimer, p1CastCooldown);
                return;
            }
            // 否则靠近玩家
            sm.SetState(BossState.Move);
            actionIntervalTimer = 0f;
            return;
        }

        // 超出施法但在索敌范围内 → 靠近玩家
        sm.SetState(BossState.Move);
        actionIntervalTimer = 0f;
    }

    // ═══════════════════════════════════════════════════════
    // Phase 2 决策
    // ═══════════════════════════════════════════════════════

    private void Phase2Decision(float dist)
    {
        // 近战范围 → 爆炸 / 传送 / 施法
        if (dist <= meleeRange)
        {
            if (explodeTimer <= 0f)
            {
                ExecuteAction(BossState.Explode, 2f, ref explodeTimer, p2ExplodeCooldown);
                return;
            }
            if (teleportTimer <= 0f)
            {
                ExecuteAction(BossState.Teleportation, 1f, ref teleportTimer, p2TeleportCooldown);
                // Teleport 动作开始时执行瞬移
                PerformTeleport();
                return;
            }
            if (castTimer <= 0f)
            {
                ExecuteAction(BossState.Casting, 1.6f, ref castTimer, p2CastCooldown);
                return;
            }
            // 没技能可用就后撤（朝远离玩家方向移动）
            sm.SetState(BossState.Idle);
            actionIntervalTimer = 0f;
            return;
        }

        // 施法范围 → 法术 / 召唤 / 传送接近
        if (dist <= castRange)
        {
            if (castTimer <= 0f && Random.value < 0.4f)
            {
                ExecuteAction(BossState.Casting, 1.6f, ref castTimer, p2CastCooldown);
                return;
            }
            if (spellTimer <= 0f && Random.value < 0.35f)
            {
                ExecuteAction(BossState.Spell, 1.8f, ref spellTimer, p2SpellCooldown);
                return;
            }
            if (summonTimer <= 0f && Random.value < 0.3f)
            {
                ExecuteAction(BossState.Summon, 2f, ref summonTimer, p2SummonCooldown);
                PerformSummon();
                return;
            }
            if (teleportTimer <= 0f && Random.value < 0.25f)
            {
                ExecuteAction(BossState.Teleportation, 1f, ref teleportTimer, p2TeleportCooldown);
                PerformTeleport();
                return;
            }

            // 靠近玩家
            sm.SetState(BossState.Move);
            actionIntervalTimer = 0f;
            return;
        }

        // 远处 → 传送接近或释放远程
        if (dist <= aggroRange)
        {
            if (teleportTimer <= 0f)
            {
                ExecuteAction(BossState.Teleportation, 1f, ref teleportTimer, p2TeleportCooldown);
                PerformTeleportNearPlayer();
                return;
            }
            if (castTimer <= 0f)
            {
                ExecuteAction(BossState.CastSpell, 2f, ref castTimer, p2CastCooldown);
                return;
            }
            sm.SetState(BossState.Move);
            actionIntervalTimer = 0f;
        }
    }

    // ═══════════════════════════════════════════════════════
    // 动作执行
    // ═══════════════════════════════════════════════════════

    /// <summary>执行一个动作状态</summary>
    private void ExecuteAction(BossState state, float maxDuration,
                               ref float cooldownTimer, float cooldown)
    {
        sm.SetState(state);
        sm.StopMoving();
        isActing = true;
        currentAction = state;
        actionDurationTimer = 0f;
        actionMaxDuration = maxDuration;
        cooldownTimer = cooldown;
        actionIntervalTimer = 0f;
    }

    /// <summary>动作完成回调（由动画事件触发）</summary>
    private void OnActionEnd()
    {
        isActing = false;
        currentAction = BossState.Idle;
        actionDurationTimer = 0f;
        sm.SetState(BossState.Idle);
        actionIntervalTimer = 0f;
    }

    /// <summary>近战结束 — 追踪是否有 combo</summary>
    private void OnMeleeAttackEnd()
    {
        OnActionEnd();
    }

    /// <summary>超时强制结束动作</summary>
    private void ForceEndAction()
    {
        Debug.LogWarning($"[BossAI] Action {currentAction} timed out ({actionMaxDuration}s), forcing end.");
        OnActionEnd();
    }

    // ═══════════════════════════════════════════════════════
    // 特殊技能实现
    // ═══════════════════════════════════════════════════════

    /// <summary>传送：随机位置偏移</summary>
    private void PerformTeleport()
    {
        float dir = Random.value > 0.5f ? 1f : -1f;
        float dist = Random.Range(teleportMinDist, teleportMaxDist);
        Vector2 offset = new Vector2(dir * dist, 0f);
        transform.position = (Vector2)transform.position + offset;
    }

    /// <summary>传送：靠近玩家</summary>
    private void PerformTeleportNearPlayer()
    {
        if (player == null) return;
        float dir = player.position.x > transform.position.x ? 1f : -1f;
        float dist = Random.Range(meleeRange + 1f, meleeRange + 3f);
        Vector2 target = (Vector2)player.position + new Vector2(-dir * dist, 0f);
        transform.position = target;
    }

    /// <summary>召唤</summary>
    private void PerformSummon()
    {
        if (summonPrefab == null) return;

        // 优先使用配置的出生点
        if (summonSpawnPoints != null && summonSpawnPoints.Length > 0)
        {
            foreach (var sp in summonSpawnPoints)
            {
                if (sp != null)
                    Instantiate(summonPrefab, sp.position, Quaternion.identity);
            }
        }
        else
        {
            // 在 Boss 周围随机生成 2 个召唤物
            for (int i = 0; i < 2; i++)
            {
                Vector2 offset = Random.insideUnitCircle * 2f + Vector2.up * 1f;
                Vector2 pos = (Vector2)transform.position + offset;
                Instantiate(summonPrefab, pos, Quaternion.identity);
            }
        }
    }

    /// <summary>激活护盾</summary>
    private void ActivateShield()
    {
        shieldActive = true;
        shieldTimer_remaining = shieldDuration;
    }

    /// <summary>护盾倒计时</summary>
    private void TickShield()
    {
        if (!shieldActive) return;
        shieldTimer_remaining -= Time.deltaTime;
        if (shieldTimer_remaining <= 0f)
        {
            shieldActive = false;
            shieldTimer_remaining = 0f;
        }
    }

    // ═══════════════════════════════════════════════════════
    // 事件响应
    // ═══════════════════════════════════════════════════════

    private void OnBossDamaged(int _)
    {
        hitCounter++;
    }

    // ═══════════════════════════════════════════════════════
    // 工具
    // ═══════════════════════════════════════════════════════

    /// <summary>护盾是否激活（BossHealth 可查询以减免伤害）</summary>
    public bool IsShieldActive() => shieldActive;
    public float GetShieldReduction() => shieldDamageReduction;

    /// <summary>是否有玩家目标</summary>
    public bool HasPlayer() => player != null;

    /// <summary>获取到玩家的距离</summary>
    public float GetPlayerDistance() => DistToPlayer;

    /// <summary>玩家是否在近战范围内</summary>
    public bool IsPlayerInMeleeRange() => DistToPlayer <= meleeRange;

    /// <summary>玩家是否在索敌范围内</summary>
    public bool IsPlayerInAggroRange() => DistToPlayer <= aggroRange;

    private void TickCooldowns()
    {
        float dt = Time.deltaTime;
        if (meleeTimer > 0f) meleeTimer -= dt;
        if (castTimer > 0f) castTimer -= dt;
        if (shieldTimer > 0f) shieldTimer -= dt;
        if (spellTimer > 0f) spellTimer -= dt;
        if (explodeTimer > 0f) explodeTimer -= dt;
        if (summonTimer > 0f) summonTimer -= dt;
        if (teleportTimer > 0f) teleportTimer -= dt;
    }

    // ═══════════════════════════════════════════════════════
    // Editor Gizmos
    // ═══════════════════════════════════════════════════════

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        Vector3 p = transform.position;

        // 索敌范围
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(p, aggroRange);

        // 脱战范围
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f);
        Gizmos.DrawWireSphere(p, loseAggroRange);

        // 施法范围
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(p, castRange);

        // 近战范围
        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        Gizmos.DrawWireSphere(p, meleeRange);

        // 如果玩家存在，画连线
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(p, playerObj.transform.position);
        }
    }
#endif
}
