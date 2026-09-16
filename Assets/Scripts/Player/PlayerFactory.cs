using UnityEngine;

/// <summary>
/// Assembles the player GameObject from a <see cref="PlayerCharacterDefinition"/> snapshot.
/// </summary>
/// <remarks>
/// Adds physics, rendering, hand slots, optional enemy spawner child, then calls Player.Initialize.
/// Called by <see cref="PlayerCatalog.SpawnPlayer"/> — not directly from gameplay code.
/// </remarks>
public static class PlayerFactory
{
    /// <summary>Builds a fully wired player at the given world position.</summary>
    public static Player Create(
        PlayerCharacterDefinition definition,
        int characterIndex,
        Vector3? positionOverride = null)
    {
        if (definition == null)
            return null;

        PlayerCharacterDefinition snapshot = definition.Clone();
        snapshot.Normalize();

        Vector3 position = positionOverride ?? snapshot.spawnPosition;
        position.z = 0f;

        string objectName = string.IsNullOrWhiteSpace(snapshot.CharacterName)
            ? "Player"
            : snapshot.CharacterName;

        var playerObject = new GameObject(objectName);
        playerObject.tag = "Player";
        playerObject.transform.position = position;

        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer != -1)
            playerObject.layer = playerLayer;

        SpriteRenderer spriteRenderer = playerObject.AddComponent<SpriteRenderer>();
        PlayerSpriteUtility.ConfigureRenderer(spriteRenderer, snapshot.core?.sprite, sortingOrder: 1);

        Rigidbody2D rb = playerObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        CircleCollider2D collider = playerObject.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;

        WeaponSlotManager slotManager = playerObject.AddComponent<WeaponSlotManager>();
        slotManager.ConfigureHandSlots(snapshot.GetResolvedHandSlots());

        playerObject.AddComponent<PlayerBurrowSignal>();

        Player player = playerObject.AddComponent<Player>();
        player.Initialize(characterIndex, snapshot);
        CharacterBlobShadow.Ensure(playerObject, sortingOrder: CharacterPresentationConstants.PlayerShadowSortingOrder);

        if (snapshot.enemySpawner.attachToPlayer)
            CreateEnemySpawner(playerObject.transform, snapshot.enemySpawner);

        return player;
    }

    private static void CreateEnemySpawner(Transform playerTransform, PlayerEnemySpawnerDefinition config)
    {
        if (playerTransform == null || config == null)
            return;

        var spawnerObject = new GameObject("EnemySpawner");
        spawnerObject.transform.SetParent(playerTransform, false);
        spawnerObject.transform.localPosition = config.localOffset;

        EnemySpawner spawner = spawnerObject.AddComponent<EnemySpawner>();
        spawner.maxSpawnAttempts = config.maxSpawnAttempts;
        spawner.player = playerTransform;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (config.showSpawnRings)
            spawnerObject.AddComponent<EnemySpawnRingVisualizer>();
#endif
    }
}