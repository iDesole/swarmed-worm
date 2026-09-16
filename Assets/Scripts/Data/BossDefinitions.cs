using System;
using System.Collections.Generic;
using UnityEngine;

// =============================================================================
// BOSS DATA LAYER
// BossDatabase.asset holds definitions consumed by BossFactory.
// Each entry stores sprite + stats and a hook for a BossBehavior script.
// =============================================================================

[Serializable]
public class BossCoreStats
{
    public string bossName = "New Boss";
    public Sprite sprite;
    public float displayScale = 1.6f;
    public Color tintColor = Color.white;

    [Header("Combat")]
    public float maxHealth = 500f;
    public float moveSpeed = 2.5f;
    public float damage = 24f;
    public float attackCooldown = 1.5f;

    [Header("Ranges")]
    public float detectionRange = 14f;
    public float attackRange = 3f;
    [Range(0f, 1f)] public float aggression = 0.8f;

    [Header("Defense")]
    public float knockbackResistance = 0.85f;
    public float damageResistance;

    [Header("Animation Clips")]
    [Tooltip("Named animation clips for this boss. Use titles like Idle, Walk, Injured, or Death.")]
    public CharacterAnimationClipEntry[] animationClips = Array.Empty<CharacterAnimationClipEntry>();

    public BossCoreStats Clone() =>
        new()
        {
            bossName = bossName,
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

    public void Normalize()
    {
        displayScale = Mathf.Max(0.1f, displayScale);
        if (string.IsNullOrWhiteSpace(bossName))
            bossName = "New Boss";

        if (tintColor.a <= 0f)
            tintColor = Color.white;

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
public class BossDefinition
{
    public BossCoreStats core = new();
    public EnemyLootPool lootPool = new();

    [Tooltip("Assign a script that inherits BossBehavior. Leave empty to use default EnemyAI.")]
    public string behaviorTypeName = "";

    public string BossName => core?.bossName ?? "Boss";

    public BossDefinition Clone() =>
        new()
        {
            core = core?.Clone() ?? new BossCoreStats(),
            lootPool = lootPool?.Clone() ?? new EnemyLootPool(),
            behaviorTypeName = behaviorTypeName
        };

    public bool HasCustomBehavior => !string.IsNullOrWhiteSpace(behaviorTypeName);

    public void Configure(Boss boss)
    {
        if (boss == null)
            return;

        boss.ApplyBossCore(core);
        boss.ApplyLootPool(lootPool);
    }

    public void Normalize()
    {
        core?.Normalize();
        lootPool?.Normalize();
    }

#if UNITY_EDITOR
    public void SetBehaviorType(Type behaviorType)
    {
        if (behaviorType == null || !typeof(BossBehavior).IsAssignableFrom(behaviorType))
        {
            behaviorTypeName = "";
            return;
        }

        behaviorTypeName = behaviorType.AssemblyQualifiedName;
    }

    public Type ResolveBehaviorType()
    {
        if (string.IsNullOrWhiteSpace(behaviorTypeName))
            return null;

        Type type = Type.GetType(behaviorTypeName);
        return type != null && typeof(BossBehavior).IsAssignableFrom(type) ? type : null;
    }
#endif
}

[Serializable]
public struct BossSelection
{
    public int index;

    public bool IsValid => index >= 0;

    public static BossSelection None => new() { index = -1 };

    public static BossSelection At(int bossIndex) => new() { index = bossIndex };
}