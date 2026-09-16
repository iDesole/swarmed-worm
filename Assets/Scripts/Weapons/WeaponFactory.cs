using UnityEngine;

/// <summary>
/// Builds weapon GameObjects from database definitions at runtime.
/// </summary>
/// <remarks>
/// Data flow: WeaponDatabase asset → definition Clone() → Configure() on a new WeaponBase component.
/// Never edit database assets at runtime; Clone() keeps the source data safe.
/// </remarks>
public static class WeaponFactory
{
    /// <summary>Creates a weapon from a type + index pair (e.g. Projectile #0 = AR).</summary>
    public static WeaponBase Create(
        WeaponSelection selection,
        WeaponDatabase database,
        Transform parent = null)
    {
        if (database == null || !selection.IsValid)
            return null;

        WeaponBase weapon = selection.type switch
        {
            WeaponType.None => null,
            WeaponType.Projectile when selection.index < database.ProjectileWeapons.Count =>
                Create(database.ProjectileWeapons[selection.index], database, parent),
            WeaponType.Melee when selection.index < database.MeleeWeapons.Count =>
                Create(database.MeleeWeapons[selection.index], database, parent),
            WeaponType.Summon when selection.index < database.SummonWeapons.Count =>
                Create(database.SummonWeapons[selection.index], database, parent),
            _ => null
        };

        if (weapon != null)
            weapon.sourceSelection = selection;

        return weapon;
    }

    public static WeaponBase Create(
        ProjectileWeaponDefinition definition,
        WeaponDatabase database = null,
        Transform parent = null)
    {
        if (definition?.core == null)
            return null;

        ProjectileWeaponDefinition snapshot = definition.Clone();
        var weapon = BuildBase<ProjectileWeapon>(snapshot.core, parent);
        snapshot.Configure(weapon, database);
        return weapon;
    }

    public static WeaponBase Create(
        MeleeWeaponDefinition definition,
        WeaponDatabase database = null,
        Transform parent = null)
    {
        if (definition?.core == null)
            return null;

        MeleeWeaponDefinition snapshot = definition.Clone();
        var weapon = BuildBase<MeleeWeapon>(snapshot.core, parent);
        snapshot.Configure(weapon, database);
        return weapon;
    }

    public static WeaponBase Create(
        SummonWeaponDefinition definition,
        WeaponDatabase database = null,
        Transform parent = null)
    {
        if (definition?.core == null)
            return null;

        SummonWeaponDefinition snapshot = definition.Clone();
        var weapon = BuildBase<SummonWeapon>(snapshot.core, parent);
        snapshot.Configure(weapon, database);
        return weapon;
    }

    private static T BuildBase<T>(WeaponCoreStats core, Transform parent) where T : WeaponBase
    {
        var weaponObject = new GameObject(core.weaponName);
        weaponObject.tag = "Weapon";

        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer != -1)
            weaponObject.layer = playerLayer;

        if (parent != null)
            weaponObject.transform.SetParent(parent, false);

        var spriteRenderer = weaponObject.AddComponent<SpriteRenderer>();
        if (core.sprite != null)
            spriteRenderer.sprite = core.sprite;
        RenderVisibilityUtility.ConfigureSpriteRenderer(spriteRenderer, sortingOrder: 2);

        if (core.displayScale > 0f)
            weaponObject.transform.localScale = Vector3.one * core.displayScale;

        return weaponObject.AddComponent<T>();
    }
}