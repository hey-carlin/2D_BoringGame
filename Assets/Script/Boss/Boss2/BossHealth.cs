using UnityEngine;
using Game.Boss;

public class BossHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 1000;

    public int Health { get; private set; }
    public BossPhase CurrentPhase { get; private set; } = BossPhase.Phase1;

    /// <summary>受伤事件：参数为剩余血量</summary>
    public System.Action<int> OnDamaged;
    /// <summary>死亡事件</summary>
    public System.Action OnDied;
    /// <summary>进入 P2 事件</summary>
    public System.Action OnPhase2Entered;

    private BossStateMachine sm;
    private BossAI ai;

    private void Awake()
    {
        sm = GetComponent<BossStateMachine>();
        ai = GetComponent<BossAI>();
        Health = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (Health <= 0) return;

        // 护盾减伤
        if (ai != null && ai.IsShieldActive())
        {
            damage = Mathf.RoundToInt(damage * (1f - ai.GetShieldReduction()));
            sm.ShieldHit();
        }

        Health -= damage;
        Health = Mathf.Max(Health, 0);
        OnDamaged?.Invoke(Health);

        if (Health <= 0)
        {
            sm.Die();
            OnDied?.Invoke();
            return;
        }

        // P1 → P2 转换
        if (Health <= maxHealth * 0.5f && CurrentPhase == BossPhase.Phase1)
        {
            EnterPhase2();
        }
        else if (ai == null || !ai.IsShieldActive())
        {
            // 非护盾状态下受伤播放受击
            sm.Hit();
        }
    }

    private void EnterPhase2()
    {
        CurrentPhase = BossPhase.Phase2;
        sm.SetPhase(BossPhase.Phase2);
        OnPhase2Entered?.Invoke();
    }
}
