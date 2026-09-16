using UnityEngine;

/// <summary>
/// Builds interactable NPCs from <see cref="InteractableCharacterDatabase"/> definitions.
/// </summary>
public static class InteractableCharacterFactory
{
    public static InteractableCharacter Create(InteractableCharacterDefinition definition, Transform parent = null)
    {
        if (definition == null)
            return null;

        string objectName = string.IsNullOrWhiteSpace(definition.ResolvedName)
            ? "Interactable Character"
            : definition.ResolvedName;

        var characterObject = new GameObject(objectName);
        characterObject.SetActive(false);

        if (parent != null)
            characterObject.transform.SetParent(parent, false);

        SpriteRenderer spriteRenderer = characterObject.AddComponent<SpriteRenderer>();
        RenderVisibilityUtility.ConfigureSpriteRenderer(spriteRenderer, sortingOrder: 0);
        if (definition.sprite != null)
            spriteRenderer.sprite = definition.sprite;

        characterObject.AddComponent<CircleCollider2D>();
        InteractableCharacter character = characterObject.AddComponent<InteractableCharacter>();
        character.ApplyDefinition(definition);
        characterObject.SetActive(true);
        return character;
    }
}