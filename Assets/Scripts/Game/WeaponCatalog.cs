using UnityEngine;

/// <summary>Runtime accessor for <see cref="WeaponDatabase"/> and weapon creation helpers.</summary>
public static class WeaponCatalog
{
    private static WeaponDatabase database;

    public static WeaponDatabase Database =>
        CatalogResolver.Resolve(ref database, nameof(WeaponDatabase));

    public static void Register(WeaponDatabase weaponDatabase)
    {
        if (weaponDatabase != null)
            database = weaponDatabase;
    }
}