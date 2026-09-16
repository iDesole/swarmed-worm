using System;
using UnityEngine;

public enum ShopOfferKind
{
    Player,
    Weapon
}

[Serializable]
public class ShopOffer
{
    [Tooltip("Sell a playable character or a weapon.")]
    public ShopOfferKind kind = ShopOfferKind.Player;

    [Tooltip("Which character from PlayerDatabase.")]
    public int playerIndex;

    [Tooltip("Which weapon from WeaponDatabase.")]
    public WeaponSelection weapon = WeaponSelection.None;

    [Tooltip("Available immediately without purchase.")]
    public bool unlockedByDefault;
}

public readonly struct ShopOfferDisplay
{
    public ShopOfferDisplay(
        string offerKey,
        ShopOffer source,
        string displayName,
        Sprite sprite,
        float displayScale,
        int price,
        bool isValid)
    {
        OfferKey = offerKey;
        Source = source;
        DisplayName = displayName;
        Sprite = sprite;
        DisplayScale = displayScale;
        Price = price;
        IsValid = isValid;
    }

    public string OfferKey { get; }
    public ShopOffer Source { get; }
    public string DisplayName { get; }
    public Sprite Sprite { get; }
    public float DisplayScale { get; }
    public int Price { get; }
    public bool IsValid { get; }

    public ShopOfferKind Kind => Source != null ? Source.kind : ShopOfferKind.Player;
    public bool IsPlayer => Kind == ShopOfferKind.Player;

    public static ShopOfferDisplay Invalid =>
        new(string.Empty, null, "Unknown", null, 1f, 0, false);
}