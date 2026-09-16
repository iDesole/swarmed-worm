using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns every entry from <see cref="InteractableCharacterDatabase"/> at runtime.
/// </summary>
[DefaultExecutionOrder(50)]
public class InteractableCharacterSpawner : MonoBehaviour
{
    [SerializeField] private InteractableCharacterDatabase database;
    [SerializeField] private Transform characterRoot;

    private readonly List<InteractableCharacter> spawnedCharacters = new();
    private bool hasSpawned;

    private void Reset()
    {
        characterRoot = transform.Find("Character Root");
    }

    private void Awake()
    {
        if (database == null)
            database = InteractableCharacterCatalog.Database;

        EnsureCharacterRoot();
    }

    private void OnEnable()
    {
        EnvironmentEvents.EnvironmentChanged += HandleEnvironmentChanged;
    }

    private void OnDisable()
    {
        EnvironmentEvents.EnvironmentChanged -= HandleEnvironmentChanged;
    }

    private void Start() => SpawnAllCharacters();

    private void HandleEnvironmentChanged(WaveEnvironmentDefinition environment)
    {
        if (!hasSpawned)
        {
            SpawnAllCharacters();
            return;
        }

        for (int i = 0; i < spawnedCharacters.Count; i++)
        {
            InteractableCharacter character = spawnedCharacters[i];
            if (character != null)
                character.ApplySpawnPlacement();
        }
    }

    private void EnsureCharacterRoot()
    {
        if (characterRoot != null)
            return;

        characterRoot = transform.Find("Character Root");
        if (characterRoot != null)
            return;

        var rootObject = new GameObject("Character Root");
        characterRoot = rootObject.transform;
        characterRoot.SetParent(transform, false);
    }

    private void SpawnAllCharacters()
    {
        if (database == null)
            database = InteractableCharacterCatalog.Database;

        ClearSpawnedCharacters();

        if (database == null)
        {
            GameLog.Warning("InteractableCharacterSpawner has no database assigned.");
            return;
        }

        IReadOnlyList<InteractableCharacterDefinition> definitions = database.Characters;
        if (definitions == null || definitions.Count == 0)
        {
            GameLog.Warning($"InteractableCharacterSpawner database '{database.name}' has no characters.");
            return;
        }

        for (int i = 0; i < definitions.Count; i++)
        {
            InteractableCharacterDefinition definition = definitions[i];
            if (definition == null)
                continue;

            InteractableCharacter character = database.CreateCharacter(definition, characterRoot);
            if (character == null)
                continue;

            spawnedCharacters.Add(character);
            GameLog.Info(
                $"Spawned NPC '{definition.ResolvedName}' for map '{definition.spawnPlacement.environmentName}' at {definition.spawnPlacement.worldPosition}.");
        }

        hasSpawned = spawnedCharacters.Count > 0;
    }

    private void ClearSpawnedCharacters()
    {
        for (int i = 0; i < spawnedCharacters.Count; i++)
        {
            InteractableCharacter character = spawnedCharacters[i];
            if (character != null)
                Destroy(character.gameObject);
        }

        spawnedCharacters.Clear();
        hasSpawned = false;
    }
}