using UnityEngine;
using Enemy; // IDamageable

/// <summary>
/// Boss2 召唤物：原地不动，玩家碰到受伤+召唤物死亡，玩家可攻击消灭。
/// </summary>
public class Boss2Summon : MonoBehaviour, IDamageable
{
    [Header("接触伤害")]
    public int contactDamage = 10;

    [Header("生命值")]
    public int maxHealth = 25;

    private int currentHealth;
    private bool isDying;
    private Animator anim;
    private Collider2D col;

    private void Start()
    {
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        currentHealth = maxHealth;
        if (anim != null) anim.Play("B2_SummonAppear");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDying) return;
        if (!other.CompareTag("Player")) return;

        var ph = other.GetComponent<PlayerHealth>();
        if (ph != null) ph.TakeDamage(contactDamage);

        Die();
    }

    public void TakeDamage(int damage)
    {
        if (isDying) return;
        currentHealth -= damage;
        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        if (isDying) return;
        isDying = true;
        if (col != null) col.enabled = false;
        if (anim != null)
        {
            anim.Play("B2_SummonDeath");
            Destroy(gameObject, anim.GetCurrentAnimatorStateInfo(0).length + 0.1f);
        }
        else
        {
            Destroy(gameObject, 0.3f);
        }
    }
}
