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
}