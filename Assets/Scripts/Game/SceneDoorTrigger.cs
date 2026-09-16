using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

/// <summary>How a door decides where the player goes after entering the trigger.</summary>
public enum DoorDestinationType
{
    [Tooltip("Load a different Unity scene (add it to Build Settings first).")]
    LoadScene,

    [Tooltip("Swap to a map preset from the Wave Database (Lobby, Beach, Training, etc.).")]
    SwitchMapPreset,

    [Tooltip("Instantly move the player to a SceneSpawnPoint in this scene.")]
    TeleportToSpawnPoint
}

/// <summary>
/// Trigger volume that moves the player between maps, scenes, or spawn points.
/// </summary>
/// <remarks>
/// Most doors use <see cref="DoorDestinationType.SwitchMapPreset"/> which calls GameManager to swap
/// the active Wave Database environment (Lobby → Beach, etc.) without a full scene reload.
/// Pair with <see cref="SceneTransitionState"/> to avoid double-triggering and to set arrival spawn IDs.
/// </remarks>
[RequireComponent(typeof(BoxCollider2D))]
public class SceneDoorTrigger : MonoBehaviour
{
    [Header("Map Preset")]
    [SerializeField] private WaveDatabase waveDatabase;

    [SerializeField] private DoorDestinationType destinationType = DoorDestinationType.SwitchMapPreset;

    [Tooltip("Map preset from Wave Database > Environments.")]
    [SerializeField] private string targetEnvironmentName = "Beach";

    [FormerlySerializedAs("environmentIndex")]
    [SerializeField] private int targetEnvironmentIndex;

    [Header("Scene Load")]
    [Tooltip("Scene name from File > Build Settings. Only used when Destination Type is Load Scene.")]
    [SerializeField] private string sceneName = "Beach";

    [Header("Spawn")]
    [Tooltip("Optional SceneSpawnPoint ID to place the player after switching maps.")]
    [SerializeField] private string targetSpawnPointId = "Beach_Entrance";

    [Header("Behavior")]
    [SerializeField] private bool oneShot = true;
    [SerializeField] private float reentryCooldown = 0.75f;
    [SerializeField] private bool showGizmoInSceneView = true;

    private bool hasTriggered;

    public WaveDatabase WaveDatabase => waveDatabase;
    public string TargetEnvironmentName => targetEnvironmentName;
    public int TargetEnvironmentIndex => targetEnvironmentIndex;

    private void Reset()
    {
        ConfigureCollider();
        destinationType = DoorDestinationType.SwitchMapPreset;
        targetEnvironmentName = "Beach";
    }

    private void Awake()
    {
        ConfigureCollider();

        if (waveDatabase == null)
            waveDatabase = WaveCatalog.Database;
    }

    private void ConfigureCollider()
    {
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        collider.isTrigger = true;

        if (collider.size == Vector2.zero)
            collider.size = new Vector2(1.5f, 2f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (hasTriggered && oneShot)
            return;

        if (SceneTransitionState.IsReentryBlocked)
            return;

        Player player = other.GetComponent<Player>();
        if (player == null || player.IsDead)
            return;

        hasTriggered = true;
        SceneTransitionState.BlockReentry(reentryCooldown);
        ExecuteTransition();
    }

    private void ExecuteTransition()
    {
        switch (destinationType)
        {
            case DoorDestinationType.SwitchMapPreset:
                SwitchToMapPreset();
                break;

            case DoorDestinationType.LoadScene:
                LoadTargetScene();
                break;

            case DoorDestinationType.TeleportToSpawnPoint:
                TeleportPlayerToSpawnPoint();
                break;
        }
    }

    private void SwitchToMapPreset()
    {
        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            GameLog.Warning($"Door '{name}' could not find GameManager.");
            ResetTriggerState();
            return;
        }

        if (!TryResolveTargetEnvironmentIndex(out int environmentIndex, out string environmentLabel))
        {
            GameLog.Warning($"Door '{name}' has no valid map preset.");
            ResetTriggerState();
            return;
        }

        if (!string.IsNullOrWhiteSpace(targetSpawnPointId))
            SceneTransitionState.SetPendingSpawnPoint(targetSpawnPointId);

        GameLog.Info($"Door '{name}' switching to '{environmentLabel}'.");
        gameManager.SetStartEnvironmentIndex(environmentIndex);
    }

    private bool TryResolveTargetEnvironmentIndex(out int environmentIndex, out string environmentLabel)
    {
        environmentIndex = -1;
        environmentLabel = string.Empty;

        WaveDatabase database = waveDatabase != null ? waveDatabase : WaveCatalog.Database;
        if (database == null)
        {
            environmentIndex = targetEnvironmentIndex;
            environmentLabel = string.IsNullOrWhiteSpace(targetEnvironmentName)
                ? $"Environment {targetEnvironmentIndex}"
                : targetEnvironmentName;
            return environmentIndex >= 0;
        }

        if (!string.IsNullOrWhiteSpace(targetEnvironmentName) &&
            database.TryGetEnvironmentIndex(targetEnvironmentName, out environmentIndex))
        {
            WaveEnvironmentDefinition environment = database.Environments[environmentIndex];
            environmentLabel = environment != null ? environment.EnvironmentName : targetEnvironmentName;
            return true;
        }

        if (targetEnvironmentIndex >= 0 && targetEnvironmentIndex < database.Environments.Count)
        {
            environmentIndex = targetEnvironmentIndex;
            WaveEnvironmentDefinition environment = database.Environments[environmentIndex];
            environmentLabel = environment != null ? environment.EnvironmentName : $"Environment {environmentIndex}";
            return true;
        }

        return false;
    }

    private void LoadTargetScene()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            GameLog.Warning($"Door '{name}' has no scene name assigned.");
            ResetTriggerState();
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            GameLog.Error($"Scene '{sceneName}' is not in Build Settings.");
            ResetTriggerState();
            return;
        }

        if (!string.IsNullOrWhiteSpace(targetSpawnPointId))
            SceneTransitionState.SetPendingSpawnPoint(targetSpawnPointId);

        SceneManager.LoadScene(sceneName);
    }

    private void TeleportPlayerToSpawnPoint()
    {
        Player player = Object.FindFirstObjectByType<Player>();
        if (player == null)
        {
            GameLog.Warning($"Door '{name}' could not find a player to teleport.");
            ResetTriggerState();
            return;
        }

        SceneSpawnPoint spawnPoint = SceneSpawnPoint.FindById(targetSpawnPointId);
        if (spawnPoint == null)
        {
            GameLog.Warning($"Door '{name}' could not find spawn point '{targetSpawnPointId}'.");
            ResetTriggerState();
            return;
        }

        spawnPoint.ApplyToPlayer(player);
    }

    private void ResetTriggerState()
    {
        if (!oneShot)
            hasTriggered = false;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmoInSceneView)
            return;

        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider == null)
            return;

        Gizmos.color = new Color(0.95f, 0.75f, 0.2f, 0.35f);
        Vector3 center = transform.TransformPoint(collider.offset);
        Vector3 size = Vector3.Scale(collider.size, transform.lossyScale);
        Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
        Gizmos.DrawCube(Vector3.zero, size);
        Gizmos.matrix = Matrix4x4.identity;
    }
}