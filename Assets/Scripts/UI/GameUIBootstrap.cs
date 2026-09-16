using UnityEngine;

/// <summary>
/// Creates the runtime HUD if the scene does not already contain one.
/// </summary>
/// <remarks>
/// Runs automatically after scene load. Spawns damage numbers, health/wave UI, and stack-visual settings.
/// </remarks>
public static class GameUIBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureGameUI()
    {
        if (Object.FindFirstObjectByType<GameHUD>() != null &&
            Object.FindFirstObjectByType<PlayerInventoryUI>() != null &&
            Object.FindFirstObjectByType<CharacterShopUI>() != null)
            return;

        var go = Object.FindFirstObjectByType<GameHUD>() != null
            ? Object.FindFirstObjectByType<GameHUD>().gameObject
            : new GameObject("GameUI");

        if (go.GetComponent<DamageNumberSpawner>() == null)
            go.AddComponent<DamageNumberSpawner>();
        if (go.GetComponent<GameHUD>() == null)
            go.AddComponent<GameHUD>();
        if (go.GetComponent<PlayerInventoryUI>() == null)
            go.AddComponent<PlayerInventoryUI>();
        if (go.GetComponent<CharacterShopUI>() == null)
            go.AddComponent<CharacterShopUI>();
        if (go.GetComponent<ElementalStackVisualBootstrap>() == null)
            go.AddComponent<ElementalStackVisualBootstrap>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (go.GetComponent<ElementalStackVisualDebug>() == null)
            go.AddComponent<ElementalStackVisualDebug>();
#endif
    }
}