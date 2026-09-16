using System.Collections.Generic;
using UnityEngine;

public static class VoidTracker
{
    private static readonly Dictionary<int, HashSet<Enemy>> VoidsByWeapon = new();

    public static int GetActiveCount(int weaponId)
    {
        if (weaponId == 0 || !VoidsByWeapon.TryGetValue(weaponId, out HashSet<Enemy> anchors))
            return 0;

        CleanupVoids(anchors, weaponId);
        return anchors.Count;
    }

    public static int GetAvailableSlots(int weaponId, int maxVoidCount) =>
        Mathf.Max(0, maxVoidCount - GetActiveCount(weaponId));

    public static bool TryRegister(Enemy anchor, int weaponId, int maxVoidCount)
    {
        if (anchor == null || weaponId == 0)
            return false;

        if (!VoidsByWeapon.TryGetValue(weaponId, out HashSet<Enemy> anchors))
        {
            anchors = new HashSet<Enemy>();
            VoidsByWeapon[weaponId] = anchors;
        }

        CleanupVoids(anchors, weaponId);

        if (anchors.Contains(anchor))
            return true;

        if (anchors.Count >= maxVoidCount)
            return false;

        anchors.Add(anchor);
        return true;
    }

    public static void Unregister(Enemy anchor, int weaponId)
    {
        if (anchor == null || weaponId == 0 || !VoidsByWeapon.TryGetValue(weaponId, out HashSet<Enemy> anchors))
            return;

        anchors.Remove(anchor);
    }

    private static void CleanupVoids(HashSet<Enemy> anchors, int weaponId)
    {
        var stale = new List<Enemy>();

        foreach (Enemy anchor in anchors)
        {
            if (anchor == null || !anchor.IsVoidAnchored || anchor.VoidAnchorWeaponId != weaponId)
                stale.Add(anchor);
        }

        for (int i = 0; i < stale.Count; i++)
            anchors.Remove(stale[i]);
    }
}