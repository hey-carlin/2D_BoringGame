using UnityEngine;

using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Enemy/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("移动")]
    public float moveSpeed = 2f;
    public float sightRange = 10f;
    public float chaseRange = 8f;          // 追击检测线长度
    public float chaseHeight = 2f;         // 检测线垂直高度容差
    public float chaseYOffset = 0f;        // 检测线纵向偏移
    public float idleDuration = 2f;
    public float alertDuration = 0.5f;
    public float walkDuration = 3f;

    [Header("攻击")]
    public int damage = 10;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.2f;
    public float attackHitTime = 0.3f;   // 攻击开始后多久判定命中

    [Header("生命")]
    public int maxHealth = 50;
}