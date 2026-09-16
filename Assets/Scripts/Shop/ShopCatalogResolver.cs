using UnityEngine;

/// <summary>
/// Pulls shop card name, sprite, and price from PlayerDatabase and WeaponDatabase.
/// </summary>
public static class ShopCatalogResolver
{
    public static ShopOfferDisplay Resolve(ShopOffer offer)
    {
        if (offer == null)
            return ShopOfferDisplay.Invalid;

        return offer.kind switch
        {
            ShopOfferKind.Player => ResolvePlayer(offer),
            ShopOfferKind.Weapon => ResolveWeapon(offer),
            _ => ShopOfferDisplay.Invalid
        };
    }

    public static string BuildOfferKey(ShopOffer offer)
    {
        if (offer == null)
            return string.Empty;

        return offer.kind switch
        {
            ShopOfferKind.Player => $"player:{offer.playerIndex}",
            ShopOfferKind.Weapon when offer.weapon.IsValid =>
                $"weapon:{offer.weapon.type}:{offer.weapon.index}",
            _ => string.Empty
        };
    }

    private static ShopOfferDisplay ResolvePlayer(ShopOffer offer)
    {
        PlayerDatabase database = PlayerCatalog.Database;
        PlayerCharacterDefinition character = database != null
            ? database.GetCharacter(PlayerSelection.At(offer.playerIndex))
            : null;

        if (character?.core == null)
            return ShopOfferDisplay.Invalid;

        PlayerCoreStats core = character.core;

        Sprite portrait = core.shopSprite != null ? core.shopSprite : core.sprite;

        return new ShopOfferDisplay(
            BuildOfferKey(offer),
            offer,
            character.CharacterName,
            portrait,
            core.displayScale,
            core.shopPrice,
            true);
    }

    private static ShopOfferDisplay ResolveWeapon(ShopOffer offer)
    {
        WeaponDatabase database = WeaponCatalog.Database;
        if (database == null || !offer.weapon.IsValid)
            return ShopOfferDisplay.Invalid;

        if (!database.TryGetWeaponCore(offer.weapon, out WeaponCoreStats core) || core == null)
            return ShopOfferDisplay.Invalid;

        Sprite portrait = core.shopSprite != null ? core.shopSprite : database.GetWeaponSprite(offer.weapon);

        return new ShopOfferDisplay(
            BuildOfferKey(offer),
            offer,
            database.GetWeaponName(offer.weapon),
            portrait,
            core.displayScale,
            core.shopPrice,
            true);
    }
}