using System;
using System.Collections.Generic;
using UnityEngine;

public enum LootRewardKind
{
    Weapon,
    Currency,
    Item,
    Artifact
}

public static class LootRollConstants
{
    public const int RollScale = 10000;
    public const int MaxDropsPerKill = 2;
}

[Serializable]
public struct LootReward
{
    public LootRewardKind kind;
    public WeaponSelection weapon;
    public string rewardId;
    public string displayName;
    public int quantity;
}

[Serializable]
public class EnemyLootEntry
{
    public LootRewardKind kind = LootRewardKind.Weapon;

    [Tooltip("Weapon from WeaponDatabase when kind is Weapon.")]
    public WeaponSelection weapon;

    [Tooltip("Identifier for currency, items, or artifacts.")]
    public string rewardId = "";

    [Tooltip("Display name used for inventory popups.")]
    public string displayName = "Loot";

    [Tooltip("Used when kind is Currency.")]
    public int currencyAmount = 1;

    [Tooltip("Drop chance out of 10000. 10000 = guaranteed, 2500 = 25%, 1000 = 10%.")]
    [Range(0, LootRollConstants.RollScale)]
    public int weight = 500;

    public int minQuantity = 1;
    public int maxQuantity = 1;

    public bool IsValid =>
        kind switch
        {
            LootRewardKind.Weapon => weapon.IsValid,
            LootRewardKind.Currency => currencyAmount > 0,
            LootRewardKind.Item or LootRewardKind.Artifact => !string.IsNullOrWhiteSpace(rewardId),
            _ => false
        };

    public bool RollsSuccess() =>
        IsValid && weight > 0 && UnityEngine.Random.Range(0, LootRollConstants.RollScale) < weight;

    public EnemyLootEntry Clone() =>
        new()
        {
            kind = kind,
            weapon = weapon,
            rewardId = rewardId,
            displayName = displayName,
            currencyAmount = currencyAmount,
            weight = weight,
            minQuantity = minQuantity,
            maxQuantity = maxQuantity
        };

    public LootReward? ToReward()
    {
        if (!IsValid)
            return null;

        int quantity = Mathf.Max(1, UnityEngine.Random.Range(minQuantity, maxQuantity + 1));
        string resolvedName = ResolveDisplayName();

        return kind switch
        {
            LootRewardKind.Weapon => new LootReward
            {
                kind = LootRewardKind.Weapon,
                weapon = weapon,
                displayName = resolvedName,
                quantity = 1
            },
            LootRewardKind.Currency => new LootReward
            {
                kind = LootRewardKind.Currency,
                rewardId = string.IsNullOrWhiteSpace(rewardId) ? "credits" : rewardId,
                displayName = resolvedName,
                quantity = Mathf.Max(1, currencyAmount) * quantity
            },
            LootRewardKind.Item or LootRewardKind.Artifact => new LootReward
            {
                kind = kind,
                rewardId = rewardId,
                displayName = resolvedName,
                quantity = quantity
            },
            _ => null
        };
    }

    private string ResolveDisplayName()
    {
        if (kind == LootRewardKind.Weapon)
        {
            WeaponDatabase database = WeaponCatalog.Database;
            if (database != null && weapon.IsValid)
            {
                string weaponName = database.GetWeaponName(weapon);
                if (!string.IsNullOrWhiteSpace(weaponName))
                    return weaponName;
            }
        }

        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName;

        if (!string.IsNullOrWhiteSpace(rewardId))
            return rewardId;

        return kind.ToString();
    }
}

[Serializable]
public class EnemyLootPool
{
    [Tooltip("Chance out of 10000 to allow a second drop when multiple entries succeed.")]
    [Range(0, LootRollConstants.RollScale)]
    public int secondDropChance = 150;

    [Tooltip("Each entry rolls independently out of 10000 on enemy death.")]
    public List<EnemyLootEntry> entries = new();

    public bool HasEntries => entries != null && entries.Count > 0;

    public EnemyLootPool Clone()
    {
        var clone = new EnemyLootPool
        {
            secondDropChance = secondDropChance,
            entries = new List<EnemyLootEntry>()
        };

        if (entries == null)
            return clone;

        foreach (EnemyLootEntry entry in entries)
        {
            if (entry != null)
                clone.entries.Add(entry.Clone());
        }

        return clone;
    }

    public void Normalize()
    {
        secondDropChance = Mathf.Clamp(secondDropChance, 0, LootRollConstants.RollScale);
        entries ??= new List<EnemyLootEntry>();

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            EnemyLootEntry entry = entries[i];
            if (entry == null)
            {
                entries.RemoveAt(i);
                continue;
            }

            entry.weight = Mathf.Clamp(entry.weight, 0, LootRollConstants.RollScale);
            entry.minQuantity = Mathf.Max(1, entry.minQuantity);
            entry.maxQuantity = Mathf.Max(entry.minQuantity, entry.maxQuantity);
        }
    }

    public List<LootReward> RollRewards()
    {
        var results = new List<LootReward>();
        Normalize();

        if (!HasEntries)
            return results;

        foreach (EnemyLootEntry entry in entries)
        {
            if (entry == null || !entry.RollsSuccess())
                continue;

            LootReward? reward = entry.ToReward();
            if (reward.HasValue)
                results.Add(reward.Value);
        }

        ApplyDropCap(results);
        return results;
    }

    private void ApplyDropCap(List<LootReward> results)
    {
        if (results.Count <= 1)
            return;

        bool allowSecondDrop = UnityEngine.Random.Range(0, LootRollConstants.RollScale) < secondDropChance;
        int keepCount = allowSecondDrop
            ? Mathf.Min(results.Count, LootRollConstants.MaxDropsPerKill)
            : 1;

        Shuffle(results);

        while (results.Count > keepCount)
            results.RemoveAt(results.Count - 1);
    }

    private static void Shuffle(List<LootReward> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int swapIndex = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
        }
    }
}