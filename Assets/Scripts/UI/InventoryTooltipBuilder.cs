using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Builds tooltip text for inventory slots (weapons pull live stats from WeaponDatabase).
/// </summary>
public static class InventoryTooltipBuilder
{
    public static bool TryBuild(InventorySlotData slot, out string title, out string subtitle, out string body)
    {
        subtitle = string.Empty;

        if (!TryBuildColumns(slot, out title, out subtitle, out string bodyLeft, out string bodyRight))
        {
            body = string.Empty;
            return false;
        }

        body = string.IsNullOrEmpty(bodyRight)
            ? bodyLeft
            : $"{bodyLeft}\n\n{bodyRight}";
        return true;
    }

    public static bool TryBuildColumns(InventorySlotData slot, out string title, out string subtitle, out string bodyLeft, out string bodyRight)
    {
        title = string.Empty;
        subtitle = string.Empty;
        bodyLeft = string.Empty;
        bodyRight = string.Empty;

        if (slot.IsEmpty)
            return false;

        switch (slot.kind)
        {
            case InventoryItemKind.Weapon:
                return TryBuildWeaponColumns(slot, out title, out subtitle, out bodyLeft, out bodyRight);

            case InventoryItemKind.Artifact:
                title = ResolveTitle(slot, "Artifact");
                subtitle = "Artifact";
                bodyLeft = BuildGenericBody(slot, "Passive relic effect.");
                return true;

            case InventoryItemKind.Currency:
                title = ResolveTitle(slot, "Currency");
                subtitle = "Currency";
                bodyLeft = BuildGenericBody(slot, "Spendable currency.");
                return true;

            default:
                title = ResolveTitle(slot, "Item");
                subtitle = "Item";
                bodyLeft = BuildGenericBody(slot, "Consumable or material.");
                return true;
        }
    }

    private static bool TryBuildWeaponColumns(InventorySlotData slot, out string title, out string subtitle,
        out string bodyLeft, out string bodyRight)
    {
        title = ResolveTitle(slot, "Weapon");
        subtitle = string.Empty;
        bodyLeft = string.Empty;
        bodyRight = string.Empty;

        if (!slot.weapon.IsValid)
        {
            subtitle = "Unknown";
            bodyLeft = "Unknown weapon.";
            return true;
        }

        WeaponDatabase database = WeaponCatalog.Database;
        if (database == null)
        {
            subtitle = slot.weapon.type.ToString();
            bodyLeft = "Weapon database unavailable.";
            return true;
        }

        title = database.GetWeaponName(slot.weapon);
        if (string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(slot.displayName))
            title = slot.displayName;

        subtitle = ResolveWeaponSubtitle(database, slot.weapon);

        var lines = new List<string>();
        AppendWeaponStats(database, slot.weapon, lines);
        SplitLinesIntoColumns(lines, out bodyLeft, out bodyRight);
        return true;
    }

    private static string ResolveWeaponSubtitle(WeaponDatabase database, WeaponSelection selection)
    {
        WeaponElementProfile profile = database.GetWeaponElementProfile(selection);
        string elements = WeaponElementColors.FormatRichTextElements(profile);
        if (!string.IsNullOrEmpty(elements))
            return elements;

        return selection.type.ToString();
    }

    private static void SplitLinesIntoColumns(List<string> lines, out string left, out string right)
    {
        if (lines == null || lines.Count == 0)
        {
            left = string.Empty;
            right = string.Empty;
            return;
        }

        int splitIndex = (lines.Count + 1) / 2;
        left = string.Join("\n", lines.GetRange(0, splitIndex));
        right = splitIndex < lines.Count
            ? string.Join("\n", lines.GetRange(splitIndex, lines.Count - splitIndex))
            : string.Empty;
    }

    private static void AppendWeaponStats(WeaponDatabase database, WeaponSelection selection, List<string> lines)
    {
        switch (selection.type)
        {
            case WeaponType.Projectile when selection.index < database.ProjectileWeapons.Count:
                AppendCoreStats(database.ProjectileWeapons[selection.index].core, lines);
                ProjectileWeaponDefinition projectile = database.ProjectileWeapons[selection.index];
                lines.Add($"Projectiles: {projectile.projectileCount}");
                lines.Add($"Projectile Speed: {Format(projectile.projectileSpeed)}");
                if (projectile.spreadAngle > 0f)
                    lines.Add($"Spread: {Format(projectile.spreadAngle)}°");
                if (projectile.pierceCount > 0)
                    lines.Add($"Pierce: {projectile.pierceCount}");
                if (projectile.bounceCount > 0)
                    lines.Add($"Bounce: {projectile.bounceCount}");
                if (projectile.homingStrength > 0f)
                    lines.Add($"Homing: {FormatPercent(projectile.homingStrength)}");
                if (Mathf.Abs(projectile.projectileGravityScale) > 0.001f)
                    lines.Add($"Gravity: {Format(projectile.projectileGravityScale)}");
                if (projectile.explodeOnHit)
                {
                    lines.Add($"Explosion Radius: {Format(projectile.explosionRadius)}");
                    lines.Add($"Explosion Initial: {Format(projectile.explosionInitialDamage)}");
                    lines.Add($"Explosion Pulse: {Format(projectile.explosionRadiusDamage)}");
                    lines.Add($"Pulses: {projectile.explosionPulseCount}");
                }
                break;

            case WeaponType.Melee when selection.index < database.MeleeWeapons.Count:
                AppendCoreStats(database.MeleeWeapons[selection.index].core, lines);
                MeleeWeaponDefinition melee = database.MeleeWeapons[selection.index];
                lines.Add($"Swing Radius: {Format(melee.swingRadius)}");
                lines.Add($"Swing Arc: {Format(melee.swingArcAngle)}°");
                lines.Add($"Swings / Attack: {melee.swingsPerAttack}");
                if (melee.lifesteal > 0f)
                    lines.Add($"Lifesteal: {FormatPercent(melee.lifesteal)}");
                break;

            case WeaponType.Summon when selection.index < database.SummonWeapons.Count:
                AppendCoreStats(database.SummonWeapons[selection.index].core, lines);
                SummonWeaponDefinition summon = database.SummonWeapons[selection.index];
                lines.Add($"Summons / Cast: {summon.summonsPerCast}");
                lines.Add($"Max Active: {summon.maxActiveSummons}");
                lines.Add($"Summon Lifetime: {Format(summon.summonLifetime)}s");
                lines.Add($"Summon Range: {Format(summon.summonRange)}");
                lines.Add($"Summon Damage: x{Format(summon.summonDamageMultiplier)}");
                break;
        }
    }

    private static void AppendCoreStats(WeaponCoreStats core, List<string> lines)
    {
        if (core == null)
            return;

        lines.Add($"Damage: {Format(core.damage)}");
        lines.Add($"Attack Speed: {Format(core.attackSpeed)}/s");
        lines.Add($"Range: {Format(core.range)}");
        lines.Add($"Crit Chance: {FormatPercent(core.critChance)}");
        lines.Add($"Crit Damage: x{Format(core.critMultiplier)}");
        lines.Add($"Knockback: {Format(core.knockback)}");
    }

    private static string BuildGenericBody(InventorySlotData slot, string description)
    {
        var builder = new StringBuilder();
        builder.AppendLine(description);

        if (!string.IsNullOrWhiteSpace(slot.itemId))
            builder.AppendLine($"ID: {slot.itemId}");

        if (slot.quantity > 1)
            builder.AppendLine($"Quantity: {slot.quantity}");

        return builder.ToString().TrimEnd();
    }

    private static string ResolveTitle(InventorySlotData slot, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(slot.displayName))
            return slot.displayName;

        if (!string.IsNullOrWhiteSpace(slot.itemId))
            return slot.itemId;

        return fallback;
    }

    private static string Format(float value) => value.ToString("0.##");

    private static string FormatPercent(float value) => $"{(value * 100f):0.#}%";
}