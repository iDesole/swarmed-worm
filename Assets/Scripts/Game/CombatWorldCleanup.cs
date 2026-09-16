using UnityEngine;

/// <summary>
/// Removes combat-session objects that are not parented to the active environment map.
/// </summary>
public static class CombatWorldCleanup
{
    public static void ClearRemnants()
    {
        DestroyAll<WorldLootPickup>();
        DestroyAll<Projectile>();
        DestroyAll<Enemy>();
        DamageNumberSpawner.Instance?.RecycleAll();
    }

    private static void DestroyAll<T>() where T : MonoBehaviour
    {
        T[] instances = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < instances.Length; i++)
        {
            T instance = instances[i];
            if (instance != null && instance.gameObject != null)
                Object.Destroy(instance.gameObject);
        }
    }
}