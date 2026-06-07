using UnityEngine;
using Enemy;

namespace Enemy
{
    public class EnemyAttack : MonoBehaviour
    {
        public int damage { get; private set; }
        public float attackRange { get; private set; }

        public Transform attackPoint;
        public LayerMask playerLayer;

        void Update()
        {
            if (attackPoint == null) return;

            // ✅ 根据朝向画攻击范围
            Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            Debug.DrawRay(attackPoint.position, dir * attackRange, Color.red);
        }

        public void Init(int dmg, float range)
        {
            damage = dmg;
            attackRange = range;
        }

        public void PerformAttack()
        {
            Collider2D[] hits =
                Physics2D.OverlapCircleAll(attackPoint.position, attackRange, playerLayer);

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(damage);
                }
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