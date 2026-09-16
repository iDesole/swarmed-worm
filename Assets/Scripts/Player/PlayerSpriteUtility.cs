using UnityEngine;

public static class PlayerSpriteUtility
{
    public static void ConfigureRenderer(SpriteRenderer renderer, Sprite sprite, int sortingOrder = 0)
    {
        if (renderer == null)
            return;

        Sprite resolvedSprite = ResolveSprite(sprite);
        if (resolvedSprite != null)
            renderer.sprite = resolvedSprite;

        RenderVisibilityUtility.ConfigureSpriteRenderer(renderer, sortingOrder);
    }

    public static Sprite ResolveSprite(Sprite assignedSprite)
    {
        if (assignedSprite != null)
            return assignedSprite;

        return RenderVisibilityUtility.LoadDefaultPlayerSprite();
    }
}