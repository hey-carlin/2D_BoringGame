using UnityEngine;
using Game.Boss;

public class BossStateMachine : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;

    private Animator animator;
    private Rigidbody2D rb;

    private BossPhase currentPhase = BossPhase.Phase1;
    private BossState currentState = BossState.Idle;

    public BossPhase CurrentPhase => currentPhase;
    public BossState CurrentState => currentState;
    public bool IsIdle => currentState == BossState.Idle;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        SetPhase(BossPhase.Phase1);
        SetState(BossState.Idle);
    }

    /* ========== 对外接口 ========== */

    public void SetPhase(BossPhase phase)
    {
        currentPhase = phase;
        animator.SetInteger("Phase", (int)phase);
    }

    public void SetState(BossState state)
    {
        // Death 状态不可覆盖
        if (currentState == BossState.Death && state != BossState.Death)
            return;

        currentState = state;
        animator.SetInteger("State", (int)state);
    }

    public void Hit()
    {
        SetState(BossState.Hurt);
        animator.SetTrigger("Hit");
    }

    public void ShieldHit()
    {
        SetState(BossState.ShieldHit);
        animator.SetTrigger("ShieldHit");
    }

    public void Die()
    {
        SetState(BossState.Death);
        animator.SetTrigger("Dead");
    }

    /* ========== 移动 ========== */

    /// <summary>朝目标方向移动（由 AI 在 FixedUpdate 中调用）</summary>
    public void MoveToward(Vector2 direction)
    {
        if (rb == null) return;
        rb.velocity = new Vector2(direction.x * moveSpeed, rb.velocity.y);
    }

    /// <summary>停止水平移动</summary>
    public void StopMoving()
    {
        if (rb == null) return;
        rb.velocity = new Vector2(0f, rb.velocity.y);
    }

    /// <summary>朝目标方向翻转</summary>
    public void FaceDirection(float xDirection)
    {
        if (Mathf.Abs(xDirection) < 0.01f) return;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(xDirection);
        transform.localScale = scale;
    }
}
