/// <summary>
/// Shared tuning values for character sprites, shadows, and locomotion presentation.
/// </summary>
public static class CharacterPresentationConstants
{
    public const float LocomotionSpeedThresholdSqr = 0.01f;
    public const float FacingVelocityThreshold = 0.05f;

    public const int CharacterSpriteSortingOrder = 1;
    public const int PlayerShadowSortingOrder = 0;
    public const int EnemyShadowSortingOrder = -1;

    public const float ShadowMinWidth = 0.18f;
    public const float ShadowMinHeight = 0.05f;
    public const float ShadowFallbackWidth = 0.55f;
    public const float ShadowFallbackHeight = 0.14f;
    public const float ShadowFallbackYOffset = -0.08f;
}