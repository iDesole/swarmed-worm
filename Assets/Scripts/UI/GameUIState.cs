/// <summary>
/// Shared UI state for overlays that pause gameplay and block world interaction.
/// </summary>
public static class GameUIState
{
    public static bool BlocksGameplay =>
        PlayerInventoryUI.BlocksCameraZoom || CharacterShopUI.IsOpen;
}