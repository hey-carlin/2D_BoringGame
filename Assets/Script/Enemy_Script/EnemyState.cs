using UnityEngine;

namespace Enemy
{
    public abstract class EnemyState
    {
        protected EnemyController enemy;
        protected StateMachine stateMachine;

        public EnemyState(EnemyController enemy, StateMachine stateMachine)
        {
            this.enemy = enemy;
            this.stateMachine = stateMachine;
        }

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual void OnUpdate() { }
        public virtual void OnFixedUpdate() { }
    }
}