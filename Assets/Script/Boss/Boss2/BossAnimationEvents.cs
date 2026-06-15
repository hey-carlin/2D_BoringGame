using UnityEngine;

/// <summary>
/// 动画事件 → C# 事件。每个动画的最后一帧调用对应的 Finished 方法。
///
/// 链条动画（Casting→Spell、TeleportStart→TeleportEnd）：
///   动画 A 最后一帧调用 OnXxxFinished → Phase 脚本收到回调 → 切到动画 B
/// </summary>
public class BossAnimationEvents : MonoBehaviour
{
    public System.Action OnMeleeAttackEnd;
    public System.Action OnSpellEnd;
    public System.Action OnCastingEnd;          // 施法引导结束 → 触发 Spell
    public System.Action OnCreateShieldEnd;
    public System.Action OnTeleportStartEnd;    // 传送消失结束 → 瞬移 → 触发 TeleportEnd
    public System.Action OnTeleportEndEnd;      // 传送出现结束 → 进入 Rest
    public System.Action OnShieldHitEnd;
    public System.Action OnHurtEnd;
    public System.Action OnRestEnd;
    public System.Action OnAnySkillEnd;         // 兼容旧动画

    // ── Animation Events 入口 ──

    public void OnMeleeAttackFinished()    => OnMeleeAttackEnd?.Invoke();
    public void OnSpellFinished()          => OnSpellEnd?.Invoke();
    public void OnCastingFinished()        => OnCastingEnd?.Invoke();
    public void OnCreateShieldFinished()   => OnCreateShieldEnd?.Invoke();
    public void OnTeleportStartFinished()  => OnTeleportStartEnd?.Invoke();
    public void OnTeleportEndFinished()    => OnTeleportEndEnd?.Invoke();
    public void OnShieldHitFinished()      => OnShieldHitEnd?.Invoke();
    public void OnHurtFinished()           => OnHurtEnd?.Invoke();
    public void OnRestFinished()           => OnRestEnd?.Invoke();

    public void OnSkillEnd() => OnAnySkillEnd?.Invoke();
}
