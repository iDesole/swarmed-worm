using UnityEngine;

/// <summary>Runtime accessor for <see cref="BossDatabase"/> (boss definitions and spawn helpers).</summary>
public static class BossCatalog
{
    private static BossDatabase database;

    public static BossDatabase Database =>
        CatalogResolver.Resolve(ref database, nameof(BossDatabase));

    public static void Register(BossDatabase bossDatabase)
    {
        if (bossDatabase != null)
            database = bossDatabase;
    }
}