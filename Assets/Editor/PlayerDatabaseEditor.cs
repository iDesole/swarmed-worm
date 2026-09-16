#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(PlayerDatabase))]
public class PlayerDatabaseEditor : Editor
{
    private const float Gap = 4f;
    private const string WeaponDatabasePath = "Assets/GameData/WeaponDatabase.asset";

    private PlayerDatabase database;
    private SerializedProperty charactersProp;
    private SerializedProperty defaultCharacterIndexProp;
    private ReorderableList characterList;

    private void OnEnable()
    {
        database = (PlayerDatabase)target;
        charactersProp = serializedObject.FindProperty("characters");
        defaultCharacterIndexProp = serializedObject.FindProperty("defaultCharacterIndex");

        characterList = new ReorderableList(serializedObject, charactersProp, true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Playable Characters"),
            drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                SerializedProperty element = charactersProp.GetArrayElementAtIndex(index);
                DrawCharacterElement(rect, element);
            },
            elementHeightCallback = index =>
            {
                SerializedProperty element = charactersProp.GetArrayElementAtIndex(index);
                return GetCharacterElementHeight(element);
            },
            onAddCallback = list =>
            {
                int index = list.serializedProperty.arraySize;
                list.serializedProperty.arraySize++;
                SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
                SerializedProperty core = element.FindPropertyRelative("core");
                if (core != null)
                    core.FindPropertyRelative("characterName").stringValue = "New Character";

                EditorUtility.SetDirty(database);
            }
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Player Database", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Define playable characters here. Animation clips use sprite frame lists (not .anim files). " +
            "Starting weapons are configured per hand slot — set Type to None for empty slots.",
            MessageType.Info);

        EditorGUILayout.PropertyField(defaultCharacterIndexProp, new GUIContent("Default Character Index"));
        EditorGUILayout.Space(6f);
        characterList.DoLayoutList();

        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(database);

        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawCharacterElement(Rect rect, SerializedProperty element)
    {
        rect.x += 6f;
        rect.width -= 12f;

        float y = rect.y + 2f;
        float lineH = EditorGUIUtility.singleLineHeight;
        float oldLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 130f;

        SerializedProperty core = element.FindPropertyRelative("core");
        string characterName = core?.FindPropertyRelative("characterName")?.stringValue ?? "Character";
        EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineH), characterName, EditorStyles.boldLabel);
        y += lineH + Gap;

        y = DrawProperty(rect, y, lineH, element, "spawnPosition", "Spawn Position");
        y = DrawCoreStats(rect, y, lineH, core);
        y = DrawHandSlots(rect, y, lineH, element.FindPropertyRelative("handSlots"));
        y = DrawEnemySpawner(rect, y, lineH, element.FindPropertyRelative("enemySpawner"));
        y = DrawAnimationClips(rect, y, lineH, element.FindPropertyRelative("animationClips"));

        EditorGUIUtility.labelWidth = oldLabelWidth;
    }

    private static float DrawCoreStats(Rect rect, float y, float lineH, SerializedProperty core)
    {
        if (core == null)
            return y;

        y = DrawProperty(rect, y, lineH, core, "characterName", "Name");
        y = DrawProperty(rect, y, lineH, core, "shopPrice", "Shop Price");
        y = DrawProperty(rect, y, lineH, core, "shopSprite", "Shop Sprite");
        y = DrawProperty(rect, y, lineH, core, "sprite", "Sprite");
        y = DrawProperty(rect, y, lineH, core, "displayScale", "Display Scale");
        y = DrawProperty(rect, y, lineH, core, "tintColor", "Tint");
        y = DrawProperty(rect, y, lineH, core, "maxHealth", "Max Health");
        y = DrawProperty(rect, y, lineH, core, "maxShield", "Max Shield");
        y = DrawProperty(rect, y, lineH, core, "shieldRegenRate", "Shield Regen");
        y = DrawProperty(rect, y, lineH, core, "moveSpeed", "Move Speed");
        y = DrawProperty(rect, y, lineH, core, "dodgeSpeedMultiplier", "Dodge Speed Mult");
        y = DrawProperty(rect, y, lineH, core, "attackSpeedMultiplier", "Attack Speed Mult");
        y = DrawProperty(rect, y, lineH, core, "critChance", "Crit Chance");
        y = DrawProperty(rect, y, lineH, core, "critDamageMultiplier", "Crit Damage Mult");
        y = DrawProperty(rect, y, lineH, core, "damageMultiplier", "Damage Mult");
        y = DrawProperty(rect, y, lineH, core, "burrowDuration", "Burrow Duration");
        y = DrawProperty(rect, y, lineH, core, "burrowCooldown", "Burrow Cooldown");
        y = DrawProperty(rect, y, lineH, core, "burrowDamageReduction", "Burrow Damage Reduction");
        y = DrawProperty(rect, y, lineH, core, "knockbackResistance", "Knockback Resist");
        y = DrawProperty(rect, y, lineH, core, "lifesteal", "Lifesteal");
        y = DrawProperty(rect, y, lineH, core, "invulnerabilityFrames", "I-Frames");
        return y;
    }

    private static float GetCharacterElementHeight(SerializedProperty element)
    {
        float lineH = EditorGUIUtility.singleLineHeight;
        float height = lineH + Gap;

        SerializedProperty core = element.FindPropertyRelative("core");
        height += CountPropertyHeight(core, "characterName", "Name");
        height += CountPropertyHeight(core, "shopPrice", "Shop Price");
        height += CountPropertyHeight(core, "shopSprite", "Shop Sprite");
        height += CountPropertyHeight(core, "sprite", "Sprite");
        height += CountPropertyHeight(core, "displayScale", "Display Scale");
        height += CountPropertyHeight(core, "tintColor", "Tint");
        height += CountPropertyHeight(core, "maxHealth", "Max Health");
        height += CountPropertyHeight(core, "maxShield", "Max Shield");
        height += CountPropertyHeight(core, "shieldRegenRate", "Shield Regen");
        height += CountPropertyHeight(core, "moveSpeed", "Move Speed");
        height += CountPropertyHeight(core, "dodgeSpeedMultiplier", "Dodge Speed Mult");
        height += CountPropertyHeight(core, "attackSpeedMultiplier", "Attack Speed Mult");
        height += CountPropertyHeight(core, "critChance", "Crit Chance");
        height += CountPropertyHeight(core, "critDamageMultiplier", "Crit Damage Mult");
        height += CountPropertyHeight(core, "damageMultiplier", "Damage Mult");
        height += CountPropertyHeight(core, "burrowDuration", "Burrow Duration");
        height += CountPropertyHeight(core, "burrowCooldown", "Burrow Cooldown");
        height += CountPropertyHeight(core, "burrowDamageReduction", "Burrow Damage Reduction");
        height += CountPropertyHeight(core, "knockbackResistance", "Knockback Resist");
        height += CountPropertyHeight(core, "lifesteal", "Lifesteal");
        height += CountPropertyHeight(core, "invulnerabilityFrames", "I-Frames");
        height += CountPropertyHeight(element, "spawnPosition", "Spawn Position");
        height += CountHandSlotsHeight(element.FindPropertyRelative("handSlots"));
        height += CountEnemySpawnerHeight(element.FindPropertyRelative("enemySpawner"));
        height += CountAnimationClipsHeight(element.FindPropertyRelative("animationClips"));
        return height + 10f;
    }

    private static float DrawHandSlots(Rect rect, float y, float lineH, SerializedProperty handSlotsProp)
    {
        if (handSlotsProp == null)
            return y;

        EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineH), "Weapon Hand Slots", EditorStyles.miniBoldLabel);
        y += lineH + Gap;

        EnsureHandSlotArray(handSlotsProp);

        for (int i = 0; i < handSlotsProp.arraySize; i++)
        {
            SerializedProperty slot = handSlotsProp.GetArrayElementAtIndex(i);
            EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineH), $"Hand Slot {i + 1}", EditorStyles.boldLabel);
            y += lineH + Gap;
            y = DrawProperty(rect, y, lineH, slot, "localPosition", "Local Position");
            y = DrawWeaponSelection(rect, y, lineH, slot.FindPropertyRelative("startingWeapon"), "Starting Weapon");
        }

        return y;
    }

    private static float DrawAnimationClips(Rect rect, float y, float lineH, SerializedProperty clipsProp)
    {
        if (clipsProp == null)
            return y;

        EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineH), "Animation Clips", EditorStyles.miniBoldLabel);
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

    private static float DrawEnemySpawner(Rect rect, float y, float lineH, SerializedProperty spawnerProp)
    {
        if (spawnerProp == null)
            return y;

        EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineH), "Enemy Spawner", EditorStyles.miniBoldLabel);
        y += lineH + Gap;
        y = DrawProperty(rect, y, lineH, spawnerProp, "attachToPlayer", "Attach To Player");
        y = DrawProperty(rect, y, lineH, spawnerProp, "localOffset", "Local Offset");
        y = DrawProperty(rect, y, lineH, spawnerProp, "maxSpawnAttempts", "Max Spawn Attempts");
        y = DrawProperty(rect, y, lineH, spawnerProp, "showSpawnRings", "Show Spawn Rings");
        return y;
    }

    private static float DrawWeaponSelection(Rect rect, float y, float lineH, SerializedProperty weaponProp, string label)
    {
        if (weaponProp == null)
            return y;

        SerializedProperty typeProp = weaponProp.FindPropertyRelative("type");
        SerializedProperty indexProp = weaponProp.FindPropertyRelative("index");
        if (typeProp == null || indexProp == null)
            return y;

        WeaponType displayedType = (WeaponType)typeProp.enumValueIndex;
        if (indexProp.intValue < 0 || displayedType == WeaponType.None)
            displayedType = WeaponType.None;

        EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineH), label, EditorStyles.miniBoldLabel);
        y += lineH + Gap;

        WeaponType nextType = (WeaponType)EditorGUI.EnumPopup(
            new Rect(rect.x, y, rect.width, lineH),
            "Type",
            displayedType);
        y += lineH + Gap;

        if (nextType == WeaponType.None)
        {
            typeProp.enumValueIndex = (int)WeaponType.None;
            indexProp.intValue = -1;
            return y;
        }

        typeProp.enumValueIndex = (int)nextType;
        if (indexProp.intValue < 0)
            indexProp.intValue = 0;

        WeaponDatabase database = AssetDatabase.LoadAssetAtPath<WeaponDatabase>(WeaponDatabasePath);
        if (database == null)
            return DrawProperty(rect, y, lineH, weaponProp, "index", "Weapon Index");

        string[] weaponNames = GetWeaponNames(database, nextType);
        if (weaponNames.Length == 0)
        {
            typeProp.enumValueIndex = (int)WeaponType.None;
            indexProp.intValue = -1;
            EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineH), "No weapons in WeaponDatabase.");
            return y + lineH + Gap;
        }

        int weaponIndex = Mathf.Clamp(indexProp.intValue, 0, weaponNames.Length - 1);
        int selectedWeapon = EditorGUI.Popup(
            new Rect(rect.x, y, rect.width, lineH),
            "Weapon",
            weaponIndex,
            weaponNames);
        indexProp.intValue = selectedWeapon;
        return y + lineH + Gap;
    }

    private static float CountHandSlotsHeight(SerializedProperty handSlotsProp)
    {
        if (handSlotsProp == null)
            return 0f;

        float lineH = EditorGUIUtility.singleLineHeight;
        float height = lineH + Gap;
        EnsureHandSlotArray(handSlotsProp);

        for (int i = 0; i < handSlotsProp.arraySize; i++)
        {
            SerializedProperty slot = handSlotsProp.GetArrayElementAtIndex(i);
            height += lineH + Gap;
            height += CountPropertyHeight(slot, "localPosition", "Local Position");
            height += (lineH + Gap) * 3;
        }

        return height;
    }

    private static float CountEnemySpawnerHeight(SerializedProperty spawnerProp)
    {
        if (spawnerProp == null)
            return 0f;

        float lineH = EditorGUIUtility.singleLineHeight;
        float height = lineH + Gap;
        height += CountPropertyHeight(spawnerProp, "attachToPlayer", "Attach To Player");
        height += CountPropertyHeight(spawnerProp, "localOffset", "Local Offset");
        height += CountPropertyHeight(spawnerProp, "maxSpawnAttempts", "Max Spawn Attempts");
        height += CountPropertyHeight(spawnerProp, "showSpawnRings", "Show Spawn Rings");
        return height;
    }

    private static void EnsureHandSlotArray(SerializedProperty handSlotsProp)
    {
        if (handSlotsProp.arraySize != 5)
            handSlotsProp.arraySize = 5;
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

    private static string[] GetWeaponNames(WeaponDatabase database, WeaponType type)
    {
        return type switch
        {
            WeaponType.Projectile => BuildNames(database.ProjectileWeapons, weapon => weapon.WeaponName),
            WeaponType.Melee => BuildNames(database.MeleeWeapons, weapon => weapon.WeaponName),
            WeaponType.Summon => BuildNames(database.SummonWeapons, weapon => weapon.WeaponName),
            WeaponType.None => System.Array.Empty<string>(),
            _ => System.Array.Empty<string>()
        };
    }

    private static string[] BuildNames<T>(
        System.Collections.Generic.IReadOnlyList<T> items,
        System.Func<T, string> getName)
    {
        if (items == null || items.Count == 0)
            return System.Array.Empty<string>();

        var names = new string[items.Count];
        for (int i = 0; i < items.Count; i++)
            names[i] = string.IsNullOrWhiteSpace(getName(items[i])) ? $"Entry {i}" : getName(items[i]);

        return names;
    }
}
#endif