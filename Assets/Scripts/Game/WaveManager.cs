using UnityEngine;

/// <summary>
/// Drives wave progression: timing, kill targets, and spawner coordination.
/// </summary>
/// <remarks>
/// Hub mode (lobby): combat disabled, enemies cleared. Combat maps call <see cref="BeginCombat"/>.
/// Listens to <see cref="WaveEvents"/> for kills and player death. Does not spawn enemies directly —
/// it tells <see cref="EnemySpawner"/> which wave config to use.
/// </remarks>
public class WaveManager : MonoBehaviour
{
    public EnemySpawner spawner;
    public bool waitForKeyBetweenWaves;
    public KeyCode nextWaveKey = KeyCode.F;

    public int WaveNumber => Mathf.Max(1, waveNumber);
    public int NextWaveNumber => waveNumber + 1;
    public WaveType CurrentWaveType { get; private set; }
    public bool IsWaveActive => waveActive;
    public bool CanStartNextWave => waitForKeyBetweenWaves && !waveActive && waveCompletedPending;
    public float TimeLeft => waveActive ? Mathf.Max(0f, waveEndTime - Time.time) : 0f;
    public int KillCount => killCount;
    public int KillTarget => killTarget;
    public bool IsGameOver { get; private set; }
    public bool IsHubMode => hubMode;

    [SerializeField] private int waveNumber;

    private bool hubMode;
    private bool combatInitialized;
    private WaveDatabase waveDatabase;
    private WaveRangeDefinition currentRange;
    private float waveEndTime;
    private float nextSpawnTime;
    private bool waveActive;
    private bool waveCompletedPending;
    private int killCount;
    private int killTarget;

    private void OnEnable()
    {
        WaveEvents.EnemyKilled += OnEnemyKilled;
        WaveEvents.PlayerDied += OnPlayerDied;
    }

    private void OnDisable()
    {
        WaveEvents.EnemyKilled -= OnEnemyKilled;
        WaveEvents.PlayerDied -= OnPlayerDied;
    }

    private void Start()
    {
        if (spawner == null)
            spawner = FindFirstObjectByType<EnemySpawner>();

        waveDatabase = spawner != null ? spawner.waveDatabase : null;
        if (waveDatabase == null)
            waveDatabase = WaveCatalog.Database;

        if (waveDatabase == null || spawner == null)
        {
            GameLog.Error("Assign WaveDatabase on the Enemy Spawner.");
            enabled = false;
            return;
        }

        waveDatabase.EnsureHasEnvironments();
        waveDatabase.Schedule?.EnsureHasRanges();
        waveNumber = 0;

        if (!hubMode)
            BeginCombat();
    }

    public void SetHubMode(bool isHub)
    {
        hubMode = isHub;

        if (!isHub)
            return;

        waveActive = false;
        waveCompletedPending = false;
        spawner?.StopAutomaticSpawning();
        DespawnAllEnemies();
    }

    public void BeginCombat()
    {
        if (hubMode || combatInitialized)
            return;

        combatInitialized = true;
        waveNumber = 0;
        StartNextWave();
    }

    public void ResetCombat()
    {
        combatInitialized = false;
        waveActive = false;
        waveCompletedPending = false;
        waveNumber = 0;
        spawner?.StopAutomaticSpawning();
        DespawnAllEnemies();
    }

    public void ClearGameOverState()
    {
        IsGameOver = false;
        waveActive = false;
        waveCompletedPending = false;
        spawner?.StopAutomaticSpawning();
        DespawnAllEnemies();
    }

    public void RestartAfterGameOver()
    {
        ClearGameOverState();
        killCount = 0;
        waveNumber = 0;

        if (hubMode)
            return;

        StartNextWave();
    }

    private void Update()
    {
        if (hubMode || IsGameOver)
            return;

        if (CanStartNextWave && Input.GetKeyDown(nextWaveKey))
        {
            waveCompletedPending = false;
            StartNextWave();
        }

        if (!waveActive) return;

        if (Time.time >= nextSpawnTime)
        {
            spawner.ExecuteSpawnTick();
            nextSpawnTime = Time.time + spawner.GetSpawnInterval();
        }

        if (IsRoundDone())
            EndRound();
    }

    private void StartNextWave()
    {
        waveNumber++;
        BeginRound();
    }

    private void BeginRound()
    {
        WaveScheduleData schedule = waveDatabase.Schedule;
        if (schedule == null || schedule.WaveRanges.Count == 0)
        {
            GameLog.Error($"'{waveDatabase.name}' has no wave schedule.");
            enabled = false;
            return;
        }

        CurrentWaveType = schedule.RollWaveType();
        currentRange = schedule.GetRangeAndIndexForWave(waveNumber).def;

        float difficulty = (1f + (waveNumber - 1) * 0.06f) * waveDatabase.DifficultyScale;
        if (currentRange != null)
            difficulty *= currentRange.SpawnCountMultiplier;

        spawner.ConfigureWave(CurrentWaveType, waveNumber, currentRange, difficulty);

        killCount = 0;
        if (CurrentWaveType == WaveType.KillCount)
            killTarget = waveDatabase.RollKillTarget(waveNumber);

        waveEndTime = Time.time + (CurrentWaveType == WaveType.Survival ? waveDatabase.GetSurvivalDuration() : 0f);
        nextSpawnTime = Time.time + 1f;
        waveActive = true;
        waveCompletedPending = false;
        spawner.ResumeAutomaticSpawning();
    }

    private void EndRound()
    {
        waveActive = false;
        spawner.StopAutomaticSpawning();
        DespawnAllEnemies();

        if (waitForKeyBetweenWaves)
            waveCompletedPending = true;
        else
            StartNextWave();
    }

    private bool IsRoundDone()
    {
        if (CurrentWaveType == WaveType.KillCount)
            return killCount >= killTarget;
        if (CurrentWaveType == WaveType.Survival)
            return Time.time >= waveEndTime;
        return false;
    }

    private void OnEnemyKilled(Enemy enemy) => killCount++;

    private void OnPlayerDied(Player player)
    {
        if (IsGameOver)
            return;

        IsGameOver = true;
        waveActive = false;
        waveCompletedPending = false;
        spawner?.StopAutomaticSpawning();
        DespawnAllEnemies();
    }

    private static void DespawnAllEnemies()
    {
        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (enemy != null)
                Destroy(enemy.gameObject);
        }
    }
}