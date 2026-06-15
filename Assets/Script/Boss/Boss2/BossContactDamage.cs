using UnityEngine;

/// <summary>
/// 挂在 Boss 身体碰撞体上。玩家碰到 Boss 身体时周期性受伤。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BossContactDamage : MonoBehaviour
{
    [Header("Contact Damage")]
    public int damage = 5;
    [Tooltip("每次伤害间隔")]
    public float hitInterval = 0.8f;

    private float lastHitTime = -999f;
    private Collider2D col;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time - lastHitTime < hitInterval) return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        playerHealth.TakeDamage(damage);
        lastHitTime = Time.time;
    }
}
