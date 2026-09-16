using System;
using System.Collections.Generic;
using UnityEngine;

// =============================================================================
// WAVE & ENVIRONMENT DATA LAYER
// WaveDatabase.asset defines maps (Lobby, Beach, …), per-environment enemy tables,
// wave schedules, and spawn distance bands used by EnemySpawner.
// =============================================================================

/// <summary>Whether a wave spawn entry pulls from EnemyDatabase or BossDatabase.</summary>
public enum WaveSpawnKind
{
    Enemy,
    Boss
}

/// <summary>Distance band from the player where enemies may spawn.</summary>
public enum EnemySpawnRange
{
    Close,
    Nearby,
    Medium,
    Far,
    SuperFar,
    SetLocation
}

[Serializable]
public class WaveSettings
{
    public float timedDuration = 90f;
    public int killTargetMin = 9;
    public int killTargetMax = 13;

    public void Normalize()
    {
        timedDuration = Mathf.Max(1f, timedDuration);
        killTargetMin = Mathf.Max(1, killTargetMin);
        killTargetMax = Mathf.Max(killTargetMin, killTargetMax);
    }
}

[Serializable]
public class WaveRangeDefinition
{
    public int waveCount = 6;
    public float healthMultiplier = 1f;
    public float damageMultiplier = 1f;
    public float spawnCountMultiplier = 1f;
    public float spawnRateMultiplier = 1f;

    public int WaveCount => Mathf.Max(1, waveCount);
    public float HealthMultiplier => healthMultiplier;
    public float DamageMultiplier => damageMultiplier;
    public float SpawnCountMultiplier => spawnCountMultiplier;
    public float SpawnRateMultiplier => spawnRateMultiplier;

    public void Normalize()
    {
        waveCount = Mathf.Max(1, waveCount);
        healthMultiplier = Mathf.Max(0.01f, healthMultiplier);
        damageMultiplier = Mathf.Max(0.01f, damageMultiplier);
        spawnCountMultiplier = Mathf.Max(0.01f, spawnCountMultiplier);
        spawnRateMultiplier = Mathf.Max(0.01f, spawnRateMultiplier);
    }
}

[Serializable]
public struct WaveTypeChance
{
    public WaveType type;
    public int weight;
    public float percent;
}

[Serializable]
public class WaveScheduleData
{
    public List<WaveRangeDefinition> waveRanges = new();
    public int killCountWeight = 50;
    public int survivalWeight = 50;

    public void EnsureHasRanges()
    {
        waveRanges ??= new List<WaveRangeDefinition>();

        if (waveRanges.Count > 0)
            return;

        waveRanges.Add(new WaveRangeDefinition { waveCount = 999 });
    }

    public void Normalize()
    {
        EnsureHasRanges();
        killCountWeight = Mathf.Max(0, killCountWeight);
        survivalWeight = Mathf.Max(0, survivalWeight);

        foreach (WaveRangeDefinition range in waveRanges)
            range?.Normalize();
    }

    public int GetTotalDefinedWaves()
    {
        int total = 0;
        foreach (WaveRangeDefinition range in waveRanges)
            total += Mathf.Max(1, range.WaveCount);
        return total;
    }

    public int GetStartWaveForRange(int rangeIndex)
    {
        if (rangeIndex < 0 || rangeIndex >= waveRanges.Count) return 1;
        int start = 1;
        for (int i = 0; i < rangeIndex; i++)
            start += Mathf.Max(1, waveRanges[i].WaveCount);
        return start;
    }

    public int GetEndWaveForRange(int rangeIndex)
    {
        if (rangeIndex < 0 || rangeIndex >= waveRanges.Count) return 1;
        return GetStartWaveForRange(rangeIndex) + Mathf.Max(1, waveRanges[rangeIndex].WaveCount) - 1;
    }

    public (WaveRangeDefinition def, int index) GetRangeAndIndexForWave(int waveNumber)
    {
        int currentStart = 1;
        for (int i = 0; i < waveRanges.Count; i++)
        {
            int count = Mathf.Max(1, waveRanges[i].WaveCount);
            int end = currentStart + count - 1;
            if (waveNumber >= currentStart && waveNumber <= end)
                return (waveRanges[i], i);
            currentStart += count;
        }

        if (waveRanges.Count > 0)
            return (waveRanges[waveRanges.Count - 1], waveRanges.Count - 1);

        return (null, -1);
    }

    public WaveType RollWaveType()
    {
        int total = killCountWeight + survivalWeight;
        if (total <= 0) return WaveType.KillCount;
        return UnityEngine.Random.Range(0, total) < survivalWeight ? WaveType.Survival : WaveType.KillCount;
    }

    public List<WaveTypeChance> GetWaveTypeChances()
    {
        int total = killCountWeight + survivalWeight;
        return new List<WaveTypeChance>
        {
            new WaveTypeChance
            {
                type = WaveType.KillCount,
                weight = killCountWeight,
                percent = total > 0 ? killCountWeight / (float)total * 100f : 0f
            },
            new WaveTypeChance
            {
                type = WaveType.Survival,
                weight = survivalWeight,
                percent = total > 0 ? survivalWeight / (float)total * 100f : 0f
            }
        };
    }

    public IReadOnlyList<WaveRangeDefinition> WaveRanges => waveRanges;
}

[Serializable]
public class EnvironmentEnemySpawnEntry
{
    public WaveSpawnKind spawnKind = WaveSpawnKind.Enemy;
    public EnemySelection enemy;
    public BossSelection boss;
    public EnemySpawnRange spawnRange = EnemySpawnRange.Medium;
    public Vector2 setLocationOffset;

    public bool spawnOnAllWaves = true;
    public List<int> specificWaves = new();

    [Min(0.1f)] public float minSpawnInterval = 3f;
    [Min(0.1f)] public float maxSpawnInterval = 6f;
    [Min(1)] public int spawnWeight = 1;

    public float healthMultiplier = 1f;
    public float damageMultiplier = 1f;
    public float sizeMultiplier = 1f;

    public bool HasValidSpawn =>
        spawnKind == WaveSpawnKind.Boss ? boss.IsValid : enemy.IsValid;

    public bool CanSpawnOnWave(int waveNumber)
    {
        if (waveNumber < 1)
            return false;

        if (spawnOnAllWaves)
            return true;

        if (specificWaves == null || specificWaves.Count == 0)
            return false;

        for (int i = 0; i < specificWaves.Count; i++)
        {
            if (specificWaves[i] == waveNumber)
                return true;
        }

        return false;
    }

    public float RollSpawnInterval()
    {
        float min = Mathf.Max(0.1f, minSpawnInterval);
        float max = Mathf.Max(min, maxSpawnInterval);
        return UnityEngine.Random.Range(min, max);
    }
}

[Serializable]
public class WaveEnvironmentDefinition
{
    [Tooltip("Optional label for this environment. Leave blank to use the tileset prefab name.")]
    public string environmentName = "";

    [Tooltip("Tilemap prefab spawned for this environment (Grid + Tilemap, optionally EnvironmentMap).")]
    public GameObject environmentPrefab;

    [Tooltip("World position where the tileset prefab is instantiated. Leave at zero to spawn at the player spawn point.")]
    public Vector3 mapSpawnPosition;

    [Tooltip("Hub areas (lobby) are safe — no waves or enemy spawning.")]
    public bool isHubEnvironment;

    public bool useCustomPlayerSpawn;
    public Vector3 playerSpawnPosition;
    public List<EnvironmentEnemySpawnEntry> enemies = new();

    public bool IsHub => isHubEnvironment;

    public string EnvironmentName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(environmentName))
                return environmentName.Trim();

            if (environmentPrefab != null)
                return environmentPrefab.name;

            return "Environment";
        }
    }

    public bool HasTilesetPrefab => environmentPrefab != null;

    public bool HasCustomMapSpawnPosition =>
        Mathf.Abs(mapSpawnPosition.x) > 0.01f ||
        Mathf.Abs(mapSpawnPosition.y) > 0.01f ||
        Mathf.Abs(mapSpawnPosition.z) > 0.01f;
}

[Serializable]
public class SpawnRangeBands
{
    public float closeMin = 6f;
    public float closeMax = 10f;
    public float nearbyMin = 10f;
    public float nearbyMax = 14f;
    public float mediumMin = 14f;
    public float mediumMax = 18f;
    public float farMin = 18f;
    public float farMax = 22f;
    public float superFarMin = 22f;
    public float superFarMax = 28f;

    public void Normalize()
    {
        closeMin = Mathf.Max(0.5f, closeMin);
        closeMax = Mathf.Max(closeMin, closeMax);
        nearbyMin = Mathf.Max(0.5f, nearbyMin);
        nearbyMax = Mathf.Max(nearbyMin, nearbyMax);
        mediumMin = Mathf.Max(0.5f, mediumMin);
        mediumMax = Mathf.Max(mediumMin, mediumMax);
        farMin = Mathf.Max(0.5f, farMin);
        farMax = Mathf.Max(farMin, farMax);
        superFarMin = Mathf.Max(0.5f, superFarMin);
        superFarMax = Mathf.Max(superFarMin, superFarMax);
    }

    public void GetDistanceRange(EnemySpawnRange range, out float min, out float max)
    {
        switch (range)
        {
            case EnemySpawnRange.Close:
                min = closeMin;
                max = closeMax;
                break;
            case EnemySpawnRange.Nearby:
                min = nearbyMin;
                max = nearbyMax;
                break;
            case EnemySpawnRange.Medium:
                min = mediumMin;
                max = mediumMax;
                break;
            case EnemySpawnRange.Far:
                min = farMin;
                max = farMax;
                break;
            case EnemySpawnRange.SuperFar:
                min = superFarMin;
                max = superFarMax;
                break;
            default:
                min = mediumMin;
                max = mediumMax;
                break;
        }

        if (max < min)
            max = min;
    }
}