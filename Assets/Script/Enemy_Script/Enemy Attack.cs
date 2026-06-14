using UnityEngine;

namespace Enemy
{
    public class EnemyAttack : MonoBehaviour
    {
        public int damage { get; private set; }
        public float attackRange { get; private set; }
        public float windup { get; private set; }

        public Transform attackPoint;
        public LayerMask playerLayer;

        public void Init(int dmg, float range, float windupTime)
        {
            damage = dmg;
            attackRange = range;
            windup = windupTime;
        }

        /// <summary>执行攻击判定（由 AttackState 在命中帧调用）</summary>
        public void PerformAttack()
        {
            if (attackPoint == null) return;

            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, playerLayer);

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<IDamageable>(out var damageable))
                    damageable.TakeDamage(damage);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (attackPoint == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}
