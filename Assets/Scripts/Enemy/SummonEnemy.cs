using UnityEngine;

public class SummonEnemy : Enemy
{
    [Header("Summon")]
    public GameObject[] summonPrefabs = System.Array.Empty<GameObject>();
    public int maxActiveSummons = 3;
    public float summonInterval = 4f;
    public float summonLifetime = 12f;

    private int activeSummons;
    private float nextSummonTime;

    protected override void Awake()
    {
        base.Awake();
        nextSummonTime = Time.time + summonInterval;
    }

    public override void PerformAttack(Transform target)
    {
        if (summonPrefabs == null || summonPrefabs.Length == 0)
            return;

        if (activeSummons >= maxActiveSummons || Time.time < nextSummonTime)
            return;

        nextSummonTime = Time.time + summonInterval;

        Vector2 dir = target != null
            ? ((Vector2)(target.position - transform.position)).normalized
            : Vector2.right;

        GameObject prefab = summonPrefabs[Random.Range(0, summonPrefabs.Length)];
        Vector3 spawnPos = transform.position + (Vector3)dir * 1.2f;
        GameObject summon = Instantiate(prefab, spawnPos, Quaternion.identity);

        activeSummons++;
        var lifetime = summon.AddComponent<SummonLifetimeTracker>();
        lifetime.Initialize(this, summonLifetime);
    }

    internal void NotifySummonDestroyed() =>
        activeSummons = Mathf.Max(0, activeSummons - 1);

    private sealed class SummonLifetimeTracker : MonoBehaviour
    {
        private SummonEnemy owner;

        public void Initialize(SummonEnemy enemy, float lifetime)
        {
            owner = enemy;
            Destroy(gameObject, lifetime);
        }

        private void OnDestroy() => owner?.NotifySummonDestroyed();
    }
}