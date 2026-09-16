using System.Collections.Generic;
using UnityEngine;

public static class PoisonTracker
{
    private static readonly Dictionary<int, HashSet<Enemy>> PoisonsByWeapon = new();

    public static int GetActiveCount(int weaponId)
    {
        if (weaponId == 0 || !PoisonsByWeapon.TryGetValue(weaponId, out HashSet<Enemy> poisons))
            return 0;

        CleanupPoisons(poisons, weaponId);
        return poisons.Count;
    }

    public static int GetAvailableSlots(int weaponId, int maxPoisonCount) =>
        Mathf.Max(0, maxPoisonCount - GetActiveCount(weaponId));

    public static bool TryRegister(Enemy enemy, WeaponCombatContext context)
    {
        if (enemy == null || context.sourceWeaponId == 0)
            return false;

        PoisonElementStats stats = context.poison ?? new PoisonElementStats();
        stats.Normalize();

        int weaponId = context.sourceWeaponId;
        int maxPoisonCount = stats.maxPoisonCount;

        if (!PoisonsByWeapon.TryGetValue(weaponId, out HashSet<Enemy> poisons))
        {
            poisons = new HashSet<Enemy>();
            PoisonsByWeapon[weaponId] = poisons;
        }

        CleanupPoisons(poisons, weaponId);

        if (poisons.Contains(enemy))
            return true;

        if (poisons.Count >= maxPoisonCount)
            return false;

        poisons.Add(enemy);
        return true;
    }

    public static void Unregister(Enemy enemy, int weaponId)
    {
        if (enemy == null || weaponId == 0 || !PoisonsByWeapon.TryGetValue(weaponId, out HashSet<Enemy> poisons))
            return;

        poisons.Remove(enemy);
    }

    private static void CleanupPoisons(HashSet<Enemy> poisons, int weaponId)
    {
        var stale = new List<Enemy>();

        foreach (Enemy enemy in poisons)
        {
            if (enemy == null || !enemy.IsPoisonedFromWeapon(weaponId))
                stale.Add(enemy);
        }

        for (int i = 0; i < stale.Count; i++)
            poisons.Remove(stale[i]);
    }
}