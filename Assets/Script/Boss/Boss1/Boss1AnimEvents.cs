using UnityEngine;

/// <summary>Boss1 动画事件桥接</summary>
public class Boss1AnimEvents : MonoBehaviour
{
    private Boss1AI ai;
    private void Start() => ai = GetComponent<Boss1AI>();

    /// <summary>攻击判定帧（B1_Attack 动画事件）</summary>
    public void OnAttackHit()
    {
        ai?.SendMessage("DoAttackDamage", SendMessageOptions.DontRequireReceiver);
    }
}
