using UnityEngine;
using Game.Boss;

/// <summary>
/// Boss 二阶段（50%-0% HP）行为骨架。
/// 具体逻辑后续完善。
/// </summary>
public class BossPhase2 : MonoBehaviour
{
    [Header("Timings")]
    public float telegraphTime = 0.3f;
    public float restTime = 0.9f;

    // ── 组件 ──
    private BossStateMachine sm;

    // 子步骤（与 Phase1 保持一致接口）
    public enum SubStep { Idle, Telegraph, Execute, ChainWait, Rest }
    [HideInInspector] public SubStep currentStep = SubStep.Idle;
    [HideInInspector] public float stepTimer;
    [HideInInspector] public float idealDistance;

    private Transform player;

    private void Awake()
    {
        sm = GetComponent<BossStateMachine>();
    }

    public void SetPlayer(Transform t) => player = t;

    public void OnEnterPhase()
    {
        currentStep = SubStep.Idle;
    }

    public void OnExitPhase()
    {
        sm.StopTelegraphFlash();
    }

    public void UpdatePhase(float distToPlayer)
    {
        // TODO: Phase 2 攻击逻辑
        // 暂时回退到 Idle
    }

    public void OnArrivedAtIdealDistance()
    {
        // TODO
    }

    public bool CanTakeAction() =>
        sm.CurrentState != BossState.Hurt
        && sm.CurrentState != BossState.ShieldHit;

    public bool IsApproaching() => false;
}
