#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(WaveDatabase))]
public class WaveDatabaseEditor : Editor
{
    private const float Gap = 4f;

    private WaveDatabase database;
    private SerializedProperty enemyDatabaseProp;
    private SerializedProperty bossDatabaseProp;
    private SerializedProperty environmentsProp;
    private SerializedProperty activeEnvironmentIndexProp;
    private SerializedProperty scheduleProp;
    private SerializedProperty settingsProp;
    private SerializedProperty spawnRangeBandsProp;
    private SerializedProperty difficultyScaleProp;

    private int selectedEnvironmentSubTab;
    private int selectedEnvironmentIndex;
    private int selectedSpawnEntryIndex;
    private ReorderableList waveRangesList;

    private void OnEnable()
    {
        database = (WaveDatabase)target;
        enemyDatabaseProp = serializedObject.FindProperty("enemyDatabase");
        bossDatabaseProp = serializedObject.FindProperty("bossDatabase");
        environmentsProp = serializedObject.FindProperty("environments");
        activeEnvironmentIndexProp = serializedObject.FindProperty("activeEnvironmentIndex");
        scheduleProp = serializedObject.FindProperty("schedule");
        settingsProp = serializedObject.FindProperty("settings");
        spawnRangeBandsProp = serializedObject.FindProperty("spawnRangeBands");
        difficultyScaleProp = serializedObject.FindProperty("difficultyScale");

        selectedEnvironmentIndex = Mathf.Clamp(
            activeEnvironmentIndexProp.intValue,
            0,
            Mathf.Max(0, environmentsProp.arraySize - 1));

        ClampSpawnEntryIndex();
        RebuildWaveRangesList();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Wave Database", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Select an environment, assign a tileset prefab, then configure spawn entries and wave rules.",
            MessageType.Info);

        DrawDatabaseReferences();
        EditorGUILayout.Space(6f);
        DrawEnvironmentsSection();

        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(database);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScheduleSection()
    {
        if (scheduleProp == null)
            return;

        SerializedProperty killCountWeightProp = scheduleProp.FindPropertyRelative("killCountWeight");
        SerializedProperty survivalWeightProp = scheduleProp.FindPropertyRelative("survivalWeight");

        EditorGUILayout.LabelField("Wave Type Weights", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(killCountWeightProp, new GUIContent("Kill Count Weight"));
        EditorGUILayout.PropertyField(survivalWeightProp, new GUIContent("Survival Weight"));

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Difficulty Ranges", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Each range covers a block of waves. Multipliers scale enemy health, damage, and spawn pressure.",
            MessageType.None);

        if (waveRangesList != null)
            waveRangesList.DoLayoutList();
    }

    private void DrawSettingsSection()
    {
        if (settingsProp != null)
        {
            EditorGUILayout.LabelField("Wave Objectives", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(settingsProp.FindPropertyRelative("timedDuration"), new GUIContent("Survival Duration"));
            EditorGUILayout.PropertyField(settingsProp.FindPropertyRelative("killTargetMin"), new GUIContent("Kill Target Min"));
            EditorGUILayout.PropertyField(settingsProp.FindPropertyRelative("killTargetMax"), new GUIContent("Kill Target Max"));
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Global Scaling", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(difficultyScaleProp, new GUIContent("Difficulty Scale"));

        if (spawnRangeBandsProp == null)
            return;

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Spawn Distance Bands", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Distance rings used when a spawn entry selects Close, Nearby, Medium, Far, or Super Far.",
            MessageType.None);

        DrawBandFields("Close", "closeMin", "closeMax");
        DrawBandFields("Nearby", "nearbyMin", "nearbyMax");
        DrawBandFields("Medium", "mediumMin", "mediumMax");
        DrawBandFields("Far", "farMin", "farMax");
        DrawBandFields("Super Far", "superFarMin", "superFarMax");
    }

    private void DrawBandFields(string label, string minProperty, string maxProperty)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(label);
        EditorGUILayout.PropertyField(spawnRangeBandsProp.FindPropertyRelative(minProperty), GUIContent.none);
        EditorGUILayout.PropertyField(spawnRangeBandsProp.FindPropertyRelative(maxProperty), GUIContent.none);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawEnvironmentsSection()
    {
        database.EnsureHasEnvironments();

        if (environmentsProp.arraySize == 0)
        {
            if (GUILayout.Button("Add Environment", GUILayout.Height(26f)))
                AddEnvironment();
            return;
        }

        DrawEnvironmentToolbar();

        SerializedProperty environment = GetSelectedEnvironment();
        if (environment == null)
            return;

        DrawTilesetSection(environment);

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Display Name (Optional)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Leave blank to use the tileset prefab name (e.g. New Tile Palette).",
            MessageType.None);
        EditorGUILayout.PropertyField(
            environment.FindPropertyRelative("environmentName"),
            GUIContent.none);

        EditorGUILayout.Space(4f);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(activeEnvironmentIndexProp, new GUIContent("Active At Runtime"));
        if (GUILayout.Button("Use This Environment", GUILayout.Width(140f)))
            activeEnvironmentIndexProp.intValue = selectedEnvironmentIndex;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8f);
        selectedEnvironmentSubTab = GUILayout.Toolbar(selectedEnvironmentSubTab, new[]
        {
            "Enemies", "Schedule", "Settings"
        });

        EditorGUILayout.Space(6f);
        switch (selectedEnvironmentSubTab)
        {
            case 0:
                DrawEnvironmentEnemiesSection(environment);
                break;
            case 1:
                DrawScheduleSection();
                break;
            default:
                DrawSettingsSection();
                break;
        }
    }

    private void DrawTilesetSection(SerializedProperty environment)
    {
        SerializedProperty prefabProp = environment.FindPropertyRelative("environmentPrefab");
        if (prefabProp == null)
            return;

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Tileset Prefab", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Drag a tilemap prefab from the Project window. It should contain a Grid and Tilemap (EnvironmentMap is optional).",
            MessageType.None);

        EditorGUI.BeginChangeCheck();
        var currentPrefab = prefabProp.objectReferenceValue as GameObject;
        GameObject newPrefab = EditorGUILayout.ObjectField(
            new GUIContent("Prefab"),
            currentPrefab,
            typeof(GameObject),
            false) as GameObject;

        if (EditorGUI.EndChangeCheck())
            prefabProp.objectReferenceValue = newPrefab;

        DrawTilesetPrefabStatus(newPrefab ?? currentPrefab);

        EditorGUILayout.Space(4f);
        EditorGUILayout.PropertyField(
            environment.FindPropertyRelative("mapSpawnPosition"),
            new GUIContent("Map Spawn Position"));
        EditorGUILayout.PropertyField(
            environment.FindPropertyRelative("useCustomPlayerSpawn"),
            new GUIContent("Use Custom Player Spawn"));
        EditorGUILayout.PropertyField(
            environment.FindPropertyRelative("playerSpawnPosition"),
            new GUIContent("Player Spawn Position"));
    }

    private static void DrawTilesetPrefabStatus(GameObject prefab)
    {
        if (prefab == null)
        {
            EditorGUILayout.HelpBox("No tileset prefab assigned for this environment.", MessageType.Warning);
            return;
        }

        PrefabAssetType assetType = PrefabUtility.GetPrefabAssetType(prefab);
        if (assetType == PrefabAssetType.NotAPrefab)
        {
            EditorGUILayout.HelpBox(
                "Assign a project prefab asset, not a scene object.",
                MessageType.Error);
            return;
        }

        GameObject prefabRoot = PrefabUtility.IsPartOfPrefabAsset(prefab)
            ? prefab
            : PrefabUtility.GetCorrespondingObjectFromSource(prefab);

        if (prefabRoot == null)
            prefabRoot = prefab;

        Tilemap tilemap = prefabRoot.GetComponentInChildren<Tilemap>(true);
        EnvironmentMap environmentMap = prefabRoot.GetComponent<EnvironmentMap>();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Resolved Prefab", prefabRoot.name, EditorStyles.miniLabel);
        if (GUILayout.Button("Ping", GUILayout.Width(48f)))
            EditorGUIUtility.PingObject(prefabRoot);
        EditorGUILayout.EndHorizontal();

        if (tilemap == null)
        {
            EditorGUILayout.HelpBox(
                $"Prefab '{prefabRoot.name}' has no Tilemap. Add a Grid + Tilemap before using it as an environment.",
                MessageType.Warning);
            return;
        }

        string mapComponent = environmentMap != null ? "EnvironmentMap present" : "No EnvironmentMap (will be added at spawn)";
        EditorGUILayout.HelpBox(
            $"Tilemap found on '{tilemap.gameObject.name}'. {mapComponent}.",
            MessageType.Info);
    }

    private void DrawDatabaseReferences()
    {
        EditorGUILayout.LabelField("Spawn Databases", EditorStyles.boldLabel);

        if (enemyDatabaseProp != null && enemyDatabaseProp.objectReferenceValue == null)
            LinkEnemyDatabase();

        if (bossDatabaseProp != null && bossDatabaseProp.objectReferenceValue == null)
            LinkBossDatabase();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(enemyDatabaseProp, new GUIContent("Enemy Database"));
        if (GUILayout.Button("Auto-Link", GUILayout.Width(80f)))
            LinkEnemyDatabase();
        EditorGUILayout.EndHorizontal();

        EnemyDatabase enemyDatabase = GetEnemyDatabase();
        if (enemyDatabase == null)
        {
            EditorGUILayout.HelpBox(
                "Enemy Database not found. Click Auto-Link or assign Assets/GameData/Enemies/EnemyDatabase.asset.",
                MessageType.Warning);
        }
        else
        {
            EnemySelectionEditorContext.Database = enemyDatabase;
            EditorGUILayout.LabelField("Loaded Enemies", GetEnemyCountSummary(enemyDatabase), EditorStyles.miniLabel);
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(bossDatabaseProp, new GUIContent("Boss Database"));
        if (GUILayout.Button("Auto-Link", GUILayout.Width(80f)))
            LinkBossDatabase();
        EditorGUILayout.EndHorizontal();

        BossDatabase bossDatabase = GetBossDatabase();
        if (bossDatabase == null)
        {
            EditorGUILayout.HelpBox(
                "Boss Database not found. Click Auto-Link or assign Assets/GameData/BossDatabase.asset.",
                MessageType.Warning);
        }
        else
        {
            EditorGUILayout.LabelField("Loaded Bosses", $"{bossDatabase.Bosses.Count} boss(es)", EditorStyles.miniLabel);
        }
    }

    private void DrawEnvironmentEnemiesSection(SerializedProperty environment)
    {
        EnemyDatabase enemyDatabase = GetEnemyDatabase();
        BossDatabase bossDatabase = GetBossDatabase();
        if (enemyDatabase == null && bossDatabase == null)
        {
            EditorGUILayout.HelpBox(
                "Assign an Enemy Database and/or Boss Database above before configuring spawn entries.",
                MessageType.Warning);
            return;
        }

        if (enemyDatabase != null)
            EnemySelectionEditorContext.Database = enemyDatabase;

        SerializedProperty enemiesProp = environment.FindPropertyRelative("enemies");
        if (enemiesProp == null)
            return;

        EditorGUILayout.LabelField("Spawn Entries", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Each entry can spawn a regular enemy or a boss. Use Specific Waves and longer intervals for one-off boss fights.",
            MessageType.None);
        DrawSpawnEntryToolbar(enemiesProp, enemyDatabase, bossDatabase);

        if (enemiesProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("Add a spawn entry, then choose enemy or boss and tune spawn rules.", MessageType.Info);
            return;
        }

        ClampSpawnEntryIndex(enemiesProp);
        SerializedProperty entry = enemiesProp.GetArrayElementAtIndex(selectedSpawnEntryIndex);

        EditorGUILayout.Space(8f);
        SerializedProperty spawnKindProp = entry.FindPropertyRelative("spawnKind");
        EditorGUILayout.PropertyField(spawnKindProp, new GUIContent("Spawn Type"));

        var spawnKind = spawnKindProp != null
            ? (WaveSpawnKind)spawnKindProp.enumValueIndex
            : WaveSpawnKind.Enemy;

        if (spawnKind == WaveSpawnKind.Boss)
        {
            EditorGUILayout.LabelField("Selected Boss", EditorStyles.boldLabel);
            if (bossDatabase == null)
            {
                EditorGUILayout.HelpBox("Assign a Boss Database above to pick a boss.", MessageType.Warning);
            }
            else
            {
                SerializedProperty bossProp = entry.FindPropertyRelative("boss");
                if (BossSelectionEditorUtility.DrawSelectionPopupLayout(
                        bossProp,
                        new GUIContent("Boss From Database"),
                        bossDatabase))
                {
                    EditorUtility.SetDirty(database);
                }

                BossSelection selection = BossSelectionEditorUtility.ReadSelection(bossProp);
                string bossName = BossSelectionEditorUtility.GetSelectionLabel(bossDatabase, selection);
                if (!string.IsNullOrEmpty(bossName))
                    EditorGUILayout.LabelField("Editing", bossName, EditorStyles.miniBoldLabel);
                else
                    EditorGUILayout.HelpBox("Choose a boss before tuning spawn settings.", MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.LabelField("Selected Enemy", EditorStyles.boldLabel);
            if (enemyDatabase == null)
            {
                EditorGUILayout.HelpBox("Assign an Enemy Database above to pick an enemy.", MessageType.Warning);
            }
            else
            {
                SerializedProperty enemyProp = entry.FindPropertyRelative("enemy");
                if (EnemySelectionEditorUtility.DrawSelectionPopupLayout(
                        enemyProp,
                        new GUIContent("Enemy From Database"),
                        enemyDatabase))
                {
                    EditorUtility.SetDirty(database);
                }

                EnemySelection selection = EnemySelectionEditorUtility.ReadSelection(enemyProp);
                string enemyName = EnemySelectionEditorUtility.GetSelectionLabel(enemyDatabase, selection);
                if (!string.IsNullOrEmpty(enemyName))
                    EditorGUILayout.LabelField("Editing", enemyName, EditorStyles.miniBoldLabel);
                else
                    EditorGUILayout.HelpBox("Choose an enemy before tuning spawn settings.", MessageType.Warning);
            }
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Spawn Rules", EditorStyles.boldLabel);
        DrawSpawnEntryFields(entry);
    }

    private void DrawSpawnEntryToolbar(
        SerializedProperty enemiesProp,
        EnemyDatabase enemyDatabase,
        BossDatabase bossDatabase)
    {
        EditorGUILayout.BeginHorizontal();

        if (enemiesProp.arraySize > 0)
        {
            string[] labels = BuildSpawnEntryLabels(enemiesProp, enemyDatabase, bossDatabase);
            int newIndex = EditorGUILayout.Popup("Entry", selectedSpawnEntryIndex, labels);
            if (newIndex != selectedSpawnEntryIndex)
                selectedSpawnEntryIndex = newIndex;
        }
        else
        {
            EditorGUILayout.Popup("Entry", 0, new[] { "(No Entries)" });
        }

        if (GUILayout.Button("+", GUILayout.Width(28f)))
            AddSpawnEntry(enemiesProp, enemyDatabase, bossDatabase);

        GUI.enabled = enemiesProp.arraySize > 0;
        if (GUILayout.Button("-", GUILayout.Width(28f)))
            RemoveSpawnEntry(enemiesProp);
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSpawnEntryFields(SerializedProperty entry)
    {
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("spawnRange"), new GUIContent("Spawn Range"));

        if ((EnemySpawnRange)entry.FindPropertyRelative("spawnRange").enumValueIndex == EnemySpawnRange.SetLocation)
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("setLocationOffset"), new GUIContent("Set Location Offset"));

        SerializedProperty allWavesProp = entry.FindPropertyRelative("spawnOnAllWaves");
        EditorGUILayout.PropertyField(allWavesProp, new GUIContent("Spawn On All Waves"));
        if (!allWavesProp.boolValue)
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("specificWaves"), new GUIContent("Specific Waves"), true);

        EditorGUILayout.PropertyField(entry.FindPropertyRelative("minSpawnInterval"), new GUIContent("Min Spawn Interval"));
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("maxSpawnInterval"), new GUIContent("Max Spawn Interval"));
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("spawnWeight"), new GUIContent("Spawn Weight"));
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("healthMultiplier"), new GUIContent("Health Multiplier"));
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("damageMultiplier"), new GUIContent("Damage Multiplier"));
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("sizeMultiplier"), new GUIContent("Size Multiplier"));
    }

    private void AddSpawnEntry(
        SerializedProperty enemiesProp,
        EnemyDatabase enemyDatabase,
        BossDatabase bossDatabase)
    {
        int index = enemiesProp.arraySize;
        enemiesProp.arraySize++;
        SerializedProperty entry = enemiesProp.GetArrayElementAtIndex(index);

        entry.FindPropertyRelative("spawnKind").enumValueIndex = (int)WaveSpawnKind.Enemy;
        EnemySelectionEditorUtility.WriteSelection(
            entry.FindPropertyRelative("enemy"),
            EnemySelectionEditorUtility.GetDefaultSelection(enemyDatabase));
        BossSelectionEditorUtility.WriteSelection(
            entry.FindPropertyRelative("boss"),
            BossSelectionEditorUtility.GetDefaultSelection(bossDatabase));

        entry.FindPropertyRelative("spawnOnAllWaves").boolValue = true;
        entry.FindPropertyRelative("minSpawnInterval").floatValue = 3f;
        entry.FindPropertyRelative("maxSpawnInterval").floatValue = 6f;
        entry.FindPropertyRelative("spawnWeight").intValue = 1;
        entry.FindPropertyRelative("healthMultiplier").floatValue = 1f;
        entry.FindPropertyRelative("damageMultiplier").floatValue = 1f;
        entry.FindPropertyRelative("sizeMultiplier").floatValue = 1f;

        selectedSpawnEntryIndex = index;
    }

    private void RemoveSpawnEntry(SerializedProperty enemiesProp)
    {
        if (enemiesProp.arraySize == 0)
            return;

        enemiesProp.DeleteArrayElementAtIndex(selectedSpawnEntryIndex);
        selectedSpawnEntryIndex = Mathf.Clamp(selectedSpawnEntryIndex, 0, enemiesProp.arraySize - 1);
    }

    private string[] BuildSpawnEntryLabels(
        SerializedProperty enemiesProp,
        EnemyDatabase enemyDatabase,
        BossDatabase bossDatabase)
    {
        var labels = new string[enemiesProp.arraySize];
        for (int i = 0; i < enemiesProp.arraySize; i++)
        {
            SerializedProperty entry = enemiesProp.GetArrayElementAtIndex(i);
            SerializedProperty spawnKindProp = entry.FindPropertyRelative("spawnKind");
            var spawnKind = spawnKindProp != null
                ? (WaveSpawnKind)spawnKindProp.enumValueIndex
                : WaveSpawnKind.Enemy;

            if (spawnKind == WaveSpawnKind.Boss)
            {
                BossSelection selection = BossSelectionEditorUtility.ReadSelection(entry.FindPropertyRelative("boss"));
                string bossName = BossSelectionEditorUtility.GetSelectionLabel(bossDatabase, selection);
                labels[i] = string.IsNullOrEmpty(bossName)
                    ? $"Entry {i + 1}: (No Boss)"
                    : $"Entry {i + 1}: Boss {bossName}";
            }
            else
            {
                EnemySelection selection = EnemySelectionEditorUtility.ReadSelection(entry.FindPropertyRelative("enemy"));
                string enemyName = EnemySelectionEditorUtility.GetSelectionLabel(enemyDatabase, selection);
                labels[i] = string.IsNullOrEmpty(enemyName)
                    ? $"Entry {i + 1}: (No Enemy)"
                    : $"Entry {i + 1}: {enemyName}";
            }
        }

        return labels;
    }

    private void LinkEnemyDatabase()
    {
        EnemyDatabase resolved = EnemySelectionEditorUtility.ResolveDatabase();
        if (resolved == null || enemyDatabaseProp == null)
            return;

        enemyDatabaseProp.objectReferenceValue = resolved;
        EnemySelectionEditorContext.Database = resolved;
        EditorUtility.SetDirty(database);
    }

    private void LinkBossDatabase()
    {
        BossDatabase resolved = BossSelectionEditorUtility.ResolveDatabase();
        if (resolved == null || bossDatabaseProp == null)
            return;

        bossDatabaseProp.objectReferenceValue = resolved;
        EditorUtility.SetDirty(database);
    }

    private EnemyDatabase GetEnemyDatabase()
    {
        EnemyDatabase assigned = null;
        if (enemyDatabaseProp != null)
            assigned = enemyDatabaseProp.objectReferenceValue as EnemyDatabase;

        if (assigned != null)
        {
            EnemySelectionEditorContext.Database = assigned;
            return assigned;
        }

        EnemyDatabase resolved = EnemySelectionEditorUtility.ResolveDatabase();
        if (resolved != null)
            EnemySelectionEditorContext.Database = resolved;

        return resolved;
    }

    private BossDatabase GetBossDatabase()
    {
        if (bossDatabaseProp != null)
        {
            BossDatabase assigned = bossDatabaseProp.objectReferenceValue as BossDatabase;
            if (assigned != null)
                return assigned;
        }

        return BossSelectionEditorUtility.ResolveDatabase();
    }

    private static string GetEnemyCountSummary(EnemyDatabase database) =>
        $"Melee {database.MeleeEnemies.Count}, Projectile {database.ProjectileEnemies.Count}, " +
        $"Summon {database.SummonEnemies.Count}, Miniboss {database.MinibossEnemies.Count}, Boss {database.BossEnemies.Count}";

    private void DrawEnvironmentToolbar()
    {
        EditorGUILayout.BeginHorizontal();

        var labels = new string[environmentsProp.arraySize];
        for (int i = 0; i < environmentsProp.arraySize; i++)
        {
            SerializedProperty env = environmentsProp.GetArrayElementAtIndex(i);
            string name = env.FindPropertyRelative("environmentName").stringValue;
            int count = env.FindPropertyRelative("enemies").arraySize;
            var prefab = env.FindPropertyRelative("environmentPrefab").objectReferenceValue as GameObject;
            string prefabLabel = prefab != null ? prefab.name : "No Tileset";
            string displayName = string.IsNullOrWhiteSpace(name) ? prefabLabel : name.Trim();
            labels[i] = $"{displayName} ({count})";
        }

        int newIndex = EditorGUILayout.Popup("Environment", selectedEnvironmentIndex, labels);
        if (newIndex != selectedEnvironmentIndex)
        {
            selectedEnvironmentIndex = newIndex;
            selectedSpawnEntryIndex = 0;
        }

        if (GUILayout.Button("+", GUILayout.Width(28f)))
            AddEnvironment();

        GUI.enabled = environmentsProp.arraySize > 1;
        if (GUILayout.Button("-", GUILayout.Width(28f)))
            RemoveEnvironment(selectedEnvironmentIndex);
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
    }

    private void AddEnvironment(string environmentName = null)
    {
        environmentsProp.arraySize++;
        SerializedProperty element = environmentsProp.GetArrayElementAtIndex(environmentsProp.arraySize - 1);
        element.FindPropertyRelative("environmentName").stringValue = environmentName ?? string.Empty;
        element.FindPropertyRelative("enemies").arraySize = 0;

        selectedEnvironmentIndex = environmentsProp.arraySize - 1;
        selectedSpawnEntryIndex = 0;
    }

    private void RemoveEnvironment(int index)
    {
        if (environmentsProp.arraySize <= 1)
            return;

        environmentsProp.DeleteArrayElementAtIndex(index);
        selectedEnvironmentIndex = Mathf.Clamp(selectedEnvironmentIndex, 0, environmentsProp.arraySize - 1);
        activeEnvironmentIndexProp.intValue = Mathf.Clamp(
            activeEnvironmentIndexProp.intValue,
            0,
            environmentsProp.arraySize - 1);
        selectedSpawnEntryIndex = 0;
    }

    private SerializedProperty GetSelectedEnvironment()
    {
        if (environmentsProp == null || environmentsProp.arraySize == 0)
            return null;

        selectedEnvironmentIndex = Mathf.Clamp(selectedEnvironmentIndex, 0, environmentsProp.arraySize - 1);
        return environmentsProp.GetArrayElementAtIndex(selectedEnvironmentIndex);
    }

    private void ClampSpawnEntryIndex(SerializedProperty enemiesProp = null)
    {
        enemiesProp ??= GetSelectedEnvironment()?.FindPropertyRelative("enemies");
        if (enemiesProp == null || enemiesProp.arraySize == 0)
        {
            selectedSpawnEntryIndex = 0;
            return;
        }

        selectedSpawnEntryIndex = Mathf.Clamp(selectedSpawnEntryIndex, 0, enemiesProp.arraySize - 1);
    }

    private void RebuildWaveRangesList()
    {
        SerializedProperty rangesProp = scheduleProp?.FindPropertyRelative("waveRanges");
        if (rangesProp == null)
        {
            waveRangesList = null;
            return;
        }

        waveRangesList = new ReorderableList(serializedObject, rangesProp, true, true, true, true);
        waveRangesList.drawHeaderCallback = rect =>
            EditorGUI.LabelField(rect, "Wave Ranges");

        waveRangesList.drawElementCallback = (rect, index, isActive, isFocused) =>
            DrawWaveRangeElement(rect, rangesProp.GetArrayElementAtIndex(index));

        waveRangesList.elementHeightCallback = index =>
            GetWaveRangeElementHeight(rangesProp.GetArrayElementAtIndex(index));

        waveRangesList.onAddCallback = list =>
        {
            list.serializedProperty.arraySize++;
            SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(
                list.serializedProperty.arraySize - 1);
            element.FindPropertyRelative("waveCount").intValue = 6;
            element.FindPropertyRelative("healthMultiplier").floatValue = 1f;
            element.FindPropertyRelative("damageMultiplier").floatValue = 1f;
            element.FindPropertyRelative("spawnCountMultiplier").floatValue = 1f;
            element.FindPropertyRelative("spawnRateMultiplier").floatValue = 1f;
        };
    }

    private static void DrawWaveRangeElement(Rect rect, SerializedProperty element)
    {
        rect.x += 6f;
        rect.width -= 12f;

        float y = rect.y + 2f;
        float lineH = EditorGUIUtility.singleLineHeight;
        float oldLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 150f;

        y = DrawProperty(rect, y, lineH, element, "waveCount", "Wave Count");
        y = DrawProperty(rect, y, lineH, element, "healthMultiplier", "Health Multiplier");
        y = DrawProperty(rect, y, lineH, element, "damageMultiplier", "Damage Multiplier");
        y = DrawProperty(rect, y, lineH, element, "spawnCountMultiplier", "Spawn Count Multiplier");
        y = DrawProperty(rect, y, lineH, element, "spawnRateMultiplier", "Spawn Rate Multiplier");

        EditorGUIUtility.labelWidth = oldLabelWidth;
    }

    private static float GetWaveRangeElementHeight(SerializedProperty element)
    {
        float lineH = EditorGUIUtility.singleLineHeight;
        float height = 0f;

        height += CountHeight(element, "waveCount", "Wave Count");
        height += CountHeight(element, "healthMultiplier", "Health Multiplier");
        height += CountHeight(element, "damageMultiplier", "Damage Multiplier");
        height += CountHeight(element, "spawnCountMultiplier", "Spawn Count Multiplier");
        height += CountHeight(element, "spawnRateMultiplier", "Spawn Rate Multiplier");

        return height + lineH + 10f;
    }

    private static float DrawProperty(
        Rect rect,
        float y,
        float lineH,
        SerializedProperty parent,
        string propertyName,
        string label)
    {
        SerializedProperty property = parent.FindPropertyRelative(propertyName);
        if (property == null)
            return y;

        float height = EditorGUI.GetPropertyHeight(property, new GUIContent(label), true);
        EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, height), property, new GUIContent(label), true);
        return y + height + Gap;
    }

    private static float CountHeight(SerializedProperty parent, string propertyName, string label)
    {
        SerializedProperty property = parent.FindPropertyRelative(propertyName);
        if (property == null)
            return 0f;

        return EditorGUI.GetPropertyHeight(property, new GUIContent(label), true) + Gap;
    }
}

public static class EnemySelectionEditorContext
{
    public static EnemyDatabase Database { get; set; }
}

public static class EnemySelectionEditorUtility
{
    public const string DefaultDatabasePath = "Assets/GameData/Enemies/EnemyDatabase.asset";
    public const string DefaultDatabaseGuid = "c7d8e9f0a1b234567890abcdef123456";

    public static EnemyDatabase ResolveDatabase()
    {
        if (EnemySelectionEditorContext.Database != null)
            return EnemySelectionEditorContext.Database;

        EnemyDatabase database = LoadDatabaseAtPath(DefaultDatabasePath);
        if (database != null)
            return database;

        string guidPath = AssetDatabase.GUIDToAssetPath(DefaultDatabaseGuid);
        if (!string.IsNullOrEmpty(guidPath))
        {
            database = LoadDatabaseAtPath(guidPath);
            if (database != null)
                return database;
        }

        string[] guids = AssetDatabase.FindAssets("t:EnemyDatabase");
        for (int i = 0; i < guids.Length; i++)
        {
            database = LoadDatabaseAtPath(AssetDatabase.GUIDToAssetPath(guids[i]));
            if (database != null)
                return database;
        }

        return null;
    }

    public static EnemyDatabase LoadDatabaseAtPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        return AssetDatabase.LoadAssetAtPath<EnemyDatabase>(path);
    }

    public static void BuildOptions(
        EnemyDatabase database,
        List<string> options,
        List<EnemySelection> selections)
    {
        options.Clear();
        selections.Clear();
        options.Add("(None)");
        selections.Add(EnemySelection.None);

        AddEnemyOptions(database.MeleeEnemies, EnemyCategory.Melee, "Melee", options, selections);
        AddEnemyOptions(database.ProjectileEnemies, EnemyCategory.Projectile, "Projectile", options, selections);
        AddEnemyOptions(database.SummonEnemies, EnemyCategory.Summon, "Summon", options, selections);
        AddEnemyOptions(database.MinibossEnemies, EnemyCategory.Miniboss, "Miniboss", options, selections);
        AddEnemyOptions(database.BossEnemies, EnemyCategory.Boss, "Boss", options, selections);
    }

    public static EnemySelection ReadSelection(SerializedProperty property)
    {
        if (property == null)
            return EnemySelection.None;

        SerializedProperty categoryProp = property.FindPropertyRelative("category");
        SerializedProperty indexProp = property.FindPropertyRelative("index");
        if (categoryProp == null || indexProp == null)
            return EnemySelection.None;

        return new EnemySelection
        {
            category = (EnemyCategory)categoryProp.enumValueIndex,
            index = indexProp.intValue
        };
    }

    public static void WriteSelection(SerializedProperty property, EnemySelection selection)
    {
        if (property == null)
            return;

        SerializedProperty categoryProp = property.FindPropertyRelative("category");
        SerializedProperty indexProp = property.FindPropertyRelative("index");
        if (categoryProp == null || indexProp == null)
            return;

        categoryProp.enumValueIndex = (int)selection.category;
        indexProp.intValue = selection.index;
    }

    public static int FindSelectedIndex(IReadOnlyList<EnemySelection> selections, EnemySelection current)
    {
        for (int i = 0; i < selections.Count; i++)
        {
            if (selections[i].category == current.category && selections[i].index == current.index)
                return i;
        }

        return 0;
    }

    public static EnemySelection GetDefaultSelection(EnemyDatabase database)
    {
        if (database == null)
            return EnemySelection.None;

        if (database.MeleeEnemies.Count > 0)
            return EnemySelection.Melee(0);
        if (database.ProjectileEnemies.Count > 0)
            return EnemySelection.Projectile(0);
        if (database.SummonEnemies.Count > 0)
            return EnemySelection.Summon(0);
        if (database.MinibossEnemies.Count > 0)
            return EnemySelection.Miniboss(0);
        if (database.BossEnemies.Count > 0)
            return EnemySelection.Boss(0);

        return EnemySelection.None;
    }

    public static bool DrawSelectionPopup(
        Rect position,
        SerializedProperty property,
        GUIContent label,
        EnemyDatabase database)
    {
        database ??= ResolveDatabase();

        if (database == null || property == null)
        {
            EditorGUI.LabelField(position, label, new GUIContent("Enemy Database missing"));
            return false;
        }

        var options = new List<string>();
        var selections = new List<EnemySelection>();
        BuildOptions(database, options, selections);

        if (options.Count <= 1)
        {
            EditorGUI.LabelField(position, label, new GUIContent("No enemies in database"));
            return false;
        }

        EnemySelection current = ReadSelection(property);
        int selectedIndex = FindSelectedIndex(selections, current);

        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.BeginChangeCheck();
        int newIndex = EditorGUI.Popup(position, label.text, selectedIndex, options.ToArray());
        bool changed = EditorGUI.EndChangeCheck();
        if (changed && newIndex != selectedIndex)
            WriteSelection(property, selections[newIndex]);
        EditorGUI.EndProperty();

        return changed;
    }

    public static bool DrawSelectionPopupLayout(SerializedProperty property, GUIContent label, EnemyDatabase database)
    {
        database ??= ResolveDatabase();

        if (database == null || property == null)
        {
            EditorGUILayout.LabelField(label, new GUIContent("Enemy Database missing"));
            return false;
        }

        var options = new List<string>();
        var selections = new List<EnemySelection>();
        BuildOptions(database, options, selections);

        if (options.Count <= 1)
        {
            EditorGUILayout.LabelField(label, new GUIContent("No enemies in database"));
            return false;
        }

        EnemySelection current = ReadSelection(property);
        int selectedIndex = FindSelectedIndex(selections, current);

        EditorGUI.BeginChangeCheck();
        int newIndex = EditorGUILayout.Popup(label, selectedIndex, options.ToArray());
        bool changed = EditorGUI.EndChangeCheck();
        if (changed && newIndex != selectedIndex)
            WriteSelection(property, selections[newIndex]);

        return changed;
    }

    public static string GetSelectionLabel(EnemyDatabase database, EnemySelection selection)
    {
        if (database == null || !selection.IsValid)
            return string.Empty;

        return database.GetEnemyName(selection);
    }

    private static void AddEnemyOptions<T>(
        IReadOnlyList<T> enemies,
        EnemyCategory category,
        string label,
        List<string> options,
        List<EnemySelection> selections)
    {
        if (enemies == null)
            return;

        for (int i = 0; i < enemies.Count; i++)
        {
            string enemyName = enemies[i] switch
            {
                MeleeEnemyDefinition melee => melee.EnemyName,
                ProjectileEnemyDefinition projectile => projectile.EnemyName,
                SummonEnemyDefinition summon => summon.EnemyName,
                MinibossEnemyDefinition miniboss => miniboss.EnemyName,
                BossEnemyDefinition boss => boss.EnemyName,
                _ => $"Enemy {i}"
            };

            options.Add($"{label}: {enemyName}");
            selections.Add(category switch
            {
                EnemyCategory.Melee => EnemySelection.Melee(i),
                EnemyCategory.Projectile => EnemySelection.Projectile(i),
                EnemyCategory.Summon => EnemySelection.Summon(i),
                EnemyCategory.Miniboss => EnemySelection.Miniboss(i),
                EnemyCategory.Boss => EnemySelection.Boss(i),
                _ => EnemySelection.None
            });
        }
    }
}

internal static class BossSelectionEditorUtility
{
    private const string DefaultBossDatabasePath = "Assets/GameData/BossDatabase.asset";

    public static BossDatabase ResolveDatabase()
    {
        string[] guids = AssetDatabase.FindAssets("t:BossDatabase");
        if (guids.Length > 0)
            return AssetDatabase.LoadAssetAtPath<BossDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));

        return LoadDatabaseAtPath(DefaultBossDatabasePath);
    }

    public static BossDatabase LoadDatabaseAtPath(string path) =>
        string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<BossDatabase>(path);

    public static BossSelection ReadSelection(SerializedProperty property)
    {
        if (property == null)
            return BossSelection.None;

        SerializedProperty indexProp = property.FindPropertyRelative("index");
        return indexProp == null
            ? BossSelection.None
            : BossSelection.At(indexProp.intValue);
    }

    public static void WriteSelection(SerializedProperty property, BossSelection selection)
    {
        if (property == null)
            return;

        SerializedProperty indexProp = property.FindPropertyRelative("index");
        if (indexProp != null)
            indexProp.intValue = selection.index;
    }

    public static BossSelection GetDefaultSelection(BossDatabase database)
    {
        if (database == null || database.Bosses.Count == 0)
            return BossSelection.None;

        return BossSelection.At(0);
    }

    public static string GetSelectionLabel(BossDatabase database, BossSelection selection)
    {
        if (database == null || !selection.IsValid)
            return string.Empty;

        return database.GetBossName(selection);
    }

    public static bool DrawSelectionPopupLayout(SerializedProperty property, GUIContent label, BossDatabase database)
    {
        database ??= ResolveDatabase();

        if (database == null || property == null)
        {
            EditorGUILayout.LabelField(label, new GUIContent("Boss Database missing"));
            return false;
        }

        var options = new List<string> { "(None)" };
        var selections = new List<BossSelection> { BossSelection.None };

        for (int i = 0; i < database.Bosses.Count; i++)
        {
            BossDefinition boss = database.Bosses[i];
            string bossName = boss != null ? boss.BossName : $"Boss {i}";
            options.Add(bossName);
            selections.Add(BossSelection.At(i));
        }

        if (options.Count <= 1)
        {
            EditorGUILayout.LabelField(label, new GUIContent("No bosses in database"));
            return false;
        }

        BossSelection current = ReadSelection(property);
        int selectedIndex = 0;
        for (int i = 0; i < selections.Count; i++)
        {
            if (selections[i].index == current.index)
            {
                selectedIndex = i;
                break;
            }
        }

        EditorGUI.BeginChangeCheck();
        int newIndex = EditorGUILayout.Popup(label, selectedIndex, options.ToArray());
        bool changed = EditorGUI.EndChangeCheck();
        if (changed && newIndex != selectedIndex)
            WriteSelection(property, selections[newIndex]);

        return changed;
    }
}

[CustomPropertyDrawer(typeof(EnemySelection))]
public class EnemySelectionDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) =>
        EnemySelectionEditorUtility.DrawSelectionPopup(
            position,
            property,
            label,
            EnemySelectionEditorUtility.ResolveDatabase());

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
        EditorGUIUtility.singleLineHeight;
}
#endif