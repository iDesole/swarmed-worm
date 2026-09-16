#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SceneDoorTrigger))]
public class SceneDoorTriggerEditor : Editor
{
    private SerializedProperty waveDatabaseProp;
    private SerializedProperty destinationTypeProp;
    private SerializedProperty targetEnvironmentNameProp;
    private SerializedProperty targetEnvironmentIndexProp;
    private SerializedProperty sceneNameProp;
    private SerializedProperty targetSpawnPointIdProp;
    private SerializedProperty oneShotProp;
    private SerializedProperty reentryCooldownProp;
    private SerializedProperty showGizmoProp;

    private void OnEnable()
    {
        waveDatabaseProp = serializedObject.FindProperty("waveDatabase");
        destinationTypeProp = serializedObject.FindProperty("destinationType");
        targetEnvironmentNameProp = serializedObject.FindProperty("targetEnvironmentName");
        targetEnvironmentIndexProp = serializedObject.FindProperty("targetEnvironmentIndex");
        sceneNameProp = serializedObject.FindProperty("sceneName");
        targetSpawnPointIdProp = serializedObject.FindProperty("targetSpawnPointId");
        oneShotProp = serializedObject.FindProperty("oneShot");
        reentryCooldownProp = serializedObject.FindProperty("reentryCooldown");
        showGizmoProp = serializedObject.FindProperty("showGizmoInSceneView");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Scene Door Trigger", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(destinationTypeProp);

        DoorDestinationType destinationType =
            (DoorDestinationType)destinationTypeProp.enumValueIndex;

        EditorGUILayout.Space(6f);

        switch (destinationType)
        {
            case DoorDestinationType.SwitchMapPreset:
                DrawMapPresetSection();
                break;

            case DoorDestinationType.LoadScene:
                EditorGUILayout.PropertyField(sceneNameProp, new GUIContent("Scene Name"));
                EditorGUILayout.HelpBox(
                    "The scene must be listed in File > Build Settings.",
                    MessageType.None);
                break;

            case DoorDestinationType.TeleportToSpawnPoint:
                EditorGUILayout.HelpBox(
                    "Moves the player to a SceneSpawnPoint in the current scene. Does not change map presets.",
                    MessageType.Info);
                break;
        }

        if (destinationType != DoorDestinationType.TeleportToSpawnPoint)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(
                targetSpawnPointIdProp,
                new GUIContent("Target Spawn Point Id",
                    "Optional SceneSpawnPoint ID used after the transition."));
        }
        else
        {
            EditorGUILayout.PropertyField(targetSpawnPointIdProp, new GUIContent("Spawn Point Id"));
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Behavior", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(oneShotProp);
        EditorGUILayout.PropertyField(reentryCooldownProp);
        EditorGUILayout.PropertyField(showGizmoProp);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawMapPresetSection()
    {
        EditorGUILayout.LabelField("Map Preset", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(waveDatabaseProp);

        WaveDatabase database = waveDatabaseProp.objectReferenceValue as WaveDatabase;
        if (database == null)
            database = ResolveWaveDatabaseFromScene();

        if (database == null)
        {
            EditorGUILayout.HelpBox(
                "Assign a Wave Database, or add GameManager with a Wave Database in this scene.",
                MessageType.Warning);
            EditorGUILayout.PropertyField(targetEnvironmentNameProp, new GUIContent("Map Preset Name"));
            return;
        }

        database.EnsureHasEnvironments();
        var environments = database.Environments;
        if (environments.Count == 0)
        {
            EditorGUILayout.HelpBox("Wave Database has no map presets.", MessageType.Warning);
            return;
        }

        string[] labels = BuildEnvironmentLabels(environments);
        int currentIndex = ResolveSelectedIndex(database, labels);

        EditorGUILayout.HelpBox(
            "Choose which map preset this door loads from the Wave Database.",
            MessageType.None);

        int selectedIndex = EditorGUILayout.Popup("Map Preset", currentIndex, labels);
        if (selectedIndex != currentIndex)
        {
            targetEnvironmentIndexProp.intValue = selectedIndex;
            targetEnvironmentNameProp.stringValue = environments[selectedIndex] != null
                ? environments[selectedIndex].EnvironmentName
                : string.Empty;
        }

        WaveEnvironmentDefinition selectedEnvironment = environments[selectedIndex];
        if (selectedEnvironment == null)
            return;

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField(
            "Tileset Prefab",
            selectedEnvironment.environmentPrefab,
            typeof(GameObject),
            false);
        EditorGUI.EndDisabledGroup();

        if (selectedEnvironment.environmentPrefab == null)
        {
            EditorGUILayout.HelpBox(
                "This map preset has no tileset prefab assigned on the Wave Database.",
                MessageType.Warning);
        }

        if (selectedEnvironment.IsHub)
        {
            EditorGUILayout.HelpBox("Hub preset: waves stay off while the player is here.", MessageType.Info);
        }

        if (GUILayout.Button("Select Wave Database"))
            Selection.activeObject = database;
    }

    private static string[] BuildEnvironmentLabels(System.Collections.Generic.IReadOnlyList<WaveEnvironmentDefinition> environments)
    {
        var labels = new string[environments.Count];
        for (int i = 0; i < environments.Count; i++)
        {
            WaveEnvironmentDefinition environment = environments[i];
            string name = environment != null ? environment.EnvironmentName : $"Environment {i + 1}";
            string hubTag = environment != null && environment.IsHub ? " [Hub]" : string.Empty;
            string prefabName = environment?.environmentPrefab != null
                ? environment.environmentPrefab.name
                : "No Prefab";
            labels[i] = $"{name}{hubTag} ({prefabName})";
        }

        return labels;
    }

    private int ResolveSelectedIndex(WaveDatabase database, string[] labels)
    {
        string currentName = targetEnvironmentNameProp.stringValue;
        if (!string.IsNullOrWhiteSpace(currentName) &&
            database.TryGetEnvironmentIndex(currentName, out int nameIndex))
        {
            return nameIndex;
        }

        int storedIndex = targetEnvironmentIndexProp.intValue;
        if (storedIndex >= 0 && storedIndex < labels.Length)
            return storedIndex;

        return 0;
    }

    private static WaveDatabase ResolveWaveDatabaseFromScene()
    {
        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        return gameManager != null ? gameManager.WaveDatabase : null;
    }
}
#endif