using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerDatabase", menuName = "Game/Player Database")]
public class PlayerDatabase : ScriptableObject
{
    [SerializeField] private List<PlayerCharacterDefinition> characters = new();
    [SerializeField] private int defaultCharacterIndex;

    public IReadOnlyList<PlayerCharacterDefinition> Characters => characters;
    public int DefaultCharacterIndex => defaultCharacterIndex;

    public string GetCharacterName(PlayerSelection selection)
    {
        PlayerCharacterDefinition character = GetCharacter(selection);
        return character != null ? character.CharacterName : string.Empty;
    }

    public PlayerSelection FindSelection(string characterName)
    {
        if (string.IsNullOrWhiteSpace(characterName) || characters == null)
            return PlayerSelection.None;

        for (int i = 0; i < characters.Count; i++)
        {
            PlayerCharacterDefinition character = characters[i];
            if (character == null)
                continue;

            if (string.Equals(character.CharacterName, characterName, StringComparison.OrdinalIgnoreCase))
                return PlayerSelection.At(i);
        }

        return PlayerSelection.None;
    }

    public PlayerCharacterDefinition GetCharacter(PlayerSelection selection)
    {
        if (!selection.IsValid || !InRange(characters, selection.index))
            return null;

        return characters[selection.index];
    }

    public PlayerCharacterDefinition GetDefaultCharacter()
    {
        if (characters == null || characters.Count == 0)
            return null;

        int index = Mathf.Clamp(defaultCharacterIndex, 0, characters.Count - 1);
        return characters[index];
    }

    public void ApplyToPlayer(Player player, PlayerSelection selection)
    {
        if (player == null)
            return;

        PlayerCharacterDefinition character = GetCharacter(selection) ?? GetDefaultCharacter();
        character?.Configure(player);
    }

    private void OnEnable() => PlayerCatalog.Register(this);

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            return;
#endif

        characters ??= new List<PlayerCharacterDefinition>();
        defaultCharacterIndex = Mathf.Max(0, defaultCharacterIndex);

        foreach (PlayerCharacterDefinition character in characters)
            character?.Normalize();
    }

    private static bool InRange<T>(IReadOnlyList<T> list, int index) =>
        list != null && index >= 0 && index < list.Count;
}