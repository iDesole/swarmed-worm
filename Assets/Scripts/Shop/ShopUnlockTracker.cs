using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks unlocked shop offers for the current play session only.
/// </summary>
public static class ShopUnlockTracker
{
    private static readonly HashSet<string> Unlocked = new();

    public static void ResetSession() => Unlocked.Clear();

    public static bool IsUnlocked(string offerKey)
    {
        if (string.IsNullOrWhiteSpace(offerKey))
            return false;

        return Unlocked.Contains(offerKey);
    }

    public static void Unlock(string offerKey)
    {
        if (string.IsNullOrWhiteSpace(offerKey))
            return;

        Unlocked.Add(offerKey);
    }

    public static void ApplyDefaultUnlocks(ShopOffer[] offers)
    {
        if (offers == null)
            return;

        for (int i = 0; i < offers.Length; i++)
        {
            ShopOffer offer = offers[i];
            if (offer == null || !offer.unlockedByDefault)
                continue;

            Unlock(ShopCatalogResolver.BuildOfferKey(offer));
        }
    }
}