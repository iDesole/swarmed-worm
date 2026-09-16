using System;
using UnityEngine;

/// <summary>
/// Builds boss GameObjects from <see cref="BossDatabase"/> definitions.
/// </summary>
public static class BossFactory
{
    public static Boss Create(BossSelection selection, BossDatabase database, Vector3 position)
    {
        if (database == null || !selection.IsValid || selection.index >= database.Bosses.Count)
            return null;

        BossDefinition definition = database.Bosses[selection.index];
        return Create(definition, position);
    }

    public static Boss Create(BossDefinition definition, Vector3 position)
    {
        if (definition?.core == null)
            return null;

        BossDefinition snapshot = definition.Clone();
        snapshot.Normalize();

        Boss boss = BuildShell(snapshot.core.bossName, position);
        snapshot.Configure(boss);
        BossSpriteUtility.EnsureBossVisible(boss, snapshot.core);
        AttachBehavior(boss, snapshot);
        return boss;
    }

    private static void AttachBehavior(Boss boss, BossDefinition definition)
    {
        if (!definition.HasCustomBehavior)
            return;

        Type behaviorType = Type.GetType(definition.behaviorTypeName);
        if (behaviorType == null || !typeof(BossBehavior).IsAssignableFrom(behaviorType))
        {
            GameLog.Warning($"Boss '{definition.BossName}' behavior type not found: {definition.behaviorTypeName}");
            return;
        }

        BossBehavior behavior = (BossBehavior)boss.gameObject.AddComponent(behaviorType);
        boss.AttachBehavior(behavior);
    }

    private static Boss BuildShell(string bossName, Vector3 position)
    {
        var bossObject = new GameObject(string.IsNullOrWhiteSpace(bossName) ? "Boss" : bossName);
        bossObject.transform.position = position;
        bossObject.tag = "Enemy";

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1)
            bossObject.layer = enemyLayer;

        SpriteRenderer spriteRenderer = bossObject.AddComponent<SpriteRenderer>();
        RenderVisibilityUtility.ConfigureSpriteRenderer(
            spriteRenderer,
            sortingOrder: CharacterPresentationConstants.CharacterSpriteSortingOrder);

        var rb = bossObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = bossObject.AddComponent<CircleCollider2D>();
        col.radius = 0.65f;

        Boss boss = bossObject.AddComponent<Boss>();
        bossObject.AddComponent<EnemyAI>();
        return boss;
    }
}