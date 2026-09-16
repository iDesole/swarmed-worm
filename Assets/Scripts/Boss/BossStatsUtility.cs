using System;

/// <summary>Converts boss data into enemy core stats so Boss can reuse Enemy combat logic.</summary>
public static class BossStatsUtility
{
    public static EnemyCoreStats ToEnemyCore(BossCoreStats boss)
    {
        if (boss == null)
            return null;

        return new EnemyCoreStats
        {
            enemyName = boss.bossName,
            sprite = boss.sprite,
            displayScale = boss.displayScale,
            tintColor = boss.tintColor,
            maxHealth = boss.maxHealth,
            moveSpeed = boss.moveSpeed,
            damage = boss.damage,
            attackCooldown = boss.attackCooldown,
            detectionRange = boss.detectionRange,
            attackRange = boss.attackRange,
            aggression = boss.aggression,
            knockbackResistance = boss.knockbackResistance,
            damageResistance = boss.damageResistance,
            animationClips = CloneAnimationClips(boss.animationClips)
        };
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