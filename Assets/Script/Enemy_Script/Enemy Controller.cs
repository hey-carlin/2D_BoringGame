using System.Collections;
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

        public IdleState idleState;
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
        internal float patrolRange = 0f;
        internal bool detectionInProgress = false;

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
            attackState = new AttackState(this, stateMachine);
            deadState = new DeadState(this, stateMachine);

            stateMachine.AddState(idleState);
            stateMachine.AddState(walkState);
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

            float dist = Vector2.Distance(transform.position, player.position);
            Debug.Log($"[{name}] Dist:{dist}");

            stateMachine.OnUpdate();

            // 如果玩家触碰敌人前方的检测线并在巡逻区域内，则短暂播放 Idle 过渡后执行攻击
            var cur = stateMachine.GetCurrentState();
            if (IsPlayerOnChaseLine() && IsPlayerInsidePatrolArea())
            {
                if (cur != attackState && cur != deadState && !detectionInProgress)
                {
                    StartCoroutine(DetectionRoutine());
                }
            }
        }

        private IEnumerator DetectionRoutine()
        {
            detectionInProgress = true;

            float elapsed = 0f;
            while (elapsed < data.alertDuration)
            {
                if (player == null)
                {
                    detectionInProgress = false;
                    yield break;
                }

                // 检测期间朝玩家移动，缩短距离以触发攻击
                Vector2 dir = (player.position - transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                    movement.SetMoveDirection(dir.normalized);

                // 提前进入攻击范围则立即攻击
                if (IsPlayerInAttackRange())
                {
                    detectionInProgress = false;
                    stateMachine.ChangeState(attackState);
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // 警报时间结束，再次检查是否在攻击范围
            if (IsPlayerInAttackRange())
            {
                stateMachine.ChangeState(attackState);
            }
            else
            {
                var cur = stateMachine.GetCurrentState();
                if (cur != walkState)
                    stateMachine.ChangeState(walkState);
            }

            detectionInProgress = false;
        }

        void FixedUpdate()
        {
            stateMachine.OnFixedUpdate();
        }

        private void InitFromData()
        {
            currentHealth = data.maxHealth;
            movement.Init(data.moveSpeed, data.chaseRange);
            attack.Init(data.damage, data.attackRange);

            patrolRange = data.sightRange * 2f;
            patrolCenter = transform.position;

            if (patrolPointA != null)
                currentPatrolTarget = patrolPointA;
            else if (patrolPointB != null)
                currentPatrolTarget = patrolPointB;
        }

        // Deprecated chase methods removed; detection uses front-line and AlertState transition.

        public bool IsPlayerInsidePatrolArea()
        {
            if (player == null) return false;

            if (patrolPointA != null && patrolPointB != null)
            {
                float minX = Mathf.Min(patrolPointA.position.x, patrolPointB.position.x);
                float maxX = Mathf.Max(patrolPointA.position.x, patrolPointB.position.x);
                return player.position.x >= minX && player.position.x <= maxX;
            }

            return Vector2.Distance(patrolCenter, player.position) <= patrolRange;
        }

        public bool IsPlayerOnChaseLine()
        {
            if (player == null) return false;

            Vector2 delta = player.position - transform.position;
            float verticalOffset = Mathf.Abs(delta.y - data.chaseYOffset);
            if (verticalOffset > data.chaseHeight * 0.5f) return false;

            float forwardSign = Mathf.Sign(movement.CurrentDirection.x); // ✅ 修正
            if (Mathf.Abs(forwardSign) < 0.1f)
                forwardSign = 1f;

            if (delta.x * forwardSign < 0f) return false;
            return Mathf.Abs(delta.x) <= data.chaseRange;
        }

        public bool IsPlayerInAttackRange()
        {
            return Vector2.Distance(transform.position, player.position) <= data.attackRange;
        }

        public void TakeDamage(int amount)
        {
            currentHealth -= amount;
            if (currentHealth <= 0)
                Die();
            else
                StartCoroutine(DetectionRoutine());
        }

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