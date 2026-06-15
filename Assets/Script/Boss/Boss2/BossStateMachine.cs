using UnityEngine;
using Game.Boss;

/// <summary>
/// Boss 表现层：动画 + 移动 + 前摇闪烁。
/// 不包含决策逻辑。
/// </summary>
public class BossStateMachine : MonoBehaviour
{
    [Header("Movement")]
    public float baseMoveSpeed = 4f;

    [Header("Visual")]
    public SpriteRenderer bossSprite;
    public Color telegraphColor = new Color(1f, 0.5f, 0.5f, 1f);

    private Animator animator;
    private Rigidbody2D rb;
    private BossPhase currentPhase = BossPhase.Phase1;
    private BossState currentState = BossState.Idle;
    private float speedMultiplier = 1f;
    private Color originalColor;
    private bool isFlashing;

    public BossPhase CurrentPhase => currentPhase;
    public BossState CurrentState => currentState;
    public bool IsIdle => currentState == BossState.Idle;
    public Animator Anim => animator;
    public Rigidbody2D Rb => rb;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        if (bossSprite == null) bossSprite = GetComponentInChildren<SpriteRenderer>();
        if (bossSprite != null) originalColor = bossSprite.color;
    }

    private void Start()
    {
        SetPhase(BossPhase.Phase1);
        SetState(BossState.Idle);
    }

    // ═══════ 阶段 / 状态 ═══════

    public void SetPhase(BossPhase phase)
    {
        currentPhase = phase;
        animator.SetInteger("Phase", (int)phase);
        speedMultiplier = phase == BossPhase.Phase2 ? 1.4f : 1f;
    }

    public void SetState(BossState state)
    {
        if (currentState == BossState.Death && state != BossState.Death) return;
        currentState = state;
        animator.SetInteger("State", (int)state);
    }

    // ═══════ 受击 ═══════

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

    // ═══════ 移动 ═══════

    public void MoveToward(Vector2 dir)
    {
        if (rb == null) return;
        rb.velocity = new Vector2(dir.x * baseMoveSpeed * speedMultiplier, rb.velocity.y);
    }

    public void StopMoving()
    {
        if (rb == null) return;
        rb.velocity = new Vector2(0f, rb.velocity.y);
    }

    public void FaceDirection(float xDir)
    {
        if (Mathf.Abs(xDir) < 0.01f) return;
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * Mathf.Sign(xDir);
        transform.localScale = s;
    }

    // ═══════ 视觉 ═══════

    public void StartTelegraphFlash()
    {
        if (bossSprite == null) return;
        StopAllCoroutines();
        StartCoroutine(TelegraphFlashRoutine());
    }

    private System.Collections.IEnumerator TelegraphFlashRoutine()
    {
        isFlashing = true;
        float interval = 0.08f;
        bool on = false;
        while (isFlashing)
        {
            on = !on;
            bossSprite.color = on ? telegraphColor : originalColor;
            yield return new WaitForSeconds(interval);
        }
        bossSprite.color = originalColor;
    }

    public void StopTelegraphFlash()
    {
        isFlashing = false;
        if (bossSprite != null) bossSprite.color = originalColor;
    }

    public float GetCurrentMoveSpeed() => baseMoveSpeed * speedMultiplier;
}
