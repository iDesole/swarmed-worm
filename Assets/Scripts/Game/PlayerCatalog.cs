using System;
using UnityEngine;

/// <summary>
/// Runtime access point for the active player and <see cref="PlayerDatabase"/>.
/// </summary>
/// <remarks>
/// Tracks the single live player instance. Other systems (spawner, camera, loot) query ActiveTransform here
/// instead of searching the scene every frame.
/// </remarks>
public static class PlayerCatalog
{
    private static PlayerDatabase database;
    private static Player activePlayer;

    public static event Action<Player> PlayerSpawned;

    public static Player ActivePlayer => activePlayer;
    public static Transform ActiveTransform => activePlayer != null ? activePlayer.transform : null;

    public static PlayerDatabase Database =>
        CatalogResolver.Resolve(ref database, nameof(PlayerDatabase));

    public static void Register(PlayerDatabase playerDatabase)
    {
        if (playerDatabase != null)
            database = playerDatabase;
    }

    public static Player SpawnPlayer(PlayerSelection selection, Vector3? positionOverride = null)
    {
        PlayerDatabase resolvedDatabase = Database;
        if (resolvedDatabase == null)
        {
            GameLog.Error("Cannot spawn player without PlayerDatabase.");
            return null;
        }

        PlayerCharacterDefinition character = resolvedDatabase.GetCharacter(selection)
            ?? resolvedDatabase.GetDefaultCharacter();

        if (character == null)
        {
            GameLog.Error("PlayerDatabase has no characters configured.");
            return null;
        }

        int characterIndex = selection.IsValid
            ? selection.index
            : resolvedDatabase.DefaultCharacterIndex;

        ClearActivePlayer();

        Player player = PlayerFactory.Create(character, characterIndex, positionOverride);
        if (player == null)
            return null;

        activePlayer = player;
        PlayerSpawned?.Invoke(player);
        return player;
    }

    public static void ClearActivePlayer()
    {
        if (activePlayer == null)
            return;

        if (activePlayer.gameObject != null)
            UnityEngine.Object.Destroy(activePlayer.gameObject);

        activePlayer = null;
    }

}