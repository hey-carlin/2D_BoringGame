using UnityEngine;

namespace Enemy
{
    public class EnemyController : MonoBehaviour, IDamageable
    {
        [Header("Data")]
        public EnemyData data;

        [Header("Components")]
        public Animator animator;
        public EnemyMovement movement;
        public EnemyAttack attack;
        public Transform player;

        [HideInInspector] public int currentHealth;

        // ── 状态实例 ──
        public WalkState walkState;
        public ChaseState chaseState;
        public AttackState attackState;
        public HurtState hurtState;
        public DeadState deadState;

        private StateMachine stateMachine;
        internal int patrolFacing = 1;

        // ── 内部计时 / 标记 ──
        private float attackCooldownTimer;          // 攻击冷却倒计时
        private float alertTimer;                  // 发现玩家后的"！"停顿
        private bool isAlerted;                    // 是否已进入警觉
        private Vector3 spawnPosition;             // 初始生成位置（用于重生）

        // ── 事件 ──
        public System.Action<int> OnDamaged;
        public System.Action OnDied;

        void Awake()
        {
            animator = GetComponent<Animator>();
            movement = GetComponent<EnemyMovement>();
            attack = GetComponent<EnemyAttack>();

            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player")?.transform;

            stateMachine = new StateMachine();

            walkState   = new WalkState(this, stateMachine);
            chaseState  = new ChaseState(this, stateMachine);
            attackState = new AttackState(this, stateMachine);
            hurtState   = new HurtState(this, stateMachine);
            deadState   = new DeadState(this, stateMachine);

            stateMachine.AddState(walkState);
            stateMachine.AddState(chaseState);
            stateMachine.AddState(attackState);
            stateMachine.AddState(hurtState);
            stateMachine.AddState(deadState);
        }

        void Start()
        {
            spawnPosition = transform.position;   // 记录初始位置
            InitFromData();
            stateMachine.ChangeState(walkState);
        }

        void Update()
        {
            if (player == null) return;

            var cur = stateMachine.GetCurrentState();
            if (cur == deadState || cur == hurtState) return;

            // ── 计时器 ──
            if (attackCooldownTimer > 0f)
                attackCooldownTimer -= Time.deltaTime;

            if (alertTimer > 0f)
                alertTimer -= Time.deltaTime;

            float dist = DistanceToPlayer();

            // 超出脱战范围 → 重置警觉
            if (dist > data.loseAggroRadius)
                isAlerted = false;

            // Alert 停顿期间暂停状态切换（但计时器继续走）
            if (alertTimer > 0f)
                return;

            // ═══════════════════════════════════════
            // 状态切换（优先级从高到低）
            // ═══════════════════════════════════════

            // 1. 攻击：在攻击范围内 + 冷却完毕
            if (dist <= data.attackRange && attackCooldownTimer <= 0f
                && cur != attackState)
            {
                attackCooldownTimer = data.attackCooldown;
                stateMachine.ChangeState(attackState);
            }
            // 2. 追击：在索敌范围内
            else if (dist <= data.aggroRadius
                     && cur != attackState && cur != chaseState)
            {
                // 首次发现 → Alert 停顿
                if (!isAlerted && dist > data.attackRange)
                {
                    isAlerted = true;
                    alertTimer = data.alertDuration;
                    movement.StopMoving();
                    animator.Play("Idle");
                }
                else
                {
                    stateMachine.ChangeState(chaseState);
                }
            }
            // 3. 脱战：超出索敌 + 正在追击 → 回巡逻
            else if (dist > data.aggroRadius && cur == chaseState)
            {
                stateMachine.ChangeState(walkState);
            }

            stateMachine.OnUpdate();
        }

        void FixedUpdate()
        {
            stateMachine.OnFixedUpdate();
        }

        // ──────────────────────── 初始化 ────────────────────────

        private void InitFromData()
        {
            currentHealth = data.maxHealth;
            movement.Init(data.moveSpeed, data.chaseSpeed);
            attack.Init(data.damage, data.attackRange, data.attackWindup);
        }

        // ──────────────────────── 检测 ────────────────────────

        public float DistanceToPlayer()
        {
            if (player == null) return float.MaxValue;
            return Vector2.Distance(transform.position, player.position);
        }

        public bool IsPlayerInAttackRange()
        {
            return DistanceToPlayer() <= data.attackRange;
        }

        public bool IsPlayerInAggroRange()
        {
            return DistanceToPlayer() <= data.aggroRadius;
        }

        // ──────────────────────── IDamageable ────────────────────────

        public void TakeDamage(int amount)
        {
            if (stateMachine.GetCurrentState() == deadState) return;

            currentHealth -= amount;
            OnDamaged?.Invoke(currentHealth);

            // 被打中时进入警觉
            isAlerted = true;

            // 转向攻击者方向
            if (player != null)
            {
                Vector2 toPlayer = player.position - transform.position;
                if (Mathf.Abs(toPlayer.x) > 0.01f)
                    movement.FaceDirection(new Vector2(Mathf.Sign(toPlayer.x), 0f), true);
            }

            if (currentHealth <= 0)
                Die();
            else
                stateMachine.ChangeState(hurtState);
        }

        public void Die()
        {
            OnDied?.Invoke();
            stateMachine.ChangeState(deadState);
            StartCoroutine(RespawnRoutine());
        }

        /// <summary>死亡后等待 → 原地重生</summary>
        private System.Collections.IEnumerator RespawnRoutine()
        {
            // 等待死亡动画 + 渐隐完成
            yield return new WaitForSeconds(data.deathDestroyDelay + 0.3f);

            // 隐藏（GameObject 不关，只是看不到）
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
            foreach (var col in GetComponentsInChildren<Collider2D>())
                col.enabled = false;

            movement.StopMoving();

            // 等待重生时间
            yield return new WaitForSeconds(data.respawnDelay);

            // ── 重生 ──
            transform.position = spawnPosition;
            currentHealth = data.maxHealth;
            isAlerted = false;
            alertTimer = 0f;
            attackCooldownTimer = 0f;
            patrolFacing = 1;

            // 恢复渲染和碰撞
            if (sr != null) sr.enabled = true;
            foreach (var col in GetComponentsInChildren<Collider2D>())
                col.enabled = true;

            // 重置 Rigidbody2D
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.simulated = true;
            }

            stateMachine.ChangeState(walkState);
        }
    }
}
