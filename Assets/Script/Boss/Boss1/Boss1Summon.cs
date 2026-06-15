using UnityEngine;
using Enemy;

/// <summary>
/// Boss1 召唤物：原地不动，玩家碰到受伤 + 召唤物死亡。
/// 玩家也可以攻击召唤物使其死亡。
/// </summary>
public class Boss1Summon : MonoBehaviour, IDamageable
{
    [Header("接触伤害")]
    public int contactDamage = 10;

    [Header("生命值")]
    public int maxHealth = 30;

    private int currentHealth;
    private bool isDying;
    private Animator anim;
    private Collider2D col;

    private void Start()
    {
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();

        currentHealth = maxHealth;

        // 出现动画
        if (anim != null)
            anim.Play("B1_SummonAppear");
    }

    // ═══ 玩家碰到召唤物 ═══

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDying) return;
        if (!other.CompareTag("Player")) return;

        // 玩家受伤
        var ph = other.GetComponent<PlayerHealth>();
        if (ph != null)
            ph.TakeDamage(contactDamage);

        // 召唤物死亡
        Die();
    }

    // ═══ 玩家攻击召唤物 ═══

    public void TakeDamage(int damage)
    {
        if (isDying) return;
        currentHealth -= damage;
        if (currentHealth <= 0)
            Die();
    }

    // ═══ 死亡 ═══

    private void Die()
    {
        if (isDying) return;
        isDying = true;

        // 关闭碰撞体（防止重复触发）
        if (col != null) col.enabled = false;

        // 播放死亡动画，播完销毁
        if (anim != null)
        {
            anim.Play("B1_SummonDeath");
            float deathLength = anim.GetCurrentAnimatorStateInfo(0).length;
            Destroy(gameObject, deathLength + 0.1f);
        }
        else
        {
            Destroy(gameObject, 0.3f);
        }
    }
}
