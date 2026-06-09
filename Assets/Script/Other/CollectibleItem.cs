using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 道具收集脚本：
/// - 宝箱打开后生成在宝箱内部，缓慢飞到指定位置并停住
/// - 玩家走到道具附近即可收集（可配置拾取范围和按键）
/// - 收集后触发 UnityEvent，缩放消失
///
/// 挂载到道具预制体上，由 Chest.SpawnItem() 调用 SetTargetPosition()
/// </summary>
public class CollectibleItem : MonoBehaviour
{
    [Header("道具信息")]
    public string itemName = "道具";
    public string itemID = "item_001";

    [Header("飞行（从宝箱 → 目标位置）")]
    public float flyDuration = 1.2f;                     // 飞行总时长（越长越慢，轨迹越明显）
    public AnimationCurve flyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);  // 飞行进度曲线
    public float arcHeight = 0.6f;                       // 飞行弧线高度（0=直线，正值=先上后下的抛物线）

    [Header("停留效果")]
    public float idleBobAmplitude = 0.15f;               // 停留时上下浮动幅度（0=不浮动）
    public float idleBobSpeed = 2f;                      // 浮动频率

    [Header("视觉")]
    public bool rotateDuringFloat = true;                // 飞行/停留时是否旋转
    public float rotateSpeed = 60f;                      // 旋转速度（度/秒）

    [Header("拾取")]
    public bool autoCollectOnProximity = true;            // 玩家靠近自动拾取
    public float collectRange = 0.8f;                    // 自动拾取距离
    public bool requirePressToCollect = false;            // 是否需要按键拾取
    public KeyCode collectKey = KeyCode.F;                // 拾取按键
    public bool destroyOnCollect = true;                 // 收集后销毁

    [Header("事件")]
    public UnityEvent<CollectibleItem> onCollected;      // 被收集时触发

    // ── 内部状态 ──
    private enum State { Flying, Idle, Collected }
    private State currentState = State.Flying;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float stateTimer;
    private float flyProgress;
    private Transform playerTransform;

    void Start()
    {
        startPosition = transform.position;

        // 如果还没设置目标位置，默认使用当前位置上方
        if (targetPosition == Vector3.zero)
            targetPosition = startPosition + Vector3.up * 1.5f;
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Flying:
                FlyUpdate();
                break;
            case State.Idle:
                IdleUpdate();
                break;
            case State.Collected:
                break;
        }

        // ── 旋转 ──
        if (rotateDuringFloat && currentState != State.Collected)
        {
            transform.Rotate(Vector3.forward, rotateSpeed * Time.deltaTime);
        }
    }

    // ──────────────────────── 飞行阶段 ────────────────────────

    void FlyUpdate()
    {
        stateTimer += Time.deltaTime;
        float t = Mathf.Clamp01(stateTimer / flyDuration);

        // 使用 AnimationCurve 控制飞行进度（支持 ease-out 减速）
        float curvedT = flyCurve.Evaluate(t);

        // 直线插值 + 弧线高度
        Vector3 flatPos = Vector3.Lerp(startPosition, targetPosition, curvedT);

        // 抛物线弧线：sin(t*PI) 在 t=0.5 达到峰值
        float arc = Mathf.Sin(curvedT * Mathf.PI) * arcHeight;
        flatPos.y += arc;

        transform.position = flatPos;

        if (t >= 1f)
        {
            // 到达目标位置
            transform.position = targetPosition;
            currentState = State.Idle;
            stateTimer = 0f;
        }
    }

    // ──────────────────────── 停留阶段 ────────────────────────

    void IdleUpdate()
    {
        // 上下浮动
        if (idleBobAmplitude > 0f)
        {
            float bob = Mathf.Sin(Time.time * idleBobSpeed) * idleBobAmplitude;
            transform.position = targetPosition + Vector3.up * bob;
        }

        // ── 自动拾取检测 ──
        if (autoCollectOnProximity)
        {
            if (playerTransform == null)
                TryFindPlayer();

            if (playerTransform != null)
            {
                float distance = Vector3.Distance(transform.position, playerTransform.position);
                if (distance <= collectRange)
                {
                    if (requirePressToCollect)
                    {
                        if (Input.GetKeyDown(collectKey))
                            Collect();
                    }
                    else
                    {
                        Collect();
                    }
                }
            }
        }
    }

    // ──────────────────────── 收集 ────────────────────────

    void Collect()
    {
        if (currentState == State.Collected) return;

        currentState = State.Collected;

        Debug.Log($"[CollectibleItem] 玩家获得道具: {itemName} (ID: {itemID})");

        onCollected?.Invoke(this);

        if (destroyOnCollect)
        {
            StartCoroutine(CollectEffectRoutine());
        }
    }

    System.Collections.IEnumerator CollectEffectRoutine()
    {
        float duration = 0.2f;
        float elapsed = 0f;
        Vector3 originalScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            yield return null;
        }

        Destroy(gameObject);
    }

    // ──────────────────────── 公开方法 ────────────────────────

    /// <summary>设置道具飞行的目标位置（由 Chest 或其它生成器调用）</summary>
    public void SetTargetPosition(Vector3 targetPos)
    {
        targetPosition = targetPos;
        startPosition = transform.position;
        stateTimer = 0f;
        flyProgress = 0f;
    }

    /// <summary>设置道具信息</summary>
    public void SetItemInfo(string name, string id)
    {
        itemName = name;
        itemID = id;
    }

    // ──────────────────────── 内部辅助 ────────────────────────

    void TryFindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

    // ──────────────────────── 编辑器辅助 ────────────────────────

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // 飞行目标位置
        if (!Application.isPlaying)
        {
            Vector3 dest = transform.position + Vector3.up * 1.5f;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(dest, 0.2f);

            // 弧线预览
            Vector3 from = transform.position;
            int segments = 20;
            for (int i = 0; i < segments; i++)
            {
                float t0 = i / (float)segments;
                float t1 = (i + 1) / (float)segments;
                Vector3 flatA = Vector3.Lerp(from, dest, t0);
                Vector3 flatB = Vector3.Lerp(from, dest, t1);
                float arcA = Mathf.Sin(t0 * Mathf.PI) * arcHeight;
                float arcB = Mathf.Sin(t1 * Mathf.PI) * arcHeight;
                flatA.y += arcA;
                flatB.y += arcB;
                Gizmos.DrawLine(flatA, flatB);
            }
        }

        // 拾取范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, collectRange);
    }
#endif
}
