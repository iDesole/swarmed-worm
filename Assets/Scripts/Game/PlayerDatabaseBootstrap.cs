using UnityEngine;

/// <summary>Registers <see cref="PlayerDatabase"/> with <see cref="PlayerCatalog"/> at scene start.</summary>
[DefaultExecutionOrder(-100)]
public class PlayerDatabaseBootstrap : MonoBehaviour, IGameDatabaseBootstrap<PlayerDatabase>
{
    [SerializeField] private PlayerDatabase database;

    public PlayerDatabase Database => database;

    private void Awake()
    {
        if (database != null)
            PlayerCatalog.Register(database);
    }
}