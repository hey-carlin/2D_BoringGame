using UnityEngine;
using DungeonKIT;

/// <summary>
/// 灵魂碎片拾取脚本：挂载到场景中的灵魂碎片 GameObject 上。
/// 玩家触碰即拾取，PlayerStats.soulFragments +1，更新 UI。
///
/// 要求：Collider2D（IsTrigger = true）
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SoulFragmentPickup : MonoBehaviour
{
    [Header("拾取音效")]
    public bool playSound = true;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 灵魂碎片 +1
            PlayerStats.Instance.soulFragments++;
            Debug.Log($"获得灵魂碎片！当前数量: {PlayerStats.Instance.soulFragments}");

            // 更新 UI
            UIManager.Instance?.UpdateUI();

            // 播放音效
            if (playSound && AudioManager.Instance != null)
            {
                AudioManager.Instance.Play(
                    PlayerStats.Instance.audioSource,
                    AudioManager.Instance.pickUpItems,
                    false
                );
            }

            // 销毁
            Destroy(gameObject);
        }
    }
}
