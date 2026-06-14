using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Enemy/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("移动")]
    public float moveSpeed = 2f;              // 巡逻移速
    public float chaseSpeed = 3.5f;           // 追击移速（比巡逻快）

    [Header("巡逻")]
    public float minWalkDuration = 2f;        // 随机行走最短时长
    public float maxWalkDuration = 5f;        // 随机行走最长时长
    public float minIdleDuration = 0.8f;      // 随机待机最短时长
    public float maxIdleDuration = 2f;        // 随机待机最长时长

    [Header("检测")]
    public float aggroRadius = 6f;            // 圆形索敌半径（空洞骑士风格）
    public float loseAggroRadius = 10f;       // 脱战距离（大于索敌，避免边缘抖动）
    public float alertDuration = 0.4f;        // 发现玩家后"！"停顿时间

    [Header("攻击")]
    public int damage = 10;                   // 攻击伤害
    public float attackRange = 1.5f;          // 攻击判定距离
    public float attackWindup = 0.35f;        // 攻击前摇（抬手/蓄力 telegraph）
    public float attackCooldown = 1.2f;       // 两次攻击之间的冷却

    [Header("生命")]
    public int maxHealth = 50;

    [Header("受伤")]
    public float hurtDuration = 0.3f;         // 受伤硬直时长
    public float knockbackForce = 4f;         // 受击击退力
    public float hurtFlashInterval = 0.06f;   // 受伤闪烁频率

    [Header("死亡")]
    public float deathDestroyDelay = 2f;      // 死亡后多久开始渐隐销毁
    public float deathFadeDuration = 1.5f;    // 死亡渐隐时长
    public float respawnDelay = 60f;          // 死亡后多久重新生成（秒）
}
