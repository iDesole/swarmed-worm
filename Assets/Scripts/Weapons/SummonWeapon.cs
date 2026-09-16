using UnityEngine;

/// <summary>
/// Spawns allied units near the aim direction, capped by maxActiveSummons.
/// </summary>
public class SummonWeapon : WeaponBase
{
    [Header("Summon")]
    public GameObject[] summonPrefabs;
    public int maxActiveSummons = 3;
    public float summonLifetime = 12f;
    public float summonRange = 6f;
    public float summonDamageMultiplier = 1f;
    public float summonHealthMultiplier = 1f;
    public float summonAttackSpeedMultiplier = 1f;
    public int summonsPerCast = 1;

    private int activeSummons;

    protected override void PerformAttack()
    {
        if (summonPrefabs == null || summonPrefabs.Length == 0) return;
        if (activeSummons >= maxActiveSummons) return;

        Vector2 dir = GetFireDirection();
        int summonsToCreate = Mathf.Min(summonsPerCast, maxActiveSummons - activeSummons);

        for (int i = 0; i < summonsToCreate; i++)
        {
            Vector3 spawnPos = transform.position + (Vector3)dir * summonRange * 0.6f;
            GameObject prefab = summonPrefabs[Random.Range(0, summonPrefabs.Length)];
            GameObject summon = Instantiate(prefab, spawnPos, Quaternion.identity);

            activeSummons++;
            var lifetime = summon.AddComponent<SummonLifetimeTracker>();
            lifetime.Initialize(this, summonLifetime);
        }
    }

    internal void NotifySummonDestroyed() => activeSummons = Mathf.Max(0, activeSummons - 1);

    private sealed class SummonLifetimeTracker : MonoBehaviour
    {
        private SummonWeapon owner;

        public void Initialize(SummonWeapon weapon, float lifetime)
        {
            owner = weapon;
            Destroy(gameObject, lifetime);
        }

        private void OnDestroy()
        {
            owner?.NotifySummonDestroyed();
        }
    }
}