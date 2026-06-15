using UnityEngine;
using Enemy; // IDamageable

/// <summary>
/// Boss1：追击 + 攻击 / 召唤 / 技能1。
/// </summary>
public class Boss1AI : MonoBehaviour, IDamageable
{
    [Header("移动")]
    public float moveSpeed = 2.5f;
    public float chaseRange = 10f;
    public float attackRange = 2f;

    [Header("攻击 — B1_Attack")]
    public float attackCooldown = 1.2f;
    public float attackExecTime = 0.8f;
    public int attackDamage = 15;
    public Transform attackPoint;
    public float attackRadius = 1.2f;

    [Header("召唤 — B1_Summon")]
    public float summonCooldown = 8f;
    public float summonExecTime = 1.5f;
    public GameObject summonPrefab;
    public Transform[] summonSpawnPoints;
    public int maxSummonsAlive = 3;
    [Range(0f, 1f)] public float summonHPThresholdHigh = 0.8f;  // 高于 80% 不召唤
    [Range(0f, 1f)] public float summonHPThresholdLow = 0.3f;   // 低于 30% 不召唤

    [Header("技能1 — B1_Skill1")]
    public float skill1Cooldown = 5f;
    public float skill1ExecTime = 1.2f;
    public int skill1Damage = 25;
    public float skill1Range = 4f;

    [Header("生命值")]
    public int maxHealth = 300;
    public float hurtInvincibleTime = 0.3f;

    // ═══ 内部 ═══

    private enum State { Idle, Telegraph, Attacking, Summoning, Skilling, Dead }
    private State state = State.Idle;
    private float stateTimer;

    private Transform player;
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;

    private int currentHealth;
    private bool isDead;
    private System.Collections.Generic.List<GameObject> aliveSummons = new System.Collections.Generic.List<GameObject>();
    private float HealthPercent => (float)currentHealth / maxHealth;
    private float lastAttackTime = -999f;
    private float lastSummonTime = -999f;
    private float lastSkill1Time = -999f;
    private float invincibleTimer;

    private void Start()
    {
        var obj = GameObject.FindGameObjectWithTag("Player");
        if (obj == null) { Debug.LogError("[Boss1] 未找到 Player！"); enabled = false; return; }
        player = obj.transform;

        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        currentHealth = maxHealth;
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

    private void Update()
    {
        if (isDead || player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        Vector2 dir = (player.position - transform.position).normalized;

        if (Mathf.Abs(dir.x) > 0.05f)
            sr.flipX = dir.x < 0;

        Debug.DrawLine(transform.position, player.position, dist <= chaseRange ? Color.yellow : Color.clear);

        if (invincibleTimer > 0f) invincibleTimer -= Time.deltaTime;
        if (currentHealth <= 0) { Die(); return; }

        // 攻击/召唤/技能执行中
        if (state == State.Attacking || state == State.Summoning || state == State.Skilling)
        {
            stateTimer -= Time.deltaTime;
            rb.velocity = Vector2.zero;
            if (stateTimer <= 0f)
                state = State.Idle;
            return;
        }

        // 脱战
        if (dist > chaseRange)
        {
            rb.velocity = Vector2.zero;
            anim.Play("B1_Idle");
            return;
        }

        // 攻击
        if (dist <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            StartAttack();
            return;
        }

        // 技能1
        if (dist <= skill1Range && Time.time >= lastSkill1Time + skill1Cooldown)
        {
            StartSkill1();
            return;
        }

        // 召唤（HP 在 30%-80% 且没有存活的召唤物）
        CleanDeadSummons();
        bool canSummon = aliveSummons.Count < maxSummonsAlive
                      && HealthPercent <= summonHPThresholdHigh
                      && HealthPercent > summonHPThresholdLow
                      && Time.time >= lastSummonTime + summonCooldown;
        if (canSummon)
        {
            StartSummon();
            return;
        }

        // 追击
        rb.velocity = new Vector2(dir.x * moveSpeed, rb.velocity.y);
        anim.Play("B1_Idle"); // 没有 Move 动画，用 Idle
    }

    // ═══ 攻击 ═══

    private void StartAttack()
    {
        state = State.Attacking;
        stateTimer = attackExecTime;
        lastAttackTime = Time.time;
        anim.Play("B1_Attack");
        rb.velocity = Vector2.zero;

        // 延迟判定（动画打到时）
        Invoke(nameof(DoAttackDamage), attackExecTime * 0.5f);
    }

    private void DoAttackDamage()
    {
        if (attackPoint == null) return;
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;
            hit.GetComponent<PlayerHealth>()?.TakeDamage(attackDamage);
        }
    }

    // ═══ 召唤 ═══

    private void StartSummon()
    {
        state = State.Summoning;
        stateTimer = summonExecTime;
        lastSummonTime = Time.time;
        anim.Play("B1_Summon");
        rb.velocity = Vector2.zero;

        // 延迟生成召唤物（动画播到召唤动作时）
        Invoke(nameof(DoSpawnSummon), summonExecTime * 0.4f);
    }

    private void DoSpawnSummon()
    {
        if (summonPrefab == null) return;
        CleanDeadSummons();

        int canSpawn = maxSummonsAlive - aliveSummons.Count;
        if (canSpawn <= 0) return;

        if (summonSpawnPoints != null && summonSpawnPoints.Length > 0)
        {
            for (int i = 0; i < Mathf.Min(canSpawn, summonSpawnPoints.Length); i++)
            {
                if (summonSpawnPoints[i] != null)
                {
                    var s = Instantiate(summonPrefab, summonSpawnPoints[i].position, Quaternion.identity);
                    aliveSummons.Add(s);
                }
            }
        }
        else
        {
            for (int i = 0; i < canSpawn; i++)
            {
                Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle * 2f;
                var s = Instantiate(summonPrefab, pos, Quaternion.identity);
                aliveSummons.Add(s);
            }
        }
    }

    private void CleanDeadSummons()
    {
        aliveSummons.RemoveAll(s => s == null);
    }

    // ═══ 技能1 ═══

    private void StartSkill1()
    {
        state = State.Skilling;
        stateTimer = skill1ExecTime;
        lastSkill1Time = Time.time;
        anim.Play("B1_Skill1");
        rb.velocity = Vector2.zero;

        Invoke(nameof(DoSkill1Damage), skill1ExecTime * 0.5f);
    }

    private void DoSkill1Damage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, skill1Range);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;
            hit.GetComponent<PlayerHealth>()?.TakeDamage(skill1Damage);
        }
    }

    // ═══ 受伤 ═══

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        if (invincibleTimer > 0f) return;
        if (state == State.Dead) return;

        currentHealth -= damage;
        invincibleTimer = hurtInvincibleTime;

        if (currentHealth <= 0) { Die(); return; }

        // 受伤闪烁，不打断当前动画
        StartCoroutine(HurtFlashRoutine());
    }

    private System.Collections.IEnumerator HurtFlashRoutine()
    {
        Color original = sr.color;
        for (int i = 0; i < 3; i++)
        {
            sr.color = Color.red;
            yield return new WaitForSeconds(0.08f);
            sr.color = original;
            yield return new WaitForSeconds(0.08f);
        }
    }

    // ═══ 死亡 ═══

    private void Die()
    {
        isDead = true;
        state = State.Dead;
        anim.Play("B1_Death");
        rb.velocity = Vector2.zero;
        CancelInvoke();
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        enabled = false;
    }

    // ═══ Gizmos ═══

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 p = transform.position;
        Gizmos.color = Color.yellow;  Gizmos.DrawWireSphere(p, chaseRange);
        Gizmos.color = Color.red;     Gizmos.DrawWireSphere(p, attackRange);
        Gizmos.color = Color.cyan;    Gizmos.DrawWireSphere(p, skill1Range);
        if (attackPoint != null) { Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(attackPoint.position, attackRadius); }
    }
#endif
}
