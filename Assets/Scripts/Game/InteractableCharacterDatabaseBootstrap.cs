using UnityEngine;

/// <summary>Registers <see cref="InteractableCharacterDatabase"/> with <see cref="InteractableCharacterCatalog"/> at scene start.</summary>
[DefaultExecutionOrder(-100)]
public class InteractableCharacterDatabaseBootstrap : MonoBehaviour, IGameDatabaseBootstrap<InteractableCharacterDatabase>
{
    [SerializeField] private InteractableCharacterDatabase database;

    public InteractableCharacterDatabase Database => database;

    private void Awake()
    {
        if (database != null)
            InteractableCharacterCatalog.ForceRegister(database);
    }
}