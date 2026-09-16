using UnityEngine;
using UnityEngine.Tilemaps;

// Copyright (c) 2026 Chase Wilson <chasewilsonbusiness@gmail.com>

/// <summary>
/// Session orchestrator: databases, environment, player spawn, and hub/combat rules.
/// </summary>
[DefaultExecutionOrder(-90)]
public class GameManager : MonoBehaviour
{
    [SerializeField] private WaveDatabase waveDatabase;
    [SerializeField] private PlayerDatabase playerDatabase;
    [SerializeField] private int startCharacterIndex;
    [SerializeField] private string startEnvironmentName = "Lobby";
    [SerializeField] private GameObject hubEnvironmentPrefab;

    private EnvironmentMap activeEnvironmentMap;

    public WaveDatabase WaveDatabase => waveDatabase;
    public int StartEnvironmentIndex =>
        waveDatabase != null ? waveDatabase.ActiveEnvironmentIndex : 0;

    public PlayerDatabase PlayerDatabase => playerDatabase;
    public int StartCharacterIndex => startCharacterIndex;
    public EnvironmentMap ActiveEnvironmentMap => activeEnvironmentMap;
    public Tilemap ActiveGroundTilemap => activeEnvironmentMap != null ? activeEnvironmentMap.GroundTilemap : null;

    public WaveEnvironmentDefinition StartEnvironment =>
        waveDatabase != null ? waveDatabase.GetActiveEnvironment() : null;

    public static GameManager Instance { get; private set; }

    public Player ActivePlayer => PlayerCatalog.ActivePlayer;

    /// <summary>Returns the sprite currently shown on the active player (same source as spawn visibility).</summary>
    public Sprite GetActivePlayerSprite()
    {
        Player activePlayer = ActivePlayer;
        if (activePlayer == null)
            return null;

        SpriteRenderer spriteRenderer = activePlayer.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
            return spriteRenderer.sprite;

        return activePlayer.ActiveCore?.sprite ?? RenderVisibilityUtility.LoadDefaultPlayerSprite();
    }

    // --- Startup: databases and first environment ---
    private void Awake()
    {
        Instance = this;

        if (waveDatabase == null)
            waveDatabase = WaveCatalog.Database;

        if (waveDatabase == null)
        {
            GameLog.Error("Assign WaveDatabase on GameManager.");
            enabled = false;
            return;
        }

        waveDatabase.EnsureHasEnvironments();
        WaveCatalog.Register(waveDatabase);
        EnsureStartEnvironment();

        if (playerDatabase == null)
            playerDatabase = PlayerCatalog.Database;
        else
            PlayerCatalog.Register(playerDatabase);

        ResetPlaySessionState();
        SpawnEnvironment();
        ApplyEnvironmentRules();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        SpawnAndWirePlayer();
        ApplyEnvironmentRules();
    }

    public void SetStartEnvironmentIndex(int index)
    {
        if (waveDatabase == null)
            return;

        CombatWorldCleanup.ClearRemnants();

        waveDatabase.SetActiveEnvironment(index);
        EnvironmentFactory.DestroyExistingMaps();
        SpawnEnvironment();
        SpawnAndWirePlayer();
        ApplyEnvironmentRules();
    }

    public void SetStartCharacterIndex(int index)
    {
        startCharacterIndex = Mathf.Max(0, index);
        SpawnAndWirePlayer();
    }

    /// <summary>Swaps the live player character without respawning (keeps position and NPC interaction state).</summary>
    public void ApplyCharacterIndex(int index)
    {
        startCharacterIndex = Mathf.Max(0, index);

        Player player = ActivePlayer;
        if (player != null)
            player.SetCharacterIndex(startCharacterIndex);
        else
            SpawnAndWirePlayer();
    }

    public void ResetPlaySessionState()
    {
        ShopUnlockTracker.ResetSession();
        InteractableCharacter.ResetAllSessionState();
    }

    public void RestartAfterDeath()
    {
        CombatWorldCleanup.ClearRemnants();

        WaveManager waveManager = FindFirstObjectByType<WaveManager>();
        waveManager?.RestartAfterGameOver();
        ResetPlaySessionState();
        SpawnAndWirePlayer();
    }

    public void ReturnToLobbyAfterDeath()
    {
        CombatWorldCleanup.ClearRemnants();

        WaveManager waveManager = FindFirstObjectByType<WaveManager>();
        waveManager?.ClearGameOverState();

        ResetPlaySessionState();

        if (waveDatabase == null)
            return;

        string lobbyName = string.IsNullOrWhiteSpace(startEnvironmentName) ? "Lobby" : startEnvironmentName;
        if (waveDatabase.TryGetEnvironmentIndex(lobbyName, out int lobbyIndex))
            SetStartEnvironmentIndex(lobbyIndex);
        else
            SetStartEnvironmentIndex(0);
    }

    private void EnsureStartEnvironment()
    {
        if (waveDatabase == null)
            return;

        if (!string.IsNullOrWhiteSpace(startEnvironmentName) &&
            waveDatabase.TrySetActiveEnvironmentByName(startEnvironmentName))
        {
            return;
        }

        waveDatabase.SetActiveEnvironment(0);
    }

    private bool IsHubEnvironment(WaveEnvironmentDefinition environment) =>
        environment != null && environment.IsHub;

    private GameObject ResolveEnvironmentPrefab(WaveEnvironmentDefinition environment)
    {
        if (environment == null)
            return null;

        if (IsHubEnvironment(environment) && hubEnvironmentPrefab != null)
            return hubEnvironmentPrefab;

        return environment.environmentPrefab;
    }

    // --- Map spawning: destroys old tilemaps, instantiates the active environment prefab ---
    private void SpawnEnvironment()
    {
        activeEnvironmentMap = null;
        EnvironmentFactory.DestroyExistingMaps();

        WaveEnvironmentDefinition environment = StartEnvironment;
        GameObject prefab = ResolveEnvironmentPrefab(environment);
        if (prefab == null)
        {
            string environmentName = environment != null ? environment.EnvironmentName : "active environment";
            GameLog.Error(
                $"Environment '{environmentName}' has no tileset prefab. Assign it on the Wave Database.");
            return;
        }

        Vector3 mapPosition = ResolveEnvironmentSpawnPosition();
        activeEnvironmentMap = EnvironmentFactory.Spawn(prefab, mapPosition);

        if (IsHubEnvironment(environment))
        {
            Vector3 playerSpawn = ResolvePlayerSpawnPosition();
            EnvironmentFactory.AlignMapCenterToWorld(activeEnvironmentMap, playerSpawn);
        }

        EnvironmentFactory.FinalizeEnvironmentMap(activeEnvironmentMap);

        string spawnedName = environment.EnvironmentName;
        Tilemap spawnedTilemap = activeEnvironmentMap != null ? activeEnvironmentMap.GroundTilemap : null;
        if (spawnedTilemap == null)
            GameLog.Warning($"Spawned '{spawnedName}' but prefab '{prefab.name}' has no Tilemap.");

        GameLog.Info($"Spawned '{spawnedName}' from '{prefab.name}'.");
        EnvironmentEvents.NotifyEnvironmentChanged(environment);
    }

    // --- Hub vs combat: WaveManager and EnemySpawner behave differently in the lobby ---
    private void ApplyEnvironmentRules()
    {
        bool isHub = IsHubEnvironment(StartEnvironment);

        WaveManager waveManager = Object.FindFirstObjectByType<WaveManager>();
        if (waveManager != null)
        {
            if (isHub)
            {
                waveManager.SetHubMode(true);
                waveManager.ResetCombat();
            }
            else
            {
                waveManager.SetHubMode(false);
                waveManager.ResetCombat();
                waveManager.BeginCombat();
            }
        }

        EnemySpawner[] spawners = Object.FindObjectsByType<EnemySpawner>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (EnemySpawner spawner in spawners)
        {
            if (spawner == null)
                continue;

            if (isHub)
                spawner.StopAutomaticSpawning();
            else
                EnemySpawnRingVisualizer.Refresh(spawner);
        }
    }

    private Vector3 ResolvePlayerSpawnPosition()
    {
        WaveEnvironmentDefinition environment = StartEnvironment;
        if (environment != null && environment.useCustomPlayerSpawn)
            return WithZeroZ(environment.playerSpawnPosition);

        if (playerDatabase == null)
            playerDatabase = PlayerCatalog.Database;

        PlayerCharacterDefinition character = playerDatabase != null
            ? playerDatabase.GetCharacter(PlayerSelection.At(startCharacterIndex))
                ?? playerDatabase.GetDefaultCharacter()
            : null;

        if (character != null)
            return WithZeroZ(character.spawnPosition);

        return Vector3.zero;
    }

    private Vector3 ResolveEnvironmentSpawnPosition()
    {
        WaveEnvironmentDefinition environment = StartEnvironment;
        if (environment != null && environment.HasCustomMapSpawnPosition)
            return WithZeroZ(environment.mapSpawnPosition);

        return ResolvePlayerSpawnPosition();
    }

    private static Vector3 WithZeroZ(Vector3 position) =>
        new Vector3(position.x, position.y, 0f);

    // --- Player spawn: factory creates the worm, then we connect camera + spawner ---
    private void SpawnAndWirePlayer()
    {
        if (playerDatabase == null)
            playerDatabase = PlayerCatalog.Database;

        DestroyScenePlayers();

        PlayerSelection selection = PlayerSelection.At(startCharacterIndex);
        if (playerDatabase != null && playerDatabase.GetCharacter(selection) == null)
        {
            int fallbackIndex = playerDatabase.DefaultCharacterIndex;
            GameLog.Warning(
                $"Start character index {startCharacterIndex} is invalid. Using default index {fallbackIndex}.");
            startCharacterIndex = fallbackIndex;
            selection = PlayerSelection.At(startCharacterIndex);
        }

        Vector3 spawnPosition = ResolvePlayerSpawnPosition();
        Player player = PlayerCatalog.SpawnPlayer(selection, spawnPosition);
        if (player == null)
        {
            GameLog.Error("Failed to spawn player from PlayerDatabase.");
            return;
        }

        player.gameObject.SetActive(true);
        EnsurePlayerVisible(player);
        EnsureStarterCredits(player);
        WirePlayerReferences(player);

        string characterName = playerDatabase != null
            ? playerDatabase.GetCharacterName(selection)
            : player.CharacterName;

        if (!string.IsNullOrWhiteSpace(characterName))
            GameLog.Info($"Starting as '{characterName}'.");

        ApplyPendingSpawnPoint(player);
    }

    private static void EnsureStarterCredits(Player player)
    {
        if (player?.inventory == null || player.inventory.GetCurrencyTotal() > 0)
            return;

        player.inventory.TryAddInventoryItem(new InventorySlotData
        {
            kind = InventoryItemKind.Currency,
            itemId = "credits",
            displayName = "Credits",
            quantity = 400
        });
    }

    private static void ApplyPendingSpawnPoint(Player player)
    {
        if (player == null || !SceneTransitionState.HasPendingSpawnPoint)
            return;

        string spawnPointId = SceneTransitionState.PendingSpawnPointId;
        SceneSpawnPoint spawnPoint = SceneSpawnPoint.FindById(spawnPointId);
        if (spawnPoint != null)
        {
            spawnPoint.ApplyToPlayer(player);
            SceneTransitionState.BlockReentry();
        }
        else
        {
            GameLog.Warning($"Spawn point '{spawnPointId}' was not found.");
        }

        SceneTransitionState.ClearPendingSpawnPoint();
    }

    private static void DestroyScenePlayers()
    {
        InteractableCharacter.ClearAllPlayerPresence();

        Player[] existingPlayers = Object.FindObjectsByType<Player>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Player existingPlayer in existingPlayers)
        {
            if (existingPlayer == null || existingPlayer.gameObject == null)
                continue;

            DetachEnemySpawner(existingPlayer.transform);
            Object.Destroy(existingPlayer.gameObject);
        }
    }

    private static void DetachEnemySpawner(Transform playerTransform)
    {
        if (playerTransform == null)
            return;

        EnemySpawner spawner = playerTransform.GetComponentInChildren<EnemySpawner>();
        if (spawner == null)
            return;

        spawner.transform.SetParent(null);
        spawner.StopAutomaticSpawning();
    }

    private static void EnsurePlayerVisible(Player player)
    {
        if (player == null)
            return;

        SpriteRenderer spriteRenderer = player.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            return;

        Sprite sprite = player.ActiveCore?.sprite ?? RenderVisibilityUtility.LoadDefaultPlayerSprite();
        PlayerSpriteUtility.ConfigureRenderer(spriteRenderer, sprite, sortingOrder: 1);

        if (spriteRenderer.sprite == null)
            GameLog.Warning("Player spawned without a visible sprite.");

        spriteRenderer.color = Color.white;
    }

    private void WirePlayerReferences(Player player)
    {
        if (player == null)
            return;

        Tilemap groundTilemap = ActiveGroundTilemap ?? Object.FindFirstObjectByType<Tilemap>();
        EnsureCameraFollowsPlayer(player.transform);
        WireEnemySpawners(player, groundTilemap);
    }

    private void EnsureCameraFollowsPlayer(Transform playerTransform)
    {
        CameraFollow cameraFollow = Object.FindFirstObjectByType<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.target = playerTransform;
            cameraFollow.SnapToTarget();
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        Vector3 position = mainCamera.transform.position;
        position.x = playerTransform.position.x;
        position.y = playerTransform.position.y;
        mainCamera.transform.position = position;
    }

    private void WireEnemySpawners(Player player, Tilemap groundTilemap)
    {
        EnemySpawner primarySpawner = null;
        EnemySpawner[] spawners = Object.FindObjectsByType<EnemySpawner>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (EnemySpawner spawner in spawners)
        {
            if (spawner == null)
                continue;

            spawner.player = player.transform;
            spawner.waveDatabase = waveDatabase;
            spawner.enemyDatabase = EnemyCatalog.Database;
            spawner.bossDatabase = waveDatabase != null ? waveDatabase.BossDatabase : BossCatalog.Database;
            spawner.groundTilemap = groundTilemap;
            spawner.EnsureAttachedToPlayer();
            primarySpawner ??= spawner;
        }

        if (primarySpawner == null)
            primarySpawner = CreateEnemySpawnerForPlayer(player, groundTilemap);

        WaveManager waveManager = Object.FindFirstObjectByType<WaveManager>();
        if (waveManager != null)
            waveManager.spawner = primarySpawner;
    }

    private EnemySpawner CreateEnemySpawnerForPlayer(Player player, Tilemap groundTilemap)
    {
        if (player == null)
            return null;

        var spawnerObject = new GameObject("EnemySpawner");
        spawnerObject.transform.SetParent(player.transform, false);
        spawnerObject.transform.localPosition = Vector3.zero;

        EnemySpawner spawner = spawnerObject.AddComponent<EnemySpawner>();
        spawner.player = player.transform;
        spawner.waveDatabase = waveDatabase;
        spawner.enemyDatabase = EnemyCatalog.Database;
        spawner.bossDatabase = waveDatabase != null ? waveDatabase.BossDatabase : BossCatalog.Database;
        spawner.groundTilemap = groundTilemap;
        return spawner;
    }
}