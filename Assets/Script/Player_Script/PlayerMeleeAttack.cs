using UnityEngine;
using System.Collections;
using Enemy; // IDamageable

namespace Player
{
    /// <summary>
    /// 玩家近战攻击组件。
    /// 挂载到玩家 GameObject 上，由 Animation Event 触发伤害判定。
    /// </summary>
    public class PlayerMeleeAttack : MonoBehaviour
    {
        [Header("伤害")]
        public int lightAttackDamage = 3;        // Attack1 / Attack2 / Attack3 伤害
        public int comboAttackDamage = 3;        // Combo 伤害（Attack1→2→3 连击终结）
        public int heavyAttackDamage = 5;        // Heavy_Attack 伤害

        [Header("检测")]
        public Transform attackPoint;            // 攻击判定中心点（放在玩家前方）
        public float attackRadius = 1.2f;        // 攻击判定半径
        public LayerMask enemyLayer;             // 敌人所在 Layer

        [Header("击退")]
        public float knockbackForce = 1.2f;      // 击退力度（像素级轻推）

        [Header("打击感")]
        public float hitstopDuration = 0.04f;    // 命中后停顿时间（秒）
        public float hitstopTimeScale = 0.05f;   // 停顿时的时间倍率

        [Header("调试")]
        public bool showDebugGizmos = true;

        private int pendingDamage;               // 当前一击的伤害值

        /// <summary>由 PlayerStateMachine 在触发攻击时调用，设定本次伤害</summary>
        public void SetPendingDamage(int damage)
        {
            pendingDamage = damage;
        }

        /// <summary>由 Animation Event 调用：攻击命中判定帧</summary>
        public void OnAttackHitFrame()
        {
            if (attackPoint == null)
            {
                Debug.LogWarning($"[PlayerMeleeAttack] attackPoint 未赋值！请在 Inspector 中设置。", this);
                return;
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, enemyLayer);

            if (hits.Length > 0)
            {
                // ── 命中停顿 ──
                StartCoroutine(HitstopRoutine());

                foreach (var hit in hits)
                {
                    // 优先 IDamageable（新敌人系统）
                    IDamageable damageable = hit.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        damageable.TakeDamage(pendingDamage);
                        continue;
                    }

                    // 回退旧的 AIStats
                    DungeonKIT.AIStats oldEnemy = hit.GetComponent<DungeonKIT.AIStats>();
                    if (oldEnemy != null)
                    {
                        oldEnemy.TakingDamage(pendingDamage);
                    }
                }

#if UNITY_EDITOR
                if (showDebugGizmos)
                    Debug.Log($"[PlayerMeleeAttack] 命中 {hits.Length} 个敌人，伤害={pendingDamage}");
#endif
            }
        }

        /// <summary>对命中的敌人施加轻击退（纯水平，像素级）</summary>
        private void ApplyKnockback(Collider2D enemyCollider)
        {
            Rigidbody2D enemyRb = enemyCollider.GetComponent<Rigidbody2D>();
            if (enemyRb == null) return;

            Vector2 dir = (enemyCollider.transform.position - transform.position).normalized;
            dir.y = 0f;  // 纯水平，不上挑
            enemyRb.AddForce(dir * knockbackForce, ForceMode2D.Force);  // Force 轻推，不用 Impulse 瞬击
        }

        /// <summary>打击停顿协程：瞬间慢动作</summary>
        private IEnumerator HitstopRoutine()
        {
            float originalScale = Time.timeScale;
            Time.timeScale = hitstopTimeScale;
            yield return new WaitForSecondsRealtime(hitstopDuration);
            Time.timeScale = originalScale;
        }

        /// <summary>由 Animation Event 调用：重击命中判定帧</summary>
        public void OnHeavyAttackHitFrame()
        {
            SetPendingDamage(heavyAttackDamage);
            OnAttackHitFrame();
        }

        /// <summary>由 Animation Event 调用：轻击1命中</summary>
        public void OnLightAttack1Hit()
        {
            SetPendingDamage(lightAttackDamage);
            OnAttackHitFrame();
        }

        /// <summary>由 Animation Event 调用：轻击2命中</summary>
        public void OnLightAttack2Hit()
        {
            SetPendingDamage(lightAttackDamage);
            OnAttackHitFrame();
        }

        /// <summary>由 Animation Event 调用：轻击3命中</summary>
        public void OnLightAttack3Hit()
        {
            SetPendingDamage(lightAttackDamage);
            OnAttackHitFrame();
        }

        /// <summary>由 Animation Event 调用：Combo 命中</summary>
        public void OnComboAttackHit()
        {
            SetPendingDamage(comboAttackDamage);
            OnAttackHitFrame();
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos || attackPoint == null) return;
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.7f);
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
#endif
    }
}
