using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Ensures sprites and tilemaps render correctly under URP 2D (shared unlit material, sorting).
/// </summary>
/// <remarks>
/// Called when spawning players, weapons, environments, and loot so art is visible without per-prefab setup.
/// </remarks>
public static class RenderVisibilityUtility
{
    private const string UnlitShaderName = "Universal Render Pipeline/2D/Sprite-Unlit-Default";
    private const string FallbackShaderName = "Sprites-Default";

    private static Material sharedUnlitMaterial;

    public static void ConfigureSpriteRenderer(SpriteRenderer renderer, int sortingOrder = 0)
    {
        if (renderer == null)
            return;

        renderer.sortingOrder = sortingOrder;
        renderer.enabled = true;
        ApplyUnlitMaterial(renderer);
    }

    public static void ConfigureTilemapRenderer(TilemapRenderer renderer, int? sortingOrder = null)
    {
        if (renderer == null)
            return;

        if (sortingOrder.HasValue)
            renderer.sortingOrder = sortingOrder.Value;

        renderer.enabled = true;
        ApplyUnlitMaterial(renderer);
    }

    public static void RepairTilemapSpriteReferences(Tilemap tilemap)
    {
        if (tilemap == null)
            return;

        BoundsInt bounds = tilemap.cellBounds;
        if (bounds.size.x <= 0 || bounds.size.y <= 0)
            return;

        TileBase[] tiles = tilemap.GetTilesBlock(bounds);
        int index = 0;

        foreach (Vector3Int position in bounds.allPositionsWithin)
        {
            TileBase tile = tiles[index++];
            if (tile == null)
                continue;

            tilemap.SetTile(position, null);
            tilemap.SetTile(position, tile);
        }
    }

    public static void ApplyUnlitMaterial(Renderer renderer)
    {
        if (renderer == null)
            return;

        Material material = GetSharedUnlitMaterial();
        if (material != null)
            renderer.sharedMaterial = material;
    }

    public static Material GetSharedUnlitMaterial()
    {
        if (sharedUnlitMaterial != null)
            return sharedUnlitMaterial;

        Material packageMaterial = LoadPackageUnlitMaterial();
        if (packageMaterial != null)
        {
            sharedUnlitMaterial = packageMaterial;
            return sharedUnlitMaterial;
        }

        Shader shader = ResolveUnlitShader();
        if (shader == null)
            return null;

        sharedUnlitMaterial = new Material(shader)
        {
            name = "SharedSpriteUnlit",
            hideFlags = HideFlags.HideAndDontSave
        };

        return sharedUnlitMaterial;
    }

    private static Material LoadPackageUnlitMaterial()
    {
#if UNITY_EDITOR
        const string packagePath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
        return AssetDatabase.LoadAssetAtPath<Material>(packagePath);
#else
        return null;
#endif
    }

    private static Shader ResolveUnlitShader()
    {
        Shader shader = Shader.Find(UnlitShaderName);
        if (shader != null)
            return shader;

        return Shader.Find(FallbackShaderName);
    }

    public static Sprite LoadDefaultPlayerSprite()
    {
        PlayerDatabase database = PlayerCatalog.Database;
        PlayerCharacterDefinition character = database?.GetDefaultCharacter();
        if (character?.core?.sprite != null)
            return character.core.sprite;

#if UNITY_EDITOR
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("Assets/GameData/Player/Alien Worm.png");
        if (assets != null)
        {
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite)
                    return sprite;
            }
        }
#endif

        Sprite[] loadedSprites = Resources.LoadAll<Sprite>("Player/Alien Worm");
        if (loadedSprites != null && loadedSprites.Length > 0)
            return loadedSprites[0];

        return null;
    }
}