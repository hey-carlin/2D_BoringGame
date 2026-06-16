using UnityEngine;

/// <summary>
/// Boss 竞技场入口触发器：玩家进入时激活 Boss 战音乐。
/// 挂载到 Boss 区域入口的 Trigger Collider 上。
/// </summary>
public class BossArenaTrigger : MonoBehaviour
{
    [Tooltip("Scene3 的音乐控制器")]
    public Scene3MusicController musicController;

    private bool triggered;

    void Start()
    {
        if (musicController == null)
            musicController = FindObjectOfType<Scene3MusicController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            triggered = true;

            if (musicController != null)
            {
                musicController.StartBossFight();
                Debug.Log("[BossArenaTrigger] Boss 战音乐已触发");
            }
            else
            {
                Debug.LogWarning("[BossArenaTrigger] Scene3MusicController 未找到！");
            }
        }
    }
}
