using UnityEngine;

namespace Enemy
{
    public class EnemyController : MonoBehaviour
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
        public IdleState idleState;
        public WalkState walkState;
        public ChaseState chaseState;
        public AttackState attackState;
        public DeadState deadState;

        private StateMachine stateMachine;
        internal int patrolFacing = 1;                   // 巡逻朝向（1=右, -1=左），保证跨状态方向一致

        void Awake()
        {
            animator = GetComponent<Animator>();
            movement = GetComponent<EnemyMovement>();
            attack = GetComponent<EnemyAttack>();

            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player")?.transform;

            stateMachine = new StateMachine();

            idleState = new IdleState(this, stateMachine);
            walkState = new WalkState(this, stateMachine);
            chaseState = new ChaseState(this, stateMachine);
            attackState = new AttackState(this, stateMachine);
            deadState = new DeadState(this, stateMachine);

            stateMachine.AddState(idleState);
            stateMachine.AddState(walkState);
            stateMachine.AddState(chaseState);
            stateMachine.AddState(attackState);
            stateMachine.AddState(deadState);
        }

        void Start()
        {
            InitFromData();
            stateMachine.ChangeState(walkState);
        }

        void Update()
        {
            if (player == null) return;

            var cur = stateMachine.GetCurrentState();

            // ═══════════════════════════════════════
            // 根据玩家距离自动切换状态
            // ═══════════════════════════════════════

            // 1. 攻击范围内 → 攻击（最高优先级）
            if (IsPlayerInAttackRange() && cur != attackState && cur != deadState)
            {
                stateMachine.ChangeState(attackState);
            }
            // 2. 玩家在追击检测线上 → 追击
            else if (IsPlayerOnChaseLine() && cur != attackState && cur != deadState && cur != chaseState)
            {
                stateMachine.ChangeState(chaseState);
            }
            // 3. 玩家离开检测线 → 恢复随机巡逻
            else if (!IsPlayerOnChaseLine() && cur == chaseState)
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
            movement.Init(data.moveSpeed, data.chaseRange);
            attack.Init(data.damage, data.attackRange);
        }

        // ──────────────────────── 检测方法 ────────────────────────

        /// <summary>玩家是否在攻击范围内</summary>
        public bool IsPlayerInAttackRange()
        {
            if (player == null) return false;
            return Vector2.Distance(transform.position, player.position) <= data.attackRange;
        }

        /// <summary>玩家是否在追击检测线上（前方扇形区域）</summary>
        public bool IsPlayerOnChaseLine()
        {
            if (player == null) return false;

            Vector2 delta = player.position - transform.position;

            // 垂直方向容差
            float verticalOffset = Mathf.Abs(delta.y - data.chaseYOffset);
            if (verticalOffset > data.chaseHeight * 0.5f) return false;

            // 必须在敌人前方
            float forwardSign = Mathf.Sign(movement.CurrentDirection.x);
            if (Mathf.Abs(forwardSign) < 0.1f)
                forwardSign = 1f;

            if (delta.x * forwardSign < 0f) return false;

            // 水平距离在追击范围内
            return Mathf.Abs(delta.x) <= data.chaseRange;
        }

        // ──────────────────────── 伤害 / 死亡 ────────────────────────

        public void TakeDamage(int amount)
        {
            currentHealth -= amount;
            if (currentHealth <= 0)
                Die();
        }

        public void Die()
        {
            stateMachine.ChangeState(deadState);
        }
    }
}
