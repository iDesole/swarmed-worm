using UnityEngine;

/// <summary>Registers <see cref="BossDatabase"/> with <see cref="BossCatalog"/> at scene start.</summary>
[DefaultExecutionOrder(-100)]
public class BossDatabaseBootstrap : MonoBehaviour, IGameDatabaseBootstrap<BossDatabase>
{
    [SerializeField] private BossDatabase database;

    public BossDatabase Database => database;

    private void Awake()
    {
        if (database != null)
            BossCatalog.Register(database);
    }
}