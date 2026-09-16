using UnityEngine;

/// <summary>
/// Scene component that registers the WeaponDatabase with <see cref="WeaponCatalog"/> on Awake.
/// </summary>
/// <remarks>
/// Same pattern as Player/Wave/Enemy database bootstraps. Place on a persistent scene object.
/// </remarks>
[DefaultExecutionOrder(-100)]
public class WeaponDatabaseBootstrap : MonoBehaviour, IGameDatabaseBootstrap<WeaponDatabase>
{
    [SerializeField] private WeaponDatabase database;

    public WeaponDatabase Database => database;

    private void Awake()
    {
        if (database != null)
            WeaponCatalog.Register(database);
    }
}