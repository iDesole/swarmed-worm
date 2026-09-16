#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(WeaponDatabase))]
public class WeaponDatabaseEditor : Editor
{
    private const float Gap = 4f;

    private WeaponDatabase database;
    private SerializedProperty projectileWeaponsProp;
    private SerializedProperty meleeWeaponsProp;
    private SerializedProperty summonWeaponsProp;
    private SerializedProperty defaultProjectileProp;
    private SerializedProperty defaultMeleeHitboxProp;

    private ReorderableList projectileList;
    private ReorderableList meleeList;
    private ReorderableList summonList;
    private int selectedTab;

    private void OnEnable()
    {
        database = (WeaponDatabase)target;

        projectileWeaponsProp = serializedObject.FindProperty("projectileWeapons");
        meleeWeaponsProp = serializedObject.FindProperty("meleeWeapons");
        summonWeaponsProp = serializedObject.FindProperty("summonWeapons");
        defaultProjectileProp = serializedObject.FindProperty("defaultProjectilePrefab");
        defaultMeleeHitboxProp = serializedObject.FindProperty("defaultMeleeHitboxPrefab");

        projectileList = BuildList(projectileWeaponsProp, "Projectile Weapons", WeaponType.Projectile);
        meleeList = BuildList(meleeWeaponsProp, "Melee Weapons", WeaponType.Melee);
        summonList = BuildList(summonWeaponsProp, "Summon Weapons", WeaponType.Summon);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Weapon Database", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Define each weapon here. Shared bullet/hitbox prefabs live above; per-weapon sprites and stats control how they look and behave.",
            MessageType.Info);

        EditorGUILayout.LabelField("Shared Prefabs", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(defaultProjectileProp, new GUIContent("Default Bullet"));
        EditorGUILayout.PropertyField(defaultMeleeHitboxProp, new GUIContent("Default Melee Hitbox"));

        EditorGUILayout.Space(8f);
        selectedTab = GUILayout.Toolbar(selectedTab, new[] { "Projectile", "Melee", "Summon" });

        EditorGUILayout.Space(6f);
        switch (selectedTab)
        {
            case 0:
                projectileList.DoLayoutList();
                break;
            case 1:
                meleeList.DoLayoutList();
                break;
            default:
                summonList.DoLayoutList();
                break;
        }

        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(database);

        serializedObject.ApplyModifiedProperties();
    }

    private ReorderableList BuildList(SerializedProperty listProp, string header, WeaponType type)
    {
        var list = new ReorderableList(serializedObject, listProp, true, true, true, true);

        list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, header);

        list.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            SerializedProperty element = listProp.GetArrayElementAtIndex(index);
            DrawWeaponElement(rect, element, type);
        };

        list.elementHeightCallback = index =>
        {
            SerializedProperty element = listProp.GetArrayElementAtIndex(index);
            return GetWeaponElementHeight(element, type);
        };

        list.onAddCallback = reorderableList =>
        {
            int index = reorderableList.serializedProperty.arraySize;
            reorderableList.serializedProperty.arraySize++;
            SerializedProperty element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
            SerializedProperty core = element.FindPropertyRelative("core");
            if (core != null)
                core.FindPropertyRelative("weaponName").stringValue = $"New {type} Weapon";
            EditorUtility.SetDirty(database);
        };

        return list;
    }

    private static void DrawWeaponElement(Rect rect, SerializedProperty element, WeaponType type)
    {
        rect.x += 6f;
        rect.width -= 12f;

        float y = rect.y + 2f;
        float lineH = EditorGUIUtility.singleLineHeight;
        float oldLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 110f;

        SerializedProperty core = element.FindPropertyRelative("core");
        string weaponName = core?.FindPropertyRelative("weaponName")?.stringValue ?? "Weapon";
        EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineH), weaponName, EditorStyles.boldLabel);
        y += lineH + Gap;

        y = DrawCoreStats(rect, y, lineH, core);

        switch (type)
        {
            case WeaponType.Projectile:
                y = DrawProperty(rect, y, lineH, element, "projectileSprite", "Projectile Sprite");
                y = DrawProperty(rect, y, lineH, element, "projectileSpeed", "Projectile Speed");
                y = DrawProperty(rect, y, lineH, element, "projectileCount", "Projectile Count");
                y = DrawProperty(rect, y, lineH, element, "spreadAngle", "Spread Angle");
                y = DrawProperty(rect, y, lineH, element, "pierceCount", "Pierce");
                y = DrawProperty(rect, y, lineH, element, "bounceCount", "Bounce");
                y = DrawProperty(rect, y, lineH, element, "homingStrength", "Homing");
                y = DrawProperty(rect, y, lineH, element, "projectileSize", "Projectile Size");
                y = DrawProperty(rect, y, lineH, element, "projectileVerticalOffset", "Projectile Up/Down");
                y = DrawProperty(rect, y, lineH, element, "projectilePitchOffset", "Projectile Pitch");
                y = DrawProperty(rect, y, lineH, element, "projectileGravityScale", "Gravity Scale");
                y += Gap;
                EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineH), "Explosion", EditorStyles.miniBoldLabel);
                y += lineH + Gap;
                y = DrawProperty(rect, y, lineH, element, "explodeOnHit", "Explode On Hit");
                if (element.FindPropertyRelative("explodeOnHit")?.boolValue == true)
                {
                    y = DrawProperty(rect, y, lineH, element, "explosionRadius", "Explosion Radius");
                    y = DrawProperty(rect, y, lineH, element, "explosionInitialDamage", "Initial Damage");
                    y = DrawProperty(rect, y, lineH, element, "explosionRadiusDamage", "Radius Damage");
                    y = DrawProperty(rect, y, lineH, element, "explosionPulseCount", "Pulse Count");
                    y = DrawProperty(rect, y, lineH, element, "explosionPulseSpacing", "Pulse Spacing");
                }
                break;

            case WeaponType.Melee:
                y = DrawProperty(rect, y, lineH, element, "swingDuration", "Swing Duration");
                y = DrawProperty(rect, y, lineH, element, "swingRadius", "Swing Radius");
                y = DrawProperty(rect, y, lineH, element, "swingsPerAttack", "Swings Per Attack");
                y = DrawProperty(rect, y, lineH, element, "swingArcAngle", "Swing Arc");
                y = DrawProperty(rect, y, lineH, element, "multiHitCount", "Multi Hit Count");
                y = DrawProperty(rect, y, lineH, element, "lifesteal", "Lifesteal");
                break;

            case WeaponType.Summon:
                y = DrawProperty(rect, y, lineH, element, "summonPrefabs", "Summon Prefabs");
                y = DrawProperty(rect, y, lineH, element, "maxActiveSummons", "Max Active Summons");
                y = DrawProperty(rect, y, lineH, element, "summonLifetime", "Summon Lifetime");
                y = DrawProperty(rect, y, lineH, element, "summonRange", "Summon Range");
                y = DrawProperty(rect, y, lineH, element, "summonDamageMultiplier", "Summon Damage Mult");
                y = DrawProperty(rect, y, lineH, element, "summonHealthMultiplier", "Summon Health Mult");
                y = DrawProperty(rect, y, lineH, element, "summonAttackSpeedMultiplier", "Summon Attack Mult");
                y = DrawProperty(rect, y, lineH, element, "summonsPerCast", "Summons Per Cast");
                break;
        }

        EditorGUIUtility.labelWidth = oldLabelWidth;
    }

    private static float DrawCoreStats(Rect rect, float y, float lineH, SerializedProperty core)
    {
        if (core == null)
            return y;

        y = DrawProperty(rect, y, lineH, core, "weaponName", "Name");
        y = DrawProperty(rect, y, lineH, core, "shopPrice", "Shop Price");
        y = DrawProperty(rect, y, lineH, core, "shopSprite", "Shop Sprite");
        y = DrawProperty(rect, y, lineH, core, "sprite", "Sprite");
        y = DrawProperty(rect, y, lineH, core, "displayScale", "Display Scale");
        y = DrawProperty(rect, y, lineH, core, "damage", "Damage");
        y = DrawProperty(rect, y, lineH, core, "attackSpeed", "Attack Speed");
        y = DrawProperty(rect, y, lineH, core, "range", "Range");
        y = DrawProperty(rect, y, lineH, core, "critChance", "Crit Chance");
        y = DrawProperty(rect, y, lineH, core, "critMultiplier", "Crit Multiplier");
        y = DrawProperty(rect, y, lineH, core, "knockback", "Knockback");
        y = DrawProperty(rect, y, lineH, core, "aimPitchOffset", "Pitch Offset");
        y = DrawElementProfile(rect, y, lineH, core);
        return y;
    }

    private static float DrawElementProfile(Rect rect, float y, float lineH, SerializedProperty core)
    {
        SerializedProperty profile = core?.FindPropertyRelative("elementProfile");
        if (profile == null)
            return y;

        SerializedProperty elementsProp = profile.FindPropertyRelative("elements");
        if (elementsProp == null)
            return y;

        y += Gap;
        EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineH), "Elements", EditorStyles.boldLabel);
        y += lineH + Gap;

        y = DrawElementToggle(rect, y, lineH, elementsProp, WeaponElement.Poison, "Poison");
        y = DrawElementToggle(rect, y, lineH, elementsProp, WeaponElement.Fire, "Fire");
        y = DrawElementToggle(rect, y, lineH, elementsProp, WeaponElement.Ice, "Ice");
        y = DrawElementToggle(rect, y, lineH, elementsProp, WeaponElement.Electric, "Electric");
        y = DrawElementToggle(rect, y, lineH, elementsProp, WeaponElement.Void, "Void");

        if (HasElement(elementsProp, WeaponElement.Void))
        {
            y += Gap;
            EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineH), "Void", EditorStyles.miniBoldLabel);
            y += lineH + Gap;

            SerializedProperty voidEffect = profile.FindPropertyRelative("voidEffect");
            y = DrawProperty(rect, y, lineH, voidEffect, "voidDamageMultiplier", "Void Damage Mult");
            y = DrawProperty(rect, y, lineH, voidEffect, "voidTickInterval", "Void Tick Interval");
            y = DrawProperty(rect, y, lineH, voidEffect, "collapseDuration", "Collapse Duration");
            y = DrawProperty(rect, y, lineH, voidEffect, "pullRadius", "Pull Radius");
            y = DrawProperty(rect, y, lineH, voidEffect, "pullStrength", "Pull Strength");
            y = DrawProperty(rect, y, lineH, voidEffect, "maxPullTargets", "Max Pull Targets");
            y = DrawProperty(rect, y, lineH, voidEffect, "maxVoidCount", "Max Void Count");
        }

        if (HasElement(elementsProp, WeaponElement.Poison))
        {
            y += Gap;
            EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineH), "Poison", EditorStyles.miniBoldLabel);
            y += lineH + Gap;

            SerializedProperty poison = profile.FindPropertyRelative("poison");
            y = DrawProperty(rect, y, lineH, poison, "poisonDuration", "Poison Duration");
            y = DrawProperty(rect, y, lineH, poison, "poisonStacksPerHit", "Poison Stacks Per Hit");
            y = DrawProperty(rect, y, lineH, poison, "stacksToSpread", "Stacks To Spread");
            y = DrawProperty(rect, y, lineH, poison, "slowPerStack", "Slow Per Stack");
            y = DrawProperty(rect, y, lineH, poison, "spreadRadius", "Spread Radius");
            y = DrawProperty(rect, y, lineH, poison, "maxPoisonCount", "Max Poison Count");
        }

        if (HasElement(elementsProp, WeaponElement.Ice))
        {
            y += Gap;
            EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineH), "Ice", EditorStyles.miniBoldLabel);
            y += lineH + Gap;

            SerializedProperty ice = profile.FindPropertyRelative("ice");
            y = DrawProperty(rect, y, lineH, ice, "slowDuration", "Slow Duration");
            y = DrawProperty(rect, y, lineH, ice, "slowStacksPerHit", "Slow Stacks Per Hit");
            y = DrawProperty(rect, y, lineH, ice, "stacksToFreeze", "Stacks To Freeze");
            y = DrawProperty(rect, y, lineH, ice, "slowPerStack", "Slow Per Stack");
            y = DrawProperty(rect, y, lineH, ice, "freezeDuration", "Freeze Duration");
            y = DrawProperty(rect, y, lineH, ice, "spreadRadius", "Spread Radius");
            y = DrawProperty(rect, y, lineH, ice, "spreadSlowStacks", "Spread Slow Stacks");
            y = DrawProperty(rect, y, lineH, ice, "maxSpreadTargets", "Max Spread Targets");
        }

        if (HasElement(elementsProp, WeaponElement.Fire))
        {
            y += Gap;
            EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineH), "Fire", EditorStyles.miniBoldLabel);
            y += lineH + Gap;

            SerializedProperty fire = profile.FindPropertyRelative("fire");
            y = DrawProperty(rect, y, lineH, fire, "burnDamageMultiplier", "Burn Damage Mult");
            y = DrawProperty(rect, y, lineH, fire, "burnTickInterval", "Burn Tick Interval");
            y = DrawProperty(rect, y, lineH, fire, "burnDuration", "Burn Duration");
            y = DrawProperty(rect, y, lineH, fire, "burnStacksPerHit", "Burn Stacks Per Hit");
            y = DrawProperty(rect, y, lineH, fire, "stacksToIncinerate", "Stacks To Incinerate");
            y = DrawProperty(rect, y, lineH, fire, "spreadRadius", "Spread Radius");
            y = DrawProperty(rect, y, lineH, fire, "maxBurnCount", "Max Burn Count");
        }

        if (HasElement(elementsProp, WeaponElement.Electric))
        {
            y += Gap;
            EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineH), "Electric", EditorStyles.miniBoldLabel);
            y += lineH + Gap;

            SerializedProperty electric = profile.FindPropertyRelative("electric");
            y = DrawProperty(rect, y, lineH, electric, "shockDamageMultiplier", "Shock Damage Mult");
            y = DrawProperty(rect, y, lineH, electric, "chainRange", "Chain Range");
            y = DrawProperty(rect, y, lineH, electric, "maxChainTargets", "Max Chain Targets");
            y = DrawProperty(rect, y, lineH, electric, "chainDamageFalloff", "Chain Damage Falloff");
            y = DrawProperty(rect, y, lineH, electric, "shockDuration", "Shock Duration");
            y = DrawProperty(rect, y, lineH, electric, "shockStacksPerHit", "Shock Stacks Per Hit");
            y = DrawProperty(rect, y, lineH, electric, "stacksToJolt", "Stacks To Jolt");
            y = DrawProperty(rect, y, lineH, electric, "joltDuration", "Jolt Duration");
        }

        return y;
    }

    private static float DrawElementToggle(
        Rect rect,
        float y,
        float lineH,
        SerializedProperty elementsProp,
        WeaponElement element,
        string label)
    {
        int current = elementsProp.intValue;
        bool enabled = (current & (int)element) != 0;
        bool next = EditorGUI.ToggleLeft(new Rect(rect.x, y, rect.width, lineH), label, enabled);

        if (next != enabled)
        {
            if (next)
                elementsProp.intValue = current | (int)element;
            else
                elementsProp.intValue = current & ~(int)element;
        }

        return y + lineH + Gap;
    }

    private static bool HasElement(SerializedProperty elementsProp, WeaponElement element) =>
        elementsProp != null && (elementsProp.intValue & (int)element) != 0;

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

    private static float GetWeaponElementHeight(SerializedProperty element, WeaponType type)
    {
        float lineH = EditorGUIUtility.singleLineHeight;
        float height = lineH + Gap; // title

        SerializedProperty core = element.FindPropertyRelative("core");
        height += CountPropertyHeight(core, "weaponName", "Name");
        height += CountPropertyHeight(core, "shopPrice", "Shop Price");
        height += CountPropertyHeight(core, "shopSprite", "Shop Sprite");
        height += CountPropertyHeight(core, "sprite", "Sprite");
        height += CountPropertyHeight(core, "displayScale", "Display Scale");
        height += CountPropertyHeight(core, "damage", "Damage");
        height += CountPropertyHeight(core, "attackSpeed", "Attack Speed");
        height += CountPropertyHeight(core, "range", "Range");
        height += CountPropertyHeight(core, "critChance", "Crit Chance");
        height += CountPropertyHeight(core, "critMultiplier", "Crit Multiplier");
        height += CountPropertyHeight(core, "knockback", "Knockback");
        height += CountPropertyHeight(core, "aimPitchOffset", "Pitch Offset");
        height += GetElementProfileHeight(core);

        switch (type)
        {
            case WeaponType.Projectile:
                height += CountPropertyHeight(element, "projectileSprite", "Projectile Sprite");
                height += CountPropertyHeight(element, "projectileSpeed", "Projectile Speed");
                height += CountPropertyHeight(element, "projectileCount", "Projectile Count");
                height += CountPropertyHeight(element, "spreadAngle", "Spread Angle");
                height += CountPropertyHeight(element, "pierceCount", "Pierce");
                height += CountPropertyHeight(element, "bounceCount", "Bounce");
                height += CountPropertyHeight(element, "homingStrength", "Homing");
                height += CountPropertyHeight(element, "projectileSize", "Projectile Size");
                height += CountPropertyHeight(element, "projectileVerticalOffset", "Projectile Up/Down");
                height += CountPropertyHeight(element, "projectilePitchOffset", "Projectile Pitch");
                height += CountPropertyHeight(element, "projectileGravityScale", "Gravity Scale");
                height += Gap + lineH + Gap;
                height += CountPropertyHeight(element, "explodeOnHit", "Explode On Hit");
                if (element.FindPropertyRelative("explodeOnHit")?.boolValue == true)
                {
                    height += CountPropertyHeight(element, "explosionRadius", "Explosion Radius");
                    height += CountPropertyHeight(element, "explosionInitialDamage", "Initial Damage");
                    height += CountPropertyHeight(element, "explosionRadiusDamage", "Radius Damage");
                    height += CountPropertyHeight(element, "explosionPulseCount", "Pulse Count");
                    height += CountPropertyHeight(element, "explosionPulseSpacing", "Pulse Spacing");
                }
                break;

            case WeaponType.Melee:
                height += CountPropertyHeight(element, "swingDuration", "Swing Duration");
                height += CountPropertyHeight(element, "swingRadius", "Swing Radius");
                height += CountPropertyHeight(element, "swingsPerAttack", "Swings Per Attack");
                height += CountPropertyHeight(element, "swingArcAngle", "Swing Arc");
                height += CountPropertyHeight(element, "multiHitCount", "Multi Hit Count");
                height += CountPropertyHeight(element, "lifesteal", "Lifesteal");
                break;

            case WeaponType.Summon:
                height += CountPropertyHeight(element, "summonPrefabs", "Summon Prefabs");
                height += CountPropertyHeight(element, "maxActiveSummons", "Max Active Summons");
                height += CountPropertyHeight(element, "summonLifetime", "Summon Lifetime");
                height += CountPropertyHeight(element, "summonRange", "Summon Range");
                height += CountPropertyHeight(element, "summonDamageMultiplier", "Summon Damage Mult");
                height += CountPropertyHeight(element, "summonHealthMultiplier", "Summon Health Mult");
                height += CountPropertyHeight(element, "summonAttackSpeedMultiplier", "Summon Attack Mult");
                height += CountPropertyHeight(element, "summonsPerCast", "Summons Per Cast");
                break;
        }

        return height + 10f;
    }

    private static float GetElementProfileHeight(SerializedProperty core)
    {
        SerializedProperty profile = core?.FindPropertyRelative("elementProfile");
        if (profile == null)
            return 0f;

        SerializedProperty elementsProp = profile.FindPropertyRelative("elements");
        if (elementsProp == null)
            return 0f;

        float lineH = EditorGUIUtility.singleLineHeight;
        float height = Gap + lineH + Gap;
        height += (lineH + Gap) * 5f;

        if (HasElement(elementsProp, WeaponElement.Void))
        {
            SerializedProperty voidEffect = profile.FindPropertyRelative("voidEffect");
            height += Gap + lineH + Gap;
            height += CountPropertyHeight(voidEffect, "voidDamageMultiplier", "Void Damage Mult");
            height += CountPropertyHeight(voidEffect, "voidTickInterval", "Void Tick Interval");
            height += CountPropertyHeight(voidEffect, "collapseDuration", "Collapse Duration");
            height += CountPropertyHeight(voidEffect, "pullRadius", "Pull Radius");
            height += CountPropertyHeight(voidEffect, "pullStrength", "Pull Strength");
            height += CountPropertyHeight(voidEffect, "maxPullTargets", "Max Pull Targets");
            height += CountPropertyHeight(voidEffect, "maxVoidCount", "Max Void Count");
        }

        if (HasElement(elementsProp, WeaponElement.Poison))
        {
            SerializedProperty poison = profile.FindPropertyRelative("poison");
            height += Gap + lineH + Gap;
            height += CountPropertyHeight(poison, "poisonDuration", "Poison Duration");
            height += CountPropertyHeight(poison, "poisonStacksPerHit", "Poison Stacks Per Hit");
            height += CountPropertyHeight(poison, "stacksToSpread", "Stacks To Spread");
            height += CountPropertyHeight(poison, "slowPerStack", "Slow Per Stack");
            height += CountPropertyHeight(poison, "spreadRadius", "Spread Radius");
            height += CountPropertyHeight(poison, "maxPoisonCount", "Max Poison Count");
        }

        if (HasElement(elementsProp, WeaponElement.Ice))
        {
            SerializedProperty ice = profile.FindPropertyRelative("ice");
            height += Gap + lineH + Gap;
            height += CountPropertyHeight(ice, "slowDuration", "Slow Duration");
            height += CountPropertyHeight(ice, "slowStacksPerHit", "Slow Stacks Per Hit");
            height += CountPropertyHeight(ice, "stacksToFreeze", "Stacks To Freeze");
            height += CountPropertyHeight(ice, "slowPerStack", "Slow Per Stack");
            height += CountPropertyHeight(ice, "freezeDuration", "Freeze Duration");
            height += CountPropertyHeight(ice, "spreadRadius", "Spread Radius");
            height += CountPropertyHeight(ice, "spreadSlowStacks", "Spread Slow Stacks");
            height += CountPropertyHeight(ice, "maxSpreadTargets", "Max Spread Targets");
        }

        if (HasElement(elementsProp, WeaponElement.Fire))
        {
            SerializedProperty fire = profile.FindPropertyRelative("fire");
            height += Gap + lineH + Gap;
            height += CountPropertyHeight(fire, "burnDamageMultiplier", "Burn Damage Mult");
            height += CountPropertyHeight(fire, "burnTickInterval", "Burn Tick Interval");
            height += CountPropertyHeight(fire, "burnDuration", "Burn Duration");
            height += CountPropertyHeight(fire, "burnStacksPerHit", "Burn Stacks Per Hit");
            height += CountPropertyHeight(fire, "stacksToIncinerate", "Stacks To Incinerate");
            height += CountPropertyHeight(fire, "spreadRadius", "Spread Radius");
            height += CountPropertyHeight(fire, "maxBurnCount", "Max Burn Count");
        }

        if (HasElement(elementsProp, WeaponElement.Electric))
        {
            SerializedProperty electric = profile.FindPropertyRelative("electric");
            height += Gap + lineH + Gap;
            height += CountPropertyHeight(electric, "shockDamageMultiplier", "Shock Damage Mult");
            height += CountPropertyHeight(electric, "chainRange", "Chain Range");
            height += CountPropertyHeight(electric, "maxChainTargets", "Max Chain Targets");
            height += CountPropertyHeight(electric, "chainDamageFalloff", "Chain Damage Falloff");
            height += CountPropertyHeight(electric, "shockDuration", "Shock Duration");
            height += CountPropertyHeight(electric, "shockStacksPerHit", "Shock Stacks Per Hit");
            height += CountPropertyHeight(electric, "stacksToJolt", "Stacks To Jolt");
            height += CountPropertyHeight(electric, "joltDuration", "Jolt Duration");
        }

        return height;
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
}
#endif