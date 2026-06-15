using UnityEngine;
using Game.Boss;

/// <summary>
/// Boss2 — 恶魔城风格：
///   浮空追击 + 前摇 + 攻击 + 休息窗口。
///
///   P1: MeleeAttack / Casting→Spell / CreateShield(护盾HP) / Teleport
///   P2 (≤50%): + Explode / Summon / CastSpell，更快节奏
/// </summary>
public class BossAI : MonoBehaviour
{
    [Header("索敌")]
    public float chaseRange = 15f;
    public float idealDistance = 5f;

    [Header("攻击 Timing")]
    public float telegraphTime = 0.35f;
    public float restTime = 1f;             // 攻击后休息窗口（玩家输出时机）
    public float attackCooldown = 1.2f;     // P1 攻击冷却

    [Header("P1 攻击权重")]
    public float meleeWeight = 3f;
    public float spellWeight = 2f;
    public float teleportWeight = 1.5f;

    [Header("P1 攻击执行时长")]
    public float meleeExecTime = 0.8f;
    public float castingExecTime = 1f;
    public float spellExecTime = 1f;
    public float shieldExecTime = 1f;
    public float teleStartTime = 0.8f;
    public float teleEndTime = 0.8f;

    [Header("护盾")]
    public int shieldHP = 150;
    public float shieldReactivateDelay = 10f; // 护盾破碎后多久可重开
    public bool shieldBroken;               // [只读] 护盾是否已碎

    [Header("传送")]
    public float teleportMinDist = 5f;
    public float teleportMaxDist = 10f;

    [Header("P2 — 解锁攻击")]
    public float p2AttackCooldown = 0.8f;
    public float p2TelegraphTime = 0.25f;
    public float explodeExecTime = 1.5f;
    public float summonExecTime = 1.5f;
    public float castSpellExecTime = 1.2f;

    [Header("生命值")]
    public int maxHealth = 600;

    [Header("碰触体")]
    public BossAttackHitbox meleeHitbox;
    public BossAttackHitbox spellHitbox;
    public BossAttackHitbox explodeHitbox;

    [Header("召唤")]
    public GameObject summonPrefab;
    public Transform[] summonSpawnPoints;

    // ═══ 内部 ═══

    private enum SubStep { Idle, Telegraph, Execute, ChainWait, Rest }
    private SubStep step = SubStep.Idle;
    private float stepTimer;

    private BossStateMachine sm;
    private Transform player;
    private Animator anim;

    private int currentHealth;
    private int shieldCurrentHP;
    private bool shieldActive;
    private float shieldReactivateTimer;
    private bool isDead;
    private bool isPhase2;
    private float lastAttackTime = -999f;

    private BossState currentAttack;
    private BossState lastAttack;
    private float idealDist;
    private float invincibleTimer;

    // ═══ 初始化 ═══

    private void Start()
    {
        sm = GetComponent<BossStateMachine>();
        anim = GetComponent<Animator>();

        var obj = GameObject.FindGameObjectWithTag("Player");
        if (obj == null) { Debug.LogError("[Boss2] 未找到 Player！"); enabled = false; return; }
        player = obj.transform;

        currentHealth = maxHealth;
        IgnorePlayerCollision();
    }

    private void IgnorePlayerCollision()
    {
        if (player == null) return;
        foreach (var pc in player.GetComponents<Collider2D>())
        {
            if (pc == null || pc.isTrigger) continue;
            foreach (var bc in GetComponents<Collider2D>())
            {
                if (bc == null || bc.isTrigger) continue;
                Physics2D.IgnoreCollision(pc, bc, true);
            }
        }
    }

    // ═══ 主循环 ═══

    private void Update()
    {
        if (isDead || player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        Vector2 dir = (player.position - transform.position).normalized;

        if (Mathf.Abs(dir.x) > 0.05f) sm.FaceDirection(dir.x);
        Debug.DrawLine(transform.position, player.position, dist <= chaseRange ? Color.yellow : Color.clear);

        if (invincibleTimer > 0f) invincibleTimer -= Time.deltaTime;
        if (shieldReactivateTimer > 0f) shieldReactivateTimer -= Time.deltaTime;
        if (currentHealth <= 0) { Die(); return; }

        // 硬直中
        if (sm.CurrentState == BossState.Hurt || sm.CurrentState == BossState.ShieldHit)
            return;

        // 子步骤状态机
        switch (step)
        {
            case SubStep.Idle:
                if (dist > chaseRange) { sm.StopMoving(); return; }
                DecideAttack(dist);
                break;

            case SubStep.Telegraph:
                stepTimer -= Time.deltaTime;
                sm.StopMoving();
                if (stepTimer <= 0f) StartExecute();
                return;

            case SubStep.Execute:
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f) OnExecTimeout();
                return;

            case SubStep.ChainWait:
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f) StartRest();
                return;

            case SubStep.Rest:
                stepTimer -= Time.deltaTime;
                sm.StopMoving();
                if (stepTimer <= 0f) FinishRest();
                return;
        }

        // 非攻击时浮空追踪
        if (step == SubStep.Idle)
            sm.HoverToward(player.position, idealDist);
    }

    // ═══ 决策 ═══

    private void DecideAttack(float dist)
    {
        // 已选好攻击，检查是否到位
        if (currentAttack != BossState.Idle)
        {
            if (Mathf.Abs(dist - idealDist) <= 1f)
            {
                sm.StopMoving();
                StartTelegraph();
            }
            return;
        }

        // 选新攻击
        currentAttack = PickAttack();
        if (currentAttack == BossState.Idle) return;

        idealDist = currentAttack == BossState.MeleeAttack ? 2.5f : idealDistance;
        lastAttackTime = Time.time;

        if (Mathf.Abs(dist - idealDist) <= 1f)
        {
            sm.StopMoving();
            StartTelegraph();
        }
    }

    private BossState PickAttack()
    {
        // 可开盾?
        bool canShield = !shieldBroken && !shieldActive && shieldReactivateTimer <= 0f;

        float total = 0f;
        if (lastAttack != BossState.MeleeAttack) total += meleeWeight;
        if (lastAttack != BossState.Casting) total += spellWeight;
        if (lastAttack != BossState.TeleportStart) total += teleportWeight;
        if (canShield && lastAttack != BossState.CreateShield) total += 2f;

        if (!isPhase2 && total <= 0f) return BossState.MeleeAttack;

        // P2 攻击池
        if (isPhase2)
        {
            if (lastAttack != BossState.Explode) total += 2f;
            if (lastAttack != BossState.Summon) total += 1.5f;
            if (lastAttack != BossState.CastSpell) total += 1.5f;
        }

        float roll = Random.Range(0f, total);
        float c = 0f;

        if (TryAdd(ref c, meleeWeight, BossState.MeleeAttack, roll)) return BossState.MeleeAttack;
        if (TryAdd(ref c, spellWeight, BossState.Casting, roll)) return BossState.Casting;
        if (canShield && TryAdd(ref c, 2f, BossState.CreateShield, roll)) return BossState.CreateShield;
        if (TryAdd(ref c, teleportWeight, BossState.TeleportStart, roll)) return BossState.TeleportStart;
        if (isPhase2 && TryAdd(ref c, 2f, BossState.Explode, roll)) return BossState.Explode;
        if (isPhase2 && TryAdd(ref c, 1.5f, BossState.Summon, roll)) return BossState.Summon;
        if (isPhase2 && TryAdd(ref c, 1.5f, BossState.CastSpell, roll)) return BossState.CastSpell;

        return BossState.MeleeAttack;
    }

    private bool TryAdd(ref float cursor, float weight, BossState state, float roll)
    {
        if (state == lastAttack) return false;
        cursor += weight;
        if (roll <= cursor) { lastAttack = state; return true; }
        return false;
    }

    // ═══ 前摇 → 执行 ═══

    private void StartTelegraph()
    {
        float t = isPhase2 ? p2TelegraphTime : telegraphTime;
        sm.StartTelegraphFlash(); // 前摇：保持 Idle，只闪烁
        step = SubStep.Telegraph;
        stepTimer = t;
        sm.StopMoving();
    }

    private void StartExecute()
    {
        sm.StopTelegraphFlash();
        step = SubStep.Execute;
        sm.SetState(currentAttack);

        switch (currentAttack)
        {
            case BossState.MeleeAttack:
                stepTimer = meleeExecTime;
                if (meleeHitbox != null) meleeHitbox.Activate(meleeExecTime);
                break;

            case BossState.Casting:
                stepTimer = castingExecTime;
                break;

            case BossState.CreateShield:
                stepTimer = shieldExecTime;
                ActivateShield();
                shieldReactivateTimer = shieldReactivateDelay;
                break;

            case BossState.TeleportStart:
                stepTimer = teleStartTime;
                break;

            // P2
            case BossState.Explode:
                stepTimer = explodeExecTime;
                if (explodeHitbox != null) explodeHitbox.Activate(explodeExecTime);
                break;

            case BossState.Summon:
                stepTimer = summonExecTime;
                DoSummon();
                break;

            case BossState.CastSpell:
                stepTimer = castSpellExecTime;
                break;
        }
        sm.StopMoving();
    }

    private void OnExecTimeout()
    {
        // 非链条攻击 → 直接休息
        if (currentAttack != BossState.Casting && currentAttack != BossState.TeleportStart)
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

    // ═══ 链条：Casting → Spell ═══

    public void OnCastingFinished()
    {
        if (step != SubStep.Execute || currentAttack != BossState.Casting) return;

        step = SubStep.ChainWait;
        sm.SetState(BossState.Spell);
        stepTimer = spellExecTime;
        if (spellHitbox != null) spellHitbox.Activate(spellExecTime);
    }

    public void OnSpellFinished()
    {
        if (step == SubStep.ChainWait)
            StartRest();
    }

    // ═══ 链条：TeleportStart → TeleportEnd ═══

    public void OnTeleportStartFinished()
    {
        if (step != SubStep.Execute || currentAttack != BossState.TeleportStart) return;

        float dir = Random.value > 0.5f ? 1f : -1f;
        float d = Random.Range(teleportMinDist, teleportMaxDist);
        transform.position = (Vector2)transform.position + new Vector2(dir * d, 0f);
        sm.UpdateHoverBaseY();

        step = SubStep.ChainWait;
        sm.SetState(BossState.TeleportEnd);
        stepTimer = teleEndTime;
    }

    public void OnTeleportEndFinished()
    {
        if (step == SubStep.ChainWait)
            StartRest();
    }

    // ═══ 护盾 ═══

    private void ActivateShield()
    {
        shieldActive = true;
        shieldCurrentHP = shieldHP;
        shieldBroken = false;
    }

    /// <summary>护盾吸收伤害，返回穿透伤害</summary>
    public int AbsorbDamage(int damage)
    {
        if (!shieldActive) return damage;

        shieldCurrentHP -= damage;

        if (shieldCurrentHP <= 0)
        {
            int overflow = -shieldCurrentHP;
            shieldCurrentHP = 0;
            shieldActive = false;
            shieldBroken = true;
            shieldReactivateTimer = shieldReactivateDelay;
            return overflow;
        }

        sm.ShieldHit();
        return 0;
    }

    public bool IsShieldActive() => shieldActive;

    // ═══ 召唤 ═══

    private void DoSummon()
    {
        if (summonPrefab == null) return;
        if (summonSpawnPoints != null && summonSpawnPoints.Length > 0)
        {
            foreach (var sp in summonSpawnPoints)
                if (sp != null) Instantiate(summonPrefab, sp.position, Quaternion.identity);
        }
        else
        {
            for (int i = 0; i < 2; i++)
            {
                Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle * 3f;
                Instantiate(summonPrefab, pos, Quaternion.identity);
            }
        }
    }

    // ═══ 休息 → 下一轮 ═══

    private void StartRest()
    {
        DeactivateAllHitboxes();
        sm.StopMoving();
        sm.SetState(BossState.Idle);
        step = SubStep.Rest;
        stepTimer = restTime;
    }

    private void FinishRest()
    {
        step = SubStep.Idle;
        currentAttack = BossState.Idle;
        idealDist = 0f;
    }

    private void DeactivateAllHitboxes()
    {
        if (meleeHitbox != null) meleeHitbox.Deactivate();
        if (spellHitbox != null) spellHitbox.Deactivate();
        if (explodeHitbox != null) explodeHitbox.Deactivate();
    }

    // ═══ 动画事件 ═══

    public void OnAttackEnd()
    {
        // 通用攻击结束 — 仅非链条攻击使用
        if (step == SubStep.Execute
            && currentAttack != BossState.Casting
            && currentAttack != BossState.TeleportStart)
            StartRest();
    }

    public void OnHitEnd()
    {
        if (sm.CurrentState == BossState.Hurt)
            sm.SetState(BossState.Idle);
    }

    public void OnSkillEnd() => OnAttackEnd();

    // ═══ 受伤 ═══

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        if (invincibleTimer > 0f) return;

        // 护盾吸收
        if (shieldActive)
        {
            damage = AbsorbDamage(damage);
            if (damage <= 0) return;
        }

        currentHealth -= damage;
        invincibleTimer = 0.3f;

        if (currentHealth <= 0) { Die(); return; }

        // 阶段切换
        if (!isPhase2 && currentHealth <= maxHealth * 0.5f)
        {
            isPhase2 = true;
            sm.SetPhase(BossPhase.Phase2);
        }

        sm.Hit();
    }

    // ═══ 死亡 ═══

    private void Die()
    {
        isDead = true;
        DeactivateAllHitboxes();
        sm.Die();
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        enabled = false;
    }

    // ═══ Gizmos ═══

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 p = transform.position;
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(p, chaseRange);
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(p, idealDistance);
    }
#endif
}
