using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns physical world pickups when enemies die (Destiny-style loot on the ground).
/// </summary>
public static class WorldLootSpawner
{
    private const float DropSpreadRadius = 0.42f;
    private static readonly Dictionary<Color, Sprite> fallbackSprites = new();

    public static void SpawnDrops(Vector3 origin, IReadOnlyList<LootReward> rewards)
    {
        if (rewards == null || rewards.Count == 0)
            return;

        int spawnIndex = 0;
        for (int i = 0; i < rewards.Count; i++)
        {
            LootReward reward = rewards[i];
            if (!IsSpawnable(reward))
                continue;

            Vector3 position = origin + (Vector3)GetSpreadOffset(spawnIndex, rewards.Count);
            SpawnPickup(position, reward);
            spawnIndex++;
        }
    }

    private static bool IsSpawnable(LootReward reward) =>
        reward.kind switch
        {
            LootRewardKind.Weapon => reward.weapon.IsValid,
            LootRewardKind.Currency => reward.quantity > 0,
            LootRewardKind.Item or LootRewardKind.Artifact => !string.IsNullOrWhiteSpace(reward.rewardId),
            _ => false
        };

    private static void SpawnPickup(Vector3 position, LootReward reward)
    {
        string label = string.IsNullOrWhiteSpace(reward.displayName)
            ? reward.kind.ToString()
            : reward.displayName;

        if (reward.quantity > 1)
            label = $"{label} x{reward.quantity}";

        var pickupObject = new GameObject($"Pickup: {label}");
        pickupObject.transform.position = position;

        var pickup = pickupObject.AddComponent<WorldLootPickup>();
        pickup.Initialize(reward, ResolvePickupSprite(reward));
    }

    private static Sprite ResolvePickupSprite(LootReward reward)
    {
        switch (reward.kind)
        {
            case LootRewardKind.Weapon:
                return ResolveWeaponSprite(reward.weapon) ?? GetFallbackSprite(new Color(0.95f, 0.82f, 0.35f));

            case LootRewardKind.Currency:
                return GetFallbackSprite(new Color(0.45f, 0.95f, 0.55f));

            case LootRewardKind.Artifact:
                return GetFallbackSprite(new Color(0.72f, 0.48f, 0.98f));

            default:
                return GetFallbackSprite(new Color(0.55f, 0.78f, 0.98f));
        }
    }

    private static Sprite ResolveWeaponSprite(WeaponSelection selection)
    {
        WeaponDatabase database = WeaponCatalog.Database;
        return database != null ? database.GetWeaponSprite(selection) : null;
    }

    private static Vector2 GetSpreadOffset(int index, int count)
    {
        if (count <= 1)
            return Vector2.zero;

        if (count == 2)
        {
            float half = DropSpreadRadius * 0.5f;
            return index == 0 ? new Vector2(-half, 0f) : new Vector2(half, 0f);
        }

        float angle = (Mathf.PI * 2f * index) / count;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * DropSpreadRadius;
    }

    private static Sprite GetFallbackSprite(Color color)
    {
        if (fallbackSprites.TryGetValue(color, out Sprite cached))
            return cached;

        var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        Color[] pixels = new Color[16 * 16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;

        texture.SetPixels(pixels);
        texture.Apply();

        cached = Sprite.Create(
            texture,
            new Rect(0f, 0f, 16f, 16f),
            new Vector2(0.5f, 0.5f),
            16f);

        fallbackSprites[color] = cached;
        return cached;
    }
}