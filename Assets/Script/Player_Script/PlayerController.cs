using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public enum State { Idle =0, Run =1, Jump =2, Attack =3, Combo =4, Dead =5 }

    [Header("Components")]
    public Rigidbody2D rb;
    public Animator animator;

    [Header("Movement")]
    public float moveSpeed =5f;
    public float jumpForce =12f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius =0.1f;
    public LayerMask groundLayer;

    [Header("Attack / Combo")]
    public int maxCombo =3; // Attack1, Attack2, Attack3 -> Combo
    public float comboWindow =0.6f; // seconds to chain next attack

    [Header("Heavy Attack (charge)")]
    public float requiredChargeTime =0.6f; // how long to charge before releasing for heavy

    private State currentState = State.Idle;
    private float horizontal;
    private bool facingRight = true;

    // combo trackers
    private int comboStep =0; //0 means not attacking,1..maxCombo are Attack1..Attack3, maxCombo+1 can be Combo
    private float comboTimer =0f;

    // charging for heavy attack
    private bool isChargingHeavy = false;
    private float chargeTimer =0f;

    private bool weaponDrawn = false;

    private bool isGrounded => Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
    private bool controlsEnabled = true;

    // Animator parameter names (avoid magic strings in multiple places)
    private static readonly int ParamSpeed = Animator.StringToHash("Speed");
    private static readonly int ParamIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int ParamJump = Animator.StringToHash("Jump");
    private static readonly int ParamDeath = Animator.StringToHash("Death");
    private static readonly int ParamHit = Animator.StringToHash("Hit");
    private static readonly int ParamAttackID = Animator.StringToHash("AttackID");
    private static readonly int ParamIsDrawing = Animator.StringToHash("IsDrawing");
    private static readonly int ParamIsSaving = Animator.StringToHash("IsSaving");
    private static readonly int ParamHeavyAttack = Animator.StringToHash("Heavy_Attack");

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        ChangeState(State.Idle);
    }

    void Update()
    {
        if (!controlsEnabled) return;

        horizontal = Input.GetAxisRaw("Horizontal");

        // Update animator common parameters
        animator.SetFloat(ParamSpeed, Mathf.Abs(horizontal));
        animator.SetBool(ParamIsGrounded, isGrounded);

        // Jump input
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            DoJump();
        }

        // Attack input (J key)
        if (Input.GetKeyDown(KeyCode.J))
        {
            // start press - if Shift is held, start charging heavy
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                StartChargingHeavy();
            }
            else
            {
                // normal attack / combo
                HandleAttackInput();
            }
        }

        // Charging heavy: count while J is held
        if (isChargingHeavy)
        {
            chargeTimer += Time.deltaTime;
            // If player cancels by releasing Shift mid-charge, we'll still allow release by J
            if (Input.GetKeyUp(KeyCode.J))
            {
                ReleaseHeavy();
            }
            // If player releases Shift while still holding J, keep charging until J release
        }

        // If not charging and J was released (in case of accidental press), nothing to do

        // Combo timer countdown
        if (comboStep >0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <=0f)
            {
                // combo window expired
                comboStep =0;
                animator.SetInteger(ParamAttackID,0);
                if (isGrounded) ChangeState(State.Idle);
            }
        }

        // Running vs Idle when grounded and not attacking/jumping/dead
        if (currentState != State.Attack && currentState != State.Combo && currentState != State.Jump && currentState != State.Dead)
        {
            if (Mathf.Abs(horizontal) >0.1f)
                ChangeState(State.Run);
            else
                ChangeState(State.Idle);
        }
    }

    void FixedUpdate()
    {
        if (!controlsEnabled) return;

        // Move horizontally unless in dead state (attacking movement could be allowed by animation)
        if (currentState != State.Dead)
        {
            // allow movement while attacking if you want; here movement is allowed except when explicitly in attack/combo states
            if (currentState != State.Attack && currentState != State.Combo)
            {
                rb.velocity = new Vector2(horizontal * moveSpeed, rb.velocity.y);
            }

            if (horizontal >0.1f && !facingRight) Flip();
            else if (horizontal < -0.1f && facingRight) Flip();
        }
    }

    private void DoJump()
    {
        if (currentState == State.Dead) return;
        ChangeState(State.Jump);
        // apply upward force
        rb.velocity = new Vector2(rb.velocity.x,0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        animator.SetTrigger(ParamJump);
    }

    private void HandleAttackInput()
    {
        if (currentState == State.Dead) return;

        // If currently charging heavy, ignore normal
        if (isChargingHeavy) return;

        // If no active combo, start one
        if (comboStep ==0)
        {
            comboStep =1;
            comboTimer = comboWindow;
            ChangeState(State.Attack);
            animator.SetInteger(ParamAttackID, comboStep); // Attack1
        }
        else
        {
            // chain if within window
            if (comboTimer >0f)
            {
                if (comboStep <3)
                {
                    comboStep++;
                    comboTimer = comboWindow;
                    ChangeState(State.Combo);
                    animator.SetInteger(ParamAttackID, comboStep); // Attack2, Attack3
                }
                else if (comboStep ==3)
                {
                    // after Attack3 -> Combo
                    comboStep =4; // represent Combo state with4
                    comboTimer = comboWindow;
                    ChangeState(State.Combo);
                    animator.SetInteger(ParamAttackID, comboStep); // Combo animation
                }
            }
        }
    }

    private void StartChargingHeavy()
    {
        if (currentState == State.Dead) return;
        isChargingHeavy = true;
        chargeTimer =0f;
        // Play draw-sword / charging animation
        animator.SetBool(ParamIsDrawing, true);
        // Optionally change state to Attack to block other actions while charging
        ChangeState(State.Attack);
    }

    private void ReleaseHeavy()
    {
        if (!isChargingHeavy) return;
        isChargingHeavy = false;

        animator.SetBool(ParamIsDrawing, false);

        // If held long enough, do a heavy attack. Otherwise do a light attack (treat as normal attack)
        if (chargeTimer >= requiredChargeTime)
        {
            // Trigger heavy attack
            animator.SetTrigger(ParamHeavyAttack);
            // Optionally set a special AttackID value; animator can respond to Heavy_Attack trigger
            ChangeState(State.Attack);

            // After heavy attack, set saving animation briefly
            StartCoroutine(DoSaveSwordRoutine());
        }
        else
        {
            // treat as normal attack if released too early
            HandleAttackInput();
        }

        chargeTimer =0f;
    }

    private IEnumerator DoSaveSwordRoutine()
    {
        // Play saving animation flag briefly so animator can transition
        animator.SetBool(ParamIsSaving, true);
        yield return new WaitForSeconds(0.2f);
        animator.SetBool(ParamIsSaving, false);
    }

    // Animation event: called at the end of any attack animation (Attack1, Attack2, Attack3, Combo)
    public void OnAttackAnimationEnd()
    {
        // At end of attack animation we normally wait for next input; if combo window expired then clear
        if (comboStep >0)
        {
            // If combo timer expired, reset combo
            if (comboTimer <=0f)
            {
                comboStep =0;
                animator.SetInteger(ParamAttackID,0);
                if (isGrounded) ChangeState(State.Idle);
            }
            else
            {
                // Stay in attack/idle until next input; some setups reset AttackID to0 here to allow transitions
                animator.SetInteger(ParamAttackID,0);
                // Keep comboStep >0 to allow chaining while within window
                if (isGrounded) ChangeState(State.Idle);
            }
        }
        else
        {
            if (isGrounded) ChangeState(State.Idle);
        }
    }

    // Animation event: heavy attack animation end
    public void OnHeavyAttackEnd()
    {
        // heavy attack finished
        if (isGrounded) ChangeState(State.Idle);
    }

    // Called when character takes hit (can be invoked by game logic)
    public void OnHit()
    {
        if (currentState == State.Dead) return;
        animator.SetTrigger(ParamHit);
        // you could interrupt combo/charge
        comboStep =0;
        comboTimer =0f;
        isChargingHeavy = false;
        animator.SetBool(ParamIsDrawing, false);
        ChangeState(State.Idle);
    }

    // External call to kill the character
    public void Die()
    {
        if (currentState == State.Dead) return;
        ChangeState(State.Dead);
        controlsEnabled = false;
        rb.velocity = Vector2.zero;
        animator.SetTrigger(ParamDeath);
    }

    private void ChangeState(State newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        // enter-state logic can be added here if needed
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 s = transform.localScale;
        s.x *= -1f;
        transform.localScale = s;
    }

    // Debug drawing for ground check
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
