using UnityEngine;
using Game.Boss;

/// <summary>
/// Boss2 表现层：浮空动画 + 移动 + 朝向 + 前摇闪烁。
/// </summary>
public class BossStateMachine : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float hoverAmplitude = 0.4f;
    public float hoverFrequency = 1.2f;

    [Header("Visual")]
    public SpriteRenderer bossSprite;
    public Color telegraphColor = new Color(1f, 0.4f, 0.4f, 1f);

    private Animator animator;
    private Rigidbody2D rb;
    private BossState currentState = BossState.Idle;
    private BossPhase currentPhase = BossPhase.Phase1;
    private Color originalColor;
    private float hoverBaseY;

    public BossState CurrentState => currentState;
    public BossPhase CurrentPhase => currentPhase;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        if (bossSprite != null) originalColor = bossSprite.color;
    }

    private void Start()
    {
        hoverBaseY = transform.position.y;
        SetPhase(BossPhase.Phase1);
        SetState(BossState.Idle);
    }

    public void SetPhase(BossPhase phase)
    {
        currentPhase = phase;
        animator.SetInteger("Phase", (int)phase);
    }

    public void SetState(BossState state)
    {
        if (currentState == BossState.Death && state != BossState.Death) return;
        currentState = state;
        animator.SetInteger("State", (int)state);
    }

    // ═══ 移动 ═══

    /// <summary>浮空追踪玩家，保持理想距离</summary>
    public void HoverToward(Vector2 playerPos, float idealDist)
    {
        if (rb == null) return;
        Vector2 toPlayer = playerPos - (Vector2)transform.position;
        float dist = toPlayer.magnitude;

        float horz = 0f;
        if (Mathf.Abs(dist - idealDist) > 0.5f)
            horz = Mathf.Sign(toPlayer.x) * moveSpeed * Mathf.Clamp01(Mathf.Abs(dist - idealDist) / 3f);

        float targetY = hoverBaseY + Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        float vert = (targetY - transform.position.y) * 3f;

        rb.velocity = new Vector2(horz, vert);
    }

    public void StopMoving()
    {
        if (rb != null) rb.velocity = Vector2.zero;
    }

    public void FaceDirection(float xDir)
    {
        if (Mathf.Abs(xDir) < 0.01f) return;
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * Mathf.Sign(xDir);
        transform.localScale = s;
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

    // ═══ 视觉 ═══

    public void StartTelegraphFlash()
    {
        if (bossSprite == null) return;
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    public void StopTelegraphFlash()
    {
        StopAllCoroutines();
        if (bossSprite != null) bossSprite.color = originalColor;
    }

    private System.Collections.IEnumerator FlashRoutine()
    {
        float interval = 0.06f;
        bool on = false;
        while (true)
        {
            on = !on;
            if (bossSprite != null)
                bossSprite.color = on ? telegraphColor : originalColor;
            yield return new WaitForSeconds(interval);
        }
    }

    public void UpdateHoverBaseY() => hoverBaseY = transform.position.y;
}
