using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Game/Weapon Database")]
public class WeaponDatabase : ScriptableObject
{
    [SerializeField] private List<ProjectileWeaponDefinition> projectileWeapons = new();
    [SerializeField] private List<MeleeWeaponDefinition> meleeWeapons = new();
    [SerializeField] private List<SummonWeaponDefinition> summonWeapons = new();

    [Header("Shared Prefabs")]
    [SerializeField] private GameObject defaultProjectilePrefab;
    [SerializeField] private GameObject defaultMeleeHitboxPrefab;

    public IReadOnlyList<ProjectileWeaponDefinition> ProjectileWeapons => projectileWeapons;
    public IReadOnlyList<MeleeWeaponDefinition> MeleeWeapons => meleeWeapons;
    public IReadOnlyList<SummonWeaponDefinition> SummonWeapons => summonWeapons;

    public void ApplyProjectileFallbacks(ProjectileWeapon weapon)
    {
        if (weapon != null && weapon.projectilePrefab == null)
            weapon.projectilePrefab = defaultProjectilePrefab;
    }

    public void ApplyMeleeFallbacks(MeleeWeapon weapon)
    {
        if (weapon != null && weapon.meleeHitboxPrefab == null)
            weapon.meleeHitboxPrefab = defaultMeleeHitboxPrefab;
    }

    public Sprite GetWeaponSprite(WeaponSelection selection)
    {
        if (!selection.IsValid)
            return null;

        return selection.type switch
        {
            WeaponType.Projectile when InRange(projectileWeapons, selection.index) =>
                projectileWeapons[selection.index].core?.sprite,
            WeaponType.Melee when InRange(meleeWeapons, selection.index) =>
                meleeWeapons[selection.index].core?.sprite,
            WeaponType.Summon when InRange(summonWeapons, selection.index) =>
                summonWeapons[selection.index].core?.sprite,
            _ => null
        };
    }

    public string GetWeaponName(WeaponSelection selection)
    {
        if (!selection.IsValid)
            return string.Empty;

        return selection.type switch
        {
            WeaponType.Projectile when InRange(projectileWeapons, selection.index) =>
                projectileWeapons[selection.index].WeaponName,
            WeaponType.Melee when InRange(meleeWeapons, selection.index) =>
                meleeWeapons[selection.index].WeaponName,
            WeaponType.Summon when InRange(summonWeapons, selection.index) =>
                summonWeapons[selection.index].WeaponName,
            _ => string.Empty
        };
    }

    public bool TryGetWeaponCore(WeaponSelection selection, out WeaponCoreStats core)
    {
        core = null;
        if (!selection.IsValid)
            return false;

        core = selection.type switch
        {
            WeaponType.Projectile when InRange(projectileWeapons, selection.index) =>
                projectileWeapons[selection.index].core,
            WeaponType.Melee when InRange(meleeWeapons, selection.index) =>
                meleeWeapons[selection.index].core,
            WeaponType.Summon when InRange(summonWeapons, selection.index) =>
                summonWeapons[selection.index].core,
            _ => null
        };

        return core != null;
    }

    public WeaponElementProfile GetWeaponElementProfile(WeaponSelection selection) =>
        TryGetWeaponCore(selection, out WeaponCoreStats core) ? core.elementProfile : null;

    public WeaponSelection FindSelection(WeaponType type, string weaponName)
    {
        if (string.IsNullOrWhiteSpace(weaponName))
            return default;

        return type switch
        {
            WeaponType.Projectile => FindIndex(projectileWeapons, weaponName, WeaponSelection.Projectile),
            WeaponType.Melee => FindIndex(meleeWeapons, weaponName, WeaponSelection.Melee),
            WeaponType.Summon => FindIndex(summonWeapons, weaponName, WeaponSelection.Summon),
            _ => default
        };
    }

    public WeaponBase CreateWeapon(WeaponSelection selection, Transform parent) =>
        WeaponFactory.Create(selection, this, parent);

    private void OnEnable() => WeaponCatalog.Register(this);

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            return;
#endif

        projectileWeapons ??= new List<ProjectileWeaponDefinition>();
        meleeWeapons ??= new List<MeleeWeaponDefinition>();
        summonWeapons ??= new List<SummonWeaponDefinition>();

        foreach (ProjectileWeaponDefinition weapon in projectileWeapons)
            weapon?.core?.Normalize();

        foreach (MeleeWeaponDefinition weapon in meleeWeapons)
            weapon?.core?.Normalize();

        foreach (SummonWeaponDefinition weapon in summonWeapons)
            weapon?.core?.Normalize();
    }

    private static bool InRange<T>(IReadOnlyList<T> list, int index) =>
        list != null && index >= 0 && index < list.Count;

    private static WeaponSelection FindIndex<T>(
        IReadOnlyList<T> weapons,
        string weaponName,
        Func<int, WeaponSelection> makeSelection) where T : class
    {
        if (weapons == null)
            return default;

        for (int i = 0; i < weapons.Count; i++)
        {
            string name = weapons[i] switch
            {
                ProjectileWeaponDefinition projectile => projectile.WeaponName,
                MeleeWeaponDefinition melee => melee.WeaponName,
                SummonWeaponDefinition summon => summon.WeaponName,
                _ => null
            };

            if (string.Equals(name, weaponName, StringComparison.OrdinalIgnoreCase))
                return makeSelection(i);
        }

        return default;
    }
}