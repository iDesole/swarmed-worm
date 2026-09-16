using UnityEngine;

/// <summary>
/// Named arrival point referenced by doors via spawn point ID strings.
/// </summary>
/// <remarks>
/// Place in the scene (or on environment prefabs). IDs must match door Target Spawn Point Id fields.
/// </remarks>
public class SceneSpawnPoint : MonoBehaviour
{
    [Tooltip("Unique ID used by door triggers. Example: Lobby_BeachEntrance")]
    [SerializeField] private string spawnPointId = "SpawnPoint";

    [Tooltip("Optional facing direction for the player after arriving.")]
    [SerializeField] private bool faceLeft;

    public string SpawnPointId => spawnPointId;
    public bool FaceLeft => faceLeft;

    public static SceneSpawnPoint FindById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        string trimmedId = id.Trim();
        SceneSpawnPoint[] spawnPoints = Object.FindObjectsByType<SceneSpawnPoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (SceneSpawnPoint spawnPoint in spawnPoints)
        {
            if (spawnPoint != null &&
                string.Equals(spawnPoint.SpawnPointId, trimmedId, System.StringComparison.OrdinalIgnoreCase))
            {
                return spawnPoint;
            }
        }

        return null;
    }

    public void ApplyToPlayer(Player player)
    {
        if (player == null)
            return;

        Vector3 position = transform.position;
        position.z = 0f;
        player.transform.position = position;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        CameraFollow cameraFollow = Object.FindFirstObjectByType<CameraFollow>();
        if (cameraFollow != null)
            cameraFollow.SnapToTarget();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.85f);
        Gizmos.DrawWireSphere(transform.position, 0.35f);

        Vector3 facing = faceLeft ? Vector3.left : Vector3.right;
        Gizmos.DrawLine(transform.position, transform.position + facing * 0.6f);
    }
}