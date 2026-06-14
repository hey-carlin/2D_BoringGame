using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// 攻击状态：
    /// - windup 阶段：前摇 telegraph（抬手/蓄力），可在此期间躲开
    /// - 命中帧：PerformAttack 判定伤害
    /// - 冷却由 Controller 的 attackCooldownTimer 管理
    /// </summary>
    public class AttackState : EnemyState
    {
        private float timer;
        private bool dealtDamage;

        // 子阶段
        private enum Phase { Windup, Active, Recovery }
        private Phase phase;

        public AttackState(EnemyController enemy, StateMachine stateMachine)
            : base(enemy, stateMachine) { }

        public override void OnEnter()
        {
            timer = 0f;
            dealtDamage = false;
            phase = Phase.Windup;

            enemy.movement.StopMoving();
            enemy.animator.Play("Attack");
        }

        public override void OnUpdate()
        {
            timer += Time.deltaTime;

            switch (phase)
            {
                case Phase.Windup:
                    // 前摇阶段：看着玩家（面朝方向跟踪）
                    FacePlayer();
                    if (timer >= enemy.attack.windup)
                    {
                        phase = Phase.Active;
                        timer = 0f;
                    }
                    break;

                case Phase.Active:
                    // 命中帧（第一帧触发）
                    if (!dealtDamage)
                    {
                        enemy.attack.PerformAttack();
                        dealtDamage = true;
                    }
                    // 短暂的后摇
                    if (timer >= 0.2f)
                    {
                        phase = Phase.Recovery;
                        timer = 0f;
                    }
                    break;

                case Phase.Recovery:
                    // 收招 → 回追击或巡逻
                    if (timer >= 0.15f)
                    {
                        if (enemy.IsPlayerInAggroRange())
                            stateMachine.ChangeState(enemy.chaseState);
                        else
                            stateMachine.ChangeState(enemy.walkState);
                    }
                    break;
            }
        }

        private void FacePlayer()
        {
            if (enemy.player == null) return;
            Vector2 toPlayer = enemy.player.position - enemy.transform.position;
            if (Mathf.Abs(toPlayer.x) > 0.01f)
                enemy.movement.FaceDirection(new Vector2(Mathf.Sign(toPlayer.x), 0f), true);
        }

        public override void OnExit()
        {
            enemy.movement.StopMoving();
        }
    }
}
