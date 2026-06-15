using UnityEngine;

/// <summary>
/// 挂在 Boss 的攻击碰撞体子对象上。由 BossAI 在 Execute 阶段激活/关闭。
/// 玩家进入触发器时受伤 + 击退。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BossAttackHitbox : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 10;
    public float knockbackForce = 8f;

    [Header("Hit Settings")]
    [Tooltip("对同一玩家最短间隔")]
    public float perTargetCooldown = 0.3f;

    private Collider2D col;
    private bool active;
    private float activatedTime;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
        col.enabled = false;
    }

    /// <summary>BossAI 调用，激活碰撞体</summary>
    public void Activate(float duration)
    {
        col.enabled = true;
        active = true;
        activatedTime = Time.time;
        // 持续时间到自动关闭
        CancelInvoke(nameof(Deactivate));
        if (duration > 0f)
            Invoke(nameof(Deactivate), duration);
    }

    /// <summary>BossAI 调用，关闭碰撞体</summary>
    public void Deactivate()
    {
        col.enabled = false;
        active = false;
        CancelInvoke(nameof(Deactivate));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!active) return;
        if (!other.CompareTag("Player")) return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        playerHealth.TakeDamage(damage);

        // 击退
        Rigidbody2D targetRb = other.GetComponent<Rigidbody2D>();
        if (targetRb != null && knockbackForce > 0f)
        {
            Vector2 dir = (other.transform.position - transform.position).normalized;
            // 击退以水平为主
            dir.y = 0.3f;
            targetRb.AddForce(dir.normalized * knockbackForce, ForceMode2D.Impulse);
        }
    }
}
