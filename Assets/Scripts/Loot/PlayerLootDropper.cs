using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drops the player's equipped and stored weapons as world pickups on death.
/// </summary>
public static class PlayerLootDropper
{
    public static void DropFrom(Player player)
    {
        if (player == null)
            return;

        var rewards = new List<LootReward>();
        CollectEquippedWeapons(player, rewards);
        CollectInventoryWeapons(player, rewards);

        if (rewards.Count == 0)
            return;

        WorldLootSpawner.SpawnDrops(player.transform.position, rewards);
    }

    private static void CollectEquippedWeapons(Player player, List<LootReward> rewards)
    {
        for (int slot = 1; slot <= 5; slot++)
        {
            WeaponBase weapon = player.GetWeaponInSlot(slot);
            if (weapon == null || !weapon.sourceSelection.IsValid)
                continue;

            rewards.Add(CreateWeaponReward(weapon.sourceSelection, weapon.weaponName));
        }
    }

    private static void CollectInventoryWeapons(Player player, List<LootReward> rewards)
    {
        PlayerInventory inventory = player.inventory;
        if (inventory == null)
            return;

        for (int i = 0; i < PlayerInventory.WeaponSlotCount; i++)
            TryAddInventoryWeapon(inventory.GetWeaponSlot(i), rewards);

        for (int i = 0; i < PlayerInventory.InventorySlotCount; i++)
            TryAddInventoryWeapon(inventory.GetInventorySlot(i), rewards);
    }

    private static void TryAddInventoryWeapon(InventorySlotData slot, List<LootReward> rewards)
    {
        if (slot.IsEmpty || slot.kind != InventoryItemKind.Weapon || !slot.weapon.IsValid)
            return;

        rewards.Add(CreateWeaponReward(slot.weapon, slot.displayName));
    }

    private static LootReward CreateWeaponReward(WeaponSelection selection, string displayName)
    {
        WeaponDatabase database = WeaponCatalog.Database;
        string resolvedName = displayName;
        if (string.IsNullOrWhiteSpace(resolvedName) && database != null)
            resolvedName = database.GetWeaponName(selection);

        if (string.IsNullOrWhiteSpace(resolvedName))
            resolvedName = "Weapon";

        return new LootReward
        {
            kind = LootRewardKind.Weapon,
            weapon = selection,
            displayName = resolvedName,
            quantity = 1
        };
    }
}