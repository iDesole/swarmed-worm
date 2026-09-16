using System;
using UnityEngine;
using UnityEngine.Serialization;

[Flags]
public enum WeaponElement
{
    None = 0,
    Poison = 1 << 0,
    Fire = 1 << 1,
    Ice = 1 << 2,
    Electric = 1 << 3,
    Void = 1 << 4
}

[Serializable]
public class ElectricElementStats
{
    [Tooltip("Bonus shock damage dealt to each chained enemy, as a multiplier of the weapon hit damage.")]
    [Range(0f, 2f)] public float shockDamageMultiplier = 0.35f;

    [Tooltip("How far the current can jump to the next enemy.")]
    public float chainRange = 5f;

    [Tooltip("Maximum number of enemies the current can jump to after the initial hit.")]
    public int maxChainTargets = 4;

    [Tooltip("How much shock damage is lost after each chain jump.")]
    [Range(0f, 1f)] public float chainDamageFalloff = 0.25f;

    [Tooltip("How long the electrocute window lasts. Refreshed on each electric hit and stack.")]
    public float shockDuration = 6f;

    [Tooltip("Shock stacks added while the enemy is electrocuted.")]
    public int shockStacksPerHit = 1;

    [Tooltip("Shock stacks required before the enemy is jolted for crit damage and briefly paused.")]
    public int stacksToJolt = 3;

    [Tooltip("How long a jolted enemy is paused.")]
    public float joltDuration = 0.6f;

    public ElectricElementStats Clone() =>
        new()
        {
            shockDamageMultiplier = shockDamageMultiplier,
            chainRange = chainRange,
            maxChainTargets = maxChainTargets,
            chainDamageFalloff = chainDamageFalloff,
            shockDuration = shockDuration,
            shockStacksPerHit = shockStacksPerHit,
            stacksToJolt = stacksToJolt,
            joltDuration = joltDuration
        };

    public void Normalize()
    {
        shockDamageMultiplier = Mathf.Max(0f, shockDamageMultiplier);
        chainRange = Mathf.Max(0.5f, chainRange);
        maxChainTargets = Mathf.Max(0, maxChainTargets);
        chainDamageFalloff = Mathf.Clamp01(chainDamageFalloff);
        shockDuration = Mathf.Max(0.1f, shockDuration);
        shockStacksPerHit = Mathf.Max(1, shockStacksPerHit);
        stacksToJolt = Mathf.Max(1, stacksToJolt);
        joltDuration = Mathf.Max(0.05f, joltDuration);
    }
}

[Serializable]
public class FireElementStats
{
    [Tooltip("Burn damage per tick, as a multiplier of the weapon base damage.")]
    [Range(0f, 2f)] public float burnDamageMultiplier = 0.2f;

    [Tooltip("Seconds between burn damage ticks.")]
    public float burnTickInterval = 0.5f;

    [Tooltip("How long the burn stack window lasts. Refreshed on each fire hit and stack.")]
    public float burnDuration = 6f;

    [Tooltip("Burn stacks added to the target on each fire hit.")]
    public int burnStacksPerHit = 1;

    [Tooltip("Burn stacks required before the enemy incinerates for crit damage.")]
    public int stacksToIncinerate = 4;

    [Tooltip("How far incineration can spread burns from the source enemy.")]
    [FormerlySerializedAs("incinerateRadius")]
    public float spreadRadius = 3f;

    [Tooltip("Maximum number of enemies this weapon can have burning at once.")]
    [FormerlySerializedAs("maxSpread")]
    [FormerlySerializedAs("maxSpreadTargets")]
    public int maxBurnCount = 3;

    public FireElementStats Clone() =>
        new()
        {
            burnDamageMultiplier = burnDamageMultiplier,
            burnTickInterval = burnTickInterval,
            burnDuration = burnDuration,
            burnStacksPerHit = burnStacksPerHit,
            stacksToIncinerate = stacksToIncinerate,
            spreadRadius = spreadRadius,
            maxBurnCount = maxBurnCount
        };

    public void Normalize()
    {
        burnDamageMultiplier = Mathf.Max(0f, burnDamageMultiplier);
        burnTickInterval = Mathf.Max(0.1f, burnTickInterval);
        burnDuration = Mathf.Max(0.1f, burnDuration);
        burnStacksPerHit = Mathf.Max(1, burnStacksPerHit);
        stacksToIncinerate = Mathf.Max(1, stacksToIncinerate);
        spreadRadius = Mathf.Max(0.5f, spreadRadius);
        maxBurnCount = Mathf.Max(1, maxBurnCount);
    }
}

[Serializable]
public class PoisonElementStats
{
    [Tooltip("How long the poison window lasts. Refreshed on each poison hit and stack.")]
    public float poisonDuration = 6f;

    [Tooltip("Poison stacks added while the enemy is poisoned.")]
    public int poisonStacksPerHit = 1;

    [Tooltip("Poison stacks required before poison spreads to nearby enemies.")]
    public int stacksToSpread = 4;

    [Tooltip("Movement speed reduction per poison stack.")]
    [Range(0f, 1f)] public float slowPerStack = 0.1f;

    [Tooltip("How far poison spreads from the source enemy.")]
    public float spreadRadius = 3f;

    [Tooltip("Maximum number of enemies this weapon can have poisoned at once.")]
    public int maxPoisonCount = 3;

    public PoisonElementStats Clone() =>
        new()
        {
            poisonDuration = poisonDuration,
            poisonStacksPerHit = poisonStacksPerHit,
            stacksToSpread = stacksToSpread,
            slowPerStack = slowPerStack,
            spreadRadius = spreadRadius,
            maxPoisonCount = maxPoisonCount
        };

    public void Normalize()
    {
        poisonDuration = Mathf.Max(0.1f, poisonDuration);
        poisonStacksPerHit = Mathf.Max(1, poisonStacksPerHit);
        stacksToSpread = Mathf.Max(1, stacksToSpread);
        slowPerStack = Mathf.Clamp01(slowPerStack);
        spreadRadius = Mathf.Max(0.5f, spreadRadius);
        maxPoisonCount = Mathf.Max(1, maxPoisonCount);
    }
}

[Serializable]
public class IceElementStats
{
    [Tooltip("How long the slow window lasts. Refreshed on each ice hit and stack.")]
    public float slowDuration = 6f;

    [Tooltip("Slow stacks added while the enemy is slowed.")]
    public int slowStacksPerHit = 1;

    [Tooltip("Slow stacks required before the enemy freezes.")]
    public int stacksToFreeze = 4;

    [Tooltip("Movement speed reduction per slow stack.")]
    [Range(0f, 1f)] public float slowPerStack = 0.12f;

    [Tooltip("How long a frozen enemy stays frozen before thawing with no shatter.")]
    public float freezeDuration = 4f;

    [Tooltip("How far shatter spreads slow stacks from the frozen enemy.")]
    public float spreadRadius = 3f;

    [Tooltip("Slow stacks applied to each enemy hit by a shatter spread.")]
    public int spreadSlowStacks = 2;

    [Tooltip("Maximum number of nearby enemies that can receive spread slow stacks.")]
    public int maxSpreadTargets = 4;

    public IceElementStats Clone() =>
        new()
        {
            slowDuration = slowDuration,
            slowStacksPerHit = slowStacksPerHit,
            stacksToFreeze = stacksToFreeze,
            slowPerStack = slowPerStack,
            freezeDuration = freezeDuration,
            spreadRadius = spreadRadius,
            spreadSlowStacks = spreadSlowStacks,
            maxSpreadTargets = maxSpreadTargets
        };

    public void Normalize()
    {
        slowDuration = Mathf.Max(0.1f, slowDuration);
        slowStacksPerHit = Mathf.Max(1, slowStacksPerHit);
        stacksToFreeze = Mathf.Max(1, stacksToFreeze);
        slowPerStack = Mathf.Clamp01(slowPerStack);
        freezeDuration = Mathf.Max(0.1f, freezeDuration);
        spreadRadius = Mathf.Max(0.5f, spreadRadius);
        spreadSlowStacks = Mathf.Max(1, spreadSlowStacks);
        maxSpreadTargets = Mathf.Max(1, maxSpreadTargets);
    }
}

[Serializable]
public class VoidElementStats
{
    [Tooltip("Void damage per tick on the anchor and pulled enemies, as a multiplier of weapon base damage.")]
    [Range(0f, 2f)] public float voidDamageMultiplier = 0.2f;

    [Tooltip("Seconds between void damage ticks.")]
    public float voidTickInterval = 0.5f;

    [Tooltip("Seconds after a void hit before the black hole collapses for crit damage.")]
    public float collapseDuration = 3f;

    [Tooltip("How far the black hole pulls nearby enemies.")]
    public float pullRadius = 4f;

    [Tooltip("How quickly pulled enemies are dragged toward the anchor.")]
    public float pullStrength = 7f;

    [Tooltip("Maximum number of enemies pulled by one black hole.")]
    public int maxPullTargets = 6;

    [Tooltip("Maximum number of active void black holes per weapon.")]
    public int maxVoidCount = 2;

    public VoidElementStats Clone() =>
        new()
        {
            voidDamageMultiplier = voidDamageMultiplier,
            voidTickInterval = voidTickInterval,
            collapseDuration = collapseDuration,
            pullRadius = pullRadius,
            pullStrength = pullStrength,
            maxPullTargets = maxPullTargets,
            maxVoidCount = maxVoidCount
        };

    public void Normalize()
    {
        voidDamageMultiplier = Mathf.Max(0f, voidDamageMultiplier);
        voidTickInterval = Mathf.Max(0.1f, voidTickInterval);
        collapseDuration = Mathf.Max(0.1f, collapseDuration);
        pullRadius = Mathf.Max(0.5f, pullRadius);
        pullStrength = Mathf.Max(0.5f, pullStrength);
        maxPullTargets = Mathf.Max(1, maxPullTargets);
        maxVoidCount = Mathf.Max(1, maxVoidCount);
    }
}

[Serializable]
public class WeaponElementProfile
{
    public WeaponElement elements = WeaponElement.None;
    public ElectricElementStats electric = new();
    public FireElementStats fire = new();
    public IceElementStats ice = new();
    public PoisonElementStats poison = new();
    public VoidElementStats voidEffect = new();

    public bool HasElement(WeaponElement element) =>
        element != WeaponElement.None && (elements & element) != 0;

    public WeaponElementProfile Clone() =>
        new()
        {
            elements = elements,
            electric = electric?.Clone() ?? new ElectricElementStats(),
            fire = fire?.Clone() ?? new FireElementStats(),
            ice = ice?.Clone() ?? new IceElementStats(),
            poison = poison?.Clone() ?? new PoisonElementStats(),
            voidEffect = voidEffect?.Clone() ?? new VoidElementStats()
        };

    public void Normalize()
    {
        electric ??= new ElectricElementStats();
        fire ??= new FireElementStats();
        ice ??= new IceElementStats();
        poison ??= new PoisonElementStats();
        voidEffect ??= new VoidElementStats();
        electric.Normalize();
        fire.Normalize();
        ice.Normalize();
        poison.Normalize();
        voidEffect.Normalize();
    }

    public void ApplyTo(WeaponBase weapon)
    {
        if (weapon == null)
            return;

        weapon.elements = elements;
        weapon.electricStats = electric?.Clone() ?? new ElectricElementStats();
        weapon.fireStats = fire?.Clone() ?? new FireElementStats();
        weapon.iceStats = ice?.Clone() ?? new IceElementStats();
        weapon.poisonStats = poison?.Clone() ?? new PoisonElementStats();
        weapon.voidStats = voidEffect?.Clone() ?? new VoidElementStats();
    }
}

public enum ElementalHitDelivery
{
    Melee,
    Projectile
}

public struct WeaponCombatContext
{
    public WeaponElement elements;
    public ElectricElementStats electric;
    public FireElementStats fire;
    public IceElementStats ice;
    public PoisonElementStats poison;
    public VoidElementStats voidStats;
    public GameObject owner;
    public int sourceWeaponId;
    public float baseDamage;
    public float critMultiplier;

    public bool HasElement(WeaponElement element) =>
        element != WeaponElement.None && (elements & element) != 0;

    public float GetCritDamage() =>
        Mathf.Max(0f, baseDamage * Mathf.Max(1f, critMultiplier));

    public float GetJoltDamage() => GetCritDamage();

    public static WeaponCombatContext FromWeapon(WeaponBase weapon)
    {
        if (weapon == null)
        {
            return new WeaponCombatContext
            {
                electric = new ElectricElementStats(),
                fire = new FireElementStats(),
                ice = new IceElementStats(),
                poison = new PoisonElementStats(),
                voidStats = new VoidElementStats(),
                critMultiplier = 1.5f
            };
        }

        return new WeaponCombatContext
        {
            elements = weapon.elements,
            electric = weapon.electricStats?.Clone() ?? new ElectricElementStats(),
            fire = weapon.fireStats?.Clone() ?? new FireElementStats(),
            ice = weapon.iceStats?.Clone() ?? new IceElementStats(),
            poison = weapon.poisonStats?.Clone() ?? new PoisonElementStats(),
            voidStats = weapon.voidStats?.Clone() ?? new VoidElementStats(),
            owner = weapon.owner,
            sourceWeaponId = weapon.GetInstanceID(),
            baseDamage = weapon.damage,
            critMultiplier = weapon.critMultiplier
        };
    }
}