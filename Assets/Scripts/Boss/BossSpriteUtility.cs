using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>Resolves boss sprites when database references are missing or animation frames are null.</summary>
public static class BossSpriteUtility
{
    private const string TrilobiteIdlePath = "Assets/GameData/Enemies/Sprites/Beach/TrilobiteIdle.png";
    private const string TrilobiteMovePath = "Assets/GameData/Enemies/Sprites/Beach/TrilobiteMovement.png";
    private const string TrilobiteIdleResourcesPath = "Bosses/Sprites/Beach/TrilobiteIdle";
    private const string TrilobiteMoveResourcesPath = "Bosses/Sprites/Beach/TrilobiteMovement";

    public static void EnsureCoreSprites(BossCoreStats core)
    {
        if (core == null)
            return;

        SanitizeClips(core);

        if (core.sprite == null)
            core.sprite = ResolvePrimarySprite(core);
    }

    public static Sprite ResolvePrimarySprite(BossCoreStats core)
    {
        if (core == null)
            return null;

        if (core.sprite != null)
            return core.sprite;

        Sprite fromClips = GetFirstValidClipSprite(core.animationClips);
        if (fromClips != null)
            return fromClips;

        return LoadBossSprite(core.bossName);
    }

    public static void EnsureBossVisible(Boss boss, BossCoreStats core)
    {
        if (boss == null || core == null)
            return;

        ApplyRendererSprite(boss, core);
    }

    public static Sprite ResolveSpriteForBossName(string bossName)
    {
        if (string.IsNullOrWhiteSpace(bossName))
            return null;

        BossSelection selection = BossCatalog.Database != null
            ? BossCatalog.Database.FindSelection(bossName)
            : BossSelection.None;

        if (selection.IsValid &&
            BossCatalog.Database.TryGetBossCore(selection, out BossCoreStats core) &&
            core != null)
        {
            BossCoreStats snapshot = core.Clone();
            EnsureCoreSprites(snapshot);
            Sprite fromCore = ResolvePrimarySprite(snapshot);
            if (fromCore != null)
                return fromCore;
        }

        return LoadBossSprite(bossName);
    }

    public static void ApplyRendererSprite(Boss boss, BossCoreStats core)
    {
        if (boss == null)
            return;

        SpriteRenderer spriteRenderer = boss.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            return;

        BossCoreStats resolvedCore = core ?? boss.VisualCore;
        Sprite sprite = resolvedCore != null
            ? ResolvePrimarySprite(resolvedCore)
            : ResolveSpriteForBossName(boss.name);

        if (sprite != null)
            spriteRenderer.sprite = sprite;

        spriteRenderer.enabled = true;
        RenderVisibilityUtility.ConfigureSpriteRenderer(
            spriteRenderer,
            sortingOrder: CharacterPresentationConstants.CharacterSpriteSortingOrder);
    }

    private static void SanitizeClips(BossCoreStats core)
    {
        if (core.animationClips == null || core.animationClips.Length == 0)
            return;

        for (int i = 0; i < core.animationClips.Length; i++)
        {
            CharacterAnimationClipEntry clip = core.animationClips[i];
            if (clip == null || ClipHasValidSprite(clip))
                continue;

            string title = clip.title ?? string.Empty;
            Sprite[] rebuilt = LoadClipSprites(core.bossName, title);
            if (rebuilt != null && rebuilt.Length > 0)
                clip.frames = rebuilt;
        }

        if (core.sprite == null)
            core.sprite = GetFirstValidClipSprite(core.animationClips);
    }

    private static bool ClipHasValidSprite(CharacterAnimationClipEntry clip)
    {
        if (clip?.frames == null)
            return false;

        for (int i = 0; i < clip.frames.Length; i++)
        {
            if (clip.frames[i] != null)
                return true;
        }

        return false;
    }

    private static Sprite GetFirstValidClipSprite(CharacterAnimationClipEntry[] clips)
    {
        if (clips == null)
            return null;

        for (int i = 0; i < clips.Length; i++)
        {
            CharacterAnimationClipEntry clip = clips[i];
            if (clip?.frames == null)
                continue;

            for (int f = 0; f < clip.frames.Length; f++)
            {
                if (clip.frames[f] != null)
                    return clip.frames[f];
            }
        }

        return null;
    }

    private static Sprite[] LoadClipSprites(string bossName, string clipTitle)
    {
        if (!IsTrilobite(bossName))
            return null;

        if (clipTitle != null &&
            (clipTitle.Equals("Walk", StringComparison.OrdinalIgnoreCase) ||
             clipTitle.Equals("Move", StringComparison.OrdinalIgnoreCase) ||
             clipTitle.Equals("Moving", StringComparison.OrdinalIgnoreCase)))
        {
            return LoadSpritesFromTexture(TrilobiteMovePath);
        }

        return LoadSpritesFromTexture(TrilobiteIdlePath);
    }

    private static Sprite LoadBossSprite(string bossName)
    {
        if (!IsTrilobite(bossName))
            return null;

        Sprite[] idleSprites = LoadSpritesFromTexture(TrilobiteIdlePath);
        return idleSprites != null && idleSprites.Length > 0 ? idleSprites[0] : null;
    }

    private static bool IsTrilobite(string bossName) =>
        string.Equals(bossName, "Trilobite", StringComparison.OrdinalIgnoreCase);

    private static Sprite[] LoadSpritesFromTexture(string assetPath)
    {
#if UNITY_EDITOR
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        if (assets != null && assets.Length > 0)
        {
            var sprites = new List<Sprite>();
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                    sprites.Add(sprite);
            }

            if (sprites.Count > 0)
            {
                sprites.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
                return sprites.ToArray();
            }
        }
#endif

        return LoadSpritesFromResources(assetPath);
    }

    private static Sprite[] LoadSpritesFromResources(string assetPath)
    {
        string resourcesPath = ResolveResourcesPath(assetPath);
        if (string.IsNullOrEmpty(resourcesPath))
            return null;

        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcesPath);
        if (sprites == null || sprites.Length == 0)
            return null;

        Array.Sort(sprites, (a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
        return sprites;
    }

    private static string ResolveResourcesPath(string assetPath)
    {
        if (string.Equals(assetPath, TrilobiteIdlePath, StringComparison.Ordinal))
            return TrilobiteIdleResourcesPath;

        if (string.Equals(assetPath, TrilobiteMovePath, StringComparison.Ordinal))
            return TrilobiteMoveResourcesPath;

        return null;
    }
}