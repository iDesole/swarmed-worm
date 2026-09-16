using UnityEngine;

/// <summary>Runtime accessor for <see cref="EnemyDatabase"/> (enemy definitions and prefabs).</summary>
public static class EnemyCatalog
{
    private static EnemyDatabase database;

    public static EnemyDatabase Database =>
        CatalogResolver.Resolve(ref database, nameof(EnemyDatabase));

    public static void Register(EnemyDatabase enemyDatabase)
    {
        if (enemyDatabase != null)
            database = enemyDatabase;
    }
}