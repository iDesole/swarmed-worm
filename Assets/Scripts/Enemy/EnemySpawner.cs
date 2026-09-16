using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

/// <summary>
/// Spawns enemies in rings around the player based on the active wave and environment config.
/// </summary>
/// <remarks>
/// Usually parented to the player so spawn bands move with them. WaveManager calls
/// <see cref="ConfigureWave"/> then polls <see cref="ExecuteSpawnTick"/> on an interval.
/// Spawn positions respect the ground Tilemap so enemies appear on walkable tiles.
/// </remarks>
public class EnemySpawner : MonoBehaviour
{
    [Header("Databases")]
    [FormerlySerializedAs("waves")]
    public WaveDatabase waveDatabase;

    public EnemyDatabase enemyDatabase;
    public BossDatabase bossDatabase;

    [Header("Scene")]
    public Transform player;
    public Tilemap groundTilemap;
    public int maxSpawnAttempts = 24;

    [HideInInspector] public WaveType currentWaveType;
    [HideInInspector] public WaveRangeDefinition currentRange;
    [HideInInspector] public int currentWaveNumber = 1;

    private readonly List<EnvironmentEnemySpawnEntry> activeSpawnEntries = new();

    public IReadOnlyList<EnvironmentEnemySpawnEntry> ActiveSpawnEntries => activeSpawnEntries;

    [HideInInspector] public float currentWaveScale = 1f;

    private bool isSpawningEnabled = true;
    private float pendingSpawnInterval = 4f;

    private void Awake() => EnsureAttachedToPlayer();

    private void Update()
    {
        if (player == null)
            EnsureAttachedToPlayer();
    }

    public void EnsureAttachedToPlayer()
    {
        if (player == null)
            player = PlayerCatalog.ActiveTransform;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (player == null) return;

        if (transform.parent != player)
        {
            transform.SetParent(player, false);
            transform.localPosition = Vector3.zero;
        }
    }

    public void ConfigureWave(WaveType waveType, int waveNumber, WaveRangeDefinition range, float difficulty)
    {
        if (waveDatabase == null)
            waveDatabase = WaveCatalog.Database;

        if (waveDatabase == null)
        {
            GameLog.Error("Assign WaveDatabase for spawn configuration.");
            activeSpawnEntries.Clear();
            return;
        }

        if (enemyDatabase == null)
            enemyDatabase = EnemyCatalog.Database;

        if (bossDatabase == null)
            bossDatabase = BossCatalog.Database;

        if (bossDatabase == null && waveDatabase.BossDatabase != null)
            bossDatabase = waveDatabase.BossDatabase;

        currentWaveType = waveType;
        currentWaveNumber = Mathf.Max(1, waveNumber);
        currentRange = range;
        currentWaveScale = difficulty;
        isSpawningEnabled = true;

        activeSpawnEntries.Clear();
        activeSpawnEntries.AddRange(waveDatabase.GetSpawnEntriesForWave(currentWaveNumber));

        if (activeSpawnEntries.Count == 0)
        {
            WaveEnvironmentDefinition environment = waveDatabase.GetActiveEnvironment();
            string environmentName = environment != null ? environment.EnvironmentName : "Environment";
            GameLog.Warning(
                $"No spawn entries configured for wave {currentWaveNumber} in '{environmentName}'.");
        }

        pendingSpawnInterval = RollNextSpawnInterval();
        EnemySpawnRingVisualizer.Refresh(this);
    }

    public void ExecuteSpawnTick()
    {
        if (!isSpawningEnabled || player == null || activeSpawnEntries.Count == 0)
            return;

        EnvironmentEnemySpawnEntry entry = PickWeightedEntry();
        if (entry == null)
            return;

        SpawnFromEntry(entry);
        pendingSpawnInterval = RollNextSpawnInterval(entry);
    }

    public float GetSpawnInterval() => Mathf.Max(0.35f, pendingSpawnInterval);

    public Enemy SpawnFromEntry(EnvironmentEnemySpawnEntry entry, float extraHealthScale = 1f)
    {
        if (entry == null || !entry.HasValidSpawn)
            return null;

        Vector3 pos = GetSpawnPosition(entry);
        if (pos == Vector3.zero)
            return null;

        Enemy instance = entry.spawnKind == WaveSpawnKind.Boss
            ? SpawnBossFromEntry(entry, pos)
            : SpawnEnemyFromEntry(entry, pos);

        if (instance == null)
            return null;

        float healthScale = currentWaveScale * entry.healthMultiplier * extraHealthScale;
        float damageScale = currentWaveScale * entry.damageMultiplier * extraHealthScale;
        ApplyScaling(instance, healthScale, damageScale);

        if (!Mathf.Approximately(entry.sizeMultiplier, 1f))
            instance.transform.localScale *= entry.sizeMultiplier;

        return instance;
    }

    private Enemy SpawnEnemyFromEntry(EnvironmentEnemySpawnEntry entry, Vector3 position)
    {
        if (!entry.enemy.IsValid)
            return null;

        if (enemyDatabase == null)
            enemyDatabase = EnemyCatalog.Database;

        if (enemyDatabase == null)
        {
            GameLog.Warning("Cannot spawn enemy without EnemyDatabase.");
            return null;
        }

        return enemyDatabase.CreateEnemy(entry.enemy, position);
    }

    private Enemy SpawnBossFromEntry(EnvironmentEnemySpawnEntry entry, Vector3 position)
    {
        if (!entry.boss.IsValid)
            return null;

        if (bossDatabase == null)
            bossDatabase = BossCatalog.Database;

        if (bossDatabase == null && waveDatabase != null)
            bossDatabase = waveDatabase.BossDatabase;

        if (bossDatabase == null)
        {
            GameLog.Warning("Cannot spawn boss without BossDatabase.");
            return null;
        }

        return bossDatabase.CreateBoss(entry.boss, position);
    }

    public void StopAutomaticSpawning() => isSpawningEnabled = false;
    public void ResumeAutomaticSpawning() => isSpawningEnabled = true;

    public Enemy SpawnSpecificEnemy(Enemy prefab, Vector3 position, float extraHealthMult = 1f, float extraDamageMult = 1f)
    {
        if (prefab == null) return null;
        Enemy instance = Instantiate(prefab, position, Quaternion.identity);
        ApplyScaling(instance, extraHealthMult, extraDamageMult);
        return instance;
    }

    private EnvironmentEnemySpawnEntry PickWeightedEntry()
    {
        if (activeSpawnEntries.Count == 0)
            return null;

        if (activeSpawnEntries.Count == 1)
            return activeSpawnEntries[0];

        int totalWeight = 0;
        foreach (EnvironmentEnemySpawnEntry entry in activeSpawnEntries)
            totalWeight += Mathf.Max(1, entry.spawnWeight);

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;

        foreach (EnvironmentEnemySpawnEntry entry in activeSpawnEntries)
        {
            cumulative += Mathf.Max(1, entry.spawnWeight);
            if (roll < cumulative)
                return entry;
        }

        return activeSpawnEntries[activeSpawnEntries.Count - 1];
    }

    private float RollNextSpawnInterval(EnvironmentEnemySpawnEntry entry = null)
    {
        entry ??= PickWeightedEntry();
        if (entry == null)
            return 4f;

        float interval = entry.RollSpawnInterval();

        if (currentRange != null && currentRange.SpawnRateMultiplier > 0f)
            interval /= currentRange.SpawnRateMultiplier;

        if (currentWaveScale > 0f)
            interval /= Mathf.Max(0.25f, currentWaveScale);

        return Mathf.Max(0.35f, interval);
    }

    private void ApplyScaling(Enemy instance, float healthMult, float damageMult)
    {
        if (instance == null) return;

        if (currentRange != null)
        {
            healthMult *= currentRange.HealthMultiplier;
            damageMult *= currentRange.DamageMultiplier;
        }

        instance.ApplyWaveScaling(healthMult, damageMult, 1f);
    }

    public Vector3 GetSpawnPosition(EnvironmentEnemySpawnEntry entry)
    {
        if (player == null || waveDatabase == null || entry == null)
            return Vector3.zero;

        if (entry.spawnRange == EnemySpawnRange.SetLocation)
            return player.position + (Vector3)entry.setLocationOffset;

        waveDatabase.SpawnRangeBands.GetDistanceRange(entry.spawnRange, out float minDistance, out float maxDistance);
        minDistance = Mathf.Max(0.5f, minDistance);
        maxDistance = Mathf.Max(minDistance, maxDistance);

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(minDistance, maxDistance);
            Vector3 candidate = player.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * distance;

            if (IsTileValid(candidate))
                return candidate;
        }

        float fallbackAngle = Random.Range(0f, Mathf.PI * 2f);
        float fallbackDistance = (minDistance + maxDistance) * 0.5f;
        return player.position + new Vector3(Mathf.Cos(fallbackAngle), Mathf.Sin(fallbackAngle), 0f) * fallbackDistance;
    }

    private bool IsTileValid(Vector3 worldPosition)
    {
        if (groundTilemap == null) return true;

        Vector3Int cell = groundTilemap.WorldToCell(worldPosition);
        var tile = groundTilemap.GetTile(cell);
        if (tile == null) return false;

        string name = tile.name.ToLower();
        return !name.Contains("water") && !name.Contains("wall") && !name.Contains("lava");
    }

    private void OnDrawGizmosSelected()
    {
        SpawnRangeBands bands = waveDatabase.SpawnRangeBands;
        if (player == null || waveDatabase == null || bands == null)
            return;

        DrawBandGizmo(bands.closeMin, bands.closeMax, new Color(1f, 0.3f, 0.3f, 0.5f));
        DrawBandGizmo(bands.nearbyMin, bands.nearbyMax, new Color(1f, 0.7f, 0.2f, 0.45f));
        DrawBandGizmo(bands.mediumMin, bands.mediumMax, new Color(0.3f, 0.9f, 0.4f, 0.4f));
        DrawBandGizmo(bands.farMin, bands.farMax, new Color(0.2f, 0.7f, 1f, 0.35f));
        DrawBandGizmo(bands.superFarMin, bands.superFarMax, new Color(0.5f, 0.3f, 1f, 0.3f));
    }

    private void DrawBandGizmo(float min, float max, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(player.position, min);
        Gizmos.DrawWireSphere(player.position, max);
    }
}