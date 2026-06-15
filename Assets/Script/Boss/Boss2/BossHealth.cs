using UnityEngine;
using Game.Boss;

/// <summary>
/// Boss 血量：2 阶段阈值 + 护盾拦截。
/// </summary>
public class BossHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 1000;

    public int Health { get; private set; }
    public float HealthPercent => (float)Health / maxHealth;
    public BossPhase CurrentPhase { get; private set; } = BossPhase.Phase1;

    public System.Action<int> OnDamaged;
    public System.Action OnDied;
    public System.Action OnPhase2Entered;
    public System.Action OnPhaseChanged;

    private BossStateMachine sm;
    private BossShield shield;

    private void Awake()
    {
        sm = GetComponent<BossStateMachine>();
        shield = GetComponent<BossShield>();
        Health = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (Health <= 0) return;

        // 护盾吸收伤害
        if (shield != null && shield.IsActive)
        {
            damage = shield.AbsorbDamage(damage);
            if (damage <= 0) return; // 完全吸收
        }

        Health -= damage;
        Health = Mathf.Max(Health, 0);
        OnDamaged?.Invoke(Health);

        if (Health <= 0)
        {
            Die();
            return;
        }

        // P1 → P2
        if (Health <= maxHealth * 0.5f && CurrentPhase == BossPhase.Phase1)
        {
            EnterPhase2();
        }
        else if (shield == null || !shield.IsActive)
        {
            sm.Hit(); // 无护盾受伤硬直
        }
    }

    private void EnterPhase2()
    {
        CurrentPhase = BossPhase.Phase2;
        sm.SetPhase(BossPhase.Phase2);
        OnPhase2Entered?.Invoke();
        OnPhaseChanged?.Invoke();
    }

    private void Die()
    {
        sm.Die();
        OnDied?.Invoke();
    }
}
