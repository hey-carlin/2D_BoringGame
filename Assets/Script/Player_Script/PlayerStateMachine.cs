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

        [Header("Attack / Combo")]
        public int maxCombo = 3;
        public float comboWindow = 0.6f;
        public float requiredChargeTime = 0.6f;

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

        private int comboStep = 0;
        private float comboTimer = 0f;
        private bool isChargingHeavy = false;
        private float chargeTimer = 0f;

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

            bool jumpPressed = Input.GetButtonDown("Jump");
            bool attackPressed = Input.GetKeyDown(KeyCode.J);
            bool attackReleased = Input.GetKeyUp(KeyCode.J);
            bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (attackPressed && shiftHeld)
            {
                StartChargingHeavy();
            }
            else if (attackPressed)
            {
                HandleAttackInput();
            }

            if (isChargingHeavy)
            {
                chargeTimer += Time.deltaTime;
                if (attackReleased)
                    ReleaseHeavy();
            }

            if (comboStep > 0)
            {
                comboTimer -= Time.deltaTime;
                if (comboTimer <= 0f)
                {
                    comboStep = 0;
                    animator.SetInteger(ParamAttackID, 0);
                    if (isGrounded)
                        ChangeState(idleState);
                }
            }

            if (currentState != attackState && currentState != hitState && currentState != deadState && currentState != jumpState)
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
                rb.velocity = new Vector2(horizontal * moveSpeed, rb.velocity.y);
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

        public void SetJumpTrigger()
        {
            animator.SetTrigger(ParamJump);
        }

        private void HandleAttackInput()
        {
            if (currentState == deadState) return;
            if (isChargingHeavy) return;

            if (comboStep == 0)
            {
                comboStep = 1;
                comboTimer = comboWindow;
                animator.SetInteger(ParamAttackID, comboStep);
                ChangeState(attackState);
            }
            else if (comboTimer > 0f)
            {
                comboStep = Mathf.Min(comboStep + 1, maxCombo);
                comboTimer = comboWindow;
                animator.SetInteger(ParamAttackID, comboStep);
                ChangeState(attackState);
            }
        }

        private void StartChargingHeavy()
        {
            if (currentState == deadState) return;
            isChargingHeavy = true;
            chargeTimer = 0f;
            animator.SetBool(ParamIsDrawing, true);
            ChangeState(attackState);
        }

        private void ReleaseHeavy()
        {
            if (!isChargingHeavy) return;
            isChargingHeavy = false;
            animator.SetBool(ParamIsDrawing, false);

            if (chargeTimer >= requiredChargeTime)
            {
                animator.SetTrigger(ParamHeavyAttack);
                ChangeState(attackState);
                StartCoroutine(DoSaveSwordRoutine());
            }
            else
            {
                HandleAttackInput();
            }

            chargeTimer = 0f;
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
