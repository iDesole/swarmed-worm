#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(EnemyDatabase))]
public class EnemyDatabaseEditor : Editor
{
    private const float Gap = 4f;

    private EnemyDatabase database;
    private SerializedProperty meleeEnemiesProp;
    private SerializedProperty projectileEnemiesProp;
    private SerializedProperty summonEnemiesProp;
    private SerializedProperty minibossEnemiesProp;
    private SerializedProperty bossEnemiesProp;
    private SerializedProperty defaultProjectileProp;

    private ReorderableList meleeList;
    private ReorderableList projectileList;
    private ReorderableList summonList;
    private ReorderableList minibossList;
    private ReorderableList bossList;
    private int selectedTab;

    private void OnEnable()
    {
        database = (EnemyDatabase)target;

        meleeEnemiesProp = serializedObject.FindProperty("meleeEnemies");
        projectileEnemiesProp = serializedObject.FindProperty("projectileEnemies");
        summonEnemiesProp = serializedObject.FindProperty("summonEnemies");
        minibossEnemiesProp = serializedObject.FindProperty("minibossEnemies");
        bossEnemiesProp = serializedObject.FindProperty("bossEnemies");
        defaultProjectileProp = serializedObject.FindProperty("defaultProjectilePrefab");

        meleeList = BuildList(meleeEnemiesProp, "Melee Enemies", EnemyCategory.Melee);
        projectileList = BuildList(projectileEnemiesProp, "Projectile Enemies", EnemyCategory.Projectile);
        summonList = BuildList(summonEnemiesProp, "Summon Enemies", EnemyCategory.Summon);
        minibossList = BuildList(minibossEnemiesProp, "Miniboss Enemies", EnemyCategory.Miniboss);
        bossList = BuildList(bossEnemiesProp, "Boss Enemies", EnemyCategory.Boss);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Enemy Database", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Define enemies by tab. Expand an enemy entry to edit stats and Animation Clips (Walk, Injured, Death — same titles as the player). Loot rolls out of 10000 per entry (10000 = 100%).",
            MessageType.Info);

        EditorGUILayout.LabelField("Shared Prefabs", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(defaultProjectileProp, new GUIContent("Default Projectile"));

        EditorGUILayout.Space(8f);
        selectedTab = GUILayout.Toolbar(selectedTab, new[]
        {
            "Melee", "Projectile", "Summon", "Miniboss", "Boss"
        });

        EditorGUILayout.Space(6f);
        switch (selectedTab)
        {
            case 0:
                meleeList.DoLayoutList();
                break;
            case 1:
                projectileList.DoLayoutList();
                break;
            case 2:
                summonList.DoLayoutList();
                break;
            case 3:
                minibossList.DoLayoutList();
                break;
            default:
                bossList.DoLayoutList();
                break;
        }

        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(database);

        serializedObject.ApplyModifiedProperties();
    }

    private ReorderableList BuildList(SerializedProperty listProp, string header, EnemyCategory category)
    {
        var list = new ReorderableList(serializedObject, listProp, true, true, true, true);

        list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, header);

        list.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            SerializedProperty element = listProp.GetArrayElementAtIndex(index);
            DrawEnemyElement(rect, element, category);
        };

        list.elementHeightCallback = index =>
        {
            SerializedProperty element = listProp.GetArrayElementAtIndex(index);
            return GetEnemyElementHeight(element, category);
        };

        list.onAddCallback = reorderableList =>
        {
            int index = reorderableList.serializedProperty.arraySize;
            reorderableList.serializedProperty.arraySize++;
            SerializedProperty element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
            SerializedProperty core = element.FindPropertyRelative("core");
            if (core != null)
            {
                string defaultName = category switch
                {
                    EnemyCategory.Miniboss => "New Miniboss",
                    EnemyCategory.Boss => "New Boss",
                    _ => $"New {category} Enemy"
                };
                core.FindPropertyRelative("enemyName").stringValue = defaultName;

                if (category is EnemyCategory.Miniboss or EnemyCategory.Boss)
                {
                    core.FindPropertyRelative("maxHealth").floatValue =
                        category == EnemyCategory.Boss ? 500f : 180f;
                    core.FindPropertyRelative("damage").floatValue =
                        category == EnemyCategory.Boss ? 24f : 14f;
                    core.FindPropertyRelative("displayScale").floatValue =
                        category == EnemyCategory.Boss ? 1.6f : 1.3f;
                }
            }

            EditorUtility.SetDirty(database);
        };

        return list;
    }

    private static void DrawEnemyElement(Rect rect, SerializedProperty element, EnemyCategory category)
    {
        rect.x += 6f;
        rect.width -= 12f;

        float y = rect.y + 2f;
        float lineH = EditorGUIUtility.singleLineHeight;
        float oldLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 120f;

        SerializedProperty core = element.FindPropertyRelative("core");
        string enemyName = core?.FindPropertyRelative("enemyName")?.stringValue ?? "Enemy";
        EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineH), enemyName, EditorStyles.boldLabel);
        y += lineH + Gap;

        y = DrawCoreStats(rect, y, lineH, core);

        switch (category)
        {
            case EnemyCategory.Melee:
                y = DrawProperty(rect, y, lineH, element, "chargeSpeedMultiplier", "Charge Speed Mult");
                y = DrawProperty(rect, y, lineH, element, "chargeDuration", "Charge Duration");
                break;

            case EnemyCategory.Projectile:
                y = DrawProperty(rect, y, lineH, element, "projectileSprite", "Projectile Sprite");
                y = DrawProperty(rect, y, lineH, element, "projectileSpeed", "Projectile Speed");
                y = DrawProperty(rect, y, lineH, element, "projectileLifetime", "Projectile Lifetime");
                break;

            case EnemyCategory.Summon:
                y = DrawProperty(rect, y, lineH, element, "summonPrefabs", "Summon Prefabs");
                y = DrawProperty(rect, y, lineH, element, "maxActiveSummons", "Max Active Summons");
                y = DrawProperty(rect, y, lineH, element, "summonInterval", "Summon Interval");
                y = DrawProperty(rect, y, lineH, element, "summonLifetime", "Summon Lifetime");
                break;

            case EnemyCategory.Miniboss:
            case EnemyCategory.Boss:
                y = DrawProperty(rect, y, lineH, element, "behavior", "Behavior");
                SerializedProperty behaviorProp = element.FindPropertyRelative("behavior");
                var behavior = behaviorProp != null
                    ? (EnemyBehaviorKind)behaviorProp.enumValueIndex
                    : EnemyBehaviorKind.Melee;

                if (behavior == EnemyBehaviorKind.Projectile)
                {
                    y = DrawProperty(rect, y, lineH, element, "projectileSprite", "Projectile Sprite");
                    y = DrawProperty(rect, y, lineH, element, "projectileSpeed", "Projectile Speed");
                    y = DrawProperty(rect, y, lineH, element, "projectileLifetime", "Projectile Lifetime");
                }
                else if (behavior == EnemyBehaviorKind.Summon)
                {
                    y = DrawProperty(rect, y, lineH, element, "summonPrefabs", "Summon Prefabs");
                    y = DrawProperty(rect, y, lineH, element, "maxActiveSummons", "Max Active Summons");
                    y = DrawProperty(rect, y, lineH, element, "summonInterval", "Summon Interval");
                    y = DrawProperty(rect, y, lineH, element, "summonLifetime", "Summon Lifetime");
                }

                break;
        }

        y += Gap;
        y = DrawLootPool(rect, y, lineH, element.FindPropertyRelative("lootPool"));

        EditorGUIUtility.labelWidth = oldLabelWidth;
    }

    private static float DrawCoreStats(Rect rect, float y, float lineH, SerializedProperty core)
    {
        if (core == null)
            return y;

        y = DrawProperty(rect, y, lineH, core, "enemyName", "Name");
        y = DrawProperty(rect, y, lineH, core, "sprite", "Sprite");
        y = DrawProperty(rect, y, lineH, core, "displayScale", "Display Scale");
        y = DrawProperty(rect, y, lineH, core, "tintColor", "Tint");
        y = DrawAnimationClips(rect, y, lineH, core.FindPropertyRelative("animationClips"));
        y = DrawProperty(rect, y, lineH, core, "maxHealth", "Max Health");
        y = DrawProperty(rect, y, lineH, core, "moveSpeed", "Move Speed");
        y = DrawProperty(rect, y, lineH, core, "damage", "Damage");
        y = DrawProperty(rect, y, lineH, core, "attackCooldown", "Attack Cooldown");
        y = DrawProperty(rect, y, lineH, core, "detectionRange", "Detection Range");
        y = DrawProperty(rect, y, lineH, core, "attackRange", "Attack Range");
        y = DrawProperty(rect, y, lineH, core, "aggression", "Aggression");
        y = DrawProperty(rect, y, lineH, core, "knockbackResistance", "Knockback Resist");
        y = DrawProperty(rect, y, lineH, core, "damageResistance", "Damage Resist");
        return y;
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

    private static float GetEnemyElementHeight(SerializedProperty element, EnemyCategory category)
    {
        float lineH = EditorGUIUtility.singleLineHeight;
        float height = lineH + Gap;

        SerializedProperty core = element.FindPropertyRelative("core");
        height += CountPropertyHeight(core, "enemyName", "Name");
        height += CountPropertyHeight(core, "sprite", "Sprite");
        height += CountPropertyHeight(core, "displayScale", "Display Scale");
        height += CountPropertyHeight(core, "tintColor", "Tint");
        height += CountAnimationClipsHeight(core?.FindPropertyRelative("animationClips"));
        height += CountPropertyHeight(core, "maxHealth", "Max Health");
        height += CountPropertyHeight(core, "moveSpeed", "Move Speed");
        height += CountPropertyHeight(core, "damage", "Damage");
        height += CountPropertyHeight(core, "attackCooldown", "Attack Cooldown");
        height += CountPropertyHeight(core, "detectionRange", "Detection Range");
        height += CountPropertyHeight(core, "attackRange", "Attack Range");
        height += CountPropertyHeight(core, "aggression", "Aggression");
        height += CountPropertyHeight(core, "knockbackResistance", "Knockback Resist");
        height += CountPropertyHeight(core, "damageResistance", "Damage Resist");

        switch (category)
        {
            case EnemyCategory.Melee:
                height += CountPropertyHeight(element, "chargeSpeedMultiplier", "Charge Speed Mult");
                height += CountPropertyHeight(element, "chargeDuration", "Charge Duration");
                break;

            case EnemyCategory.Projectile:
                height += CountPropertyHeight(element, "projectileSprite", "Projectile Sprite");
                height += CountPropertyHeight(element, "projectileSpeed", "Projectile Speed");
                height += CountPropertyHeight(element, "projectileLifetime", "Projectile Lifetime");
                break;

            case EnemyCategory.Summon:
                height += CountPropertyHeight(element, "summonPrefabs", "Summon Prefabs");
                height += CountPropertyHeight(element, "maxActiveSummons", "Max Active Summons");
                height += CountPropertyHeight(element, "summonInterval", "Summon Interval");
                height += CountPropertyHeight(element, "summonLifetime", "Summon Lifetime");
                break;

            case EnemyCategory.Miniboss:
            case EnemyCategory.Boss:
                height += CountPropertyHeight(element, "behavior", "Behavior");
                SerializedProperty behaviorProp = element.FindPropertyRelative("behavior");
                var behavior = behaviorProp != null
                    ? (EnemyBehaviorKind)behaviorProp.enumValueIndex
                    : EnemyBehaviorKind.Melee;

                if (behavior == EnemyBehaviorKind.Projectile)
                {
                    height += CountPropertyHeight(element, "projectileSprite", "Projectile Sprite");
                    height += CountPropertyHeight(element, "projectileSpeed", "Projectile Speed");
                    height += CountPropertyHeight(element, "projectileLifetime", "Projectile Lifetime");
                }
                else if (behavior == EnemyBehaviorKind.Summon)
                {
                    height += CountPropertyHeight(element, "summonPrefabs", "Summon Prefabs");
                    height += CountPropertyHeight(element, "maxActiveSummons", "Max Active Summons");
                    height += CountPropertyHeight(element, "summonInterval", "Summon Interval");
                    height += CountPropertyHeight(element, "summonLifetime", "Summon Lifetime");
                }

                break;
        }

        height += GetLootPoolHeight(element.FindPropertyRelative("lootPool"));
        return height + 10f;
    }

    private static float DrawLootPool(Rect rect, float y, float lineH, SerializedProperty lootPoolProp)
    {
        if (lootPoolProp == null)
            return y;

        EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineH), "Loot Pool", EditorStyles.miniBoldLabel);
        y += lineH + Gap;

        y = DrawProperty(rect, y, lineH, lootPoolProp, "secondDropChance", "Second Drop (/10000)");

        SerializedProperty entriesProp = lootPoolProp.FindPropertyRelative("entries");
        if (entriesProp == null)
            return y;

        EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineH), "Loot Entries", EditorStyles.miniLabel);
        y += lineH + Gap;

        for (int i = 0; i < entriesProp.arraySize; i++)
            y = DrawLootEntry(rect, y, lineH, entriesProp, i);

        float buttonWidth = 120f;
        Rect addRect = new(rect.x, y, buttonWidth, lineH);
        if (GUI.Button(addRect, "Add Loot Entry"))
        {
            entriesProp.arraySize++;
            SerializedProperty entry = entriesProp.GetArrayElementAtIndex(entriesProp.arraySize - 1);
            ResetLootEntry(entry);
        }

        return y + lineH + Gap;
    }

    private static float DrawLootEntry(Rect rect, float y, float lineH, SerializedProperty entriesProp, int index)
    {
        SerializedProperty entry = entriesProp.GetArrayElementAtIndex(index);
        float entryHeight = GetLootEntryHeight(entry);
        Rect box = new(rect.x, y, rect.width, entryHeight);
        GUI.Box(box, GUIContent.none);

        float innerX = rect.x + 8f;
        float innerWidth = rect.width - 16f;
        float innerY = y + 4f;

        Rect removeRect = new(innerX + innerWidth - 60f, innerY, 60f, lineH);
        if (GUI.Button(removeRect, "Remove"))
        {
            entriesProp.DeleteArrayElementAtIndex(index);
            return y + entryHeight + Gap;
        }

        innerY += lineH + Gap;
        innerY = DrawProperty(
            new Rect(innerX, innerY, innerWidth, lineH),
            innerY,
            lineH,
            entry,
            "kind",
            "Reward Type");

        var kind = entry.FindPropertyRelative("kind") != null
            ? (LootRewardKind)entry.FindPropertyRelative("kind").enumValueIndex
            : LootRewardKind.Weapon;

        switch (kind)
        {
            case LootRewardKind.Weapon:
                innerY = DrawWeaponSelection(
                    new Rect(innerX, innerY, innerWidth, lineH),
                    innerY,
                    lineH,
                    entry.FindPropertyRelative("weapon"));
                break;

            case LootRewardKind.Currency:
                innerY = DrawProperty(
                    new Rect(innerX, innerY, innerWidth, lineH),
                    innerY,
                    lineH,
                    entry,
                    "currencyAmount",
                    "Currency Amount");
                innerY = DrawProperty(
                    new Rect(innerX, innerY, innerWidth, lineH),
                    innerY,
                    lineH,
                    entry,
                    "rewardId",
                    "Currency Id");
                break;

            case LootRewardKind.Item:
            case LootRewardKind.Artifact:
                innerY = DrawProperty(
                    new Rect(innerX, innerY, innerWidth, lineH),
                    innerY,
                    lineH,
                    entry,
                    "rewardId",
                    "Reward Id");
                break;
        }

        innerY = DrawProperty(
            new Rect(innerX, innerY, innerWidth, lineH),
            innerY,
            lineH,
            entry,
            "displayName",
            "Display Name");

        innerY = DrawProperty(
            new Rect(innerX, innerY, innerWidth, lineH),
            innerY,
            lineH,
            entry,
            "weight",
            "Chance (/10000)");

        DrawProperty(
            new Rect(innerX, innerY, innerWidth, lineH),
            innerY,
            lineH,
            entry,
            "minQuantity",
            "Min Qty");

        return y + entryHeight + Gap;
    }

    private static float DrawWeaponSelection(Rect rect, float y, float lineH, SerializedProperty weaponProp)
    {
        if (weaponProp == null)
            return y;

        SerializedProperty typeProp = weaponProp.FindPropertyRelative("type");
        SerializedProperty indexProp = weaponProp.FindPropertyRelative("index");
        if (typeProp == null || indexProp == null)
            return y;

        EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, lineH), typeProp, new GUIContent("Weapon Type"));
        y += lineH + Gap;

        WeaponDatabase database = FindWeaponDatabase();
        var weaponType = (WeaponType)typeProp.enumValueIndex;
        string[] weaponNames = GetWeaponNames(database, weaponType);

        if (weaponNames.Length == 0)
        {
            EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineH), "No weapons in WeaponDatabase.");
            return y + lineH + Gap;
        }

        int currentIndex = Mathf.Clamp(indexProp.intValue, 0, weaponNames.Length - 1);
        int nextIndex = EditorGUI.Popup(new Rect(rect.x, y, rect.width, lineH), "Weapon", currentIndex, weaponNames);
        indexProp.intValue = nextIndex;
        return y + lineH + Gap;
    }

    private static float GetLootPoolHeight(SerializedProperty lootPoolProp)
    {
        if (lootPoolProp == null)
            return 0f;

        float lineH = EditorGUIUtility.singleLineHeight;
        float height = lineH + Gap;
        height += lineH + Gap;
        height += CountPropertyHeight(lootPoolProp, "secondDropChance", "Second Drop (/10000)");

        SerializedProperty entriesProp = lootPoolProp.FindPropertyRelative("entries");
        if (entriesProp != null)
        {
            for (int i = 0; i < entriesProp.arraySize; i++)
                height += GetLootEntryHeight(entriesProp.GetArrayElementAtIndex(i)) + Gap;

            height += lineH + Gap;
        }

        return height;
    }

    private static float GetLootEntryHeight(SerializedProperty entry)
    {
        if (entry == null)
            return EditorGUIUtility.singleLineHeight;

        float lineH = EditorGUIUtility.singleLineHeight;
        float height = lineH + 8f + Gap;
        height += CountPropertyHeight(entry, "kind", "Reward Type");

        var kind = entry.FindPropertyRelative("kind") != null
            ? (LootRewardKind)entry.FindPropertyRelative("kind").enumValueIndex
            : LootRewardKind.Weapon;

        switch (kind)
        {
            case LootRewardKind.Weapon:
                height += (lineH + Gap) * 2;
                break;
            case LootRewardKind.Currency:
                height += CountPropertyHeight(entry, "currencyAmount", "Currency Amount");
                height += CountPropertyHeight(entry, "rewardId", "Currency Id");
                break;
            case LootRewardKind.Item:
            case LootRewardKind.Artifact:
                height += CountPropertyHeight(entry, "rewardId", "Reward Id");
                break;
        }

        height += CountPropertyHeight(entry, "displayName", "Display Name");
        height += CountPropertyHeight(entry, "weight", "Chance (/10000)");
        height += CountPropertyHeight(entry, "minQuantity", "Min Qty");
        return height + 8f;
    }

    private static void ResetLootEntry(SerializedProperty entry)
    {
        if (entry == null)
            return;

        SerializedProperty kindProp = entry.FindPropertyRelative("kind");
        if (kindProp != null)
            kindProp.enumValueIndex = (int)LootRewardKind.Weapon;

        SerializedProperty weaponProp = entry.FindPropertyRelative("weapon");
        if (weaponProp != null)
        {
            weaponProp.FindPropertyRelative("type").enumValueIndex = (int)WeaponType.Projectile;
            weaponProp.FindPropertyRelative("index").intValue = 0;
        }

        SetString(entry, "displayName", "Loot");
        SetString(entry, "rewardId", "");
        SetInt(entry, "weight", 500);
        SetInt(entry, "minQuantity", 1);
        SetInt(entry, "maxQuantity", 1);
        SetInt(entry, "currencyAmount", 1);
    }

    private static void SetString(SerializedProperty parent, string propertyName, string value)
    {
        SerializedProperty property = parent.FindPropertyRelative(propertyName);
        if (property != null)
            property.stringValue = value;
    }

    private static void SetFloat(SerializedProperty parent, string propertyName, float value)
    {
        SerializedProperty property = parent.FindPropertyRelative(propertyName);
        if (property != null)
            property.floatValue = value;
    }

    private static void SetInt(SerializedProperty parent, string propertyName, int value)
    {
        SerializedProperty property = parent.FindPropertyRelative(propertyName);
        if (property != null)
            property.intValue = value;
    }

    private static WeaponDatabase FindWeaponDatabase()
    {
        string[] guids = AssetDatabase.FindAssets("t:WeaponDatabase");
        if (guids.Length == 0)
            return null;

        return AssetDatabase.LoadAssetAtPath<WeaponDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    private static string[] GetWeaponNames(WeaponDatabase database, WeaponType type)
    {
        if (database == null)
            return System.Array.Empty<string>();

        return type switch
        {
            WeaponType.Projectile => BuildNames(database.ProjectileWeapons, weapon => weapon.WeaponName),
            WeaponType.Melee => BuildNames(database.MeleeWeapons, weapon => weapon.WeaponName),
            WeaponType.Summon => BuildNames(database.SummonWeapons, weapon => weapon.WeaponName),
            _ => System.Array.Empty<string>()
        };
    }

    private static string[] BuildNames<T>(System.Collections.Generic.IReadOnlyList<T> items, System.Func<T, string> getName)
    {
        if (items == null || items.Count == 0)
            return System.Array.Empty<string>();

        var names = new string[items.Count];
        for (int i = 0; i < items.Count; i++)
            names[i] = string.IsNullOrWhiteSpace(getName(items[i])) ? $"Entry {i}" : getName(items[i]);

        return names;
    }

    private static float CountPropertyHeight(SerializedProperty parent, string propertyName, string label)
    {
        if (parent == null)
            return 0f;

        SerializedProperty property = parent.FindPropertyRelative(propertyName);
        if (property == null)
            return 0f;

        return EditorGUI.GetPropertyHeight(property, new GUIContent(label), true) + Gap;
    }

    private static float DrawAnimationClips(Rect rect, float y, float lineH, SerializedProperty clipsProp)
    {
        if (clipsProp == null)
            return y;

        EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineH), "Animation Clips (Walk / Injured / Death)", EditorStyles.boldLabel);
        y += lineH + Gap;

        for (int i = 0; i < clipsProp.arraySize; i++)
        {
            SerializedProperty entry = clipsProp.GetArrayElementAtIndex(i);
            SerializedProperty titleProp = entry.FindPropertyRelative("title");
            SerializedProperty framesProp = entry.FindPropertyRelative("frames");

            float removeWidth = 22f;
            float titleWidth = rect.width - removeWidth - 4f;

            EditorGUI.PropertyField(
                new Rect(rect.x, y, titleWidth, lineH),
                titleProp,
                new GUIContent($"Clip {i + 1} Title"));
            if (GUI.Button(new Rect(rect.x + titleWidth + 4f, y, removeWidth, lineH), "X"))
            {
                clipsProp.DeleteArrayElementAtIndex(i);
                return y;
            }

            y += lineH + Gap;
            y = DrawProperty(rect, y, lineH, entry, "framesPerSecond", "Frames / Second");
            y = DrawProperty(rect, y, lineH, entry, "loop", "Loop");

            float framesHeight = EditorGUI.GetPropertyHeight(framesProp, new GUIContent("Sprite Frames"), true);
            EditorGUI.PropertyField(
                new Rect(rect.x, y, rect.width, framesHeight),
                framesProp,
                new GUIContent("Sprite Frames"),
                true);
            y += framesHeight + Gap + 2f;
        }

        if (GUI.Button(new Rect(rect.x, y, rect.width, lineH), "Add Animation Clip"))
        {
            int newIndex = clipsProp.arraySize;
            clipsProp.InsertArrayElementAtIndex(newIndex);
            SerializedProperty entry = clipsProp.GetArrayElementAtIndex(newIndex);
            entry.FindPropertyRelative("title").stringValue = $"Clip {newIndex + 1}";
            entry.FindPropertyRelative("frames").arraySize = 0;
            entry.FindPropertyRelative("framesPerSecond").floatValue = 8f;
            entry.FindPropertyRelative("loop").boolValue = true;
        }

        y += lineH + Gap;
        return y;
    }

    private static float CountAnimationClipsHeight(SerializedProperty clipsProp)
    {
        if (clipsProp == null)
            return 0f;

        float lineH = EditorGUIUtility.singleLineHeight;
        float height = lineH + Gap;

        for (int i = 0; i < clipsProp.arraySize; i++)
        {
            SerializedProperty entry = clipsProp.GetArrayElementAtIndex(i);
            SerializedProperty framesProp = entry.FindPropertyRelative("frames");
            height += lineH + Gap;
            height += CountPropertyHeight(entry, "framesPerSecond", "Frames / Second");
            height += CountPropertyHeight(entry, "loop", "Loop");
            height += EditorGUI.GetPropertyHeight(framesProp, new GUIContent("Sprite Frames"), true) + Gap + 2f;
        }

        height += lineH + Gap;
        return height;
    }
}
#endif