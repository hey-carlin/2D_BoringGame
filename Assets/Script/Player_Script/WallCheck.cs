using UnityEngine;

/// <summary>
/// 墙面检测组件：在角色左右发射盒子（BoxCast）检测是否靠墙。
/// - 将此组件挂在 Player（或子物体）上，设置好检测起点、盒子大小和层级（wallLayer）。
/// - 提供只读属性 IsWallLeft / IsWallRight 供外部查询。
/// </summary>
[AddComponentMenu("Player/WallCheck")]
public class WallCheck : MonoBehaviour
{
    [Header("Wall Check")]
    public Transform origin;              // 盒子检测的起点（为空则使用当前 transform）
    public Vector2 boxSize = new Vector2(0.8f, 1.2f);  // 检测盒子的大小（宽≈角色宽度，高≈角色高度）
    public float checkDistance = 0.2f;    // 向前检测的距离
    public LayerMask wallLayer;           // 判定为墙的层

    [Header("调试")]
    public bool drawDebug = true;
    public Color debugColorLeft = Color.cyan;
    public Color debugColorRight = Color.magenta;

    public bool IsWallLeft
    {
        get
        {
            Vector2 center = origin != null ? (Vector2)origin.position : (Vector2)transform.position;
            Vector2 direction = Vector2.left;
            float distance = checkDistance;
            RaycastHit2D hit = Physics2D.BoxCast(center, boxSize, 0f, direction, distance, wallLayer);
            return hit.collider != null;
        }
    }

    public bool IsWallRight
    {
        get
        {
            Vector2 center = origin != null ? (Vector2)origin.position : (Vector2)transform.position;
            Vector2 direction = Vector2.right;
            float distance = checkDistance;
            RaycastHit2D hit = Physics2D.BoxCast(center, boxSize, 0f, direction, distance, wallLayer);
            return hit.collider != null;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawDebug) return;
        Vector2 center = origin != null ? (Vector2)origin.position : (Vector2)transform.position;

        // 左侧检测盒子
        Vector2 leftBoxCenter = center + Vector2.left * (checkDistance * 0.5f);
        Gizmos.color = IsWallLeft ? debugColorLeft : Color.gray;
        Gizmos.DrawWireCube(leftBoxCenter, boxSize);

        // 右侧检测盒子
        Vector2 rightBoxCenter = center + Vector2.right * (checkDistance * 0.5f);
        Gizmos.color = IsWallRight ? debugColorRight : Color.gray;
        Gizmos.DrawWireCube(rightBoxCenter, boxSize);
    }
#endif
}