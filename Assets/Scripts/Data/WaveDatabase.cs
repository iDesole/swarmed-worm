using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveDatabase", menuName = "Game/Wave Database")]
public class WaveDatabase : ScriptableObject
{
    [Header("References")]
    [SerializeField] private EnemyDatabase enemyDatabase;
    [SerializeField] private BossDatabase bossDatabase;

    [SerializeField] private List<WaveEnvironmentDefinition> environments = new();
    [SerializeField] private int activeEnvironmentIndex;

    [SerializeField] private WaveScheduleData schedule = new();
    [SerializeField] private WaveSettings settings = new();

    [Header("Spawn Range Bands")]
    [SerializeField] private SpawnRangeBands spawnRangeBands = new();

    [SerializeField] private float difficultyScale = 1f;

    public EnemyDatabase EnemyDatabase => enemyDatabase;
    public BossDatabase BossDatabase => bossDatabase;
    public IReadOnlyList<WaveEnvironmentDefinition> Environments => environments;
    public WaveScheduleData Schedule => schedule;
    public WaveSettings Settings => settings;
    public SpawnRangeBands SpawnRangeBands => spawnRangeBands;
    public float DifficultyScale => difficultyScale;

    public int ActiveEnvironmentIndex => activeEnvironmentIndex;

    public WaveEnvironmentDefinition GetActiveEnvironment()
    {
        if (environments == null || environments.Count == 0)
            return null;

        int index = Mathf.Clamp(activeEnvironmentIndex, 0, environments.Count - 1);
        return environments[index];
    }

    public WaveEnvironmentDefinition GetEnvironmentByName(string environmentName)
    {
        if (environments == null || environments.Count == 0 || string.IsNullOrWhiteSpace(environmentName))
            return null;

        string trimmedName = environmentName.Trim();
        foreach (WaveEnvironmentDefinition environment in environments)
        {
            if (environment != null &&
                string.Equals(environment.EnvironmentName, trimmedName, System.StringComparison.OrdinalIgnoreCase))
            {
                return environment;
            }
        }

        return null;
    }

    public void SetActiveEnvironment(int index)
    {
        environments ??= new List<WaveEnvironmentDefinition>();
        EnsureHasEnvironments();
        activeEnvironmentIndex = Mathf.Clamp(index, 0, environments.Count - 1);
    }

    public bool TryGetEnvironmentIndex(string environmentName, out int index)
    {
        index = -1;

        if (environments == null || environments.Count == 0 || string.IsNullOrWhiteSpace(environmentName))
            return false;

        string trimmedName = environmentName.Trim();
        for (int i = 0; i < environments.Count; i++)
        {
            if (environments[i] != null &&
                string.Equals(environments[i].EnvironmentName, trimmedName, System.StringComparison.OrdinalIgnoreCase))
            {
                index = i;
                return true;
            }
        }

        return false;
    }

    public bool TrySetActiveEnvironmentByName(string environmentName)
    {
        if (!TryGetEnvironmentIndex(environmentName, out int index))
            return false;

        SetActiveEnvironment(index);
        return true;
    }

    public List<EnvironmentEnemySpawnEntry> GetSpawnEntriesForWave(int waveNumber)
    {
        var results = new List<EnvironmentEnemySpawnEntry>();
        WaveEnvironmentDefinition environment = GetActiveEnvironment();
        if (environment?.enemies == null)
            return results;

        foreach (EnvironmentEnemySpawnEntry entry in environment.enemies)
        {
            if (entry != null && entry.HasValidSpawn && entry.CanSpawnOnWave(waveNumber))
                results.Add(entry);
        }

        return results;
    }

    public float GetSurvivalDuration() =>
        settings != null ? settings.timedDuration : 90f;

    public int RollKillTarget(int waveNumber)
    {
        if (settings == null)
            return waveNumber * UnityEngine.Random.Range(9, 13);

        int min = Mathf.Max(1, settings.killTargetMin);
        int max = Mathf.Max(min, settings.killTargetMax);
        return waveNumber * UnityEngine.Random.Range(min, max + 1);
    }

    public void EnsureHasEnvironments()
    {
        environments ??= new List<WaveEnvironmentDefinition>();

        if (environments.Count > 0)
            return;

        environments.Add(new WaveEnvironmentDefinition());
    }

    private void OnEnable() => WaveCatalog.Register(this);

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            return;
#endif

        environments ??= new List<WaveEnvironmentDefinition>();
        schedule ??= new WaveScheduleData();
        settings ??= new WaveSettings();
        spawnRangeBands ??= new SpawnRangeBands();

        schedule.EnsureHasRanges();
        schedule.Normalize();
        settings.Normalize();
        spawnRangeBands.Normalize();
        EnsureHasEnvironments();
        difficultyScale = Mathf.Max(0.01f, difficultyScale);

        if (environments != null)
            activeEnvironmentIndex = Mathf.Clamp(activeEnvironmentIndex, 0, environments.Count - 1);

#if UNITY_EDITOR
        if (enemyDatabase == null)
        {
            enemyDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyDatabase>(
                "Assets/GameData/Enemies/EnemyDatabase.asset");
        }

        if (bossDatabase == null)
        {
            bossDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<BossDatabase>(
                "Assets/GameData/BossDatabase.asset");
        }
#endif
    }
}