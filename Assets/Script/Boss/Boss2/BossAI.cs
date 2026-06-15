using UnityEngine;
using Enemy; // IDamageable

/// <summary>
/// Boss2 — 参照 Boss1 简化：
///   P1: 近战 / 施法→法术 / 护盾 / 传送
///   P2 (≤40%): + 爆炸 / 召唤 / 远程法术
/// </summary>
public class BossAI : MonoBehaviour, IDamageable
{
    [Header("移动")]
    public float moveSpeed = 3f;
    public float chaseRange = 15f;
    public float idealDistance = 5f;        // 浮空保持的理想距离
    public float hoverAmplitude = 0.4f;     // 浮空摆动幅度
    public float hoverFrequency = 1.2f;

    [Header("攻击 — MeleeAttack")]
    public float meleeCooldown = 1.2f;
    public float meleeExecTime = 0.8f;
    public int meleeDamage = 15;
    public Transform meleeAttackPoint;
    public float meleeRadius = 1.5f;

    [Header("施法→法术 — Casting→Spell")]
    public float spellCooldown = 3f;
    public float castingExecTime = 1f;
    public float spellExecTime = 1f;
    public int spellDamage = 20;
    public Transform spellAttackPoint;
    public float spellRadius = 2f;

    [Header("护盾 — CreateShield")]
    public int shieldHP = 150;
    public float shieldCooldown = 12f;
    public float shieldExecTime = 1f;

    [Header("传送 — Teleport")]
    public float teleportCooldown = 8f;
    public float teleStartTime = 0.8f;
    public float teleEndTime = 0.8f;
    public float teleportMinDist = 5f;
    public float teleportMaxDist = 10f;

    [Header("P2 阶段转换")]
    [Range(0f, 1f)] public float phase2Threshold = 0.4f;
    public float phaseTransitionTime = 2f;

    [Header("P2 — Explode")]
    public float explodeCooldown = 6f;
    public float explodeExecTime = 1.5f;
    public int explodeDamage = 30;
    public float explodeRadius = 3f;

    [Header("P2 — Summon")]
    public float summonCooldown = 10f;
    public float summonExecTime = 1.5f;
    public GameObject summonPrefab;
    public Transform[] summonSpawnPoints;
    public int maxSummonsAlive = 3;

    [Header("P2 — CastSpell")]
    public float castSpellCooldown = 4f;
    public float castSpellExecTime = 1.2f;
    public int castSpellDamage = 25;
    public Transform castSpellAttackPoint;
    public float castSpellRadius = 2.5f;

    [Header("生命值")]
    public int maxHealth = 600;
    public float hurtInvincibleTime = 0.3f;

    // ═══ 内部 ═══

    private enum State
    {
        Idle, Melee, Casting, Spell, Shielding, ShieldHit,
        TeleStart, TeleEnd, Exploding, Summoning, CastSpelling,
        PhaseTrans, Hurt, Dead
    }
    private State state = State.Idle;
    private float stateTimer;

    private Transform player;
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private float hoverBaseY;

    private int currentHealth;
    private int shieldCurrentHP;
    private bool shieldActive;
    private bool shieldBroken;
    private bool isDead;
    private bool isPhase2;

    private System.Collections.Generic.List<GameObject> aliveSummons = new System.Collections.Generic.List<GameObject>();
    private float HealthPercent => (float)currentHealth / maxHealth;
    private float lastMeleeTime = -999f;
    private float lastSpellTime = -999f;
    private float lastShieldTime = -999f;
    private float lastTeleportTime = -999f;
    private float lastExplodeTime = -999f;
    private float lastSummonTime = -999f;
    private float lastCastSpellTime = -999f;
    private float invincibleTimer;

    // ═══ 初始化 ═══

    private void Start()
    {
        var obj = GameObject.FindGameObjectWithTag("Player");
        if (obj == null) { Debug.LogError("[Boss2] 未找到 Player！"); enabled = false; return; }
        player = obj.transform;

        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        if (rb == null) Debug.LogError("[Boss2] 缺少 Rigidbody2D！");
        if (anim == null) Debug.LogError("[Boss2] 缺少 Animator！");

        if (rb != null) rb.gravityScale = 0f;
        currentHealth = maxHealth;
        hoverBaseY = transform.position.y;
        IgnorePlayerCollision();
        Debug.Log("[Boss2] 初始化完成，开始追击玩家");
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
        // Boss 身体碰撞体全部变 Trigger
        foreach (var col in GetComponents<Collider2D>())
        {
            col.isTrigger = true;
        }
    }

    // ═══ 主循环 ═══

    private void Update()
    {
        if (isDead || player == null) return;
        if (rb == null || anim == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        Vector2 dir = (player.position - transform.position).normalized;

        if (sr != null && Mathf.Abs(dir.x) > 0.05f)
            sr.flipX = dir.x < 0;

        Debug.DrawLine(transform.position, player.position, dist <= chaseRange ? Color.yellow : Color.clear);

        if (invincibleTimer > 0f) invincibleTimer -= Time.deltaTime;
        if (currentHealth <= 0) { Die(); return; }

        // 阶段转换中
        if (state == State.PhaseTrans)
        {
            stateTimer -= Time.deltaTime;
            if (rb != null) rb.velocity = Vector2.zero;
            if (stateTimer <= 0f)
            {
                isPhase2 = true;
                state = State.Idle;
            }
            return;
        }

        // 受伤 / 护盾受击
        if (state == State.Hurt || state == State.ShieldHit)
        {
            stateTimer -= Time.deltaTime;
            if (rb != null) rb.velocity = Vector2.zero;
            if (stateTimer <= 0f) state = State.Idle;
            return;
        }

        // 动作执行中
        if (state != State.Idle)
        {
            stateTimer -= Time.deltaTime;
            if (rb != null) rb.velocity = Vector2.zero;
            if (stateTimer <= 0f) state = State.Idle;
            return;
        }

        // 脱战
        if (dist > chaseRange)
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return;
        }

        // ── P1 + P2 攻击决策 ──
        if (TryMelee(dist)) return;
        if (TrySpell(dist)) return;
        if (TryShield()) return;
        if (TryTeleport()) return;
        if (isPhase2 && TryExplode(dist)) return;
        if (isPhase2 && TrySummon()) return;
        if (isPhase2 && TryCastSpell(dist)) return;

        // 浮空追击
        HoverTowardPlayer(dist);
    }

    // ═══ 浮空移动 ═══

    private void HoverTowardPlayer(float dist)
    {
        if (rb == null) return;
        Vector2 toPlayer = player.position - transform.position;
        float horz = 0f;
        if (Mathf.Abs(dist - idealDistance) > 0.5f)
            horz = Mathf.Sign(toPlayer.x) * moveSpeed * Mathf.Clamp01(Mathf.Abs(dist - idealDistance) / 3f);

        float targetY = hoverBaseY + Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        float vert = (targetY - transform.position.y) * 3f;

        rb.velocity = new Vector2(horz, vert);
    }

    // ═══ 近战 ═══

    private bool TryMelee(float dist)
    {
        if (dist > 3f) return false;
        if (Time.time < lastMeleeTime + meleeCooldown) return false;

        state = State.Melee;
        stateTimer = meleeExecTime;
        lastMeleeTime = Time.time;
        anim.Play("B2_MeleeAttack");
        Invoke(nameof(DoMeleeDamage), meleeExecTime * 0.5f);
        return true;
    }

    private void DoMeleeDamage()
    {
        if (meleeAttackPoint == null) return;
        DoDamageAt(meleeAttackPoint.position, meleeRadius, meleeDamage);
    }

    // ═══ 施法→法术 ═══

    private bool TrySpell(float dist)
    {
        if (Time.time < lastSpellTime + spellCooldown) return false;

        state = State.Casting;
        stateTimer = castingExecTime;
        lastSpellTime = Time.time;
        anim.Play("B2_Casting");
        Invoke(nameof(StartSpellAfterCast), castingExecTime);
        return true;
    }

    private void StartSpellAfterCast()
    {
        state = State.Spell;
        stateTimer = spellExecTime;
        anim.Play("B2_Spell");
        Invoke(nameof(DoSpellDamage), spellExecTime * 0.5f);
    }

    private void DoSpellDamage()
    {
        Vector2 center = spellAttackPoint != null ? spellAttackPoint.position : transform.position;
        DoDamageAt(center, spellRadius, spellDamage);
    }

    // ═══ 护盾 ═══

    private bool TryShield()
    {
        if (shieldBroken) return false;
        if (shieldActive) return false;
        if (Time.time < lastShieldTime + shieldCooldown) return false;

        state = State.Shielding;
        stateTimer = shieldExecTime;
        lastShieldTime = Time.time;
        shieldActive = true;
        shieldCurrentHP = shieldHP;
        anim.Play("B2_CreateShield");
        return true;
    }

    // ═══ 传送 ═══

    private bool TryTeleport()
    {
        if (Time.time < lastTeleportTime + teleportCooldown) return false;

        state = State.TeleStart;
        stateTimer = teleStartTime;
        lastTeleportTime = Time.time;
        anim.Play("B2_TeleportStart");
        Invoke(nameof(DoTeleportArrive), teleStartTime);
        return true;
    }

    private void DoTeleportArrive()
    {
        float dir = Random.value > 0.5f ? 1f : -1f;
        float d = Random.Range(teleportMinDist, teleportMaxDist);
        transform.position = (Vector2)transform.position + new Vector2(dir * d, 0f);
        hoverBaseY = transform.position.y;

        state = State.TeleEnd;
        stateTimer = teleEndTime;
        anim.Play("B2_TeleportEnd");
    }

    // ═══ P2 爆炸 ═══

    private bool TryExplode(float dist)
    {
        if (dist > 4f) return false;
        if (Time.time < lastExplodeTime + explodeCooldown) return false;

        state = State.Exploding;
        stateTimer = explodeExecTime;
        lastExplodeTime = Time.time;
        anim.Play("B2_Explode");
        Invoke(nameof(DoExplodeDamage), explodeExecTime * 0.5f);
        return true;
    }

    private void DoExplodeDamage()
    {
        DoDamageAt(transform.position, explodeRadius, explodeDamage);
    }

    // ═══ P2 召唤 ═══

    private bool TrySummon()
    {
        if (Time.time < lastSummonTime + summonCooldown) return false;
        CleanDeadSummons();
        if (aliveSummons.Count >= maxSummonsAlive) return false;

        state = State.Summoning;
        stateTimer = summonExecTime;
        lastSummonTime = Time.time;
        anim.Play("B2_Summon");
        Invoke(nameof(DoSpawnSummon), summonExecTime * 0.4f);
        return true;
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
                    aliveSummons.Add(Instantiate(summonPrefab, summonSpawnPoints[i].position, Quaternion.identity));
            }
        }
        else
        {
            for (int i = 0; i < canSpawn; i++)
            {
                Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle * 2f;
                aliveSummons.Add(Instantiate(summonPrefab, pos, Quaternion.identity));
            }
        }
    }

    private void CleanDeadSummons() => aliveSummons.RemoveAll(s => s == null);

    // ═══ P2 远程法术 ═══

    private bool TryCastSpell(float dist)
    {
        if (Time.time < lastCastSpellTime + castSpellCooldown) return false;

        state = State.CastSpelling;
        stateTimer = castSpellExecTime;
        lastCastSpellTime = Time.time;
        anim.Play("B2_CastSpell");
        Invoke(nameof(DoCastSpellDamage), castSpellExecTime * 0.5f);
        return true;
    }

    private void DoCastSpellDamage()
    {
        Vector2 center = castSpellAttackPoint != null ? castSpellAttackPoint.position : transform.position;
        DoDamageAt(center, castSpellRadius, castSpellDamage);
    }

    // ═══ 伤害判定 ═══

    private void DoDamageAt(Vector2 center, float radius, int damage)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;
            hit.GetComponent<PlayerHealth>()?.TakeDamage(damage);
        }
    }

    // ═══ 受伤 ═══

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        if (invincibleTimer > 0f) return;
        if (state == State.PhaseTrans) return;

        // 护盾吸收
        if (shieldActive)
        {
            shieldCurrentHP -= damage;
            if (shieldCurrentHP <= 0)
            {
                shieldCurrentHP = 0;
                shieldActive = false;
                shieldBroken = true;
            }
            state = State.ShieldHit;
            stateTimer = 0.4f;
            anim.Play("B2_ShieldHit");
            return;
        }

        currentHealth -= damage;
        invincibleTimer = hurtInvincibleTime;

        if (currentHealth <= 0) { Die(); return; }

        // 阶段转换
        if (!isPhase2 && HealthPercent <= phase2Threshold)
        {
            state = State.PhaseTrans;
            stateTimer = phaseTransitionTime;
            anim.Play("B2_PhaseTransition");
            CancelInvoke();
            return;
        }

        // 受伤闪烁
        state = State.Hurt;
        stateTimer = 0.5f;
        StartCoroutine(HurtFlash());
    }

    private System.Collections.IEnumerator HurtFlash()
    {
        if (sr == null) yield break;
        Color orig = sr.color;
        for (int i = 0; i < 3; i++)
        {
            sr.color = Color.red;
            yield return new WaitForSeconds(0.08f);
            sr.color = orig;
            yield return new WaitForSeconds(0.08f);
        }
    }

    // ═══ 死亡 ═══

    private void Die()
    {
        isDead = true;
        state = State.Dead;
        anim.Play("B2_Death");
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
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(p, chaseRange);
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(p, idealDistance);
        Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(p, explodeRadius);
    }
#endif
}
