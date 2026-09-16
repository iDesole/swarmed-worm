using System;
using System.Collections.Generic;
using UnityEngine;

// =============================================================================
// PLAYER DATA LAYER
// Edited in PlayerDatabase.asset. PlayerFactory reads these to build the runtime character.
// =============================================================================

/// <summary>Core stats and visuals for one playable character.</summary>
[Serializable]
public class PlayerCoreStats
{
    public string characterName = "New Character";

    [Header("Shop")]
    [Tooltip("Credits to unlock this character in shops.")]
    public int shopPrice;

    [Tooltip("Portrait shown in shops. Falls back to Sprite when empty.")]
    public Sprite shopSprite;

    [Header("Visuals")]
    public Sprite sprite;
    public float displayScale = 1f;
    public Color tintColor = Color.white;

    [Header("Survival")]
    public float maxHealth = 100f;
    public float maxShield = 50f;
    public float shieldRegenRate = 5f;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float dodgeSpeedMultiplier = 1.5f;

    [Header("Combat")]
    [Range(0.1f, 5f)] public float attackSpeedMultiplier = 1f;
    [Range(0f, 1f)] public float critChance = 0.05f;
    [Range(1f, 5f)] public float critDamageMultiplier = 1.5f;
    public float damageMultiplier = 1f;

    [Header("Burrow")]
    public float burrowDuration = 3f;
    public float burrowCooldown = 8f;
    public float burrowDamageReduction = 1f;

    [Header("Defense")]
    public float knockbackResistance;
    public float lifesteal;
    public float invulnerabilityFrames = 0.1f;

    public PlayerCoreStats Clone() =>
        new()
        {
            characterName = characterName,
            shopPrice = shopPrice,
            shopSprite = shopSprite,
            sprite = sprite,
            displayScale = displayScale,
            tintColor = tintColor,
            maxHealth = maxHealth,
            maxShield = maxShield,
            shieldRegenRate = shieldRegenRate,
            moveSpeed = moveSpeed,
            dodgeSpeedMultiplier = dodgeSpeedMultiplier,
            attackSpeedMultiplier = attackSpeedMultiplier,
            critChance = critChance,
            critDamageMultiplier = critDamageMultiplier,
            damageMultiplier = damageMultiplier,
            burrowDuration = burrowDuration,
            burrowCooldown = burrowCooldown,
            burrowDamageReduction = burrowDamageReduction,
            knockbackResistance = knockbackResistance,
            lifesteal = lifesteal,
            invulnerabilityFrames = invulnerabilityFrames
        };

    public void Normalize()
    {
        displayScale = Mathf.Max(0.1f, displayScale);
        shopPrice = Mathf.Max(0, shopPrice);
        maxHealth = Mathf.Max(1f, maxHealth);
        maxShield = Mathf.Max(0f, maxShield);
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        attackSpeedMultiplier = Mathf.Max(0.1f, attackSpeedMultiplier);
        critChance = Mathf.Clamp01(critChance);
        critDamageMultiplier = Mathf.Max(1f, critDamageMultiplier);
        damageMultiplier = Mathf.Max(0f, damageMultiplier);
        burrowDuration = Mathf.Max(0.1f, burrowDuration);
        burrowCooldown = Mathf.Max(0f, burrowCooldown);
        burrowDamageReduction = Mathf.Clamp01(burrowDamageReduction);
        invulnerabilityFrames = Mathf.Max(0f, invulnerabilityFrames);

        if (string.IsNullOrWhiteSpace(characterName))
            characterName = "New Character";
    }
}

[Serializable]
public class PlayerHandSlotDefinition
{
    public Vector3 localPosition;
    public WeaponSelection startingWeapon = WeaponSelection.None;

    public PlayerHandSlotDefinition Clone() =>
        new()
        {
            localPosition = localPosition,
            startingWeapon = startingWeapon
        };

    public static PlayerHandSlotDefinition Default(int slotIndex1to5)
    {
        Vector3 position = slotIndex1to5 switch
        {
            1 => new Vector3(0.42f, -0.18f, 0f),
            2 => new Vector3(0.6f, -0.31f, 0f),
            3 => new Vector3(0f, -0.53f, 0f),
            4 => new Vector3(0.6f, -0.58f, 0f),
            5 => new Vector3(-0.58f, -0.38f, 0f),
            _ => Vector3.zero
        };

        return new PlayerHandSlotDefinition
        {
            localPosition = position,
            startingWeapon = slotIndex1to5 == 1 ? WeaponSelection.Projectile(0) : WeaponSelection.None
        };
    }

    public static PlayerHandSlotDefinition[] CreateDefaults()
    {
        var slots = new PlayerHandSlotDefinition[5];
        for (int i = 0; i < slots.Length; i++)
            slots[i] = Default(i + 1);

        return slots;
    }
}

[Serializable]
public class CharacterAnimationClipEntry
{
    [Tooltip("Label used to identify this clip (e.g. Idle, Walk, Dodge).")]
    public string title = "New Clip";

    [Tooltip("Sprites played in order, one per frame.")]
    public Sprite[] frames = Array.Empty<Sprite>();

    [Min(1f)]
    public float framesPerSecond = 8f;

    public bool loop = true;

    public bool HasFrames
    {
        get
        {
            if (frames == null || frames.Length == 0)
                return false;

            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null)
                    return true;
            }

            return false;
        }
    }

    public CharacterAnimationClipEntry Clone() =>
        new()
        {
            title = title,
            frames = CloneFrames(frames),
            framesPerSecond = framesPerSecond,
            loop = loop
        };

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(title))
            title = "New Clip";

        framesPerSecond = Mathf.Max(1f, framesPerSecond);
        frames ??= Array.Empty<Sprite>();

        if (IsLocomotionClipTitle(title))
            loop = true;
    }

    private static bool IsLocomotionClipTitle(string clipTitle)
    {
        if (string.IsNullOrWhiteSpace(clipTitle))
            return false;

        return clipTitle.Equals("Idle", StringComparison.OrdinalIgnoreCase)
            || clipTitle.Equals("Walk", StringComparison.OrdinalIgnoreCase)
            || clipTitle.Equals("Walking", StringComparison.OrdinalIgnoreCase)
            || clipTitle.Equals("Move", StringComparison.OrdinalIgnoreCase)
            || clipTitle.Equals("Moving", StringComparison.OrdinalIgnoreCase);
    }

    private static Sprite[] CloneFrames(Sprite[] source)
    {
        if (source == null || source.Length == 0)
            return Array.Empty<Sprite>();

        var clones = new Sprite[source.Length];
        Array.Copy(source, clones, source.Length);
        return clones;
    }
}

[Serializable]
public class PlayerEnemySpawnerDefinition
{
    public bool attachToPlayer = true;
    public Vector3 localOffset = Vector3.zero;
    public int maxSpawnAttempts = 24;
    public bool showSpawnRings;

    public PlayerEnemySpawnerDefinition Clone() =>
        new()
        {
            attachToPlayer = attachToPlayer,
            localOffset = localOffset,
            maxSpawnAttempts = maxSpawnAttempts,
            showSpawnRings = showSpawnRings
        };

    public void Normalize()
    {
        maxSpawnAttempts = Mathf.Max(1, maxSpawnAttempts);
    }
}

[Serializable]
public class PlayerCharacterDefinition
{
    [Header("Spawn")]
    public Vector3 spawnPosition = new(750.61f, 413.68f, 1f);

    public PlayerCoreStats core = new();
    public WeaponSelection startingWeapon = WeaponSelection.None;

    [Header("Weapon Hands")]
    public PlayerHandSlotDefinition[] handSlots = PlayerHandSlotDefinition.CreateDefaults();

    [Header("Enemy Spawning")]
    public PlayerEnemySpawnerDefinition enemySpawner = new();

    [Header("Animation Clips")]
    [Tooltip("Named animation clips for this character. Use titles like Idle, Walk, or Dodge.")]
    public CharacterAnimationClipEntry[] animationClips = Array.Empty<CharacterAnimationClipEntry>();

    public string CharacterName => core?.characterName ?? "Character";

    public PlayerCharacterDefinition Clone() =>
        new()
        {
            spawnPosition = spawnPosition,
            core = core?.Clone() ?? new PlayerCoreStats(),
            startingWeapon = startingWeapon,
            handSlots = CloneHandSlots(handSlots),
            enemySpawner = enemySpawner?.Clone() ?? new PlayerEnemySpawnerDefinition(),
            animationClips = CloneAnimationClips(animationClips)
        };

    public bool TryGetAnimationEntry(string title, out CharacterAnimationClipEntry entry)
    {
        entry = null;
        if (string.IsNullOrWhiteSpace(title) || animationClips == null)
            return false;

        for (int i = 0; i < animationClips.Length; i++)
        {
            CharacterAnimationClipEntry candidate = animationClips[i];
            if (candidate == null || !candidate.HasFrames)
                continue;

            if (string.Equals(candidate.title, title, StringComparison.OrdinalIgnoreCase))
            {
                entry = candidate;
                return true;
            }
        }

        return false;
    }

    public bool TryGetAnimationEntry(out CharacterAnimationClipEntry entry, params string[] titles)
    {
        entry = null;
        if (titles == null || titles.Length == 0)
            return false;

        for (int i = 0; i < titles.Length; i++)
        {
            if (TryGetAnimationEntry(titles[i], out entry))
                return true;
        }

        return false;
    }

    public bool TryGetAnimationFrames(string title, out Sprite[] frames)
    {
        frames = null;
        if (!TryGetAnimationEntry(title, out CharacterAnimationClipEntry entry))
            return false;

        frames = entry.frames;
        return frames != null && frames.Length > 0;
    }

    public IReadOnlyList<CharacterAnimationClipEntry> GetAnimationClips() =>
        animationClips ?? Array.Empty<CharacterAnimationClipEntry>();

    public void Configure(Player player)
    {
        if (player == null)
            return;

        player.ApplyCharacterDefinition(this);
    }

    public PlayerHandSlotDefinition[] GetResolvedHandSlots()
    {
        PlayerHandSlotDefinition[] defaults = PlayerHandSlotDefinition.CreateDefaults();
        if (handSlots == null || handSlots.Length == 0)
            return defaults;

        var resolved = new PlayerHandSlotDefinition[5];
        for (int i = 0; i < resolved.Length; i++)
        {
            if (i < handSlots.Length && handSlots[i] != null)
                resolved[i] = handSlots[i].Clone();
            else
                resolved[i] = defaults[i].Clone();
        }

        return resolved;
    }

    public void Normalize()
    {
        core?.Normalize();
        enemySpawner?.Normalize();

        if (handSlots == null || handSlots.Length != 5)
            handSlots = GetResolvedHandSlots();

        if (handSlots != null && handSlots.Length > 0 && handSlots[0] != null)
            startingWeapon = handSlots[0].startingWeapon;

        NormalizeAnimationClips();
    }

    private void NormalizeAnimationClips()
    {
        animationClips ??= Array.Empty<CharacterAnimationClipEntry>();

        var seenTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < animationClips.Length; i++)
        {
            CharacterAnimationClipEntry entry = animationClips[i];
            if (entry == null)
            {
                animationClips[i] = new CharacterAnimationClipEntry { title = $"Clip {i + 1}" };
                entry = animationClips[i];
            }

            entry.Normalize();

            if (!seenTitles.Add(entry.title))
                GameLog.Warning($"Duplicate animation clip title '{entry.title}' on character '{CharacterName}'.");
        }
    }

    private static CharacterAnimationClipEntry[] CloneAnimationClips(CharacterAnimationClipEntry[] source)
    {
        if (source == null || source.Length == 0)
            return Array.Empty<CharacterAnimationClipEntry>();

        var clones = new CharacterAnimationClipEntry[source.Length];
        for (int i = 0; i < source.Length; i++)
            clones[i] = source[i]?.Clone() ?? new CharacterAnimationClipEntry();

        return clones;
    }

    private static PlayerHandSlotDefinition[] CloneHandSlots(PlayerHandSlotDefinition[] source)
    {
        PlayerHandSlotDefinition[] resolved = source != null && source.Length > 0
            ? source
            : PlayerHandSlotDefinition.CreateDefaults();

        var clones = new PlayerHandSlotDefinition[resolved.Length];
        for (int i = 0; i < resolved.Length; i++)
            clones[i] = resolved[i]?.Clone() ?? PlayerHandSlotDefinition.Default(i + 1);

        return clones;
    }
}

[Serializable]
public struct PlayerSelection
{
    public int index;

    public bool IsValid => index >= 0;

    public static PlayerSelection None =>
        new() { index = -1 };

    public static PlayerSelection At(int characterIndex) =>
        new() { index = characterIndex };
}