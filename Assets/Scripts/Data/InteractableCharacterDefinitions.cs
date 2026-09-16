using System;
using UnityEngine;

public enum InteractableRepeatPolicy
{
    [Tooltip("Never stops — barkeeps and vendors that stay open.")]
    Always,

    [Tooltip("Stops after this many interactions (opens/talks).")]
    MaxInteractions,

    [Tooltip("Stops after this many shop purchases from this character.")]
    UntilPurchases
}

[Serializable]
public class InteractableInteractionRules
{
    public InteractableRepeatPolicy repeatPolicy = InteractableRepeatPolicy.Always;

    [Min(1)]
    public int maxInteractions = 3;

    [Min(1)]
    public int maxPurchases = 1;
}

[Serializable]
public class InteractableCharacterDefinition
{
    [Tooltip("Stable id used for lookups (e.g. shopkeeper).")]
    public string characterId = "new_character";

    public string displayName = "Shopkeeper";
    public Sprite sprite;
    public string interactVerb = "Talk";
    public InteractableCharacterKind characterKind = InteractableCharacterKind.Shop;

    [Header("Spawn Placement")]
    public bool useSpawnPlacement = true;
    public InteractableCharacterSpawnPlacement spawnPlacement = new();
    public bool hideWhenWrongMap = true;

    [Header("Interaction")]
    public float interactRadius = 1.35f;
    public bool startsEnabled = true;
    public bool facePlayerOnInteract = true;
    public InteractableInteractionRules interactionRules = new();

    [Header("Shop")]
    public ShopOffer[] shopOffers = System.Array.Empty<ShopOffer>();

    public string ResolvedName =>
        string.IsNullOrWhiteSpace(displayName) ? characterId : displayName;

    public InteractableCharacterSpawnPlacement CloneSpawnPlacement()
    {
        InteractableCharacterSpawnPlacement source = spawnPlacement ?? new InteractableCharacterSpawnPlacement();
        return new InteractableCharacterSpawnPlacement
        {
            environmentName = source.environmentName,
            environmentIndex = source.environmentIndex,
            worldPosition = source.worldPosition,
            requireConditionArea = source.requireConditionArea,
            conditionAreaOffset = source.conditionAreaOffset,
            conditionAreaSize = source.conditionAreaSize
        };
    }
}