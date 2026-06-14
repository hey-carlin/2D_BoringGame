using UnityEngine;
using Enemy; // IDamageable

/// <summary>
/// 使 PlayerHealth 暴露 IDamageable 接口，供敌人 Attack / Trap 等调用。
/// </summary>
[RequireComponent(typeof(PlayerHealth))]
public class PlayerHealthAdapter : MonoBehaviour, IDamageable
{
    private PlayerHealth health;

    void Awake()
    {
        health = GetComponent<PlayerHealth>();
    }

    public void TakeDamage(int damage)
    {
        if (health != null)
            health.TakeDamage(damage);
    }
}
