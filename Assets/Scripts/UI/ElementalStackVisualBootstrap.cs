using UnityEngine;

/// <summary>
/// Applies the default on/off state for elemental stack icons above enemies.
/// </summary>
/// <remarks>
/// In editor/dev builds, <see cref="ElementalStackVisualDebug"/> adds an F6 toggle HUD.
/// </remarks>
public class ElementalStackVisualBootstrap : MonoBehaviour
{
    [SerializeField] private bool showStackVisuals = true;

    private void Awake() =>
        ElementalStackVisualSettings.SetEnabled(showStackVisuals);
}