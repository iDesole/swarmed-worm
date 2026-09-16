using System;
using UnityEngine;

public enum InventoryItemKind
{
    Empty,
    Weapon,
    Item,
    Currency,
    Artifact
}

[Serializable]
public struct InventorySlotData
{
    public InventoryItemKind kind;
    public WeaponSelection weapon;
    public string itemId;
    public string displayName;
    public int quantity;

    public bool IsEmpty => kind == InventoryItemKind.Empty;

    public static InventorySlotData EmptySlot =>
        new() { kind = InventoryItemKind.Empty, quantity = 0 };

    public static InventorySlotData FromWeapon(WeaponSelection selection, string name = null) =>
        new()
        {
            kind = InventoryItemKind.Weapon,
            weapon = selection,
            displayName = name ?? string.Empty,
            quantity = 1
        };

    public static InventorySlotData FromLoot(LootReward reward)
    {
        switch (reward.kind)
        {
            case LootRewardKind.Weapon:
                return FromWeapon(reward.weapon, reward.displayName);

            case LootRewardKind.Artifact:
                return new InventorySlotData
                {
                    kind = InventoryItemKind.Artifact,
                    itemId = reward.rewardId,
                    displayName = reward.displayName,
                    quantity = Mathf.Max(1, reward.quantity)
                };

            case LootRewardKind.Currency:
                return new InventorySlotData
                {
                    kind = InventoryItemKind.Currency,
                    itemId = string.IsNullOrWhiteSpace(reward.rewardId) ? "credits" : reward.rewardId,
                    displayName = reward.displayName,
                    quantity = Mathf.Max(1, reward.quantity)
                };

            default:
                return new InventorySlotData
                {
                    kind = InventoryItemKind.Item,
                    itemId = reward.rewardId,
                    displayName = reward.displayName,
                    quantity = Mathf.Max(1, reward.quantity)
                };
        }
    }
}

/// <summary>
/// Runtime inventory with dedicated weapon, item, and artifact grids.
/// </summary>
[Serializable]
public class PlayerInventory
{
    public const int WeaponSlotCount = 5;
    public const int InventoryColumns = 10;
    public const int InventoryRows = 3;
    public const int InventorySlotCount = InventoryColumns * InventoryRows;
    public const int ArtifactSlotCount = 5;

    [SerializeField] private InventorySlotData[] weaponSlots = CreateArray(WeaponSlotCount);
    [SerializeField] private InventorySlotData[] inventorySlots = CreateArray(InventorySlotCount);
    [SerializeField] private InventorySlotData[] artifactSlots = CreateArray(ArtifactSlotCount);

    public event Action OnChanged;

    public InventorySlotData GetWeaponSlot(int index) => weaponSlots[ClampWeapon(index)];
    public InventorySlotData GetInventorySlot(int index) => inventorySlots[ClampInventory(index)];
    public InventorySlotData GetArtifactSlot(int index) => artifactSlots[ClampArtifact(index)];

    public int GetCurrencyTotal(string currencyId = "credits")
    {
        int total = 0;
        AccumulateCurrency(weaponSlots, currencyId, ref total);
        AccumulateCurrency(inventorySlots, currencyId, ref total);
        AccumulateCurrency(artifactSlots, currencyId, ref total);
        return total;
    }

    public bool TrySpendCurrency(int amount, string currencyId = "credits")
    {
        if (amount <= 0)
            return true;

        if (GetCurrencyTotal(currencyId) < amount)
            return false;

        int remaining = amount;
        remaining = DeductCurrency(weaponSlots, currencyId, remaining);
        remaining = DeductCurrency(inventorySlots, currencyId, remaining);
        remaining = DeductCurrency(artifactSlots, currencyId, remaining);

        if (remaining > 0)
            return false;

        NotifyChanged();
        return true;
    }

    public string GetCurrencyDisplayName(string currencyId = "credits")
    {
        if (TryFindCurrencyDisplayName(weaponSlots, currencyId, out string displayName))
            return displayName;

        if (TryFindCurrencyDisplayName(inventorySlots, currencyId, out displayName))
            return displayName;

        if (TryFindCurrencyDisplayName(artifactSlots, currencyId, out displayName))
            return displayName;

        return string.IsNullOrWhiteSpace(currencyId) ? "Currency" : currencyId;
    }

    public void SetWeaponSlot(int index, InventorySlotData slot)
    {
        weaponSlots[ClampWeapon(index)] = slot;
        NotifyChanged();
    }

    public void ClearWeaponSlot(int index) => SetWeaponSlot(index, InventorySlotData.EmptySlot);

    public void ClearInventorySlot(int index)
    {
        inventorySlots[ClampInventory(index)] = InventorySlotData.EmptySlot;
        NotifyChanged();
    }

    public void ClearArtifactSlot(int index)
    {
        artifactSlots[ClampArtifact(index)] = InventorySlotData.EmptySlot;
        NotifyChanged();
    }

    public int GetWeaponCount()
    {
        int count = 0;
        AccumulateWeaponCount(weaponSlots, ref count);
        AccumulateWeaponCount(inventorySlots, ref count);
        return count;
    }

    public bool TryAddLoot(LootReward reward)
    {
        InventorySlotData slot = InventorySlotData.FromLoot(reward);

        return reward.kind switch
        {
            LootRewardKind.Weapon => TryAddWeapon(slot),
            LootRewardKind.Artifact => TryAddArtifact(slot),
            _ => TryAddInventoryItem(slot)
        };
    }

    public bool TryAddWeapon(InventorySlotData slot)
    {
        if (slot.kind != InventoryItemKind.Weapon || !slot.weapon.IsValid)
            return false;

        for (int i = 0; i < weaponSlots.Length; i++)
        {
            if (!weaponSlots[i].IsEmpty)
                continue;

            weaponSlots[i] = slot;
            NotifyChanged();
            return true;
        }

        return TryAddInventoryItem(slot);
    }

    public bool TryAddInventoryItem(InventorySlotData slot)
    {
        if (slot.IsEmpty)
            return false;

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (!inventorySlots[i].IsEmpty)
                continue;

            inventorySlots[i] = slot;
            NotifyChanged();
            return true;
        }

        return false;
    }

    public bool TryAddArtifact(InventorySlotData slot)
    {
        if (slot.kind != InventoryItemKind.Artifact)
            return false;

        for (int i = 0; i < artifactSlots.Length; i++)
        {
            if (!artifactSlots[i].IsEmpty)
                continue;

            artifactSlots[i] = slot;
            NotifyChanged();
            return true;
        }

        return TryAddInventoryItem(slot);
    }

    public void SyncWeaponSlotFromEquipped(int slotNumber1to5, WeaponSelection selection, string displayName = null)
    {
        if (slotNumber1to5 < 1 || slotNumber1to5 > WeaponSlotCount)
            return;

        int index = slotNumber1to5 - 1;
        if (!selection.IsValid)
        {
            weaponSlots[index] = InventorySlotData.EmptySlot;
            NotifyChanged();
            return;
        }

        weaponSlots[index] = InventorySlotData.FromWeapon(selection, displayName);
        NotifyChanged();
    }

    private static InventorySlotData[] CreateArray(int length)
    {
        var slots = new InventorySlotData[length];
        for (int i = 0; i < slots.Length; i++)
            slots[i] = InventorySlotData.EmptySlot;

        return slots;
    }

    private void NotifyChanged() => OnChanged?.Invoke();

    private static void AccumulateWeaponCount(InventorySlotData[] slots, ref int count)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].kind == InventoryItemKind.Weapon)
                count++;
        }
    }

    private static int DeductCurrency(InventorySlotData[] slots, string currencyId, int remaining)
    {
        for (int i = 0; i < slots.Length && remaining > 0; i++)
        {
            InventorySlotData slot = slots[i];
            if (slot.kind != InventoryItemKind.Currency)
                continue;

            if (!string.IsNullOrWhiteSpace(currencyId) &&
                !string.Equals(slot.itemId, currencyId, StringComparison.OrdinalIgnoreCase))
                continue;

            int spend = Mathf.Min(remaining, Mathf.Max(0, slot.quantity));
            if (spend <= 0)
                continue;

            slot.quantity -= spend;
            remaining -= spend;

            if (slot.quantity <= 0)
                slots[i] = InventorySlotData.EmptySlot;
            else
                slots[i] = slot;
        }

        return remaining;
    }

    private static void AccumulateCurrency(InventorySlotData[] slots, string currencyId, ref int total)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            InventorySlotData slot = slots[i];
            if (slot.kind != InventoryItemKind.Currency)
                continue;

            if (!string.IsNullOrWhiteSpace(currencyId) &&
                !string.Equals(slot.itemId, currencyId, StringComparison.OrdinalIgnoreCase))
                continue;

            total += Mathf.Max(0, slot.quantity);
        }
    }

    private static bool TryFindCurrencyDisplayName(InventorySlotData[] slots, string currencyId, out string displayName)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            InventorySlotData slot = slots[i];
            if (slot.kind != InventoryItemKind.Currency)
                continue;

            if (!string.IsNullOrWhiteSpace(currencyId) &&
                !string.Equals(slot.itemId, currencyId, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.IsNullOrWhiteSpace(slot.displayName))
            {
                displayName = slot.displayName;
                return true;
            }
        }

        displayName = string.Empty;
        return false;
    }

    private static int ClampWeapon(int index) => Mathf.Clamp(index, 0, WeaponSlotCount - 1);
    private static int ClampInventory(int index) => Mathf.Clamp(index, 0, InventorySlotCount - 1);
    private static int ClampArtifact(int index) => Mathf.Clamp(index, 0, ArtifactSlotCount - 1);
}