using UnityEngine;

/// <summary>
/// 地面检测组件：从角色脚部向下发射射线，检测是否在地面上。
/// </summary>
public class GroundCheck : MonoBehaviour
{
    [Header("射线设置")]
    public Vector2 rayOffset = new Vector2(0, -0.5f); // 射线起点偏移（相对于角色位置）
    public float rayLength = 0.1f;                   // 射线长度
    public LayerMask groundLayer;                    // 地面层

    [Header("调试")]
    public bool drawDebug = true;

    /// <summary>
    /// 是否在地面上
    /// </summary>
    public bool IsGrounded
    {
        get
        {
            Vector2 origin = (Vector2)transform.position + rayOffset;
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, rayLength, groundLayer);
            return hit.collider != null;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawDebug) return;
        Vector2 origin = (Vector2)transform.position + rayOffset;
        Vector2 end = origin + Vector2.down * rayLength;
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(origin, end);
        Gizmos.DrawWireSphere(end, 0.05f);
    }
#endif
}