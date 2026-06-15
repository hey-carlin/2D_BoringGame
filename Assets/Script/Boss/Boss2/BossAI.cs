using UnityEngine;
using Game.Boss;

/// <summary>
/// Boss AI 协调器：
///   玩家检测 + 移动 + 阶段切换 + 委托给 BossPhase1/BossPhase2。
/// </summary>
public class BossAI : MonoBehaviour
{
    [Header("Detection")]
    public float loseAggroRange = 25f;
    public float aggroRange = 14f;

    [Header("Phases")]
    public BossPhase1 phase1;
    public BossPhase2 phase2;

    // 组件
    private BossStateMachine sm;
    private BossHealth health;
    private Transform player;

    // 状态
    private bool isApproaching;          // 正在走向理想距离

    private BossPhase CurrentPhase => health != null ? health.CurrentPhase : BossPhase.Phase1;
    private float DistToPlayer => player != null
        ? Vector2.Distance(transform.position, player.position)
        : float.MaxValue;

    // ═══════ 初始化 ═══════

    private void Awake()
    {
        sm = GetComponent<BossStateMachine>();
        health = GetComponent<BossHealth>();
        if (phase1 == null) phase1 = GetComponent<BossPhase1>();
        if (phase2 == null) phase2 = GetComponent<BossPhase2>();
    }

    private void Start()
    {
        FindPlayer();
        if (health != null) health.OnPhaseChanged += OnPhaseChanged;

        // 初始进入 P1
        if (phase1 != null)
        {
            phase1.SetPlayer(player);
            phase1.OnEnterPhase();
        }
        if (phase2 != null)
        {
            phase2.SetPlayer(player);
            phase2.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (health != null) health.OnPhaseChanged -= OnPhaseChanged;
    }

    private void FindPlayer()
    {
        var obj = GameObject.FindGameObjectWithTag("Player");
        if (obj != null) player = obj.transform;
    }

    // ═══════ 主循环 ═══════

    private void Update()
    {
        if (sm == null || health == null) return;
        if (sm.CurrentState == BossState.Death) return;

        // 找玩家
        if (player == null) { FindPlayer(); sm.SetState(BossState.Idle); return; }

        // 硬直不打断
        if (sm.CurrentState == BossState.Hurt || sm.CurrentState == BossState.ShieldHit)
            return;

        float dist = DistToPlayer;

        // 脱战
        if (dist > loseAggroRange)
        {
            sm.SetState(BossState.Idle);
            sm.StopMoving();
            return;
        }

        // 索敌外 → 追击
        if (dist > aggroRange)
        {
            sm.SetState(BossState.Move);
            isApproaching = false;
            return;
        }

        // 获取当前阶段脚本
        var activePhase = CurrentPhase == BossPhase.Phase1 ? (MonoBehaviour)phase1 : phase2;
        if (activePhase == null) return;

        // 接近中
        if (isApproaching)
        {
            var p1Approaching = phase1 != null && phase1.IsApproaching();
            var p2Approaching = phase2 != null && phase2.IsApproaching();
            if (p1Approaching || p2Approaching)
            {
                // FixedUpdate 驱动移动
                return;
            }
            isApproaching = false;
        }

        // 检查是否需要接近
        float ideal = CurrentPhase == BossPhase.Phase1 ? phase1.idealDistance : phase2.idealDistance;
        if (ideal > 0f && Mathf.Abs(dist - ideal) > 1f)
        {
            sm.SetState(BossState.Move);
            isApproaching = true;
            return;
        }

        // 委托给阶段脚本
        if (CurrentPhase == BossPhase.Phase1 && phase1 != null)
        {
            phase1.UpdatePhase(dist);
        }
        else if (CurrentPhase == BossPhase.Phase2 && phase2 != null)
        {
            phase2.UpdatePhase(dist);
        }
    }

    private void FixedUpdate()
    {
        if (sm == null || player == null) return;

        // 接近玩家
        if (isApproaching || sm.CurrentState == BossState.Move)
        {
            float ideal = CurrentPhase == BossPhase.Phase1 ? phase1.idealDistance : phase2.idealDistance;
            Vector2 toPlayer = player.position - transform.position;
            float dist = toPlayer.magnitude;

            if (ideal > 0f && dist <= ideal + 0.5f)
            {
                // 到位
                sm.StopMoving();
                isApproaching = false;

                if (CurrentPhase == BossPhase.Phase1 && phase1 != null)
                    phase1.OnArrivedAtIdealDistance();
                else if (CurrentPhase == BossPhase.Phase2 && phase2 != null)
                    phase2.OnArrivedAtIdealDistance();
            }
            else
            {
                sm.MoveToward(new Vector2(Mathf.Sign(toPlayer.x), 0f));
                sm.FaceDirection(toPlayer.x);
            }
        }

        // 保持朝向（前摇和战斗中）
        if (sm.CurrentState == BossState.Telegraph
            || sm.CurrentState == BossState.MeleeAttack
            || sm.CurrentState == BossState.Spell
            || sm.CurrentState == BossState.Casting)
        {
            sm.FaceDirection(player.position.x - transform.position.x);
        }
    }

    // ═══════ 阶段切换 ═══════

    private void OnPhaseChanged()
    {
        if (CurrentPhase == BossPhase.Phase2)
        {
            phase1?.OnExitPhase();
            phase2?.SetPlayer(player);
            phase2?.OnEnterPhase();
        }
    }

    // ═══════ 公开接口 ═══════

    public bool HasPlayer() => player != null;
    public float GetPlayerDistance() => DistToPlayer;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 p = transform.position;
        Gizmos.color = new Color(1f, 1f, 0f, 0.15f); Gizmos.DrawWireSphere(p, aggroRange);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.1f); Gizmos.DrawWireSphere(p, loseAggroRange);
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) { Gizmos.color = Color.red; Gizmos.DrawLine(p, playerObj.position); }
    }
#endif
}
