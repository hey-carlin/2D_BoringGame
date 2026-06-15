using UnityEngine;

/// <summary>Boss2 血量（委托给 BossAI）</summary>
public class BossHealth : MonoBehaviour
{
    private BossAI ai;
    private void Awake() => ai = GetComponent<BossAI>();
    public void TakeDamage(int damage) => ai?.TakeDamage(damage);
}
