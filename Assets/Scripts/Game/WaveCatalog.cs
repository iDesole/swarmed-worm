using UnityEngine;

/// <summary>Runtime accessor for <see cref="WaveDatabase"/> (environments, waves, spawn tables).</summary>
public static class WaveCatalog
{
    private static WaveDatabase database;

    public static WaveDatabase Database =>
        CatalogResolver.Resolve(ref database, nameof(WaveDatabase));

    public static void Register(WaveDatabase waveDatabase)
    {
        if (waveDatabase != null)
            database = waveDatabase;
    }
}