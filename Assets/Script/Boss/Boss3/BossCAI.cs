using UnityEngine;
using Enemy; // IDamageable

/// <summary>
/// Boss C（空洞骑士风格）：
///   追击 + 前摇 + 三种攻击 + 踉跄 + 二阶段。
/// </summary>
public class BossCAI : MonoBehaviour, IDamageable
{
    // ──── 公开事件（供音乐系统订阅）────
    public System.Action OnBossDefeated;
    public System.Action OnBossPhaseTransition;
    [Header("移动")]
    public float moveSpeed = 3f;
    public float wanderSpeed = 1.5f;        // 游走速度
    public float chaseRange = 8f;
    public float wanderMinTime = 1f;        // 游走单次最短时间
    public float wanderMaxTime = 3f;        // 游走单次最长时间
    public Color chaseLineColor = Color.yellow;

    [Header("攻击")]
    public float attackCooldown = 0.8f;     // P1 攻击冷却
    public float attackRange = 2f;
    public float telegraphTime = 0.35f;     // 攻击前摇（停顿 + 闪烁/举刀）
    public Transform attackPoint;
    public float attackRadius = 1f;
    public int attackDamage = 20;
    [Tooltip("三种攻击各自的伤害倍率")]
    public float[] attackDamageMultipliers = { 1f, 1.5f, 0.8f };
    [Tooltip("三种攻击各自的判定半径倍率")]
    public float[] attackRadiusMultipliers = { 1f, 1f, 1.5f };

    [Header("攻击特殊参数")]
    public float dashForce = 15f;           // Attack1 冲锋力度
    public float slamRadius = 2f;           // Attack2 砸地 AOE 半径
    public int slamDamageBonus = 5;         // Attack2 额外伤害

    [Header("踉跄（Stagger）")]
    public int staggerHitsRequired = 5;     // 被击中 N 次后踉跄
    public float staggerDuration = 2f;      // 踉跄持续时间
    public float staggerCooldown = 8f;      // 两次踉跄最小间隔
    [Tooltip("踉跄期间受到伤害倍率")]
    public float staggerDamageMultiplier = 1.5f;

    [Header("二阶段（≤50% HP）")]
    public float p2MoveSpeed = 4.5f;
    public float p2AttackCooldown = 0.5f;
    public float p2TelegraphTime = 0.25f;
    [Tooltip("二阶段解锁额外攻击（Attack3）")]
    public bool p2UnlockAttack3 = true;

    [Header("生命值")]
    public int maxHealth = 100;
    public float hurtInvincibleTime = 0.5f;

    // ═══════ 内部状态 ═══════

    private enum AIState { Idle, Telegraphing, Acting, Staggered }
    private AIState aiState = AIState.Idle;

    private Transform player;
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;

    private int currentHealth;
    private bool isDead;

    private float lastAttackTime = -999f;
    private float invincibleTimer;
    private float wanderTimer;              // 游走方向切换计时
    private float wanderDir = 1f;           // 当前游走方向

    private bool canAct = true;
    private int currentAttackIdx;
    private float lockTimer;
    private float attackPointDefaultX;

    // 前摇
    private float telegraphTimer;

    // 踉跄
    private int hitCounter;
    private float staggerTimer;
    private float lastStaggerTime = -999f;
    private bool isStaggered;

    // 二阶段
    private bool isPhase2;
    private float currentMoveSpeed => isPhase2 ? p2MoveSpeed : moveSpeed;
    private float currentAttackCooldown => isPhase2 ? p2AttackCooldown : attackCooldown;
    private float currentTelegraphTime => isPhase2 ? p2TelegraphTime : telegraphTime;

    // ═══════════ 初始化 ═══════════

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("[BossC] 未找到 Tag='Player' 的对象！");
            enabled = false;
            return;
        }
        player = playerObj.transform;

        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        // Boss3 是地面单位，保留重力
        currentHealth = maxHealth;
        if (attackPoint != null)
            attackPointDefaultX = Mathf.Abs(attackPoint.localPosition.x);

        IgnorePlayerCollision();
    }

    private void IgnorePlayerCollision()
    {
        if (player == null) return;
        foreach (var pc in player.GetComponents<Collider2D>())
        {
            if (pc == null) continue;
            foreach (var bc in GetComponents<Collider2D>())
            {
                if (bc == null) continue;
                Physics2D.IgnoreCollision(pc, bc, true);
            }
        }
    }

    // ═══════════ 主循环 ═══════════

    private void Update()
    {
        if (isDead || player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        Vector2 dir = (player.position - transform.position).normalized;

        // 调试线
        Debug.DrawLine(transform.position, player.position,
            dist <= chaseRange ? chaseLineColor : Color.clear);

        // 朝向 + AttackPoint
        if (Mathf.Abs(dir.x) > 0.05f)
            sr.flipX = dir.x < 0;
        if (attackPoint != null)
        {
            Vector3 ap = attackPoint.localPosition;
            ap.x = attackPointDefaultX * (sr.flipX ? -1f : 1f);
            attackPoint.localPosition = ap;
        }

        // 计时器
        if (invincibleTimer > 0f) invincibleTimer -= Time.deltaTime;

        // 检查阶段切换
        CheckPhaseTransition();

        // 死亡
        if (currentHealth <= 0) { Die(); return; }

        // ── 踉跄状态 ──
        if (isStaggered)
        {
            staggerTimer -= Time.deltaTime;
            rb.velocity = Vector2.zero;
            rb.velocity = Vector2.zero;
            if (staggerTimer <= 0f)
                EndStagger();
            return;
        }

        // ── 锁定状态（攻击/跳跃/受伤动画播放中）──
        if (!canAct)
        {
            lockTimer -= Time.deltaTime;
            if (lockTimer <= 0f) canAct = true; // 超时解锁
            else
            {
                rb.velocity = Vector2.zero;
                rb.velocity = new Vector2(0f, rb.velocity.y);
                return;
            }
        }

        // ── 前摇中（空洞骑士风格：攻击前停顿）──
        if (aiState == AIState.Telegraphing)
        {
            telegraphTimer -= Time.deltaTime;
            rb.velocity = Vector2.zero;
            rb.velocity = Vector2.zero;
            if (telegraphTimer <= 0f)
                ExecuteAttack();
            return;
        }

        // ── 攻击（冷却 + 范围内）──
        if (dist <= attackRange && Time.time >= lastAttackTime + currentAttackCooldown)
        {
            BeginTelegraph();
            return;
        }

        // ── 追击 / 游走 ──
        if (dist <= chaseRange)
        {
            // 玩家在范围内 → 追击
            rb.velocity = new Vector2(dir.x * currentMoveSpeed, rb.velocity.y);
        }
        else
        {
            // 玩家不在 → 随机游走
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f)
            {
                wanderTimer = Random.Range(wanderMinTime, wanderMaxTime);
                wanderDir = Random.value > 0.5f ? 1f : -1f;
            }
            rb.velocity = new Vector2(wanderDir * wanderSpeed, rb.velocity.y);
            sr.flipX = wanderDir < 0;
        }
    }

    // ═══════════ 前摇 → 执行攻击 ═══════════

    /// <summary>进入前摇：停顿 + 播放起手动画</summary>
    private void BeginTelegraph()
    {
        canAct = false;
        aiState = AIState.Telegraphing;
        telegraphTimer = currentTelegraphTime;

        currentAttackIdx = isPhase2 && p2UnlockAttack3
            ? Random.Range(0, 4)    // P2 四选一
            : Random.Range(0, 3);

        lastAttackTime = Time.time;
        anim.SetInteger("attackIndex", currentAttackIdx);
        anim.SetTrigger("doTelegraph");     // 起手动画（举刀/蓄力）
        rb.velocity = Vector2.zero;
    }

    /// <summary>前摇结束 → 执行攻击动作</summary>
    private void ExecuteAttack()
    {
        aiState = AIState.Acting;
        canAct = false;
        lockTimer = 2f; // 攻击超时兜底

        anim.SetTrigger("doAttack");
        rb.velocity = Vector2.zero;

        // 攻击特殊行为
        switch (currentAttackIdx)
        {
            case 1: // Dash 冲锋
                float dashDir = sr.flipX ? -1f : 1f;
                rb.velocity = new Vector2(dashDir * dashForce, rb.velocity.y);
                break;

            case 2: // 砸地 AOE
                rb.velocity = new Vector2(0f, 5f);
                break;

            case 3: // P2 额外攻击：突进 AOE
                float d = sr.flipX ? -1f : 1f;
                rb.velocity = new Vector2(d * dashForce * 1.2f, 4f);
                break;
        }
    }

    /// <summary>攻击判定帧（动画事件）</summary>
    public void OnAttackHit()
    {
        // 播放攻击音效（近战 / AOE）
        if (DungeonKIT.AudioManager.Instance != null)
        {
            switch (currentAttackIdx)
            {
                case 0: // 快速斩击
                case 1: // 冲锋
                    DungeonKIT.AudioManager.Instance.PlaySFX(DungeonKIT.AudioManager.Instance.enemyMeleeAttack);
                    break;
                case 2: // 砸地 AOE
                case 3: // P2 突进 AOE
                    DungeonKIT.AudioManager.Instance.PlaySFX(DungeonKIT.AudioManager.Instance.enemyRangeAttack);
                    break;
            }
        }

        int dmg = attackDamage;
        float radius = attackRadius;

        switch (currentAttackIdx)
        {
            case 0: // 快速斩击
                if (currentAttackIdx < attackDamageMultipliers.Length)
                    dmg = Mathf.RoundToInt(dmg * attackDamageMultipliers[0]);
                if (currentAttackIdx < attackRadiusMultipliers.Length)
                    radius *= attackRadiusMultipliers[0];
                DoDamage(attackPoint != null ? attackPoint.position : transform.position, radius, dmg);
                break;

            case 1: // 冲锋
                if (currentAttackIdx < attackDamageMultipliers.Length)
                    dmg = Mathf.RoundToInt(dmg * attackDamageMultipliers[1]);
                DoDamage(attackPoint != null ? attackPoint.position : transform.position, radius, dmg);
                break;

            case 2: // 砸地 AOE
                if (currentAttackIdx < attackDamageMultipliers.Length)
                    dmg = Mathf.RoundToInt(dmg * attackDamageMultipliers[2]) + slamDamageBonus;
                DoDamage(transform.position, slamRadius, dmg);
                break;

            case 3: // P2 突进 AOE
                dmg = Mathf.RoundToInt(dmg * 1.2f);
                DoDamage(transform.position, slamRadius, dmg);
                break;
        }
    }

    private void DoDamage(Vector2 center, float radius, int damage)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;
            hit.GetComponent<PlayerHealth>()?.TakeDamage(damage);
        }
    }

    /// <summary>攻击结束（动画事件）</summary>
    public void OnAttackEnd()
    {
        canAct = true;
        lockTimer = 0f;
        aiState = AIState.Idle;
    }

    public void OnJumpEnd()
    {
        canAct = true;
        lockTimer = 0f;
    }

    // ═══════════ 受伤 ═══════════

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        if (invincibleTimer > 0f && !isStaggered) return;

        // 踉跄时受伤加重
        if (isStaggered)
            damage = Mathf.RoundToInt(damage * staggerDamageMultiplier);

        currentHealth -= damage;
        invincibleTimer = hurtInvincibleTime;
        hitCounter++;

        // 播放受击音效
        if (DungeonKIT.AudioManager.Instance != null)
            DungeonKIT.AudioManager.Instance.PlaySFX(DungeonKIT.AudioManager.Instance.enemyHit);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // 检查踉跄
        if (!isStaggered && hitCounter >= staggerHitsRequired
            && Time.time >= lastStaggerTime + staggerCooldown)
        {
            EnterStagger();
            return;
        }

        // 普通受伤硬直
        canAct = false;
        lockTimer = 0.8f;
        anim.SetTrigger("isHit");
        rb.velocity = Vector2.zero;
    }

    public void OnHitEnd()
    {
        canAct = true;
        lockTimer = 0f;
    }

    // ═══════════ 踉跄 ═══════════

    private void EnterStagger()
    {
        isStaggered = true;
        canAct = false;
        staggerTimer = staggerDuration;
        lastStaggerTime = Time.time;
        hitCounter = 0;

        anim.SetTrigger("doStagger");
        anim.SetBool("isStaggered", true);
        rb.velocity = Vector2.zero;
    }

    private void EndStagger()
    {
        isStaggered = false;
        canAct = true;
        anim.SetBool("isStaggered", false);
    }

    // ═══════════ 阶段切换 ═══════════

    private void CheckPhaseTransition()
    {
        if (!isPhase2 && currentHealth <= maxHealth * 0.5f)
        {
            isPhase2 = true;
            anim.SetBool("isPhase2", true);
            aiState = AIState.Idle;
            canAct = true;

            // 播放 Boss 咆哮 + 通知事件
            if (DungeonKIT.AudioManager.Instance != null)
                DungeonKIT.AudioManager.Instance.PlaySFX(DungeonKIT.AudioManager.Instance.bossRoar);
            OnBossPhaseTransition?.Invoke();
        }
    }

    // ═══════════ 死亡 ═══════════

    private void Die()
    {
        isDead = true;
        canAct = false;
        isStaggered = false;
        anim.SetBool("isDead", true);
        rb.velocity = Vector2.zero;
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        enabled = false;

        // 通知 Boss 被击败 → 触发胜利
        OnBossDefeated?.Invoke();
        DungeonKIT.GameManager.Instance?.LevelComplete();
    }

    // ═══════════ Gizmos ═══════════

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 p = transform.position;

        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(p, slamRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(p, chaseRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(p, attackRange);
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            float dist = Vector2.Distance(p, playerObj.transform.position);
            Gizmos.color = dist <= chaseRange ? chaseLineColor : Color.grey;
            Gizmos.DrawLine(p, playerObj.transform.position);
        }
    }
#endif
}
