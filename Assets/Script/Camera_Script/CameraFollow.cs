using UnityEngine;
using Player;   // ✅ 关键：解决 CS0246

public class CameraFollow : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target;

    [Header("基础跟随设置")]
    public Vector3 baseOffset = new Vector3(0, 0, -10);
    public float smoothSpeed = 5f;

    [Header("抬头 / 低头设置")]
    public bool enableLookOffset = true;
    public float lookUpOffsetY = 2f;
    public float lookDownOffsetY = -2f;
    public float lookLerpSpeed = 8f;

    [Header("边界限制")]
    public bool enableBounds = false;
    public float minX = -10f;
    public float maxX = 10f;
    public float minY = -5f;
    public float maxY = 5f;

    // 内部变量
    private Vector3 currentExtraOffset;
    private Vector3 targetExtraOffset;

    private void Start()
    {
        if (target != null && baseOffset == Vector3.zero)
        {
            baseOffset = transform.position - target.position;
        }

        currentExtraOffset = Vector3.zero;
        targetExtraOffset = Vector3.zero;
    }

    private void Update()
    {
        if (!enableLookOffset || target == null)
            return;

        float targetY = 0f;

        bool pressUp = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        bool pressDown = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        bool attackHeld = Input.GetKey(KeyCode.J);

        bool allowLookByState = true;

        PlayerStateMachine psm = target.GetComponent<PlayerStateMachine>();
        if (psm != null)
        {
            // 只在 Idle 状态下允许抬头 / 低头
            allowLookByState = psm.currentState is IdleState;
        }

        if (!attackHeld && allowLookByState)
        {
            if (pressUp && !pressDown)
                targetY = lookUpOffsetY;
            else if (pressDown && !pressUp)
                targetY = lookDownOffsetY;
        }

        targetExtraOffset = new Vector3(0, targetY, 0);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("摄像机跟随脚本没有指定目标！");
            return;
        }

        if (enableLookOffset)
        {
            currentExtraOffset = Vector3.Lerp(
                currentExtraOffset,
                targetExtraOffset,
                lookLerpSpeed * Time.deltaTime
            );
        }
        else
        {
            currentExtraOffset = Vector3.zero;
        }

        Vector3 targetPosition = target.position + baseOffset + currentExtraOffset;

        if (enableBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
        }

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}