#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InteractableCharacter))]
public class InteractableCharacterEditor : Editor
{
    private SerializedProperty displayNameProp;
    private SerializedProperty interactVerbProp;
    private SerializedProperty characterKindProp;
    private SerializedProperty useSpawnPlacementProp;
    private SerializedProperty spawnPlacementProp;
    private SerializedProperty hideWhenWrongMapProp;
    private SerializedProperty interactRadiusProp;
    private SerializedProperty startsEnabledProp;
    private SerializedProperty facePlayerOnInteractProp;
    private SerializedProperty showGizmoProp;

    private void OnEnable()
    {
        displayNameProp = serializedObject.FindProperty("displayName");
        interactVerbProp = serializedObject.FindProperty("interactVerb");
        characterKindProp = serializedObject.FindProperty("characterKind");
        useSpawnPlacementProp = serializedObject.FindProperty("useSpawnPlacement");
        spawnPlacementProp = serializedObject.FindProperty("spawnPlacement");
        hideWhenWrongMapProp = serializedObject.FindProperty("hideWhenWrongMap");
        interactRadiusProp = serializedObject.FindProperty("interactRadius");
        startsEnabledProp = serializedObject.FindProperty("startsEnabled");
        facePlayerOnInteractProp = serializedObject.FindProperty("facePlayerOnInteract");
        showGizmoProp = serializedObject.FindProperty("showGizmoInSceneView");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Interactable Character", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(displayNameProp);
        EditorGUILayout.PropertyField(interactVerbProp);
        EditorGUILayout.PropertyField(characterKindProp);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Spawn Placement", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(useSpawnPlacementProp);

        if (useSpawnPlacementProp.boolValue)
        {
            DrawSpawnPlacementSection();
            EditorGUILayout.PropertyField(hideWhenWrongMapProp);
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Interaction", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(interactRadiusProp);
        EditorGUILayout.PropertyField(startsEnabledProp);
        EditorGUILayout.PropertyField(facePlayerOnInteractProp);
        EditorGUILayout.PropertyField(showGizmoProp);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSpawnPlacementSection()
    {
        SerializedProperty environmentNameProp = spawnPlacementProp.FindPropertyRelative("environmentName");
        SerializedProperty environmentIndexProp = spawnPlacementProp.FindPropertyRelative("environmentIndex");
        SerializedProperty worldPositionProp = spawnPlacementProp.FindPropertyRelative("worldPosition");
        SerializedProperty requireConditionAreaProp = spawnPlacementProp.FindPropertyRelative("requireConditionArea");
        SerializedProperty conditionAreaOffsetProp = spawnPlacementProp.FindPropertyRelative("conditionAreaOffset");
        SerializedProperty conditionAreaSizeProp = spawnPlacementProp.FindPropertyRelative("conditionAreaSize");

        WaveDatabase database = ResolveWaveDatabaseFromScene();
        if (database == null)
        {
            EditorGUILayout.HelpBox(
                "Add GameManager with a Wave Database to pick maps from a dropdown.",
                MessageType.Warning);
            EditorGUILayout.PropertyField(environmentNameProp, new GUIContent("Environment Name"));
            EditorGUILayout.PropertyField(environmentIndexProp, new GUIContent("Environment Index"));
        }
        else
        {
            database.EnsureHasEnvironments();
            var environments = database.Environments;
            if (environments.Count == 0)
            {
                EditorGUILayout.HelpBox("Wave Database has no environments.", MessageType.Warning);
            }
            else
            {
                string[] labels = BuildEnvironmentLabels(environments);
                int currentIndex = ResolveSelectedIndex(database, environmentNameProp, environmentIndexProp, labels);
                int selectedIndex = EditorGUILayout.Popup("Map", currentIndex, labels);
                if (selectedIndex != currentIndex)
                {
                    environmentIndexProp.intValue = selectedIndex;
                    environmentNameProp.stringValue = environments[selectedIndex] != null
                        ? environments[selectedIndex].EnvironmentName
                        : string.Empty;
                }
            }
        }

        EditorGUILayout.PropertyField(worldPositionProp, new GUIContent("World Position"));

        if (GUILayout.Button("Use Scene Position For Spawn"))
        {
            InteractableCharacter character = (InteractableCharacter)target;
            Vector3 position = character.transform.position;
            worldPositionProp.vector3Value = new Vector3(position.x, position.y, position.z);
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.PropertyField(requireConditionAreaProp, new GUIContent("Require Condition Area"));

        if (requireConditionAreaProp.boolValue)
        {
            EditorGUILayout.PropertyField(conditionAreaOffsetProp, new GUIContent("Condition Area Offset"));
            EditorGUILayout.PropertyField(conditionAreaSizeProp, new GUIContent("Condition Area Size"));
            EditorGUILayout.HelpBox(
                "The character stays hidden until the player enters the blue condition box.",
                MessageType.Info);
        }
    }

    private static string[] BuildEnvironmentLabels(System.Collections.Generic.IReadOnlyList<WaveEnvironmentDefinition> environments)
    {
        var labels = new string[environments.Count];
        for (int i = 0; i < environments.Count; i++)
        {
            WaveEnvironmentDefinition environment = environments[i];
            string name = environment != null ? environment.EnvironmentName : $"Environment {i + 1}";
            string hubTag = environment != null && environment.IsHub ? " [Hub]" : string.Empty;
            labels[i] = $"{name}{hubTag}";
        }

        return labels;
    }

    private static int ResolveSelectedIndex(
        WaveDatabase database,
        SerializedProperty environmentNameProp,
        SerializedProperty environmentIndexProp,
        string[] labels)
    {
        string currentName = environmentNameProp.stringValue;
        if (!string.IsNullOrWhiteSpace(currentName) &&
            database.TryGetEnvironmentIndex(currentName, out int nameIndex))
        {
            return nameIndex;
        }

        int storedIndex = environmentIndexProp.intValue;
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