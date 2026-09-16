using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDatabase", menuName = "Game/Enemy Database")]
public class EnemyDatabase : ScriptableObject
{
    [SerializeField] private List<MeleeEnemyDefinition> meleeEnemies = new();
    [SerializeField] private List<ProjectileEnemyDefinition> projectileEnemies = new();
    [SerializeField] private List<SummonEnemyDefinition> summonEnemies = new();
    [SerializeField] private List<MinibossEnemyDefinition> minibossEnemies = new();
    [SerializeField] private List<BossEnemyDefinition> bossEnemies = new();

    [Header("Shared Prefabs")]
    [SerializeField] private GameObject defaultProjectilePrefab;

    public IReadOnlyList<MeleeEnemyDefinition> MeleeEnemies => meleeEnemies;
    public IReadOnlyList<ProjectileEnemyDefinition> ProjectileEnemies => projectileEnemies;
    public IReadOnlyList<SummonEnemyDefinition> SummonEnemies => summonEnemies;
    public IReadOnlyList<MinibossEnemyDefinition> MinibossEnemies => minibossEnemies;
    public IReadOnlyList<BossEnemyDefinition> BossEnemies => bossEnemies;

    public void ApplyProjectileFallbacks(RangedEnemy enemy)
    {
        if (enemy != null && enemy.projectilePrefab == null)
            enemy.projectilePrefab = defaultProjectilePrefab;
    }

    public string GetEnemyName(EnemySelection selection)
    {
        if (!selection.IsValid)
            return string.Empty;

        return selection.category switch
        {
            EnemyCategory.Melee when InRange(meleeEnemies, selection.index) =>
                meleeEnemies[selection.index].EnemyName,
            EnemyCategory.Projectile when InRange(projectileEnemies, selection.index) =>
                projectileEnemies[selection.index].EnemyName,
            EnemyCategory.Summon when InRange(summonEnemies, selection.index) =>
                summonEnemies[selection.index].EnemyName,
            EnemyCategory.Miniboss when InRange(minibossEnemies, selection.index) =>
                minibossEnemies[selection.index].EnemyName,
            EnemyCategory.Boss when InRange(bossEnemies, selection.index) =>
                bossEnemies[selection.index].EnemyName,
            _ => string.Empty
        };
    }

    public EnemySelection FindSelection(EnemyCategory category, string enemyName)
    {
        if (string.IsNullOrWhiteSpace(enemyName))
            return default;

        return category switch
        {
            EnemyCategory.Melee => FindIndex(meleeEnemies, enemyName, EnemySelection.Melee),
            EnemyCategory.Projectile => FindIndex(projectileEnemies, enemyName, EnemySelection.Projectile),
            EnemyCategory.Summon => FindIndex(summonEnemies, enemyName, EnemySelection.Summon),
            EnemyCategory.Miniboss => FindIndex(minibossEnemies, enemyName, EnemySelection.Miniboss),
            EnemyCategory.Boss => FindIndex(bossEnemies, enemyName, EnemySelection.Boss),
            _ => default
        };
    }

    public Sprite GetEnemySprite(EnemySelection selection) =>
        TryGetEnemyCore(selection, out EnemyCoreStats core) ? core.sprite : null;

    public bool TryGetEnemyCore(EnemySelection selection, out EnemyCoreStats core)
    {
        core = null;
        if (!selection.IsValid)
            return false;

        core = selection.category switch
        {
            EnemyCategory.Melee when InRange(meleeEnemies, selection.index) =>
                meleeEnemies[selection.index].core,
            EnemyCategory.Projectile when InRange(projectileEnemies, selection.index) =>
                projectileEnemies[selection.index].core,
            EnemyCategory.Summon when InRange(summonEnemies, selection.index) =>
                summonEnemies[selection.index].core,
            EnemyCategory.Miniboss when InRange(minibossEnemies, selection.index) =>
                minibossEnemies[selection.index].core,
            EnemyCategory.Boss when InRange(bossEnemies, selection.index) =>
                bossEnemies[selection.index].core,
            _ => null
        };

        return core != null;
    }

    public Enemy CreateEnemy(EnemySelection selection, Vector3 position) =>
        EnemyFactory.Create(selection, this, position);

    private void OnEnable() => EnemyCatalog.Register(this);

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            return;
#endif

        meleeEnemies ??= new List<MeleeEnemyDefinition>();
        projectileEnemies ??= new List<ProjectileEnemyDefinition>();
        summonEnemies ??= new List<SummonEnemyDefinition>();
        minibossEnemies ??= new List<MinibossEnemyDefinition>();
        bossEnemies ??= new List<BossEnemyDefinition>();

        NormalizeCores(meleeEnemies);
        NormalizeCores(projectileEnemies);
        NormalizeCores(summonEnemies);
        NormalizeCores(minibossEnemies);
        NormalizeCores(bossEnemies);

        foreach (MeleeEnemyDefinition enemy in meleeEnemies)
            enemy?.lootPool?.Normalize();

        foreach (ProjectileEnemyDefinition enemy in projectileEnemies)
            enemy?.lootPool?.Normalize();

        foreach (SummonEnemyDefinition enemy in summonEnemies)
            enemy?.lootPool?.Normalize();

        foreach (MinibossEnemyDefinition enemy in minibossEnemies)
            enemy?.lootPool?.Normalize();

        foreach (BossEnemyDefinition enemy in bossEnemies)
            enemy?.lootPool?.Normalize();
    }

    private static void NormalizeCores<T>(IReadOnlyList<T> enemies) where T : class
    {
        if (enemies == null)
            return;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyCoreStats core = enemies[i] switch
            {
                MeleeEnemyDefinition melee => melee.core,
                ProjectileEnemyDefinition projectile => projectile.core,
                SummonEnemyDefinition summon => summon.core,
                MinibossEnemyDefinition miniboss => miniboss.core,
                BossEnemyDefinition boss => boss.core,
                _ => null
            };

            core?.Normalize();
        }
    }

    private static bool InRange<T>(IReadOnlyList<T> list, int index) =>
        list != null && index >= 0 && index < list.Count;

    private static EnemySelection FindIndex<T>(
        IReadOnlyList<T> enemies,
        string enemyName,
        Func<int, EnemySelection> makeSelection) where T : class
    {
        if (enemies == null)
            return default;

        for (int i = 0; i < enemies.Count; i++)
        {
            string name = enemies[i] switch
            {
                MeleeEnemyDefinition melee => melee.EnemyName,
                ProjectileEnemyDefinition projectile => projectile.EnemyName,
                SummonEnemyDefinition summon => summon.EnemyName,
                MinibossEnemyDefinition miniboss => miniboss.EnemyName,
                BossEnemyDefinition boss => boss.EnemyName,
                _ => null
            };

            if (string.Equals(name, enemyName, StringComparison.OrdinalIgnoreCase))
                return makeSelection(i);
        }

        return default;
    }
}