using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rolls loot from an enemy's loot pool and spawns world pickups on death.
/// Every kill drops baseline credits plus any bonus rolls (Destiny-style).
/// </summary>
public static class LootDropper
{
    private const int GuaranteedCreditsMin = 4;
    private const int GuaranteedCreditsMax = 14;
    private const int BonusWeaponRollScale = 10000;
    private const int BonusWeaponChance = 1200;

    public static void DropFrom(Enemy enemy)
    {
        if (enemy == null)
            return;

        List<LootReward> rewards = RollRewards(enemy);
        rewards.Add(CreateGuaranteedCredits());
        WorldLootSpawner.SpawnDrops(enemy.transform.position, rewards);
    }

    private static List<LootReward> RollRewards(Enemy enemy)
    {
        var rewards = new List<LootReward>();

        EnemyLootPool pool = enemy.LootPool;
        if (pool != null && pool.HasEntries)
        {
            rewards.AddRange(pool.RollRewards());
            return rewards;
        }

        LootReward? bonusWeapon = TryRollBonusWeapon();
        if (bonusWeapon.HasValue)
            rewards.Add(bonusWeapon.Value);

        return rewards;
    }

    private static LootReward CreateGuaranteedCredits()
    {
        int amount = Random.Range(GuaranteedCreditsMin, GuaranteedCreditsMax + 1);
        return new LootReward
        {
            kind = LootRewardKind.Currency,
            rewardId = "credits",
            displayName = "Credits",
            quantity = amount
        };
    }

    private static LootReward? TryRollBonusWeapon()
    {
        if (Random.Range(0, BonusWeaponRollScale) >= BonusWeaponChance)
            return null;

        WeaponDatabase database = WeaponCatalog.Database;
        if (database == null)
            return null;

        List<WeaponSelection> candidates = BuildWeaponCandidates(database);
        if (candidates.Count == 0)
            return null;

        WeaponSelection selection = candidates[Random.Range(0, candidates.Count)];
        string displayName = database.GetWeaponName(selection);

        return new LootReward
        {
            kind = LootRewardKind.Weapon,
            weapon = selection,
            displayName = string.IsNullOrWhiteSpace(displayName) ? "Weapon" : displayName,
            quantity = 1
        };
    }

    private static List<WeaponSelection> BuildWeaponCandidates(WeaponDatabase database)
    {
        var candidates = new List<WeaponSelection>();

        for (int i = 0; i < database.ProjectileWeapons.Count; i++)
            candidates.Add(WeaponSelection.Projectile(i));

        for (int i = 0; i < database.MeleeWeapons.Count; i++)
            candidates.Add(WeaponSelection.Melee(i));

        for (int i = 0; i < database.SummonWeapons.Count; i++)
            candidates.Add(WeaponSelection.Summon(i));

        return candidates;
    }
}