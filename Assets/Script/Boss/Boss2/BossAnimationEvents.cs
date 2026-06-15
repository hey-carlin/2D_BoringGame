using UnityEngine;

/// <summary>Boss2 动画事件桥接</summary>
public class BossAnimationEvents : MonoBehaviour
{
    private BossAI ai;
    private void Start() => ai = GetComponent<BossAI>();

    public void OnCastingFinished()    => ai?.OnCastingFinished();
    public void OnSpellFinished()      => ai?.OnSpellFinished();
    public void OnTeleportStartFinished() => ai?.OnTeleportStartFinished();
    public void OnTeleportEndFinished()   => ai?.OnTeleportEndFinished();
    public void OnAttackEnd()          => ai?.OnAttackEnd();
    public void OnHitEnd()             => ai?.OnHitEnd();
    public void OnSkillEnd()           => ai?.OnSkillEnd();
}
