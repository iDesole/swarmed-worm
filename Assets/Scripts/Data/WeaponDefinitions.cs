using System;
using UnityEngine;

// =============================================================================
// WEAPON DATA LAYER
// Serializable definitions edited in WeaponDatabase.asset (Assets/GameData).
// Runtime flow: definition → Clone() → Configure(weapon) via WeaponFactory.
// =============================================================================

/// <summary>Which WeaponFactory code path to use when equipping.</summary>
public enum WeaponType
{
    Projectile,
    Melee,
    Summon,
    None
}

/// <summary>Stats shared by all weapon types (damage, elements, aim offset).</summary>
[Serializable]
public class WeaponCoreStats
{
    public string weaponName = "New Weapon";

    [Tooltip("Credits to buy this weapon in shops.")]
    public int shopPrice;

    [Tooltip("Portrait shown in shops. Falls back to Sprite when empty.")]
    public Sprite shopSprite;

    public Sprite sprite;
    public float displayScale = 0.4f;
    public float damage = 10f;
    public float attackSpeed = 2f;
    public float range = 12f;
    [Range(0f, 1f)] public float critChance = 0.05f;
    public float critMultiplier = 1.5f;
    public float knockback = 2f;
    [Range(-45f, 45f)] public float aimPitchOffset;
    public WeaponElementProfile elementProfile = new();

    public WeaponCoreStats Clone() =>
        new()
        {
            weaponName = weaponName,
            shopPrice = shopPrice,
            shopSprite = shopSprite,
            sprite = sprite,
            displayScale = displayScale,
            damage = damage,
            attackSpeed = attackSpeed,
            range = range,
            critChance = critChance,
            critMultiplier = critMultiplier,
            knockback = knockback,
            aimPitchOffset = aimPitchOffset,
            elementProfile = elementProfile?.Clone() ?? new WeaponElementProfile()
        };

    public void ApplyTo(WeaponBase weapon)
    {
        if (weapon == null)
            return;

        weapon.weaponName = weaponName;
        weapon.damage = damage;
        weapon.AttackSpeed = attackSpeed;
        weapon.range = range;
        weapon.critChance = critChance;
        weapon.critMultiplier = critMultiplier;
        weapon.knockback = knockback;
        weapon.aimPitchOffset = aimPitchOffset;
        elementProfile?.ApplyTo(weapon);
    }

    public void Normalize()
    {
        displayScale = Mathf.Max(0.01f, displayScale);
        shopPrice = Mathf.Max(0, shopPrice);
        if (string.IsNullOrWhiteSpace(weaponName))
            weaponName = "New Weapon";

        elementProfile ??= new WeaponElementProfile();
        elementProfile.Normalize();
    }
}

[Serializable]
public class ProjectileWeaponDefinition
{
    public WeaponCoreStats core = new();
    public Sprite projectileSprite;
    public float projectileSpeed = 18f;
    public int projectileCount = 1;
    public float spreadAngle;
    public int pierceCount;
    public int bounceCount;
    [Range(0f, 1f)] public float homingStrength;
    public float projectileSize = 1f;
    [Range(-1f, 1f)] public float projectileVerticalOffset;
    [Range(-45f, 45f)] public float projectilePitchOffset;

    [Header("Explosion")]
    [Tooltip("When enabled, the projectile triggers an expanding shockwave on hit.")]
    public bool explodeOnHit;
    [Tooltip("Maximum reach of the explosion shockwave.")]
    public float explosionRadius = 2f;
    [Tooltip("Damage dealt by the first pulse at the impact point.")]
    public float explosionInitialDamage = 10f;
    [Tooltip("Damage dealt by each outward expansion pulse.")]
    public float explosionRadiusDamage = 5f;
    [Tooltip("How many outward pulses the shockwave emits.")]
    public int explosionPulseCount = 3;
    [Tooltip("Distance between each outward pulse ring.")]
    public float explosionPulseSpacing = 0.75f;

    [Header("Physics")]
    [Tooltip("Rigidbody gravity scale. Use negative values (e.g. -0.5) for floating projectiles like bubbles.")]
    public float projectileGravityScale;

    public string WeaponName => core?.weaponName ?? "Weapon";

    public ProjectileWeaponDefinition Clone() =>
        new()
        {
            core = core?.Clone() ?? new WeaponCoreStats(),
            projectileSprite = projectileSprite,
            projectileSpeed = projectileSpeed,
            projectileCount = projectileCount,
            spreadAngle = spreadAngle,
            pierceCount = pierceCount,
            bounceCount = bounceCount,
            homingStrength = homingStrength,
            projectileSize = projectileSize,
            projectileVerticalOffset = projectileVerticalOffset,
            projectilePitchOffset = projectilePitchOffset,
            explodeOnHit = explodeOnHit,
            explosionRadius = explosionRadius,
            explosionInitialDamage = explosionInitialDamage,
            explosionRadiusDamage = explosionRadiusDamage,
            explosionPulseCount = explosionPulseCount,
            explosionPulseSpacing = explosionPulseSpacing,
            projectileGravityScale = projectileGravityScale
        };

    public void Configure(ProjectileWeapon weapon, WeaponDatabase database)
    {
        if (weapon == null)
            return;

        core?.ApplyTo(weapon);
        weapon.projectileSpeed = projectileSpeed;
        weapon.projectileCount = projectileCount;
        weapon.spreadAngle = spreadAngle;
        weapon.pierceCount = pierceCount;
        weapon.bounceCount = bounceCount;
        weapon.homingStrength = homingStrength;
        weapon.projectileSize = projectileSize;
        weapon.projectileSprite = projectileSprite;
        weapon.projectileVerticalOffset = projectileVerticalOffset;
        weapon.projectilePitchOffset = projectilePitchOffset;
        weapon.explodeOnHit = explodeOnHit;
        weapon.explosionRadius = explosionRadius;
        weapon.explosionInitialDamage = explosionInitialDamage;
        weapon.explosionRadiusDamage = explosionRadiusDamage;
        weapon.explosionPulseCount = explosionPulseCount;
        weapon.explosionPulseSpacing = explosionPulseSpacing;
        weapon.projectileGravityScale = projectileGravityScale;
        database?.ApplyProjectileFallbacks(weapon);
    }
}

[Serializable]
public class MeleeWeaponDefinition
{
    public WeaponCoreStats core = new();
    public float swingDuration = 0.25f;
    public float swingRadius = 2.2f;
    public int swingsPerAttack = 1;
    public float swingArcAngle = 90f;
    public int multiHitCount = 1;
    [Range(0f, 1f)] public float lifesteal;

    public string WeaponName => core?.weaponName ?? "Weapon";

    public MeleeWeaponDefinition Clone() =>
        new()
        {
            core = core?.Clone() ?? new WeaponCoreStats(),
            swingDuration = swingDuration,
            swingRadius = swingRadius,
            swingsPerAttack = swingsPerAttack,
            swingArcAngle = swingArcAngle,
            multiHitCount = multiHitCount,
            lifesteal = lifesteal
        };

    public void Configure(MeleeWeapon weapon, WeaponDatabase database)
    {
        if (weapon == null)
            return;

        core?.ApplyTo(weapon);
        weapon.swingDuration = swingDuration;
        weapon.swingRadius = swingRadius;
        weapon.swingsPerAttack = swingsPerAttack;
        weapon.swingArcAngle = swingArcAngle;
        weapon.multiHitCount = multiHitCount;
        weapon.lifesteal = lifesteal;
        database?.ApplyMeleeFallbacks(weapon);
    }
}

[Serializable]
public class SummonWeaponDefinition
{
    public WeaponCoreStats core = new();
    public GameObject[] summonPrefabs = Array.Empty<GameObject>();
    public int maxActiveSummons = 3;
    public float summonLifetime = 12f;
    public float summonRange = 6f;
    public float summonDamageMultiplier = 1f;
    public float summonHealthMultiplier = 1f;
    public float summonAttackSpeedMultiplier = 1f;
    public int summonsPerCast = 1;

    public string WeaponName => core?.weaponName ?? "Weapon";

    public SummonWeaponDefinition Clone() =>
        new()
        {
            core = core?.Clone() ?? new WeaponCoreStats(),
            summonPrefabs = summonPrefabs != null
                ? (GameObject[])summonPrefabs.Clone()
                : Array.Empty<GameObject>(),
            maxActiveSummons = maxActiveSummons,
            summonLifetime = summonLifetime,
            summonRange = summonRange,
            summonDamageMultiplier = summonDamageMultiplier,
            summonHealthMultiplier = summonHealthMultiplier,
            summonAttackSpeedMultiplier = summonAttackSpeedMultiplier,
            summonsPerCast = summonsPerCast
        };

    public void Configure(SummonWeapon weapon, WeaponDatabase database)
    {
        if (weapon == null)
            return;

        core?.ApplyTo(weapon);
        weapon.summonPrefabs = summonPrefabs != null
            ? (GameObject[])summonPrefabs.Clone()
            : Array.Empty<GameObject>();
        weapon.maxActiveSummons = maxActiveSummons;
        weapon.summonLifetime = summonLifetime;
        weapon.summonRange = summonRange;
        weapon.summonDamageMultiplier = summonDamageMultiplier;
        weapon.summonHealthMultiplier = summonHealthMultiplier;
        weapon.summonAttackSpeedMultiplier = summonAttackSpeedMultiplier;
        weapon.summonsPerCast = summonsPerCast;
    }
}

/// <summary>Lightweight handle: weapon type + row index in WeaponDatabase.</summary>
[Serializable]
public struct WeaponSelection
{
    public WeaponType type;
    public int index;

    public bool IsValid => type != WeaponType.None && index >= 0;

    public static WeaponSelection None =>
        new() { type = WeaponType.None, index = -1 };

    public static WeaponSelection Projectile(int weaponIndex) =>
        new() { type = WeaponType.Projectile, index = weaponIndex };

    public static WeaponSelection Melee(int weaponIndex) =>
        new() { type = WeaponType.Melee, index = weaponIndex };

    public static WeaponSelection Summon(int weaponIndex) =>
        new() { type = WeaponType.Summon, index = weaponIndex };
}