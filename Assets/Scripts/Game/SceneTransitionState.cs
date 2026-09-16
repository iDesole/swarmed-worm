using UnityEngine;

/// <summary>
/// Small static hand-off between door triggers and GameManager across map transitions.
/// </summary>
/// <remarks>
/// Doors set a pending spawn point ID before switching maps; GameManager reads it after spawning the player.
/// Reentry blocking prevents the player from immediately re-firing the same door trigger.
/// </remarks>
public static class SceneTransitionState
{
    public static string PendingSpawnPointId { get; set; }
    public static float ReentryBlockUntil { get; private set; }

    public static bool HasPendingSpawnPoint =>
        !string.IsNullOrWhiteSpace(PendingSpawnPointId);

    public static void SetPendingSpawnPoint(string spawnPointId)
    {
        PendingSpawnPointId = string.IsNullOrWhiteSpace(spawnPointId)
            ? null
            : spawnPointId.Trim();
    }

    public static void ClearPendingSpawnPoint()
    {
        PendingSpawnPointId = null;
    }

    public static void BlockReentry(float seconds = 0.75f)
    {
        ReentryBlockUntil = Time.unscaledTime + Mathf.Max(0.1f, seconds);
    }

    public static bool IsReentryBlocked =>
        Time.unscaledTime < ReentryBlockUntil;
}