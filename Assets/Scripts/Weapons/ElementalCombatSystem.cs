using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies elemental status effects when a weapon hits an <see cref="Enemy"/>.
/// </summary>
/// <remarks>
/// Called from projectiles, melee hitboxes, and other damage sources via <see cref="WeaponCombatContext"/>.
/// Each element (fire, ice, poison, electric, void) has its own handler and stack/spread rules.
/// </remarks>
public static class ElementalCombatSystem
{
    /// <summary>Entry point: runs every element present on the attacking weapon.</summary>
    public static void ApplyHitEffects(
        WeaponCombatContext context,
        Collider2D hitCollider,
        float baseDamage,
        ElementalHitDelivery delivery = ElementalHitDelivery.Melee)
    {
        if (hitCollider == null || context.elements == WeaponElement.None)
            return;

        Enemy enemy = hitCollider.GetComponent<Enemy>();
        if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.IsBurrowed)
            return;

        if (context.HasElement(WeaponElement.Electric))
            ApplyElectricChain(context, enemy, baseDamage);

        if (context.HasElement(WeaponElement.Fire))
            ApplyFire(context, enemy);

        if (context.HasElement(WeaponElement.Ice))
            ApplyIce(context, enemy, delivery);

        if (context.HasElement(WeaponElement.Poison))
            ApplyPoison(context, enemy);

        if (context.HasElement(WeaponElement.Void))
            VoidBlackHoleSystem.ApplyVoidHit(enemy, context);
    }

    private static void ApplyPoison(WeaponCombatContext context, Enemy primary)
    {
        PoisonElementStats stats = context.poison ?? new PoisonElementStats();
        stats.Normalize();

        primary.ApplyPoisonWindow(stats, context);

        if (!primary.IsPoisoned)
            return;

        primary.ApplyPoisonStacks(stats);

        if (primary.PoisonStacks >= stats.stacksToSpread)
            TriggerPoisonSpread(primary, stats, context);
    }

    private static void TriggerPoisonSpread(Enemy enemy, PoisonElementStats stats, WeaponCombatContext context)
    {
        if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.IsBurrowed)
            return;

        SpreadPoison(enemy.transform.position, stats, context, enemy);
        enemy.ClearAllPoison();
    }

    private static void SpreadPoison(
        Vector3 origin,
        PoisonElementStats stats,
        WeaponCombatContext context,
        Enemy exclude)
    {
        int availableSlots = PoisonTracker.GetAvailableSlots(context.sourceWeaponId, stats.maxPoisonCount);
        if (availableSlots <= 0)
            return;

        float spreadRadius = stats.spreadRadius;
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, spreadRadius);
        var candidates = new List<(Enemy enemy, float distance)>(hits.Length);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
                continue;

            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy == null || enemy == exclude || !enemy.gameObject.activeInHierarchy || enemy.IsBurrowed)
                continue;

            float distance = Vector2.Distance(origin, enemy.transform.position);
            if (distance > spreadRadius)
                continue;

            candidates.Add((enemy, distance));
        }

        candidates.Sort((a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < candidates.Count && availableSlots > 0; i++)
        {
            Enemy spreadTarget = candidates[i].enemy;
            bool alreadyPoisoned = spreadTarget.IsPoisonedFromWeapon(context.sourceWeaponId);
            spreadTarget.ApplyPoisonWindow(stats, context);

            if (!alreadyPoisoned)
                availableSlots--;
        }
    }

    private static void ApplyIce(
        WeaponCombatContext context,
        Enemy primary,
        ElementalHitDelivery delivery)
    {
        IceElementStats stats = context.ice ?? new IceElementStats();
        stats.Normalize();

        if (primary.IsFrozen)
        {
            if (delivery == ElementalHitDelivery.Projectile)
                Shatter(primary, stats, context);
            return;
        }

        primary.ApplySlowWindow(stats);
        if (primary.IsSlowed)
            primary.ApplySlowStacks(stats);
    }

    private static void Shatter(Enemy enemy, IceElementStats stats, WeaponCombatContext context)
    {
        if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.IsBurrowed || !enemy.IsFrozen)
            return;

        float shatterDamage = context.GetCritDamage();
        if (shatterDamage > 0f)
            enemy.TakeDamage(shatterDamage);

        SpreadIceSlow(enemy.transform.position, stats, enemy);
        enemy.ClearIce();
    }

    private static void SpreadIceSlow(Vector3 origin, IceElementStats stats, Enemy exclude)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, stats.spreadRadius);
        var candidates = new List<(Enemy enemy, float distance)>(hits.Length);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
                continue;

            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy == null || enemy == exclude || !enemy.gameObject.activeInHierarchy || enemy.IsBurrowed)
                continue;

            float distance = Vector2.Distance(origin, enemy.transform.position);
            if (distance > stats.spreadRadius)
                continue;

            candidates.Add((enemy, distance));
        }

        candidates.Sort((a, b) => a.distance.CompareTo(b.distance));

        int spreadCount = 0;
        for (int i = 0; i < candidates.Count && spreadCount < stats.maxSpreadTargets; i++)
        {
            Enemy spreadTarget = candidates[i].enemy;
            spreadTarget.ApplySlowWindow(stats);
            if (spreadTarget.IsSlowed)
                spreadTarget.ApplySlowStacks(stats, stats.spreadSlowStacks);
            spreadCount++;
        }
    }

    public static void ProcessBurnTick(
        Enemy enemy,
        float damagePerTick,
        FireElementStats stats,
        WeaponCombatContext context)
    {
        if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.IsBurrowed || !enemy.HasBurnDot)
            return;

        if (damagePerTick > 0f)
            enemy.TakeDamage(damagePerTick);
    }

    private static void ApplyFire(WeaponCombatContext context, Enemy primary)
    {
        FireElementStats stats = context.fire ?? new FireElementStats();
        stats.Normalize();

        primary.ApplyFireWindow(stats);
        TryApplyBurn(primary, stats, context);

        if (!primary.IsBurning)
            return;

        primary.ApplyBurnStacks(stats);

        if (primary.BurnStacks >= stats.stacksToIncinerate)
            TriggerIncinerate(primary, stats, context);
    }

    private static bool TryApplyBurn(Enemy enemy, FireElementStats stats, WeaponCombatContext context)
    {
        if (enemy == null)
            return false;

        return enemy.ApplyBurn(stats, context);
    }

    private static void TriggerIncinerate(Enemy enemy, FireElementStats stats, WeaponCombatContext context)
    {
        if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.IsBurrowed)
            return;

        float incinerateDamage = context.GetCritDamage();
        if (incinerateDamage > 0f)
            enemy.TakeDamage(incinerateDamage);

        SpreadBurn(enemy.transform.position, stats, context, enemy);
        enemy.ClearAllFire();
    }

    private static void SpreadBurn(
        Vector3 origin,
        FireElementStats stats,
        WeaponCombatContext context,
        Enemy exclude)
    {
        int availableSlots = FireBurnTracker.GetAvailableSlots(context.sourceWeaponId, stats.maxBurnCount);
        if (availableSlots <= 0)
            return;

        float spreadRadius = stats.spreadRadius;
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, spreadRadius);
        var candidates = new List<(Enemy enemy, float distance)>(hits.Length);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
                continue;

            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy == null || enemy == exclude || !enemy.gameObject.activeInHierarchy || enemy.IsBurrowed)
                continue;

            float distance = Vector2.Distance(origin, enemy.transform.position);
            if (distance > spreadRadius)
                continue;

            candidates.Add((enemy, distance));
        }

        candidates.Sort((a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < candidates.Count && availableSlots > 0; i++)
        {
            if (!TryApplyBurn(candidates[i].enemy, stats, context))
                continue;

            availableSlots--;
        }
    }

    private static void ApplyElectricChain(WeaponCombatContext context, Enemy primary, float baseDamage)
    {
        ElectricElementStats stats = context.electric ?? new ElectricElementStats();
        stats.Normalize();

        var visited = new HashSet<Enemy> { primary };
        ApplyElectricHit(primary, stats, context);

        Enemy current = primary;
        float shockDamage = Mathf.Max(0f, baseDamage * stats.shockDamageMultiplier);

        for (int chainIndex = 0; chainIndex < stats.maxChainTargets; chainIndex++)
        {
            Enemy next = FindNearestChainTarget(current.transform.position, stats.chainRange, visited);
            if (next == null)
                break;

            visited.Add(next);

            if (shockDamage > 0f)
                next.TakeDamage(shockDamage);

            ApplyElectricHit(next, stats, context);

            shockDamage *= 1f - stats.chainDamageFalloff;
            current = next;
        }
    }

    private static void ApplyElectricHit(Enemy enemy, ElectricElementStats stats, WeaponCombatContext context)
    {
        if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.IsBurrowed)
            return;

        enemy.ApplyElectrocute(stats);
        if (!enemy.IsElectrocuted)
            return;

        enemy.ApplyShockStacks(stats);

        if (enemy.ShockStacks >= stats.stacksToJolt)
            TriggerJolt(enemy, stats, context);
    }

    private static void TriggerJolt(Enemy enemy, ElectricElementStats stats, WeaponCombatContext context)
    {
        if (enemy == null || stats.joltDuration <= 0f)
            return;

        float joltDamage = context.GetCritDamage();
        if (joltDamage > 0f)
            enemy.TakeDamage(joltDamage);

        enemy.ApplyJolt(stats.joltDuration);
        enemy.ClearElectrocute();
    }

    private static Enemy FindNearestChainTarget(Vector3 origin, float range, HashSet<Enemy> exclude)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range);
        Enemy best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
                continue;

            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.IsBurrowed)
                continue;

            if (exclude.Contains(enemy))
                continue;

            float distance = Vector2.Distance(origin, enemy.transform.position);
            if (distance > range || distance >= bestDistance)
                continue;

            bestDistance = distance;
            best = enemy;
        }

        return best;
    }
}