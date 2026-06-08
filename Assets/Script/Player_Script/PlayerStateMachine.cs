using UnityEngine;

namespace Player
{
    public class PlayerStateMachine : MonoBehaviour
    {
        [Header("Components")]
        public Animator animator;
        public Rigidbody2D rb;

        [Header("Movement")]
        public float moveSpeed = 5f;
        public float jumpForce = 12f;

        [Header("Ground Check")]
        public Transform groundCheck;
        public float groundCheckRadius = 0.1f;
        public LayerMask groundLayer;

        [Header("Attack")]
        public float lightAttackTimeout = 0.35f;  // Attack1/2/3/Combo 攻击动画时长
        public float heavyHoldTime = 0.5f;         // 长按 H 触发重击的时间
        public float heavyCooldown = 2f;           // Heavy_Attack 冷却时间
        public float comboWindow = 5f;             // 连击储存后有效窗口
        [HideInInspector] public float activeAttackTimeout = 0.35f;  // 当前攻击的时长（进入 AttackState 前设置）

        public bool controlsEnabled = true;
        public float horizontal;
        public bool isGrounded;

        public PlayerState currentState;

        public IdleState idleState;
        public RunState runState;
        public JumpState jumpState;
        public AttackState attackState;
        public HitState hitState;
        public DeadState deadState;

        private float hKeyHoldTimer = 0f;           // H 键长按计时
        private bool isHeavyCharging = false;       // 是否正在蓄力重击
        private float heavyCooldownTimer = 0f;      // Heavy 冷却倒计时
        private int attackSequence = 0;             // 0=无, 1=已完成Attack1, 2=已完成Attack2
        private int comboStored = 0;                // 0=无, 1=连击已储存
        private float comboStoreTimer = 0f;         // 连击储存倒计时

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

            idleState = new IdleState(this);
            runState = new RunState(this);
            jumpState = new JumpState(this);
            attackState = new AttackState(this);
            hitState = new HitState(this);
            deadState = new DeadState(this);
        }

        void Start()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (animator == null) animator = GetComponent<Animator>();
            ChangeState(idleState);
        }

        void Update()
        {
            if (!controlsEnabled) return;

            horizontal = Input.GetAxisRaw("Horizontal");
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;

            animator.SetFloat(ParamSpeed, Mathf.Abs(horizontal));
            animator.SetBool(ParamIsGrounded, isGrounded);

            // ──── 计时器更新 ────

            if (heavyCooldownTimer > 0f)
                heavyCooldownTimer -= Time.deltaTime;

            if (comboStored > 0)
            {
                comboStoreTimer -= Time.deltaTime;
                if (comboStoreTimer <= 0f)
                {
                    comboStored = 0;        // 过期
                    attackSequence = 0;
                }
            }

            // ──── 输入读取 ────

            bool jumpPressed = Input.GetButtonDown("Jump");
            bool jPressed = Input.GetKeyDown(KeyCode.J);
            bool wHeld = Input.GetKey(KeyCode.W);
            bool sHeld = Input.GetKey(KeyCode.S);
            bool hHeld = Input.GetKey(KeyCode.H);

            // ═══════════════════════════════════════
            // 攻击输入处理
            // ═══════════════════════════════════════

            if (jPressed && currentState != deadState)
            {
                // J 按下时取消 H 蓄力
                isHeavyCharging = false;
                hKeyHoldTimer = 0f;

                if (sHeld && !wHeld)
                {
                    // S+J → Attack3
                    // 只有当序列为 2（完成 Attack1→Attack2）时，Attack3 才完成连击
                    if (attackSequence == 2)
                    {
                        comboStored = 1;
                        comboStoreTimer = comboWindow;
                        attackSequence = 0;
                    }
                    else
                    {
                        attackSequence = 0;  // 序列断裂
                    }
                    activeAttackTimeout = lightAttackTimeout;
                    animator.SetInteger(ParamAttackID, 3);
                    ChangeState(attackState);
                }
                else if (wHeld && !sHeld)
                {
                    // W+J → Attack2
                    if (attackSequence == 1)
                        attackSequence = 2;  // 接上 Attack1 的序列
                    else
                        attackSequence = 0;  // 序列断裂
                    activeAttackTimeout = lightAttackTimeout;
                    animator.SetInteger(ParamAttackID, 2);
                    ChangeState(attackState);
                }
                else
                {
                    // J → 如果有储存 Combo 则触发，否则 Attack1
                    if (comboStored > 0)
                    {
                        // 触发 Combo！
                        comboStored = 0;
                        comboStoreTimer = 0f;
                        attackSequence = 0;
                        activeAttackTimeout = lightAttackTimeout;
                        animator.SetInteger(ParamAttackID, 4);
                        ChangeState(attackState);
                    }
                    else
                    {
                        // Attack1 — 开始新序列
                        attackSequence = 1;
                        activeAttackTimeout = lightAttackTimeout;
                        animator.SetInteger(ParamAttackID, 1);
                        ChangeState(attackState);
                    }
                }
            }
            // H 蓄力重击（冷却中或攻击中不可用）
            else if (hHeld && heavyCooldownTimer <= 0f
                     && currentState != attackState && currentState != deadState)
            {
                if (!isHeavyCharging)
                {
                    isHeavyCharging = true;
                    hKeyHoldTimer = 0f;
                }

                hKeyHoldTimer += Time.deltaTime;

                if (hKeyHoldTimer >= heavyHoldTime)
                {
                    isHeavyCharging = false;
                    hKeyHoldTimer = 0f;

                    // 启动冷却
                    heavyCooldownTimer = heavyCooldown;
                    activeAttackTimeout = heavyCooldown;
                    // 重置序列（Heavy 打断连击累计）
                    attackSequence = 0;
                    comboStored = 0;
                    comboStoreTimer = 0f;

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
            // 移动 / 跳跃状态切换
            // ═══════════════════════════════════════

            if (currentState != attackState && currentState != hitState
                && currentState != deadState && currentState != jumpState)
            {
                if (jumpPressed && isGrounded)
                {
                    ChangeState(jumpState);
                }
                else if (Mathf.Abs(horizontal) > 0.1f)
                {
                    ChangeState(runState);
                }
                else
                {
                    ChangeState(idleState);
                }
            }

            currentState?.OnUpdate();
        }

        void FixedUpdate()
        {
            if (!controlsEnabled) return;

            currentState?.OnFixedUpdate();

            if (currentState != attackState && currentState != hitState && currentState != deadState && currentState != jumpState)
            {
                // H 蓄力期间禁止水平移动
                float moveX = isHeavyCharging ? 0f : horizontal;
                rb.velocity = new Vector2(moveX * moveSpeed, rb.velocity.y);
            }

            if (horizontal > 0.1f && transform.localScale.x < 0f)
                Flip();
            else if (horizontal < -0.1f && transform.localScale.x > 0f)
                Flip();
        }

        public void OnHit()
        {
            ChangeState(hitState);
        }

        public void Die()
        {
            controlsEnabled = false;
            ChangeState(deadState);
            animator.SetTrigger(ParamDeath);
        }

        public void AttackFinished()
        {
            if (isGrounded)
                ChangeState(idleState);
        }

        // Animation event receiver: called at the end of attack animations
        public void OnAttackAnimationEnd()
        {
            // Delegate to the existing finish handler so both PlayerController and PlayerStateMachine
            // can receive the same AnimationEvent from the Animator.
            AttackFinished();
        }

        // Animation event receiver: called at the end of heavy attack animation
        public void OnHeavyAttackEnd()
        {
            if (isGrounded) ChangeState(idleState);
        }

        public void SetJumpTrigger()
        {
            animator.SetTrigger(ParamJump);
        }

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
            currentState?.OnExit();
            currentState = newState;
            currentState?.OnEnter();
        }
    }
}
