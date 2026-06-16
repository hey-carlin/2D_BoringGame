using UnityEngine;
using Enemy;

/// <summary>墓碑：被打 10 次后 Boss3 出现</summary>
public class Boss3Summoner : MonoBehaviour, IDamageable
{
    public int maxHP = 10;
    public GameObject boss3Prefab;          // Boss3 预制体
    public float riseDuration = 2f;
    public float riseHeight = 3f;

    private int currentHP;
    private SpriteRenderer sr;
    private GameObject spawnedBoss;

    private void Start()
    {
        currentHP = maxHP;
        sr = GetComponent<SpriteRenderer>();
    }

    public void TakeDamage(int damage)
    {
        currentHP--;
        if (sr != null) StartCoroutine(FlashRed());

        if (currentHP <= 0)
            SpawnBoss();
    }

    private System.Collections.IEnumerator FlashRed()
    {
        Color o = sr.color;
        sr.color = Color.red;
        yield return new WaitForSeconds(0.06f);
        sr.color = o;
    }

    private void SpawnBoss()
    {
        GetComponent<Collider2D>().enabled = false;
        if (sr != null) sr.enabled = false;

        if (boss3Prefab != null)
        {
            spawnedBoss = Instantiate(boss3Prefab, transform.position, Quaternion.identity);
            var ai = spawnedBoss.GetComponent<BossCAI>();
            if (ai != null) ai.enabled = true;
        }
        else
        {
            Debug.LogError("[墓碑] boss3Prefab 为空！请拖入 Boss3 预制体！");
        }
    }
}
