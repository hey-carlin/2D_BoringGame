using UnityEngine;

/// <summary>
/// Boss 护盾：有独立 HP、被击中触发 ShieldHit、HP 归零后永久破碎。
/// </summary>
public class BossShield : MonoBehaviour
{
    [Header("Shield Stats")]
    public int maxShieldHP = 200;

    public int CurrentHP { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsBroken { get; private set; }          // 永久破碎
    public float HPRatio => (float)CurrentHP / maxShieldHP;

    public System.Action OnActivated;
    public System.Action<int> OnShieldHit;              // 参数：剩余护盾值
    public System.Action OnShieldBreak;                 // HP=0 破碎

    private BossStateMachine sm;

    private void Awake()
    {
        sm = GetComponent<BossStateMachine>();
    }

    /// <summary>BossPhase1 调用：开启护盾</summary>
    public void Activate()
    {
        if (IsBroken) return;

        IsActive = true;
        CurrentHP = maxShieldHP;
        OnActivated?.Invoke();
    }

    /// <summary>BossHealth 调用：护盾吸收伤害，返回剩余穿透伤害</summary>
    public int AbsorbDamage(int damage)
    {
        if (!IsActive || IsBroken) return damage;

        CurrentHP -= damage;
        OnShieldHit?.Invoke(CurrentHP);

        // 播放护盾受击动画
        if (sm != null)
            sm.ShieldHit();

        if (CurrentHP <= 0)
        {
            int overflow = -CurrentHP;  // 先记录溢出量
            CurrentHP = 0;
            IsActive = false;
            IsBroken = true;
            OnShieldBreak?.Invoke();
            return overflow;
        }

        return 0; // 完全吸收
    }

    /// <summary>手动关闭护盾（非破碎）</summary>
    public void Deactivate()
    {
        IsActive = false;
    }
}
