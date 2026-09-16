#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    private SerializedProperty waveDatabaseProp;
    private SerializedProperty playerDatabaseProp;
    private SerializedProperty startCharacterIndexProp;
    private SerializedProperty startEnvironmentNameProp;
    private SerializedProperty hubEnvironmentPrefabProp;

    private void OnEnable()
    {
        waveDatabaseProp = serializedObject.FindProperty("waveDatabase");
        playerDatabaseProp = serializedObject.FindProperty("playerDatabase");
        startCharacterIndexProp = serializedObject.FindProperty("startCharacterIndex");
        startEnvironmentNameProp = serializedObject.FindProperty("startEnvironmentName");
        hubEnvironmentPrefabProp = serializedObject.FindProperty("hubEnvironmentPrefab");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawEnvironmentSection();
        EditorGUILayout.Space(8f);
        DrawCharacterSection();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawEnvironmentSection()
    {
        EditorGUILayout.LabelField("Start Environment", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(waveDatabaseProp);
        EditorGUILayout.PropertyField(startEnvironmentNameProp, new GUIContent("Start Environment Name"));
        EditorGUILayout.PropertyField(
            hubEnvironmentPrefabProp,
            new GUIContent("Hub Environment Prefab",
                "Lobby prefab spawned at game start. Overrides the Wave Database lobby entry when set."));

        var database = waveDatabaseProp.objectReferenceValue as WaveDatabase;
        if (database == null)
        {
            EditorGUILayout.HelpBox(
                "Assign a Wave Database. Tileset prefabs are configured per environment on that asset.",
                MessageType.Info);
            return;
        }

        database.EnsureHasEnvironments();
        string startName = startEnvironmentNameProp.stringValue;
        WaveEnvironmentDefinition startEnvironment = database.GetEnvironmentByName(startName)
            ?? database.GetActiveEnvironment();

        EditorGUILayout.HelpBox(
            "Play mode always spawns the prefab from the start environment entry below. " +
            "Scene tilemaps in Beach.unity are removed at runtime.",
            MessageType.Info);

        if (startEnvironment == null)
        {
            EditorGUILayout.HelpBox("Could not find a start environment on the Wave Database.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Start Prefab", startEnvironment.EnvironmentName, EditorStyles.boldLabel);

        if (startEnvironment.environmentPrefab == null)
        {
            EditorGUILayout.HelpBox(
                $"Assign a tileset prefab to the '{startEnvironment.EnvironmentName}' environment on the Wave Database.",
                MessageType.Warning);
        }
        else
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(
                "Tileset Prefab",
                startEnvironment.environmentPrefab,
                typeof(GameObject),
                false);
            EditorGUI.EndDisabledGroup();
        }

        if (GUILayout.Button("Select Wave Database"))
            Selection.activeObject = database;

        if (startEnvironment.environmentPrefab != null && GUILayout.Button("Select Start Prefab"))
            Selection.activeObject = startEnvironment.environmentPrefab;
    }

    private void DrawCharacterSection()
    {
        EditorGUILayout.LabelField("Start Character", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(playerDatabaseProp);

        var database = playerDatabaseProp.objectReferenceValue as PlayerDatabase;
        if (database == null)
        {
            EditorGUILayout.HelpBox(
                "Assign a Player Database to pick which character the player starts as.",
                MessageType.Info);
            return;
        }

        var characters = database.Characters;
        if (characters == null || characters.Count == 0)
        {
            EditorGUILayout.HelpBox("Player Database has no characters.", MessageType.Warning);
            return;
        }

        var labels = new string[characters.Count];
        for (int i = 0; i < characters.Count; i++)
        {
            PlayerCharacterDefinition character = characters[i];
            string name = character != null ? character.CharacterName : $"Character {i + 1}";
            labels[i] = $"{name}";
        }

        int clampedIndex = Mathf.Clamp(startCharacterIndexProp.intValue, 0, characters.Count - 1);
        int selectedIndex = EditorGUILayout.Popup("Character", clampedIndex, labels);
        startCharacterIndexProp.intValue = selectedIndex;

        PlayerCharacterDefinition selectedCharacter = characters[selectedIndex];
        if (selectedCharacter?.core == null)
            return;

        EditorGUILayout.LabelField("Health", selectedCharacter.core.maxHealth.ToString(), EditorStyles.miniLabel);
        EditorGUILayout.LabelField("Shield", selectedCharacter.core.maxShield.ToString(), EditorStyles.miniLabel);
        EditorGUILayout.LabelField("Move Speed", selectedCharacter.core.moveSpeed.ToString(), EditorStyles.miniLabel);
    }
}
#endif