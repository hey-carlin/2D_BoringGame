using System.Collections.Generic;

namespace Enemy
{
    public class StateMachine
    {
        private EnemyState currentState;
        private Dictionary<System.Type, EnemyState> states = new();

        public void AddState(EnemyState state)
        {
            states[state.GetType()] = state;
        }

        public void ChangeState(EnemyState newState)
        {
            if (currentState == newState) return;
            currentState?.OnExit();
            currentState = newState;
            currentState.OnEnter();
        }

        public void ChangeState<T>() where T : EnemyState
        {
            if (states.TryGetValue(typeof(T), out var state))
                ChangeState(state);
        }

        public void OnUpdate()
        {
            currentState?.OnUpdate();
        }

        public void OnFixedUpdate()
        {
            currentState?.OnFixedUpdate();
        }

        public EnemyState GetCurrentState()
        {
            return currentState;
        }
    }
}