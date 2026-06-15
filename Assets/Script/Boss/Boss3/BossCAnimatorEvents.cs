using UnityEngine;

/// <summary>
/// Boss C 动画事件桥接。挂在 Animator 所在物体上。
/// </summary>
public class BossCAnimatorEvents : MonoBehaviour
{
    private BossCAI bossCAI;

    private void Start()
    {
        bossCAI = GetComponent<BossCAI>();
    }

    // ── 攻击流程：前摇 → 攻击 → 判定 → 结束 ──

    /// <summary>前摇动画最后一帧</summary>
    public void OnTelegraphEnd()
    {
        // 前摇结束由代码计时器驱动，动画事件选配
    }

    /// <summary>攻击判定帧（造成伤害）</summary>
    public void OnAttackHit()
    {
        bossCAI?.OnAttackHit();
    }

    /// <summary>攻击动画最后一帧</summary>
    public void OnAttackEnd()
    {
        bossCAI?.OnAttackEnd();
    }

    // ── 跳跃 ──

    public void OnJumpEnd()
    {
        bossCAI?.OnJumpEnd();
    }

    // ── 受伤 ──

    public void OnHitEnd()
    {
        bossCAI?.OnHitEnd();
    }

    // ── 踉跄 ──

    public void OnStaggerEnd()
    {
        // 踉跄结束由代码计时器驱动，动画事件选配
    }
}
