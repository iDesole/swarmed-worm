using UnityEngine;

/// <summary>
/// Shared lookup for ScriptableObject databases (weapons, enemies, waves, player).
/// </summary>
/// <remarks>
/// Resolution order: cached reference → bootstrap in scene → first asset found in project.
/// Catalog classes (e.g. <see cref="WeaponCatalog"/>) wrap this and expose a simple Database property.
/// </remarks>
public static class CatalogResolver
{
    /// <summary>Returns a database, caching the result for later calls.</summary>
    public static T Resolve<T>(ref T cache, string catalogLabel) where T : ScriptableObject
    {
        if (cache != null)
            return cache;

        MonoBehaviour[] bootstraps = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (MonoBehaviour bootstrap in bootstraps)
        {
            if (bootstrap is not IGameDatabaseBootstrap<T> provider || provider.Database == null)
                continue;

            cache = provider.Database;
            return cache;
        }

        T[] assets = Resources.FindObjectsOfTypeAll<T>();
        if (assets.Length > 0)
        {
            cache = assets[0];
            return cache;
        }

        GameLog.Warning($"No {catalogLabel} found.");
        return null;
    }
}