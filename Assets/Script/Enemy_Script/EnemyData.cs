using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Enemy/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("�ƶ�")]
    public float moveSpeed = 2f;
    public float sightRange = 10f;
    public float idleDuration = 2f;
    public float alertDuration = 0.5f;
    public float walkDuration = 3f;

    [Header("����")]
    public int damage = 10;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.2f;
    public float attackHitTime = 0.3f;   // ����������ʼ�����������˺�

    [Header("����")]
    public int maxHealth = 50;
}