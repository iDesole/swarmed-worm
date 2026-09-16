#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(InteractableCharacterDatabase))]
public class InteractableCharacterDatabaseEditor : Editor
{
    private const string PlayerDatabasePath = "Assets/GameData/PlayerDatabase.asset";
    private const string WeaponDatabasePath = "Assets/GameData/WeaponDatabase.asset";

    private InteractableCharacterDatabase database;
    private SerializedProperty charactersProp;
    private ReorderableList characterList;
    private ReorderableList shopOffersList;
    private int shopOffersCharacterIndex = -1;

    private void OnEnable()
    {
        database = (InteractableCharacterDatabase)target;
        charactersProp = serializedObject.FindProperty("characters");
        characterList = BuildCharacterList();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("NPC Database", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Select a character in the list below to edit all spawn, interaction, and appearance settings.",
            MessageType.Info);

        characterList.DoLayoutList();

        if (characterList.index >= 0 && characterList.index < charactersProp.arraySize)
        {
            EditorGUILayout.Space(10f);
            DrawCharacterDetail(charactersProp.GetArrayElementAtIndex(characterList.index));
        }
        else if (charactersProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("Add a character to begin.", MessageType.None);
        }
        else
        {
            EditorGUILayout.HelpBox("Select a character above to edit its settings.", MessageType.None);
        }

        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(database);

        serializedObject.ApplyModifiedProperties();
    }

    private ReorderableList BuildCharacterList()
    {
        var list = new ReorderableList(serializedObject, charactersProp, true, true, true, true);

        list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Characters");

        list.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            SerializedProperty element = charactersProp.GetArrayElementAtIndex(index);
            SerializedProperty idProp = element.FindPropertyRelative("characterId");
            SerializedProperty nameProp = element.FindPropertyRelative("displayName");
            SerializedProperty kindProp = element.FindPropertyRelative("characterKind");

            string title = string.IsNullOrWhiteSpace(nameProp.stringValue)
                ? idProp.stringValue
                : nameProp.stringValue;

            string subtitle = $"{idProp.stringValue}  •  {kindProp.enumDisplayNames[kindProp.enumValueIndex]}";
            float line = EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, line), title, EditorStyles.boldLabel);
            EditorGUI.LabelField(
                new Rect(rect.x, rect.y + line + 1f, rect.width, line),
                subtitle,
                EditorStyles.miniLabel);
        };

        list.elementHeightCallback = _ => EditorGUIUtility.singleLineHeight * 2f + 6f;

        list.onAddCallback = reorderableList =>
        {
            int newIndex = charactersProp.arraySize;
            charactersProp.InsertArrayElementAtIndex(newIndex);
            SerializedProperty element = charactersProp.GetArrayElementAtIndex(newIndex);
            element.FindPropertyRelative("characterId").stringValue = $"character_{newIndex + 1}";
            element.FindPropertyRelative("displayName").stringValue = "New Character";
            reorderableList.index = newIndex;
            EditorUtility.SetDirty(database);
        };

        list.onSelectCallback = reorderableList =>
        {
            GUI.FocusControl(null);
        };

        return list;
    }

    private void DrawCharacterDetail(SerializedProperty element)
    {
        SerializedProperty idProp = element.FindPropertyRelative("characterId");
        SerializedProperty nameProp = element.FindPropertyRelative("displayName");
        SerializedProperty spriteProp = element.FindPropertyRelative("sprite");
        SerializedProperty interactVerbProp = element.FindPropertyRelative("interactVerb");
        SerializedProperty kindProp = element.FindPropertyRelative("characterKind");
        SerializedProperty useSpawnPlacementProp = element.FindPropertyRelative("useSpawnPlacement");
        SerializedProperty spawnPlacementProp = element.FindPropertyRelative("spawnPlacement");
        SerializedProperty hideWhenWrongMapProp = element.FindPropertyRelative("hideWhenWrongMap");
        SerializedProperty interactRadiusProp = element.FindPropertyRelative("interactRadius");
        SerializedProperty startsEnabledProp = element.FindPropertyRelative("startsEnabled");
        SerializedProperty facePlayerOnInteractProp = element.FindPropertyRelative("facePlayerOnInteract");

        string detailTitle = string.IsNullOrWhiteSpace(nameProp.stringValue)
            ? idProp.stringValue
            : nameProp.stringValue;

        EditorGUILayout.LabelField(detailTitle, EditorStyles.boldLabel);

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Character", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(idProp, new GUIContent("Character Id"));
        EditorGUILayout.PropertyField(nameProp, new GUIContent("Display Name"));
        EditorGUILayout.PropertyField(spriteProp, new GUIContent("Sprite"));
        EditorGUILayout.PropertyField(interactVerbProp, new GUIContent("Interact Verb"));
        EditorGUILayout.PropertyField(kindProp, new GUIContent("Character Kind"));

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Spawn Placement", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(useSpawnPlacementProp, new GUIContent("Use Spawn Placement"));

        if (useSpawnPlacementProp.boolValue)
        {
            DrawSpawnPlacementSection(spawnPlacementProp);
            EditorGUILayout.PropertyField(hideWhenWrongMapProp, new GUIContent("Hide When Wrong Map"));
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Interaction", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(interactRadiusProp, new GUIContent("Interact Radius"));
        EditorGUILayout.PropertyField(startsEnabledProp, new GUIContent("Starts Enabled"));
        EditorGUILayout.PropertyField(facePlayerOnInteractProp, new GUIContent("Face Player On Interact"));

        EditorGUILayout.Space(4f);
        DrawInteractionRulesSection(element.FindPropertyRelative("interactionRules"));

        if (kindProp.enumValueIndex == (int)InteractableCharacterKind.Shop)
        {
            EditorGUILayout.Space(8f);
            DrawShopOffersSection(element);
        }
    }

    private static void DrawInteractionRulesSection(SerializedProperty rulesProp)
    {
        if (rulesProp == null)
            return;

        EditorGUILayout.LabelField("Interaction Limits", EditorStyles.boldLabel);
        SerializedProperty policyProp = rulesProp.FindPropertyRelative("repeatPolicy");
        EditorGUILayout.PropertyField(policyProp, new GUIContent("Repeat Policy"));

        InteractableRepeatPolicy policy = (InteractableRepeatPolicy)policyProp.enumValueIndex;
        if (policy == InteractableRepeatPolicy.MaxInteractions)
            EditorGUILayout.PropertyField(rulesProp.FindPropertyRelative("maxInteractions"), new GUIContent("Max Interactions"));
        else if (policy == InteractableRepeatPolicy.UntilPurchases)
            EditorGUILayout.PropertyField(rulesProp.FindPropertyRelative("maxPurchases"), new GUIContent("Max Purchases"));

        EditorGUILayout.HelpBox(
            "Always = never stops (barkeep). Max Interactions = talk/open limit. Until Purchases = stop after N buys.",
            MessageType.Info);
    }

    private void DrawShopOffersSection(SerializedProperty element)
    {
        SerializedProperty shopOffersProp = element.FindPropertyRelative("shopOffers");
        if (shopOffersProp == null)
            return;

        if (shopOffersCharacterIndex != characterList.index || shopOffersList == null)
        {
            shopOffersCharacterIndex = characterList.index;
            shopOffersList = BuildShopOffersList(shopOffersProp);
        }

        EditorGUILayout.LabelField("Shop Offers", EditorStyles.boldLabel);
        shopOffersList.DoLayoutList();
        EditorGUILayout.HelpBox(
            "Pick a player or weapon. Name, sprite, and price come from PlayerDatabase / WeaponDatabase.",
            MessageType.Info);
    }

    private ReorderableList BuildShopOffersList(SerializedProperty shopOffersProp)
    {
        var list = new ReorderableList(serializedObject, shopOffersProp, true, true, true, true);

        list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Offers");

        list.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            SerializedProperty offer = shopOffersProp.GetArrayElementAtIndex(index);
            DrawShopOfferElement(rect, offer);
        };

        list.elementHeightCallback = index =>
        {
            SerializedProperty offer = shopOffersProp.GetArrayElementAtIndex(index);
            return GetShopOfferElementHeight(offer);
        };

        list.onAddCallback = reorderableList =>
        {
            int newIndex = shopOffersProp.arraySize;
            shopOffersProp.InsertArrayElementAtIndex(newIndex);
            SerializedProperty offer = shopOffersProp.GetArrayElementAtIndex(newIndex);
            offer.FindPropertyRelative("kind").enumValueIndex = (int)ShopOfferKind.Player;
            offer.FindPropertyRelative("playerIndex").intValue = 0;
            SerializedProperty weaponProp = offer.FindPropertyRelative("weapon");
            weaponProp.FindPropertyRelative("type").enumValueIndex = (int)WeaponType.None;
            weaponProp.FindPropertyRelative("index").intValue = -1;
            offer.FindPropertyRelative("unlockedByDefault").boolValue = false;
            EditorUtility.SetDirty(database);
        };

        return list;
    }

    private static void DrawShopOfferElement(Rect rect, SerializedProperty offer)
    {
        rect.x += 4f;
        rect.width -= 8f;
        float y = rect.y + 2f;
        float lineH = EditorGUIUtility.singleLineHeight;
        float oldLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 120f;

        SerializedProperty kindProp = offer.FindPropertyRelative("kind");
        SerializedProperty playerIndexProp = offer.FindPropertyRelative("playerIndex");
        SerializedProperty weaponProp = offer.FindPropertyRelative("weapon");
        SerializedProperty unlockedProp = offer.FindPropertyRelative("unlockedByDefault");

        ShopOfferKind kind = (ShopOfferKind)kindProp.enumValueIndex;
        ShopOfferKind nextKind = (ShopOfferKind)EditorGUI.EnumPopup(
            new Rect(rect.x, y, rect.width, lineH),
            "Sell",
            kind);
        if (nextKind != kind)
        {
            kindProp.enumValueIndex = (int)nextKind;
            if (nextKind == ShopOfferKind.Player)
            {
                weaponProp.FindPropertyRelative("type").enumValueIndex = (int)WeaponType.None;
                weaponProp.FindPropertyRelative("index").intValue = -1;
            }
        }

        y += lineH + 4f;

        if (nextKind == ShopOfferKind.Player)
            y = DrawPlayerPicker(rect, y, lineH, playerIndexProp);
        else
            y = DrawWeaponPicker(rect, y, lineH, weaponProp);

        EditorGUI.PropertyField(
            new Rect(rect.x, y, rect.width, lineH),
            unlockedProp,
            new GUIContent("Unlocked By Default"));

        EditorGUIUtility.labelWidth = oldLabelWidth;
    }

    private static float GetShopOfferElementHeight(SerializedProperty offer)
    {
        float lineH = EditorGUIUtility.singleLineHeight;
        SerializedProperty kindProp = offer.FindPropertyRelative("kind");
        ShopOfferKind kind = (ShopOfferKind)kindProp.enumValueIndex;
        float height = (lineH + 4f) * 2f + lineH + 8f;
        if (kind == ShopOfferKind.Weapon)
            height += (lineH + 4f) * 2f;

        return height;
    }

    private static float DrawPlayerPicker(Rect rect, float y, float lineH, SerializedProperty playerIndexProp)
    {
        PlayerDatabase playerDatabase = AssetDatabase.LoadAssetAtPath<PlayerDatabase>(PlayerDatabasePath);
        if (playerDatabase == null || playerDatabase.Characters.Count == 0)
        {
            EditorGUI.PropertyField(
                new Rect(rect.x, y, rect.width, lineH),
                playerIndexProp,
                new GUIContent("Player Index"));
            return y + lineH + 4f;
        }

        string[] names = BuildPlayerNames(playerDatabase);
        int index = Mathf.Clamp(playerIndexProp.intValue, 0, names.Length - 1);
        int selected = EditorGUI.Popup(
            new Rect(rect.x, y, rect.width, lineH),
            "Player",
            index,
            names);
        playerIndexProp.intValue = selected;
        return y + lineH + 4f;
    }

    private static float DrawWeaponPicker(Rect rect, float y, float lineH, SerializedProperty weaponProp)
    {
        SerializedProperty typeProp = weaponProp.FindPropertyRelative("type");
        SerializedProperty indexProp = weaponProp.FindPropertyRelative("index");

        WeaponType weaponType = (WeaponType)typeProp.enumValueIndex;
        if (indexProp.intValue < 0 || weaponType == WeaponType.None)
            weaponType = WeaponType.Projectile;

        WeaponType nextType = (WeaponType)EditorGUI.EnumPopup(
            new Rect(rect.x, y, rect.width, lineH),
            "Weapon Type",
            weaponType == WeaponType.None ? WeaponType.Projectile : weaponType);
        y += lineH + 4f;

        typeProp.enumValueIndex = (int)nextType;
        if (indexProp.intValue < 0)
            indexProp.intValue = 0;

        WeaponDatabase weaponDatabase = AssetDatabase.LoadAssetAtPath<WeaponDatabase>(WeaponDatabasePath);
        if (weaponDatabase == null)
        {
            EditorGUI.PropertyField(
                new Rect(rect.x, y, rect.width, lineH),
                indexProp,
                new GUIContent("Weapon Index"));
            return y + lineH + 4f;
        }

        string[] weaponNames = GetWeaponNames(weaponDatabase, nextType);
        if (weaponNames.Length == 0)
        {
            EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineH), "No weapons in that list.");
            return y + lineH + 4f;
        }

        int weaponIndex = Mathf.Clamp(indexProp.intValue, 0, weaponNames.Length - 1);
        int selectedWeapon = EditorGUI.Popup(
            new Rect(rect.x, y, rect.width, lineH),
            "Weapon",
            weaponIndex,
            weaponNames);
        indexProp.intValue = selectedWeapon;
        return y + lineH + 4f;
    }

    private static string[] BuildPlayerNames(PlayerDatabase database)
    {
        var characters = database.Characters;
        var names = new string[characters.Count];
        for (int i = 0; i < characters.Count; i++)
        {
            PlayerCharacterDefinition character = characters[i];
            names[i] = character != null && !string.IsNullOrWhiteSpace(character.CharacterName)
                ? character.CharacterName
                : $"Character {i + 1}";
        }

        return names;
    }

    private static string[] GetWeaponNames(WeaponDatabase database, WeaponType type) =>
        type switch
        {
            WeaponType.Projectile => BuildNames(database.ProjectileWeapons, weapon => weapon.WeaponName),
            WeaponType.Melee => BuildNames(database.MeleeWeapons, weapon => weapon.WeaponName),
            WeaponType.Summon => BuildNames(database.SummonWeapons, weapon => weapon.WeaponName),
            _ => System.Array.Empty<string>()
        };

    private static string[] BuildNames<T>(
        System.Collections.Generic.IReadOnlyList<T> items,
        System.Func<T, string> getName)
    {
        if (items == null || items.Count == 0)
            return System.Array.Empty<string>();

        var names = new string[items.Count];
        for (int i = 0; i < items.Count; i++)
            names[i] = string.IsNullOrWhiteSpace(getName(items[i])) ? $"Entry {i + 1}" : getName(items[i]);

        return names;
    }

    private static void DrawSpawnPlacementSection(SerializedProperty spawnPlacementProp)
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
        if (gameManager != null && gameManager.WaveDatabase != null)
            return gameManager.WaveDatabase;

        return WaveCatalog.Database;
    }
}
#endif