using UnityEngine;

/// <summary>
/// Single logging gateway for the game. Always use this instead of Debug.Log directly.
/// </summary>
/// <remarks>
/// Info logs compile out of release builds. Warnings and errors always ship so players/devs see real problems.
/// </remarks>
public static class GameLog
{
    private const string Prefix = "[SwarmedWorm]";

    public static void Info(string message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"{Prefix} {message}");
#endif
    }

    public static void Warning(string message) =>
        Debug.LogWarning($"{Prefix} {message}");

    public static void Error(string message) =>
        Debug.LogError($"{Prefix} {message}");
}