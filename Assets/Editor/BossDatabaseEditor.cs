#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(BossDatabase))]
public class BossDatabaseEditor : Editor
{
    private const float Gap = 4f;

    private BossDatabase database;
    private SerializedProperty bossesProp;
    private ReorderableList bossesList;

    private void OnEnable()
    {
        database = (BossDatabase)target;
        bossesProp = serializedObject.FindProperty("bosses");
        bossesList = BuildList(bossesProp);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Boss Database", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Define bosses here. Each entry stores sprite, stats, loot, and an optional BossBehavior script for unique fight logic.",
            MessageType.Info);

        bossesList.DoLayoutList();

        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(database);

        serializedObject.ApplyModifiedProperties();
    }

    private ReorderableList BuildList(SerializedProperty listProp)
    {
        var list = new ReorderableList(serializedObject, listProp, true, true, true, true);

        list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Bosses");

        list.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            SerializedProperty element = listProp.GetArrayElementAtIndex(index);
            DrawBossElement(rect, element);
        };

        list.elementHeightCallback = index =>
        {
            SerializedProperty element = listProp.GetArrayElementAtIndex(index);
            return GetBossElementHeight(element);
        };

        list.onAddCallback = reorderableList =>
        {
            int index = reorderableList.serializedProperty.arraySize;
            reorderableList.serializedProperty.arraySize++;
            SerializedProperty element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
            SerializedProperty core = element.FindPropertyRelative("core");
            if (core != null)
            {
                core.FindPropertyRelative("bossName").stringValue = "New Boss";
                core.FindPropertyRelative("maxHealth").floatValue = 500f;
                core.FindPropertyRelative("damage").floatValue = 24f;
                core.FindPropertyRelative("displayScale").floatValue = 1.6f;
            }

            SetBehaviorScript(element, null);
            EditorUtility.SetDirty(database);
        };

        return list;
    }

    private static void DrawBossElement(Rect rect, SerializedProperty element)
    {
        rect.x += 6f;
        rect.width -= 12f;

        float y = rect.y + 2f;
        float lineH = EditorGUIUtility.singleLineHeight;
        float oldLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 130f;

        SerializedProperty core = element.FindPropertyRelative("core");
        string bossName = core?.FindPropertyRelative("bossName")?.stringValue ?? "Boss";
        EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineH), bossName, EditorStyles.boldLabel);
        y += lineH + Gap;

        y = DrawCoreStats(rect, y, lineH, core);
        y = DrawBehaviorScript(rect, y, lineH, element);
        y += Gap;
        y = DrawLootPool(rect, y, lineH, element.FindPropertyRelative("lootPool"));

        EditorGUIUtility.labelWidth = oldLabelWidth;
    }

    private static float DrawCoreStats(Rect rect, float y, float lineH, SerializedProperty core)
    {
        if (core == null)
            return y;

        y = DrawProperty(rect, y, lineH, core, "bossName", "Name");
        y = DrawProperty(rect, y, lineH, core, "sprite", "Sprite");
        y = DrawProperty(rect, y, lineH, core, "displayScale", "Display Scale");
        y = DrawProperty(rect, y, lineH, core, "tintColor", "Tint");
        y = DrawAnimationClips(rect, y, lineH, core.FindPropertyRelative("animationClips"), "Idle / Walk / Injured / Death");
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

    private static float DrawBehaviorScript(Rect rect, float y, float lineH, SerializedProperty element)
    {
        EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineH), "Behavior Script", EditorStyles.boldLabel);
        y += lineH + Gap;

        SerializedProperty typeNameProp = element.FindPropertyRelative("behaviorTypeName");
        MonoScript currentScript = ResolveMonoScript(typeNameProp?.stringValue);

        EditorGUI.BeginChangeCheck();
        MonoScript nextScript = (MonoScript)EditorGUI.ObjectField(
            new Rect(rect.x, y, rect.width, lineH),
            "Boss Behavior",
            currentScript,
            typeof(MonoScript),
            false);
        y += lineH + Gap;

        if (EditorGUI.EndChangeCheck())
            SetBehaviorScript(element, nextScript);

        if (nextScript == null)
        {
            EditorGUI.HelpBox(
                new Rect(rect.x, y, rect.width, lineH * 2f),
                "No behavior assigned — boss uses default EnemyAI. Create a script inheriting BossBehavior and assign it here.",
                MessageType.None);
            y += lineH * 2f + Gap;
        }
        else if (nextScript.GetClass() == null || !typeof(BossBehavior).IsAssignableFrom(nextScript.GetClass()))
        {
            EditorGUI.HelpBox(
                new Rect(rect.x, y, rect.width, lineH * 2f),
                "Selected script must inherit from BossBehavior.",
                MessageType.Warning);
            y += lineH * 2f + Gap;
        }

        return y;
    }

    private static void SetBehaviorScript(SerializedProperty element, MonoScript script)
    {
        SerializedProperty typeNameProp = element.FindPropertyRelative("behaviorTypeName");
        if (typeNameProp == null)
            return;

        if (script == null || script.GetClass() == null)
        {
            typeNameProp.stringValue = "";
            return;
        }

        if (!typeof(BossBehavior).IsAssignableFrom(script.GetClass()))
        {
            typeNameProp.stringValue = "";
            return;
        }

        typeNameProp.stringValue = script.GetClass().AssemblyQualifiedName;
    }

    private static MonoScript ResolveMonoScript(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return null;

        System.Type type = System.Type.GetType(typeName);
        if (type == null)
            return null;

        string[] guids = AssetDatabase.FindAssets($"t:MonoScript {type.Name}");
        foreach (string guid in guids)
        {
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
            if (script != null && script.GetClass() == type)
                return script;
        }

        return null;
    }

    private static float GetBossElementHeight(SerializedProperty element)
    {
        float lineH = EditorGUIUtility.singleLineHeight;
        float height = lineH + Gap;

        SerializedProperty core = element.FindPropertyRelative("core");
        height += CountPropertyHeight(core, "bossName", "Name");
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

        height += lineH + Gap;
        height += lineH + Gap;
        SerializedProperty typeNameProp = element.FindPropertyRelative("behaviorTypeName");
        MonoScript script = ResolveMonoScript(typeNameProp?.stringValue);
        if (script == null || script.GetClass() == null || !typeof(BossBehavior).IsAssignableFrom(script.GetClass()))
            height += lineH * 2f + Gap;

        height += GetLootPoolHeight(element.FindPropertyRelative("lootPool"));
        return height + 10f;
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

    private static float CountPropertyHeight(SerializedProperty parent, string propertyName, string label)
    {
        if (parent == null)
            return 0f;

        SerializedProperty property = parent.FindPropertyRelative(propertyName);
        if (property == null)
            return 0f;

        return EditorGUI.GetPropertyHeight(property, new GUIContent(label), true) + Gap;
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

        if (GUI.Button(new Rect(rect.x, y, 120f, lineH), "Add Loot Entry"))
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
        GUI.Box(new Rect(rect.x, y, rect.width, entryHeight), GUIContent.none);

        float innerX = rect.x + 8f;
        float innerWidth = rect.width - 16f;
        float innerY = y + 4f;

        if (GUI.Button(new Rect(innerX + innerWidth - 60f, innerY, 60f, lineH), "Remove"))
        {
            entriesProp.DeleteArrayElementAtIndex(index);
            return y + entryHeight + Gap;
        }

        innerY += lineH + Gap;
        innerY = DrawProperty(new Rect(innerX, innerY, innerWidth, lineH), innerY, lineH, entry, "kind", "Reward Type");

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
                innerY = DrawProperty(new Rect(innerX, innerY, innerWidth, lineH), innerY, lineH, entry, "currencyAmount", "Currency Amount");
                innerY = DrawProperty(new Rect(innerX, innerY, innerWidth, lineH), innerY, lineH, entry, "rewardId", "Currency Id");
                break;
            case LootRewardKind.Item:
            case LootRewardKind.Artifact:
                innerY = DrawProperty(new Rect(innerX, innerY, innerWidth, lineH), innerY, lineH, entry, "rewardId", "Reward Id");
                break;
        }

        innerY = DrawProperty(new Rect(innerX, innerY, innerWidth, lineH), innerY, lineH, entry, "displayName", "Display Name");
        DrawProperty(new Rect(innerX, innerY, innerWidth, lineH), innerY, lineH, entry, "weight", "Chance (/10000)");

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

        WeaponDatabase weaponDatabase = FindWeaponDatabase();
        var weaponType = (WeaponType)typeProp.enumValueIndex;
        string[] weaponNames = GetWeaponNames(weaponDatabase, weaponType);

        if (weaponNames.Length == 0)
        {
            EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineH), "No weapons in WeaponDatabase.");
            return y + lineH + Gap;
        }

        int currentIndex = Mathf.Clamp(indexProp.intValue, 0, weaponNames.Length - 1);
        indexProp.intValue = EditorGUI.Popup(new Rect(rect.x, y, rect.width, lineH), "Weapon", currentIndex, weaponNames);
        return y + lineH + Gap;
    }

    private static float GetLootPoolHeight(SerializedProperty lootPoolProp)
    {
        if (lootPoolProp == null)
            return 0f;

        float lineH = EditorGUIUtility.singleLineHeight;
        float height = lineH + Gap + lineH + Gap;
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
        float height = lineH + 8f + Gap + CountPropertyHeight(entry, "kind", "Reward Type");

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

        SetString(entry, "displayName", "Boss Loot");
        SetString(entry, "rewardId", "");
        SetInt(entry, "weight", 10000);
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

    private static float DrawAnimationClips(Rect rect, float y, float lineH, SerializedProperty clipsProp, string label = "Walk / Injured / Death")
    {
        if (clipsProp == null)
            return y;

        EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineH), $"Animation Clips ({label})", EditorStyles.boldLabel);
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

        return y + lineH + Gap;
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