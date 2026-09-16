/// <summary>
/// World object the player can focus and activate with the interact key.
/// </summary>
public interface IInteractable
{
    string DisplayName { get; }
    string InteractPrompt { get; }
    bool CanInteract(Player player);
    void Interact(Player player);
}