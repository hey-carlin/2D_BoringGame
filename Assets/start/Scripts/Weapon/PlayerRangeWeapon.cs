using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Enemy; // IDamageable 接口

namespace DungeonKIT
{
    public class PlayerRangeWeapon : RangeWeapon
    {

        public override void OnTriggerEnter2D(Collider2D collider)
        {
            base.OnTriggerEnter2D(collider);

            if (collider.gameObject.tag == "Enemy") //if contact with enemy
            {
                // ── 优先使用新版 IDamageable 接口 ──
                IDamageable damageable = collider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(Mathf.RoundToInt(damageRange.RandomFloat()));
                    Destroying();
                    return;
                }

                // ── 回退到旧版 AIStats ──
                AIStats enemy = collider.gameObject.GetComponent<AIStats>();
                if (enemy != null)
                {
                    Damage(enemy);
                }
            }
        }

        //Damage method (旧版兼容)
        void Damage(AIStats enemy)
        {
            enemy.TakingDamage(damageRange.RandomFloat());
            Destroying();
        }
    }
}
