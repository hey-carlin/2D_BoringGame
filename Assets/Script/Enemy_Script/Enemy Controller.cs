using UnityEngine;

namespace Enemy
{
    public class EnemyController : MonoBehaviour
    {
        [Header("Data")]
        public EnemyData data;   // ✅ Inspector 挂载

        [Header("Components")]
        public Animator animator;
        public EnemyMovement movement;
        public EnemyAttack attack;
        public Transform player;

        [HideInInspector] public int currentHealth;

        public IdleState idleState;
        public AlertState alertState;
        public WalkState walkState;
        public AttackState attackState;
        public DeadState deadState;

        [Header("Patrol")]
        public Transform patrolPointA;
        public Transform patrolPointB;
        internal Transform currentPatrolTarget;

        private StateMachine stateMachine;

        public GroundCheck groundChecker;

        private Vector2 patrolCenter;

        public bool IsGrounded =>
            groundChecker != null && groundChecker.IsGrounded;

        // 追击相关
        internal bool isChasing = false;
        internal float patrolRange = 0f; // 追击停止范围（玩家离开时停止追击）

        void Awake()
        {
            animator = GetComponent<Animator>();
            movement = GetComponent<EnemyMovement>();
            attack = GetComponent<EnemyAttack>();

            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player")?.transform;

            stateMachine = new StateMachine();

            idleState = new IdleState(this, stateMachine);
            alertState = new AlertState(this, stateMachine);
            walkState = new WalkState(this, stateMachine);
            attackState = new AttackState(this, stateMachine);
            deadState = new DeadState(this, stateMachine);

            stateMachine.AddState(idleState);
            stateMachine.AddState(alertState);
            stateMachine.AddState(walkState);
            stateMachine.AddState(attackState);
            stateMachine.AddState(deadState);
        }

        void Update()
        {
            if (player == null) return;

            float dist = Vector2.Distance(transform.position, player.position);
            Debug.Log($"[{gameObject.name}] Dist: {dist}, Sight: {data.sightRange}, Attack: {data.attackRange}, Chasing: {isChasing}");

            // 每帧让状态机更新
            stateMachine.OnUpdate();

            // 检测玩家进入视野并位于巡逻区域内才会追击
            if (!isChasing && IsPlayerInSight() && IsPlayerInsidePatrolArea())
            {
                StartChase();
            }

            // 如果正在追击，检测玩家是否已经超出巡逻/追击范围或离开巡逻区域，超出则停止追击
            if (isChasing)
            {
                float d = Vector2.Distance(transform.position, player.position);
                if (d > patrolRange || !IsPlayerInsidePatrolArea())
                {
                    StopChase();
                }
            }
        }

        void FixedUpdate()
        {
            stateMachine.OnFixedUpdate();
        }

        void Start()
        {
            InitFromData();
            stateMachine.ChangeState(walkState);
        }

        private void InitFromData()
        {
            currentHealth = data.maxHealth;

            movement.Init(data.moveSpeed, data.sightRange);
            attack.Init(data.damage, data.attackRange);

            // patrolRange 使用 sightRange 的倍数作为回退值（如果你在 EnemyData 中后续添加专门字段可替换）
            patrolRange = data.sightRange * 2f;

            // 记录巡逻中心点（用于无明确 patrolPoint 时的范围判断）
            patrolCenter = transform.position;

            // 初始化巡逻点（若存在）
            if (patrolPointA != null)
                currentPatrolTarget = patrolPointA;
            else if (patrolPointB != null)
                currentPatrolTarget = patrolPointB;
        }

        public void StartChase()
        {
            isChasing = true;
            // 立即面向玩家，然后进入待机/警戒状态
            if (player != null)
                movement.FaceDirection((player.position - transform.position).normalized);
            stateMachine.ChangeState(alertState);
        }

        public void StopChase()
        {
            isChasing = false;
            movement.StopMoving();
            stateMachine.ChangeState(walkState);
        }

        // 判断玩家是否在敌人的巡逻区域内
        public bool IsPlayerInsidePatrolArea()
        {
            if (player == null) return false;

            if (patrolPointA != null && patrolPointB != null)
            {
                float minX = Mathf.Min(patrolPointA.position.x, patrolPointB.position.x);
                float maxX = Mathf.Max(patrolPointA.position.x, patrolPointB.position.x);
                float px = player.position.x;
                return px >= minX && px <= maxX;
            }
            else
            {
                return Vector2.Distance(patrolCenter, player.position) <= patrolRange;
            }
        }

        public bool IsPlayerInAttackRange()
        {
            return Vector2.Distance(transform.position, player.position)
                 <= data.attackRange;
        }

        public bool IsPlayerInSight()
        {
            return Vector2.Distance(transform.position, player.position)
                 <= data.sightRange;
        }

        public void TakeDamage(int amount)
        {
            currentHealth -= amount;
            if (currentHealth <= 0)
                Die();
            else
            {
                // 受击时也可以开启追击
                if (player != null) StartChase();
            }
        }
        // 新增方法：切换到下一个 patrol 目标
        public void SwitchToNextPatrolTarget()
        {
            if (patrolPointA == null || patrolPointB == null) return;
            currentPatrolTarget = currentPatrolTarget == patrolPointA ? patrolPointB : patrolPointA;
        }

        public void Die()
        {
            stateMachine.ChangeState(deadState);
        }
    }
}