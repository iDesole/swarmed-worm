using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InteractableCharacterDatabase", menuName = "Game/Interactable Character Database")]
public class InteractableCharacterDatabase : ScriptableObject
{
    [SerializeField] private List<InteractableCharacterDefinition> characters = new();

    public IReadOnlyList<InteractableCharacterDefinition> Characters => characters;

    public InteractableCharacter CreateCharacter(int index, Transform parent = null)
    {
        if (!InRange(characters, index))
            return null;

        return CreateCharacter(characters[index], parent);
    }

    public InteractableCharacter CreateCharacter(InteractableCharacterDefinition definition, Transform parent = null) =>
        InteractableCharacterFactory.Create(definition, parent);

    public bool TryGetById(string characterId, out InteractableCharacterDefinition definition)
    {
        definition = null;
        if (string.IsNullOrWhiteSpace(characterId) || characters == null)
            return false;

        for (int i = 0; i < characters.Count; i++)
        {
            InteractableCharacterDefinition candidate = characters[i];
            if (candidate == null || string.IsNullOrWhiteSpace(candidate.characterId))
                continue;

            if (string.Equals(candidate.characterId, characterId, StringComparison.OrdinalIgnoreCase))
            {
                definition = candidate;
                return true;
            }
        }

        return false;
    }

    public int FindIndex(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId) || characters == null)
            return -1;

        for (int i = 0; i < characters.Count; i++)
        {
            InteractableCharacterDefinition candidate = characters[i];
            if (candidate != null &&
                string.Equals(candidate.characterId, characterId, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private void OnEnable() => InteractableCharacterCatalog.Register(this);

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            return;
#endif

        characters ??= new List<InteractableCharacterDefinition>();

        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (InteractableCharacterDefinition character in characters)
        {
            if (character == null)
                continue;

            if (string.IsNullOrWhiteSpace(character.characterId))
                character.characterId = "new_character";

            if (!seenIds.Add(character.characterId))
                GameLog.Warning($"Duplicate interactable character id '{character.characterId}' in {name}.");
        }
    }

    private static bool InRange<T>(IReadOnlyList<T> list, int index) =>
        list != null && index >= 0 && index < list.Count;
}