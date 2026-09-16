using System;
using System.Collections.Generic;
using UnityEngine;

// =============================================================================
// ENEMY DATA LAYER
// EnemyDatabase.asset holds definitions consumed by EnemyFactory and EnemySpawner.
// =============================================================================

/// <summary>Which list in EnemyDatabase an EnemySelection refers to.</summary>
public enum EnemyCategory
{
    Melee,
    Projectile,
    Summon,
    Miniboss,
    Boss
}

public enum EnemyBehaviorKind
{
    Melee,
    Projectile,
    Summon
}

[Serializable]
public class EnemyCoreStats
{
    public string enemyName = "New Enemy";
    public Sprite sprite;
    public float displayScale = 1f;
    public Color tintColor = Color.white;

    [Header("Combat")]
    public float maxHealth = 30f;
    public float moveSpeed = 3.5f;
    public float damage = 8f;
    public float attackCooldown = 1.2f;

    [Header("Ranges")]
    public float detectionRange = 8f;
    public float attackRange = 1.8f;
    [Range(0f, 1f)] public float aggression = 0.6f;

    [Header("Defense")]
    public float knockbackResistance = 0.5f;
    public float damageResistance;

    [Header("Animation Clips")]
    [Tooltip("Named animation clips for this enemy. Use titles like Walk, Injured, or Death.")]
    public CharacterAnimationClipEntry[] animationClips = Array.Empty<CharacterAnimationClipEntry>();

    public EnemyCoreStats Clone() =>
        new()
        {
            enemyName = enemyName,
            sprite = sprite,
            displayScale = displayScale,
            tintColor = tintColor,
            maxHealth = maxHealth,
            moveSpeed = moveSpeed,
            damage = damage,
            attackCooldown = attackCooldown,
            detectionRange = detectionRange,
            attackRange = attackRange,
            aggression = aggression,
            knockbackResistance = knockbackResistance,
            damageResistance = damageResistance,
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

    public void Normalize()
    {
        displayScale = Mathf.Max(0.1f, displayScale);
        if (string.IsNullOrWhiteSpace(enemyName))
            enemyName = "New Enemy";

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
            seenTitles.Add(entry.title);
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
}

[Serializable]
public class MeleeEnemyDefinition
{
    public EnemyCoreStats core = new();
    public EnemyLootPool lootPool = new();
    public float chargeSpeedMultiplier = 1.8f;
    public float chargeDuration = 0.6f;

    public string EnemyName => core?.enemyName ?? "Enemy";

    public MeleeEnemyDefinition Clone() =>
        new()
        {
            core = core?.Clone() ?? new EnemyCoreStats(),
            lootPool = lootPool?.Clone() ?? new EnemyLootPool(),
            chargeSpeedMultiplier = chargeSpeedMultiplier,
            chargeDuration = chargeDuration
        };

    public void Configure(MeleeEnemy enemy)
    {
        if (enemy == null)
            return;

        enemy.ApplyCore(core);
        enemy.ApplyLootPool(lootPool);
        enemy.chargeSpeedMultiplier = chargeSpeedMultiplier;
        enemy.chargeDuration = chargeDuration;
    }
}

[Serializable]
public class ProjectileEnemyDefinition
{
    public EnemyCoreStats core = new();
    public EnemyLootPool lootPool = new();
    public Sprite projectileSprite;
    public float projectileSpeed = 12f;
    public float projectileLifetime = 3f;

    public string EnemyName => core?.enemyName ?? "Enemy";

    public ProjectileEnemyDefinition Clone() =>
        new()
        {
            core = core?.Clone() ?? new EnemyCoreStats(),
            lootPool = lootPool?.Clone() ?? new EnemyLootPool(),
            projectileSprite = projectileSprite,
            projectileSpeed = projectileSpeed,
            projectileLifetime = projectileLifetime
        };

    public void Configure(RangedEnemy enemy, EnemyDatabase database)
    {
        if (enemy == null)
            return;

        enemy.ApplyCore(core);
        enemy.ApplyLootPool(lootPool);
        enemy.projectileSpeed = projectileSpeed;
        enemy.projectileLifetime = projectileLifetime;
        enemy.projectileSprite = projectileSprite;
        database?.ApplyProjectileFallbacks(enemy);
    }
}

[Serializable]
public class SummonEnemyDefinition
{
    public EnemyCoreStats core = new();
    public EnemyLootPool lootPool = new();
    public GameObject[] summonPrefabs = Array.Empty<GameObject>();
    public int maxActiveSummons = 3;
    public float summonInterval = 4f;
    public float summonLifetime = 12f;

    public string EnemyName => core?.enemyName ?? "Enemy";

    public SummonEnemyDefinition Clone() =>
        new()
        {
            core = core?.Clone() ?? new EnemyCoreStats(),
            lootPool = lootPool?.Clone() ?? new EnemyLootPool(),
            summonPrefabs = summonPrefabs != null
                ? (GameObject[])summonPrefabs.Clone()
                : Array.Empty<GameObject>(),
            maxActiveSummons = maxActiveSummons,
            summonInterval = summonInterval,
            summonLifetime = summonLifetime
        };

    public void Configure(SummonEnemy enemy)
    {
        if (enemy == null)
            return;

        enemy.ApplyCore(core);
        enemy.ApplyLootPool(lootPool);
        enemy.summonPrefabs = summonPrefabs != null
            ? (GameObject[])summonPrefabs.Clone()
            : Array.Empty<GameObject>();
        enemy.maxActiveSummons = maxActiveSummons;
        enemy.summonInterval = summonInterval;
        enemy.summonLifetime = summonLifetime;
    }
}

[Serializable]
public class MinibossEnemyDefinition
{
    public EnemyCoreStats core = new();
    public EnemyLootPool lootPool = new();
    public EnemyBehaviorKind behavior = EnemyBehaviorKind.Melee;
    public Sprite projectileSprite;
    public float projectileSpeed = 12f;
    public float projectileLifetime = 3f;
    public GameObject[] summonPrefabs = Array.Empty<GameObject>();
    public int maxActiveSummons = 3;
    public float summonInterval = 4f;
    public float summonLifetime = 12f;

    public string EnemyName => core?.enemyName ?? "Miniboss";

    public MinibossEnemyDefinition Clone() =>
        new()
        {
            core = core?.Clone() ?? new EnemyCoreStats(),
            lootPool = lootPool?.Clone() ?? new EnemyLootPool(),
            behavior = behavior,
            projectileSprite = projectileSprite,
            projectileSpeed = projectileSpeed,
            projectileLifetime = projectileLifetime,
            summonPrefabs = summonPrefabs != null
                ? (GameObject[])summonPrefabs.Clone()
                : Array.Empty<GameObject>(),
            maxActiveSummons = maxActiveSummons,
            summonInterval = summonInterval,
            summonLifetime = summonLifetime
        };

    public void Configure(Enemy enemy, EnemyDatabase database)
    {
        if (enemy == null)
            return;

        enemy.ApplyCore(core);
        enemy.ApplyLootPool(lootPool);
        ConfigureBehaviorExtras(enemy, database);
    }

    public void ConfigureBehaviorExtras(Enemy enemy, EnemyDatabase database)
    {
        switch (enemy)
        {
            case RangedEnemy ranged:
                ranged.projectileSpeed = projectileSpeed;
                ranged.projectileLifetime = projectileLifetime;
                ranged.projectileSprite = projectileSprite;
                database?.ApplyProjectileFallbacks(ranged);
                break;
            case SummonEnemy summon:
                summon.summonPrefabs = summonPrefabs != null
                    ? (GameObject[])summonPrefabs.Clone()
                    : Array.Empty<GameObject>();
                summon.maxActiveSummons = maxActiveSummons;
                summon.summonInterval = summonInterval;
                summon.summonLifetime = summonLifetime;
                break;
        }
    }
}

[Serializable]
public class BossEnemyDefinition
{
    public EnemyCoreStats core = new();
    public EnemyLootPool lootPool = new();
    public EnemyBehaviorKind behavior = EnemyBehaviorKind.Melee;
    public Sprite projectileSprite;
    public float projectileSpeed = 12f;
    public float projectileLifetime = 3f;
    public GameObject[] summonPrefabs = Array.Empty<GameObject>();
    public int maxActiveSummons = 4;
    public float summonInterval = 3f;
    public float summonLifetime = 15f;

    public string EnemyName => core?.enemyName ?? "Boss";

    public BossEnemyDefinition Clone() =>
        new()
        {
            core = core?.Clone() ?? new EnemyCoreStats(),
            lootPool = lootPool?.Clone() ?? new EnemyLootPool(),
            behavior = behavior,
            projectileSprite = projectileSprite,
            projectileSpeed = projectileSpeed,
            projectileLifetime = projectileLifetime,
            summonPrefabs = summonPrefabs != null
                ? (GameObject[])summonPrefabs.Clone()
                : Array.Empty<GameObject>(),
            maxActiveSummons = maxActiveSummons,
            summonInterval = summonInterval,
            summonLifetime = summonLifetime
        };

    public void Configure(Enemy enemy, EnemyDatabase database)
    {
        if (enemy == null)
            return;

        enemy.ApplyCore(core);
        enemy.ApplyLootPool(lootPool);
        ConfigureBehaviorExtras(enemy, database);
    }

    public void ConfigureBehaviorExtras(Enemy enemy, EnemyDatabase database)
    {
        switch (enemy)
        {
            case RangedEnemy ranged:
                ranged.projectileSpeed = projectileSpeed;
                ranged.projectileLifetime = projectileLifetime;
                ranged.projectileSprite = projectileSprite;
                database?.ApplyProjectileFallbacks(ranged);
                break;
            case SummonEnemy summon:
                summon.summonPrefabs = summonPrefabs != null
                    ? (GameObject[])summonPrefabs.Clone()
                    : Array.Empty<GameObject>();
                summon.maxActiveSummons = maxActiveSummons;
                summon.summonInterval = summonInterval;
                summon.summonLifetime = summonLifetime;
                break;
        }
    }
}

[Serializable]
public struct EnemySelection
{
    public EnemyCategory category;
    public int index;

    public bool IsValid => index >= 0;

    public static EnemySelection None =>
        new() { category = EnemyCategory.Melee, index = -1 };

    public static EnemySelection Melee(int enemyIndex) =>
        new() { category = EnemyCategory.Melee, index = enemyIndex };

    public static EnemySelection Projectile(int enemyIndex) =>
        new() { category = EnemyCategory.Projectile, index = enemyIndex };

    public static EnemySelection Summon(int enemyIndex) =>
        new() { category = EnemyCategory.Summon, index = enemyIndex };

    public static EnemySelection Miniboss(int enemyIndex) =>
        new() { category = EnemyCategory.Miniboss, index = enemyIndex };

    public static EnemySelection Boss(int enemyIndex) =>
        new() { category = EnemyCategory.Boss, index = enemyIndex };
}