using UnityEngine;
using DungeonKIT;

namespace Player
{
    public class PlayerStateMachine : MonoBehaviour
    {
        [Header("Components")]
        public Animator animator;
        public Rigidbody2D rb;
        public PlayerMeleeAttack meleeAttack;

        [Header("Movement")]
        public float moveSpeed = 6f;
        public float acceleration = 40f;
        public float deceleration = 35f;
        public float airAcceleration = 18f;
        public float airDeceleration = 12f;

        [Header("Jump")]
        public float jumpForce = 14f;
        public int maxJumps = 2;                          // 最大跳跃次数（接地补满）
        public float jumpBufferTime = 0.12f;              // 跳跃输入缓冲窗口

        [Header("Ground Check")]
        public Transform groundCheck;
        public float groundCheckRadius = 0.15f;
        public LayerMask groundLayer;

        [Header("Attack")]
        public int lightAttackDamage = 3;
        public int heavyAttackDamage = 5;
        public float lightAttackDuration = 0.35f;
        public float heavyAttackDuration = 0.8f;
        public float heavyHoldTime = 0.4f;
        public float heavyCooldown = 2f;
        public float comboWindow = 3f;
        public float attackForwardStep = 2.5f;
        public float attack2JumpForce = 8f;              // Attack2 (W+J) 跳击力度

        [Header("音效")]
        public AudioSource audioSource;         // 一次性音效
        public AudioSource runAudioSource;      // 跑步循环音效
        public AudioClip spawnSound;
        public AudioClip runSound;
        public AudioClip attack1Sound;
        public AudioClip attack2Sound;
        public AudioClip attack3Sound;
        public AudioClip jumpSound;
        public AudioClip hitSound;

        [Header("Damage")]
        public float invincibilityDuration = 1.2f;
        public float hitKnockbackForce = 6f;

        [Header("Debug")]
        public bool showDebugInfo = false;

        // ──── 运行时状态 ────
        [HideInInspector] public bool controlsEnabled = true;
        [HideInInspector] public float horizontal;
        [HideInInspector] public bool isGrounded;
        [HideInInspector] public bool isInvincible;
        [HideInInspector] public float activeAttackDuration = 0.35f;
        [HideInInspector] public float currentMoveSpeed;
        [HideInInspector] public float invincibilityTimer;
        [HideInInspector] public int remainingJumps;
        [HideInInspector] public float jumpBufferTimer;

        public PlayerState currentState;
        public IdleState idleState;
        public RunState runState;
        public JumpState jumpState;
        public AttackState attackState;
        public HitState hitState;
        public DeadState deadState;

        // ──── 内部 ────
        private float hKeyHoldTimer;
        private bool isHeavyCharging;
        private float heavyCooldownTimer;
        private int attackSequence;
        private int comboStored;
        private float comboStoreTimer;

        private static readonly int ParamSpeed = Animator.StringToHash("Speed");
        private static readonly int ParamIsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int ParamJump = Animator.StringToHash("Jump");
        private static readonly int ParamAttackID = Animator.StringToHash("AttackID");
        private static readonly int ParamIsDrawing = Animator.StringToHash("IsDrawing");
        private static readonly int ParamIsSaving = Animator.StringToHash("IsSaving");
        private static readonly int ParamHeavyAttack = Animator.StringToHash("Heavy_Attack");
        private static readonly int ParamDeath = Animator.StringToHash("Death");
        private static readonly int ParamHit = Animator.StringToHash("Hit");

        void Reset()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
        }

        void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (animator == null) animator = GetComponent<Animator>();
            if (meleeAttack == null) meleeAttack = GetComponent<PlayerMeleeAttack>();

            idleState = new IdleState(this);
            runState = new RunState(this);
            jumpState = new JumpState(this);
            attackState = new AttackState(this);
            hitState = new HitState(this);
            deadState = new DeadState(this);
        }

        void Start()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            controlsEnabled = true;
            isInvincible = false;
            remainingJumps = maxJumps;
            ChangeState(idleState);

            // 生成音效
            PlaySound(spawnSound);
        }

        /// <summary>播放一次性音效</summary>
        public void PlaySound(AudioClip clip)
        {
            if (clip == null || audioSource == null) return;
            audioSource.PlayOneShot(clip);
        }

        /// <summary>每帧更新跑步音效——地上移动时播放，否则停止</summary>
        private void UpdateRunSound()
        {
            if (runAudioSource == null || runSound == null) return;

            bool shouldRun = isGrounded && Mathf.Abs(horizontal) > 0.1f
                          && currentState != attackState
                          && currentState != hitState;

            if (shouldRun)
            {
                if (!runAudioSource.isPlaying)
                {
                    runAudioSource.clip = runSound;
                    runAudioSource.loop = true;
                    runAudioSource.Play();
                }
            }
            else
            {
                if (runAudioSource.isPlaying)
                    runAudioSource.Stop();
            }
        }

        void Update()
        {
            if (!controlsEnabled) return;

            if (UIManager.Instance != null && UIManager.Instance.isPause) return;
            if (GameManager.Instance != null && !GameManager.Instance.isGame) return;

            if (Input.GetKeyDown(KeyCode.Escape) && UIManager.Instance != null)
            {
                UIManager.Instance.Pause();
                return;
            }

            // ──── 输入 ────
            horizontal = Input.GetAxisRaw("Horizontal");
            bool jumpPressed = Input.GetButtonDown("Jump");
            bool jPressed = Input.GetKeyDown(KeyCode.J);
            bool wHeld = Input.GetKey(KeyCode.W);
            bool sHeld = Input.GetKey(KeyCode.S);
            bool hHeld = Input.GetKey(KeyCode.H);

            // ──── 地面检测（仅 groundLayer）───
            if (groundCheck != null)
                isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
            else
                isGrounded = false;

            // ──── 跳跃缓冲 ────
            if (jumpPressed)
                jumpBufferTimer = jumpBufferTime;
            else
                jumpBufferTimer -= Time.deltaTime;

            // ──── 接地补满跳跃次数 ────
            if (isGrounded)
                remainingJumps = maxJumps;

            // ──── 动画参数 ────
            animator.SetFloat(ParamSpeed, Mathf.Abs(currentMoveSpeed) / moveSpeed);
            animator.SetBool(ParamIsGrounded, isGrounded);

            // ──── 计时器 ────
            if (heavyCooldownTimer > 0f)
                heavyCooldownTimer -= Time.deltaTime;

            if (comboStored > 0)
            {
                comboStoreTimer -= Time.deltaTime;
                if (comboStoreTimer <= 0f)
                {
                    comboStored = 0;
                    attackSequence = 0;
                }
            }

            // ═══════════════════════════════════════
            // 攻击输入
            // ═══════════════════════════════════════

            if (jPressed && currentState != deadState)
            {
                isHeavyCharging = false;
                hKeyHoldTimer = 0f;

                if (sHeld && !wHeld)
                {
                    if (attackSequence == 2) { comboStored = 1; comboStoreTimer = comboWindow; attackSequence = 0; }
                    else attackSequence = 0;
                    activeAttackDuration = lightAttackDuration;
                    if (meleeAttack != null) meleeAttack.SetPendingDamage(lightAttackDamage);
                    animator.SetInteger(ParamAttackID, 3);
                    ChangeState(attackState);
                }
                else if (wHeld && !sHeld)
                {
                    if (attackSequence == 1) attackSequence = 2;
                    else attackSequence = 0;
                    activeAttackDuration = lightAttackDuration;
                    if (meleeAttack != null) meleeAttack.SetPendingDamage(lightAttackDamage);
                    animator.SetInteger(ParamAttackID, 2);
                    ChangeState(attackState);
                }
                else
                {
                    if (comboStored > 0)
                    {
                        comboStored = 0; comboStoreTimer = 0f; attackSequence = 0;
                        activeAttackDuration = lightAttackDuration;
                        if (meleeAttack != null) meleeAttack.SetPendingDamage(lightAttackDamage);
                        animator.SetInteger(ParamAttackID, 4);
                        ChangeState(attackState);
                    }
                    else
                    {
                        attackSequence = 1;
                        activeAttackDuration = lightAttackDuration;
                        if (meleeAttack != null) meleeAttack.SetPendingDamage(lightAttackDamage);
                        animator.SetInteger(ParamAttackID, 1);
                        ChangeState(attackState);
                    }
                }
            }
            else if (hHeld && heavyCooldownTimer <= 0f
                     && currentState != attackState && currentState != deadState)
            {
                if (!isHeavyCharging) { isHeavyCharging = true; hKeyHoldTimer = 0f; }
                hKeyHoldTimer += Time.deltaTime;

                if (hKeyHoldTimer >= heavyHoldTime)
                {
                    isHeavyCharging = false; hKeyHoldTimer = 0f;
                    heavyCooldownTimer = heavyCooldown;
                    activeAttackDuration = heavyAttackDuration;
                    attackSequence = 0; comboStored = 0; comboStoreTimer = 0f;
                    if (meleeAttack != null) meleeAttack.SetPendingDamage(heavyAttackDamage);
                    animator.SetTrigger(ParamHeavyAttack);
                    ChangeState(attackState);
                    StartCoroutine(DoSaveSwordRoutine());
                }
            }
            else
            {
                isHeavyCharging = false;
                hKeyHoldTimer = 0f;
            }

            // ═══════════════════════════════════════
            // 跳跃：接地补满次数，每次起跳消耗1次，空中二段跳由 JumpState 内部处理
            // ═══════════════════════════════════════

            bool wantsJump = jumpBufferTimer > 0f;
            bool canJump = remainingJumps > 0
                        && currentState != attackState
                        && currentState != hitState
                        && currentState != deadState
                        && currentState != jumpState;

            if (wantsJump && canJump)
            {
                jumpBufferTimer = 0f;
                ChangeState(jumpState);
            }
            // 地面移动（非特殊状态下）
            else if (currentState != attackState && currentState != hitState
                     && currentState != deadState && currentState != jumpState)
            {
                if (Mathf.Abs(horizontal) > 0.1f)
                    ChangeState(runState);
                else if (currentState == runState)
                    ChangeState(idleState);
            }

            currentState?.OnUpdate();
            UpdateRunSound();
        }

        void FixedUpdate()
        {
            if (!controlsEnabled) return;

            // ──── 无敌倒计时 ────
            if (isInvincible)
            {
                invincibilityTimer -= Time.fixedDeltaTime;
                if (invincibilityTimer <= 0f)
                {
                    isInvincible = false;
                    invincibilityTimer = 0f;
                }
            }

            currentState?.OnFixedUpdate();

            // ──── 翻转 ────
            if (horizontal > 0.1f && transform.localScale.x < 0f)
                Flip();
            else if (horizontal < -0.1f && transform.localScale.x > 0f)
                Flip();
        }

        // ──── 公共接口 ────

        public void OnHit()
        {
            if (isInvincible) return;
            ChangeState(hitState);
        }

        public void Die()
        {
            controlsEnabled = false;
            isInvincible = true;
            ChangeState(deadState);
            animator.SetTrigger(ParamDeath);
        }

        public void AttackFinished()
        {
            // 攻击结束直接切到 Idle，不检查 isGrounded
            // （空中攻击结束后也能正常降落，移动块会在下一帧接管）
            ChangeState(idleState);
        }

        // ──── Animation Events ────

        public void OnAttackHitFrame()
        {
            if (meleeAttack != null)
                meleeAttack.OnAttackHitFrame();
        }

        public void OnAttackAnimationEnd()
        {
            AttackFinished();
        }

        public void OnHeavyAttackEnd()
        {
            ChangeState(idleState);
        }

        public void SetJumpTrigger()
        {
            animator.SetTrigger(ParamJump);
        }

        // ──── 内部 ────

        private System.Collections.IEnumerator DoSaveSwordRoutine()
        {
            animator.SetBool(ParamIsSaving, true);
            yield return new WaitForSeconds(0.2f);
            animator.SetBool(ParamIsSaving, false);
        }

        private void Flip()
        {
            Vector3 s = transform.localScale;
            s.x *= -1f;
            transform.localScale = s;
        }

        public void ChangeState(PlayerState newState)
        {
            if (currentState == newState) return;
            currentState?.OnExit();
            currentState = newState;
            currentState?.OnEnter();
        }

        public int FacingDirection()
        {
            return transform.localScale.x >= 0f ? 1 : -1;
        }

#if UNITY_EDITOR
        void OnGUI()
        {
            if (!showDebugInfo) return;
            GUILayout.BeginArea(new Rect(10, 10, 260, 250));
            GUILayout.Label($"State: {currentState?.GetType().Name}");
            GUILayout.Label($"Grounded: {isGrounded}");
            GUILayout.Label($"Velocity: {rb?.velocity}");
            GUILayout.Label($"CurrentSpeed: {currentMoveSpeed:F2}");
            GUILayout.Label($"Invincible: {isInvincible}");
            GUILayout.Label($"Jumps Left: {remainingJumps}");
            GUILayout.Label($"Buffer: {jumpBufferTimer:F3}");
            GUILayout.EndArea();
        }
#endif
    }
}
