using System;

/// <summary>
/// Fired when the active map/environment changes (lobby, beach, etc.).
/// </summary>
public static class EnvironmentEvents
{
    public static event Action<WaveEnvironmentDefinition> EnvironmentChanged;

    public static void NotifyEnvironmentChanged(WaveEnvironmentDefinition environment) =>
        EnvironmentChanged?.Invoke(environment);
}