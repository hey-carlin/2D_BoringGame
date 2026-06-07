using UnityEngine;
using Enemy;

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
        health.TakeDamage(damage);
    }
}