using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Runtime helpers for instantiating and cleaning up tilemap environment prefabs.
/// </summary>
/// <remarks>
/// GameManager calls Spawn → AlignMapCenterToWorld (hubs) → FinalizeEnvironmentMap.
/// DestroyExistingMaps removes stale maps before each transition.
/// </remarks>
public static class EnvironmentFactory
{
    public static EnvironmentMap Spawn(GameObject prefab, Vector3 position, Transform parent = null)
    {
        if (prefab == null)
            return null;

        GameObject instance = Object.Instantiate(prefab, position, Quaternion.identity, parent);
        instance.name = prefab.name;
        instance.transform.position = position;

        EnvironmentMap environmentMap = instance.GetComponent<EnvironmentMap>();
        if (environmentMap == null)
            environmentMap = instance.AddComponent<EnvironmentMap>();

        if (environmentMap.GroundTilemap == null)
            GameLog.Warning($"Prefab '{prefab.name}' has no Tilemap.");

        return environmentMap;
    }

    public static void FinalizeEnvironmentMap(EnvironmentMap environmentMap, bool repairSprites = false)
    {
        if (environmentMap == null)
            return;

        Tilemap[] tilemaps = environmentMap.GetComponentsInChildren<Tilemap>(true);
        foreach (Tilemap tilemap in tilemaps)
            FinalizeSpawnedTilemap(tilemap, repairSprites);
    }

    public static void FinalizeSpawnedTilemap(Tilemap tilemap, bool repairSprites = false)
    {
        if (tilemap == null)
            return;

        tilemap.color = Color.white;
        tilemap.CompressBounds();

        if (repairSprites)
        {
            RenderVisibilityUtility.RepairTilemapSpriteReferences(tilemap);
            tilemap.RefreshAllTiles();
        }

        TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();
        RenderVisibilityUtility.ConfigureTilemapRenderer(renderer);
    }

    public static void AlignMapCenterToWorld(EnvironmentMap environmentMap, Vector3 worldPosition)
    {
        if (environmentMap == null)
            return;

        Tilemap tilemap = environmentMap.GroundTilemap;
        if (tilemap == null)
            return;

        tilemap.CompressBounds();

        Bounds bounds = tilemap.localBounds;
        if (bounds.size.sqrMagnitude <= 0f)
            return;

        Vector3 worldCenter = tilemap.transform.TransformPoint(bounds.center);
        environmentMap.transform.position += worldPosition - worldCenter;
    }

    public static void DestroyExistingMaps()
    {
        EnvironmentMap[] maps = Object.FindObjectsByType<EnvironmentMap>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (EnvironmentMap map in maps)
        {
            if (map != null && map.gameObject != null)
                Object.Destroy(map.gameObject);
        }

        Grid[] grids = Object.FindObjectsByType<Grid>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Grid grid in grids)
        {
            if (grid == null || grid.gameObject == null)
                continue;

            Object.Destroy(grid.gameObject);
        }
    }
}