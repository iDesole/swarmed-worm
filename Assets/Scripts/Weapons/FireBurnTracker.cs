using System.Collections.Generic;
using UnityEngine;

public static class FireBurnTracker
{
    private static readonly Dictionary<int, HashSet<Enemy>> BurnsByWeapon = new();

    public static int GetActiveCount(int weaponId)
    {
        if (weaponId == 0 || !BurnsByWeapon.TryGetValue(weaponId, out HashSet<Enemy> burns))
            return 0;

        CleanupBurns(burns, weaponId);
        return burns.Count;
    }

    public static int GetAvailableSlots(int weaponId, int maxBurnCount) =>
        Mathf.Max(0, maxBurnCount - GetActiveCount(weaponId));

    public static bool TryRegister(Enemy enemy, WeaponCombatContext context)
    {
        if (enemy == null || context.sourceWeaponId == 0)
            return false;

        FireElementStats stats = context.fire ?? new FireElementStats();
        stats.Normalize();

        int weaponId = context.sourceWeaponId;
        int maxBurnCount = stats.maxBurnCount;

        if (!BurnsByWeapon.TryGetValue(weaponId, out HashSet<Enemy> burns))
        {
            burns = new HashSet<Enemy>();
            BurnsByWeapon[weaponId] = burns;
        }

        CleanupBurns(burns, weaponId);

        if (burns.Contains(enemy))
            return true;

        if (burns.Count >= maxBurnCount)
            return false;

        burns.Add(enemy);
        return true;
    }

    public static void Unregister(Enemy enemy, int weaponId)
    {
        if (enemy == null || weaponId == 0 || !BurnsByWeapon.TryGetValue(weaponId, out HashSet<Enemy> burns))
            return;

        burns.Remove(enemy);
    }

    private static void CleanupBurns(HashSet<Enemy> burns, int weaponId)
    {
        var stale = new List<Enemy>();

        foreach (Enemy enemy in burns)
        {
            if (enemy == null || !enemy.IsBurningFromWeapon(weaponId))
                stale.Add(enemy);
        }

        for (int i = 0; i < stale.Count; i++)
            burns.Remove(stale[i]);
    }
}