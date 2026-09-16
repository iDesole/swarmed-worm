using UnityEngine;

/// <summary>
/// Builds enemy GameObjects from <see cref="EnemyDatabase"/> definitions (mirrors <see cref="WeaponFactory"/>).
/// </summary>
/// <remarks>
/// EnemySpawner picks a weighted spawn entry, then calls Create with position on valid ground tiles.
/// </remarks>
public static class EnemyFactory
{
    /// <summary>Creates an enemy from category + index (e.g. Melee #0).</summary>
    public static Enemy Create(
        EnemySelection selection,
        EnemyDatabase database,
        Vector3 position)
    {
        if (database == null || !selection.IsValid)
            return null;

        return selection.category switch
        {
            EnemyCategory.Melee when selection.index < database.MeleeEnemies.Count =>
                Create(database.MeleeEnemies[selection.index], position),
            EnemyCategory.Projectile when selection.index < database.ProjectileEnemies.Count =>
                Create(database.ProjectileEnemies[selection.index], database, position),
            EnemyCategory.Summon when selection.index < database.SummonEnemies.Count =>
                Create(database.SummonEnemies[selection.index], position),
            EnemyCategory.Miniboss when selection.index < database.MinibossEnemies.Count =>
                Create(database.MinibossEnemies[selection.index], database, position),
            EnemyCategory.Boss when selection.index < database.BossEnemies.Count =>
                Create(database.BossEnemies[selection.index], database, position),
            _ => null
        };
    }

    public static Enemy Create(MeleeEnemyDefinition definition, Vector3 position)
    {
        if (definition?.core == null)
            return null;

        MeleeEnemyDefinition snapshot = definition.Clone();
        MeleeEnemy enemy = IsTrilobite(snapshot.core.enemyName)
            ? BuildBase<TrilobiteEnemy>(snapshot.core, position)
            : BuildBase<MeleeEnemy>(snapshot.core, position);
        snapshot.Configure(enemy);
        return enemy;
    }

    private static bool IsTrilobite(string enemyName) =>
        string.Equals(enemyName, "Trilobite", System.StringComparison.OrdinalIgnoreCase)
        || string.Equals(enemyName, "TriloBite", System.StringComparison.OrdinalIgnoreCase);

    public static Enemy Create(
        ProjectileEnemyDefinition definition,
        EnemyDatabase database,
        Vector3 position)
    {
        if (definition?.core == null)
            return null;

        ProjectileEnemyDefinition snapshot = definition.Clone();
        RangedEnemy enemy = BuildBase<RangedEnemy>(snapshot.core, position);
        snapshot.Configure(enemy, database);
        return enemy;
    }

    public static Enemy Create(SummonEnemyDefinition definition, Vector3 position)
    {
        if (definition?.core == null)
            return null;

        SummonEnemyDefinition snapshot = definition.Clone();
        SummonEnemy enemy = BuildBase<SummonEnemy>(snapshot.core, position);
        snapshot.Configure(enemy);
        return enemy;
    }

    public static Enemy Create(
        MinibossEnemyDefinition definition,
        EnemyDatabase database,
        Vector3 position)
    {
        if (definition?.core == null)
            return null;

        MinibossEnemyDefinition snapshot = definition.Clone();
        Enemy enemy = CreateFromBehavior(snapshot.behavior, snapshot.core, database, position, null);
        snapshot.Configure(enemy, database);
        return enemy;
    }

    public static Enemy Create(
        BossEnemyDefinition definition,
        EnemyDatabase database,
        Vector3 position)
    {
        if (definition?.core == null)
            return null;

        BossEnemyDefinition snapshot = definition.Clone();
        Enemy enemy = CreateFromBehavior(snapshot.behavior, snapshot.core, database, position, null);
        snapshot.Configure(enemy, database);
        return enemy;
    }

    public static Enemy Create(EnemyStats stats, Vector3 position)
    {
        if (stats == null)
            return null;

        Enemy enemy = stats.BehaviorType switch
        {
            EnemyBehaviorType.Ranged => BuildShell<RangedEnemy>(stats.EnemyName, position),
            EnemyBehaviorType.Burrower => BuildShell<BurrowerEnemy>(stats.EnemyName, position),
            _ => BuildShell<MeleeEnemy>(stats.EnemyName, position)
        };

        enemy.AssignStats(stats);

        if (enemy is RangedEnemy ranged)
            ranged.projectileSpeed = stats.ProjectileSpeed;

        return enemy;
    }

    private static Enemy CreateFromBehavior(
        EnemyBehaviorKind behavior,
        EnemyCoreStats core,
        EnemyDatabase database,
        Vector3 position,
        System.Action<Enemy> configure)
    {
        Enemy enemy = behavior switch
        {
            EnemyBehaviorKind.Projectile => BuildBase<RangedEnemy>(core, position),
            EnemyBehaviorKind.Summon => BuildBase<SummonEnemy>(core, position),
            _ => BuildBase<MeleeEnemy>(core, position)
        };

        configure?.Invoke(enemy);
        return enemy;
    }

    private static T BuildBase<T>(EnemyCoreStats core, Vector3 position) where T : Enemy
    {
        T enemy = BuildShell<T>(core.enemyName, position);
        enemy.ApplyCore(core);
        return enemy;
    }

    private static T BuildShell<T>(string enemyName, Vector3 position) where T : Enemy
    {
        var enemyObject = new GameObject(string.IsNullOrWhiteSpace(enemyName) ? "Enemy" : enemyName);
        enemyObject.transform.position = position;
        enemyObject.tag = "Enemy";

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1)
            enemyObject.layer = enemyLayer;

        SpriteRenderer spriteRenderer = enemyObject.AddComponent<SpriteRenderer>();
        RenderVisibilityUtility.ConfigureSpriteRenderer(spriteRenderer, sortingOrder: 0);

        var rb = enemyObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = enemyObject.AddComponent<CircleCollider2D>();
        col.radius = 0.45f;

        T enemy = enemyObject.AddComponent<T>();
        enemyObject.AddComponent<EnemyAI>();
        return enemy;
    }
}