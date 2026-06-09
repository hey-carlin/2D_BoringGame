using UnityEngine;
using Player;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth { get; private set; }

    public System.Action OnDeath;

    void Awake()
    {
        currentState = FindObjectOfType<PlayerStateMachine>();
        currentHealth = maxHealth;
    }

    private PlayerStateMachine currentState;

    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        if (currentHealth <= 0)
        {
            currentState.Die();
            OnDeath?.Invoke();
        }
        else
            currentState.OnHit();
    }

    /// <summary>恢复生命值（供 UnityEvent 调用）</summary>
    public void Heal(int amount)
    {
        if (currentHealth <= 0) return;
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
    }

    /// <summary>收集道具时调用（供 CollectibleItem.onCollected 事件挂载）</summary>
    public void OnCollectItem(CollectibleItem item)
    {
        Debug.Log($"获得了道具: {item.itemName}");
        // 根据道具 ID 做不同处理
        switch (item.itemID)
        {
            case "item_health_potion":
                Heal(30);
                break;
            case "item_coin":
                // 加分逻辑（如果有 ScoreManager）
                break;
            default:
                Heal(10);  // 默认回复少量生命
                break;
        }
    }
}