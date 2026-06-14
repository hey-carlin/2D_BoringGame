using UnityEngine;
using Player;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth { get; private set; }

    public System.Action<int> OnHealthChanged;           // 血量变化回调
    public System.Action OnDeath;

    private PlayerStateMachine stateMachine;

    void Awake()
    {
        currentHealth = maxHealth;
        stateMachine = GetComponent<PlayerStateMachine>();
        if (stateMachine == null)
            stateMachine = FindObjectOfType<PlayerStateMachine>();
    }

    /// <summary>受到伤害</summary>
    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0) return;

        // 无敌中不受伤害
        if (stateMachine != null && stateMachine.isInvincible) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);
        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            if (stateMachine != null)
                stateMachine.Die();
            OnDeath?.Invoke();
        }
        else
        {
            if (stateMachine != null)
                stateMachine.OnHit();
        }
    }

    /// <summary>恢复生命值</summary>
    public void Heal(int amount)
    {
        if (currentHealth <= 0) return;
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
    }

    /// <summary>收集道具时调用</summary>
    public void OnCollectItem(CollectibleItem item)
    {
        Debug.Log($"获得了道具: {item.itemName}");
        switch (item.itemID)
        {
            case "item_health_potion":
                Heal(30);
                break;
            case "item_coin":
                break;
            default:
                Heal(10);
                break;
        }
    }
}
