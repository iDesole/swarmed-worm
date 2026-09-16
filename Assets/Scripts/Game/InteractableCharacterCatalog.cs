using UnityEngine;

/// <summary>Runtime accessor for <see cref="InteractableCharacterDatabase"/>.</summary>
public static class InteractableCharacterCatalog
{
    private static InteractableCharacterDatabase database;

    public static InteractableCharacterDatabase Database =>
        CatalogResolver.Resolve(ref database, nameof(InteractableCharacterDatabase));

    public static void Register(InteractableCharacterDatabase characterDatabase)
    {
        if (characterDatabase == null)
            return;

        if (database == null || characterDatabase.Characters.Count > 0)
            database = characterDatabase;
    }

    public static void ForceRegister(InteractableCharacterDatabase characterDatabase)
    {
        if (characterDatabase != null)
            database = characterDatabase;
    }
}