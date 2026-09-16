using UnityEngine;

/// <summary>Registers <see cref="WaveDatabase"/> with <see cref="WaveCatalog"/> at scene start.</summary>
[DefaultExecutionOrder(-100)]
public class WaveDatabaseBootstrap : MonoBehaviour, IGameDatabaseBootstrap<WaveDatabase>
{
    [SerializeField] private WaveDatabase database;

    public WaveDatabase Database => database;

    private void Awake()
    {
        if (database != null)
            WaveCatalog.Register(database);
    }
}