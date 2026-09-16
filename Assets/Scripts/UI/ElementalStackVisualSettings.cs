using System;

public static class ElementalStackVisualSettings
{
    public static bool ShowStackVisuals { get; private set; } = true;

    public static event Action<bool> Changed;

    public static void SetEnabled(bool enabled)
    {
        if (ShowStackVisuals == enabled)
            return;

        ShowStackVisuals = enabled;
        Changed?.Invoke(enabled);
    }

    public static void Toggle() => SetEnabled(!ShowStackVisuals);
}