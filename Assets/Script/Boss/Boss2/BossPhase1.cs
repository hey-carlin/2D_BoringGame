using UnityEngine;
using Game.Boss;

/// <summary>
/// Boss 一阶段（100%-50% HP）行为：
///   MeleeAttack / Casting→Spell / CreateShield / TeleportStart→TeleportEnd
/// </summary>
public class BossPhase1 : MonoBehaviour
{
    [Header("Timings")]
    public float telegraphTime = 0.35f;
    public float restTime = 1.2f;

    [Header("Attack Ranges")]
    public float meleeIdealDist = 2f;
    public float spellIdealDist = 6f;

    [Header("Attack Execute Times")]
    public float meleeExecuteTime = 0.8f;
    public float castingExecuteTime = 1.0f;
    public float spellExecuteTime = 1.0f;
    public float shieldAnimTime = 1.0f;
    public float teleportStartTime = 0.8f;
    public float teleportEndTime = 0.8f;

    [Header("Attack Weights")]
    public float meleeWeight = 3f;
    public float spellWeight = 2f;
    public float shieldWeight = 2f;
    public float teleportWeight = 1.5f;

    [Header("Shield")]
    public int shieldHP = 200;
    public float shieldMinInterval = 12f;   // 两次开盾最小间隔

    [Header("Teleport")]
    public float teleportMinDist = 5f;
    public float teleportMaxDist = 10f;

    [Header("Hitboxes")]
    public BossAttackHitbox meleeHitbox;
    public BossAttackHitbox spellHitbox;

    [Header("Teleport VFX (optional)")]
    public GameObject teleportStartVFX;
    public GameObject teleportEndVFX;

    // ═══════ 运行时 ═══════

    // 子步骤
    public enum SubStep { Idle, Telegraph, Execute, ChainWait, Rest }
    [HideInInspector] public SubStep currentStep = SubStep.Idle;
    [HideInInspector] public float stepTimer;
    [HideInInspector] public float idealDistance;     // 当前攻击的理想距离

    // 攻击选择
    private BossState currentAttack;
    private BossState lastAttack;
    private float shieldLastUsedTime = -999f;

    // 组件
    private BossStateMachine sm;
    private BossShield shield;
    private BossAnimationEvents animEvents;
    private Transform player;

    // 属性
    public BossState CurrentAttack => currentAttack;

    // ═══════ 初始化 ═══════

    private void Awake()
    {
        sm = GetComponent<BossStateMachine>();
        shield = GetComponent<BossShield>();
        animEvents = GetComponent<BossAnimationEvents>();
    }

    private void Start()
    {
        // 订阅动画事件——链条触发
        if (animEvents != null)
        {
            animEvents.OnCastingEnd += OnCastingFinished;
            animEvents.OnTeleportStartEnd += OnTeleportStartFinished;
        }
    }

    private void OnDestroy()
    {
        if (animEvents != null)
        {
            animEvents.OnCastingEnd -= OnCastingFinished;
            animEvents.OnTeleportStartEnd -= OnTeleportStartFinished;
        }
    }

    public void SetPlayer(Transform t) => player = t;

    /// <summary>进入此阶段时调用</summary>
    public void OnEnterPhase()
    {
        currentStep = SubStep.Idle;
        lastAttack = BossState.Idle;
        DeactivateAllHitboxes();
    }

    /// <summary>离开此阶段时调用</summary>
    public void OnExitPhase()
    {
        DeactivateAllHitboxes();
        sm.StopTelegraphFlash();
    }

    // ═══════ 主逻辑 —— 由 BossAI 每帧调用 ═══════

    public void UpdatePhase(float distToPlayer)
    {
        switch (currentStep)
        {
            case SubStep.Idle:
                DecideAndStart(distToPlayer);
                break;

            case SubStep.Telegraph:
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f) StartExecute();
                break;

            case SubStep.Execute:
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f) OnExecuteTimeout();
                break;

            case SubStep.ChainWait:
                // 等待动画事件回调，不做任何事
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f) OnChainTimeout();
                break;

            case SubStep.Rest:
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f) FinishRest();
                break;
        }
    }

    // ═══════ 决策 ═══════

    private void DecideAndStart(float dist)
    {
        currentAttack = PickAttack(dist);
        if (currentAttack == BossState.Idle) return;

        idealDistance = currentAttack switch
        {
            BossState.MeleeAttack => meleeIdealDist,
            _ => spellIdealDist
        };

        // 检查是否已在理想距离
        if (Mathf.Abs(dist - idealDistance) <= 1f)
        {
            sm.StopMoving();
            StartTelegraph();
        }
        // 否则 BossAI 会先移动到理想距离，再触发 StartTelegraph
    }

    /// <summary>BossAI 移动到理想距离后调用</summary>
    public void OnArrivedAtIdealDistance()
    {
        if (currentStep == SubStep.Idle && currentAttack != BossState.Idle)
        {
            sm.StopMoving();
            StartTelegraph();
        }
    }

    private BossState PickAttack(float dist)
    {
        bool canShield = shield != null
                      && !shield.IsBroken
                      && !shield.IsActive
                      && Time.time - shieldLastUsedTime >= shieldMinInterval;

        float totalWeight = 0f;

        // MeleeAttack: 总是可选
        if (lastAttack != BossState.MeleeAttack) totalWeight += meleeWeight;

        // Casting→Spell: 不是上一次攻击即可
        if (lastAttack != BossState.Casting) totalWeight += spellWeight;

        // CreateShield: 条件满足 + 不是上一次
        if (canShield && lastAttack != BossState.CreateShield) totalWeight += shieldWeight;

        // Teleport: 不是上一次
        if (lastAttack != BossState.TeleportStart) totalWeight += teleportWeight;

        if (totalWeight <= 0f)
        {
            // 全部被排除 → 允许重复，优先近战
            return dist <= meleeIdealDist + 1f ? BossState.MeleeAttack : BossState.Casting;
        }

        float roll = Random.Range(0f, totalWeight);
        float cursor = 0f;
        if (lastAttack != BossState.MeleeAttack)
        {
            cursor += meleeWeight;
            if (roll <= cursor) { lastAttack = BossState.MeleeAttack; return BossState.MeleeAttack; }
        }
        if (lastAttack != BossState.Casting)
        {
            cursor += spellWeight;
            if (roll <= cursor) { lastAttack = BossState.Casting; return BossState.Casting; }
        }
        if (canShield && lastAttack != BossState.CreateShield)
        {
            cursor += shieldWeight;
            if (roll <= cursor) { lastAttack = BossState.CreateShield; return BossState.CreateShield; }
        }
        if (lastAttack != BossState.TeleportStart)
        {
            cursor += teleportWeight;
            if (roll <= cursor) { lastAttack = BossState.TeleportStart; return BossState.TeleportStart; }
        }

        // fallback
        return dist <= meleeIdealDist + 1f ? BossState.MeleeAttack : BossState.Casting;
    }

    // ═══════ 前摇 → 执行 ═══════

    private void StartTelegraph()
    {
        sm.SetState(BossState.Telegraph);
        sm.StartTelegraphFlash();
        currentStep = SubStep.Telegraph;
        stepTimer = telegraphTime;
    }

    private void StartExecute()
    {
        sm.StopTelegraphFlash();
        currentStep = SubStep.Execute;

        // 确定实际执行的状态
        BossState executeState = currentAttack;
        float execTime = meleeExecuteTime;

        switch (currentAttack)
        {
            case BossState.MeleeAttack:
                execTime = meleeExecuteTime;
                sm.SetState(BossState.MeleeAttack);
                if (meleeHitbox != null) meleeHitbox.Activate(execTime);
                break;

            case BossState.Casting:
                // 链条第一步：施法引导
                execTime = castingExecuteTime;
                sm.SetState(BossState.Casting);
                // Spell 的碰撞体在第二步激活
                break;

            case BossState.CreateShield:
                execTime = shieldAnimTime;
                sm.SetState(BossState.CreateShield);
                // 实际开盾
                if (shield != null) shield.Activate();
                if (shield != null) shieldLastUsedTime = Time.time;
                break;

            case BossState.TeleportStart:
                // 链条第一步：传送消失
                execTime = teleportStartTime;
                sm.SetState(BossState.TeleportStart);
                if (teleportStartVFX != null)
                    Instantiate(teleportStartVFX, transform.position, Quaternion.identity);
                break;
        }

        stepTimer = execTime;
        sm.StopMoving();
    }

    // ═══════ 链条动画回调 ═══════

    /// <summary>Casting 动画播放完毕 → 触发 Spell</summary>
    private void OnCastingFinished()
    {
        if (currentStep != SubStep.Execute || currentAttack != BossState.Casting) return;

        currentStep = SubStep.ChainWait;
        sm.SetState(BossState.Spell);
        stepTimer = spellExecuteTime;
        if (spellHitbox != null) spellHitbox.Activate(spellExecuteTime);

        // Spell 结束后进 Rest
        if (animEvents != null)
        {
            animEvents.OnSpellEnd -= OnSpellFinished;
            animEvents.OnSpellEnd += OnSpellFinished;
        }
    }

    private void OnSpellFinished()
    {
        if (animEvents != null) animEvents.OnSpellEnd -= OnSpellFinished;
        if (currentStep == SubStep.ChainWait)
            StartRest();
    }

    /// <summary>TeleportStart 动画播放完毕 → 瞬移 → 触发 TeleportEnd</summary>
    private void OnTeleportStartFinished()
    {
        if (currentStep != SubStep.Execute || currentAttack != BossState.TeleportStart) return;

        // 执行瞬移
        float dir = Random.value > 0.5f ? 1f : -1f;
        float dist = Random.Range(teleportMinDist, teleportMaxDist);
        transform.position = (Vector2)transform.position + new Vector2(dir * dist, 0f);

        // 播放出现动画
        currentStep = SubStep.ChainWait;
        sm.SetState(BossState.TeleportEnd);
        stepTimer = teleportEndTime;
        if (teleportEndVFX != null)
            Instantiate(teleportEndVFX, transform.position, Quaternion.identity);

        // TeleportEnd 结束后进 Rest
        if (animEvents != null)
        {
            animEvents.OnTeleportEndEnd -= OnTeleportEndFinished;
            animEvents.OnTeleportEndEnd += OnTeleportEndFinished;
        }
    }

    private void OnTeleportEndFinished()
    {
        if (animEvents != null) animEvents.OnTeleportEndEnd -= OnTeleportEndFinished;
        if (currentStep == SubStep.ChainWait)
            StartRest();
    }

    // ═══════ 超时兜底 ═══════

    private void OnExecuteTimeout()
    {
        // 非链条攻击超时 → 直接休息
        if (currentAttack == BossState.MeleeAttack || currentAttack == BossState.CreateShield)
        {
            StartRest();
        }
        else
        {
            // 链条攻击超时 → 模拟动画事件
            if (currentAttack == BossState.Casting)
                OnCastingFinished();
            else if (currentAttack == BossState.TeleportStart)
                OnTeleportStartFinished();
        }
    }

    private void OnChainTimeout()
    {
        StartRest();
    }

    // ═══════ 休息 → 下一轮 ═══════

    private void StartRest()
    {
        DeactivateAllHitboxes();
        sm.StopMoving();
        sm.SetState(BossState.Rest);
        currentStep = SubStep.Rest;
        stepTimer = restTime;
    }

    private void FinishRest()
    {
        sm.SetState(BossState.Idle);
        DeactivateAllHitboxes();
        currentStep = SubStep.Idle;
        currentAttack = BossState.Idle;
        idealDistance = 0f;
    }

    // ═══════ 碰撞体 ═══════

    private void DeactivateAllHitboxes()
    {
        if (meleeHitbox != null) meleeHitbox.Deactivate();
        if (spellHitbox != null) spellHitbox.Deactivate();
    }

    // ═══════ 公开 ═══════

    public bool CanTakeAction() =>
        currentStep != SubStep.Execute
        && currentStep != SubStep.ChainWait
        && sm.CurrentState != BossState.Hurt
        && sm.CurrentState != BossState.ShieldHit;

    public bool IsApproaching() =>
        currentStep == SubStep.Idle
        && currentAttack != BossState.Idle
        && player != null
        && Mathf.Abs(Vector2.Distance(transform.position, player.position) - idealDistance) > 1f;
}
