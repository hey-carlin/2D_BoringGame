using UnityEngine;
using Game.Boss;

/// <summary>
/// 挂载在 Boss Animator 上，由 Animation Events 调用。
/// 每个攻击/技能动画结束后回调对应方法，由 BossAI 监听处理。
/// </summary>
public class BossAnimationEvents : MonoBehaviour
{
    private BossStateMachine sm;

    // C# 事件 — BossAI 订阅这些
    public System.Action OnMeleeAttackEnd;
    public System.Action OnCastingEnd;
    public System.Action OnCreateShieldEnd;
    public System.Action OnExplodeEnd;
    public System.Action OnSummonEnd;
    public System.Action OnSpellEnd;
    public System.Action OnCastSpellEnd;
    public System.Action OnTeleportationEnd;
    public System.Action OnHurtEnd;
    public System.Action OnShieldHitEnd;
    /// <summary>通用技能结束事件（BossAI 订阅此事件）</summary>
    public System.Action OnAnySkillEnd;

    private void Awake()
    {
        sm = GetComponent<BossStateMachine>();
    }

    // ── Animation Events（在动画关键帧调用）──

    public void OnMeleeAttackFinished() => OnMeleeAttackEnd?.Invoke();
    public void OnCastingFinished() => OnCastingEnd?.Invoke();
    public void OnCreateShieldFinished() => OnCreateShieldEnd?.Invoke();
    public void OnExplodeFinished() => OnExplodeEnd?.Invoke();
    public void OnSummonFinished() => OnSummonEnd?.Invoke();
    public void OnSpellFinished() => OnSpellEnd?.Invoke();
    public void OnCastSpellFinished() => OnCastSpellEnd?.Invoke();
    public void OnTeleportationFinished() => OnTeleportationEnd?.Invoke();
    public void OnHurtFinished() => OnHurtEnd?.Invoke();
    public void OnShieldHitFinished() => OnShieldHitEnd?.Invoke();

    /// <summary>通用技能结束回调 — 旧动画事件的入口</summary>
    public void OnSkillEnd()
    {
        if (sm != null)
            sm.SetState(BossState.Idle);
        OnAnySkillEnd?.Invoke();
    }
}
