#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Player))]
public class PlayerEditor : Editor
{
    private const string PlayerDatabasePath = "Assets/GameData/PlayerDatabase.asset";

    private SerializedProperty characterIndexProp;

    private void OnEnable()
    {
        characterIndexProp = serializedObject.FindProperty("characterIndex");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "m_Script", "characterIndex", "handSlots");

        EditorGUILayout.Space(6f);
        DrawCharacterPicker();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCharacterPicker()
    {
        EditorGUILayout.LabelField("Character", EditorStyles.boldLabel);

        PlayerDatabase database = AssetDatabase.LoadAssetAtPath<PlayerDatabase>(PlayerDatabasePath);
        if (database == null)
        {
            EditorGUILayout.HelpBox(
                $"Assign PlayerDatabase at {PlayerDatabasePath}.",
                MessageType.Warning);
            EditorGUILayout.PropertyField(characterIndexProp, new GUIContent("Character Index"));
            return;
        }

        IReadOnlyList<PlayerCharacterDefinition> characters = database.Characters;
        if (characters == null || characters.Count == 0)
        {
            EditorGUILayout.HelpBox("PlayerDatabase has no characters configured.", MessageType.Warning);
            return;
        }

        var options = new List<string>();
        for (int i = 0; i < characters.Count; i++)
        {
            string name = characters[i] != null ? characters[i].CharacterName : $"Character {i}";
            options.Add($"{i}: {name}");
        }

        int currentIndex = Mathf.Clamp(characterIndexProp.intValue, 0, characters.Count - 1);
        int newIndex = EditorGUILayout.Popup("Playable Character", currentIndex, options.ToArray());
        if (newIndex != currentIndex)
        {
            characterIndexProp.intValue = newIndex;
            EditorUtility.SetDirty(target);
        }

        PlayerCharacterDefinition selected = characters[newIndex];
        if (selected?.core == null)
            return;

        EditorGUILayout.LabelField("Stats Source", "Player Database", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("Health", $"{selected.core.maxHealth}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("Shield", $"{selected.core.maxShield}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("Move Speed", $"{selected.core.moveSpeed}", EditorStyles.miniLabel);
    }
}
#endif