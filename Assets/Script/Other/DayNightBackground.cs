using UnityEngine;
using UnityEngine.Tilemaps;

public class DayNightBackground : MonoBehaviour
{
    public static DayNightBackground Instance { get; private set; }

    [Header("白天")]
    public Tilemap[] dayLayers = new Tilemap[3];

    [Header("夜晚")]
    public Tilemap[] nightLayers = new Tilemap[3];

    [Header("时间")]
    public float dayDuration = 300f; // 白天持续多久
    public float nightDuration = 180f;

    [Header("渐变")]
    public float blendSpeed = 1.5f;

    private float timer;
    private bool isNight;

    private void Awake()
    {
        Instance = this;
        SetAllAlpha(dayLayers, 1f);
        SetAllAlpha(nightLayers, 0f);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float max = isNight ? nightDuration : dayDuration;

        float t = Mathf.Clamp01(timer / max);

        // 核心渐变：根据当前是白天还是夜晚，决定渐变方向
        float dayAlpha, nightAlpha;
        if (isNight)
        {
            // 夜晚阶段：白天→黑夜，day淡出，night淡入
            dayAlpha = 1f - t;
            nightAlpha = t;
        }
        else
        {
            // 白天阶段：黑夜→白天，day淡入，night淡出
            dayAlpha = t;
            nightAlpha = 1f - t;
        }

        SetAllAlpha(dayLayers, dayAlpha);
        SetAllAlpha(nightLayers, nightAlpha);

        if (t >= 1f)
        {
            timer = 0f;
            isNight = !isNight;
        }
    }

    void SetAllAlpha(Tilemap[] maps, float alpha)
    {
        foreach (var map in maps)
        {
            if (map == null) continue;
            map.color = new Color(1, 1, 1, alpha);
        }
    }
}