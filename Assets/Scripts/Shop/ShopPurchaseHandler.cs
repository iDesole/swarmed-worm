using UnityEngine;

/// <summary>
/// Applies shop purchases to the active player.
/// </summary>
public static class ShopPurchaseHandler
{
    public static bool IgnoreCurrency = true;

    public static bool TryPurchase(Player player, ShopOfferDisplay display, bool selectingOwned = false)
    {
        if (player == null || !display.IsValid || display.Source == null)
            return false;

        bool unlocked = ShopUnlockTracker.IsUnlocked(display.OfferKey);
        if (!unlocked)
        {
            if (!IgnoreCurrency &&
                display.Price > 0 &&
                (player.inventory == null || !player.inventory.TrySpendCurrency(display.Price)))
            {
                GameLog.Info("Not enough credits for that offer.");
                return false;
            }

            ShopUnlockTracker.Unlock(display.OfferKey);
            GameLog.Info($"Unlocked '{display.DisplayName}'.");
            return true;
        }

        if (!selectingOwned)
            return true;

        return ApplyOwnedSelection(player, display);
    }

    public static bool IsActiveSelection(Player player, ShopOfferDisplay display)
    {
        if (player == null || !display.IsValid || display.Source == null)
            return false;

        return display.IsPlayer && player.CharacterIndex == display.Source.playerIndex;
    }

    private static bool ApplyOwnedSelection(Player player, ShopOfferDisplay display)
    {
        switch (display.Kind)
        {
            case ShopOfferKind.Player:
                if (player.CharacterIndex == display.Source.playerIndex)
                    return false;

                if (GameManager.Instance != null)
                    GameManager.Instance.ApplyCharacterIndex(display.Source.playerIndex);
                else
                    player.SetCharacterIndex(display.Source.playerIndex);

                GameLog.Info($"Now playing as '{display.DisplayName}'.");
                return true;

            case ShopOfferKind.Weapon:
                return GrantWeapon(player, display);

            default:
                return false;
        }
    }

    private static bool GrantWeapon(Player player, ShopOfferDisplay display)
    {
        WeaponDatabase database = WeaponCatalog.Database;
        if (database == null)
            return false;

        WeaponSelection selection = display.Source.weapon;
        if (!selection.IsValid)
            return false;

        string weaponName = database.GetWeaponName(selection);
        bool added = player.inventory.TryAddWeapon(
            InventorySlotData.FromWeapon(selection, weaponName));

        if (!added)
        {
            GameLog.Info("Inventory full — couldn't add weapon.");
            return false;
        }

        GameLog.Info($"Added '{weaponName}' to inventory.");
        return true;
    }
}