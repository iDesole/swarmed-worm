using UnityEngine;

/// <summary>Registers <see cref="EnemyDatabase"/> with <see cref="EnemyCatalog"/> at scene start.</summary>
[DefaultExecutionOrder(-100)]
public class EnemyDatabaseBootstrap : MonoBehaviour, IGameDatabaseBootstrap<EnemyDatabase>
{
    [SerializeField] private EnemyDatabase database;

    public EnemyDatabase Database => database;

    private void Awake()
    {
        if (database != null)
            EnemyCatalog.Register(database);
    }
}