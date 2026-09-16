using UnityEngine;

/// <summary>
/// Base class for every equippable weapon (projectile, melee, summon).
/// </summary>
/// <remarks>
/// Pattern: <see cref="Fire"/> enforces rate-of-fire → <see cref="PerformAttack"/> does the weapon-specific work.
/// <see cref="OnEquipped"/> parents the weapon to a hand slot; <see cref="ApplyWeaponAim"/> runs each LateUpdate.
/// Stats are copied from <see cref="WeaponCoreStats"/> via <see cref="WeaponFactory"/>.
/// </remarks>
public abstract class WeaponBase : MonoBehaviour
{
    [Header("Core")]
    public string weaponName = "Weapon";
    [HideInInspector] public int handSlot = 1;
    [HideInInspector] public WeaponSelection sourceSelection;

    [Header("Stats")]
    public float damage = 10f;
    [SerializeField] private float fireRate = 2f;
    public float range = 12f;

    [Header("Combat Modifiers")]
    [Range(0f, 1f)] public float critChance = 0.05f;
    public float critMultiplier = 1.5f;
    public float knockback = 2f;

    [Header("Elements")]
    public WeaponElement elements = WeaponElement.None;
    public ElectricElementStats electricStats = new();
    public FireElementStats fireStats = new();
    public IceElementStats iceStats = new();
    public PoisonElementStats poisonStats = new();
    public VoidElementStats voidStats = new();

    public float AttackSpeed
    {
        get => fireRate;
        set => fireRate = Mathf.Max(0.01f, value);
    }

    [Header("Ownership")]
    public GameObject owner;

    [Header("Aim Alignment")]
    [Tooltip("Rotate weapon sprite relative to aim (degrees).")]
    [Range(-45f, 45f)] public float aimPitchOffset;

    protected float lastFireTime;

    /// <summary>Called while the player holds the shoot key. Respects attack speed cooldown.</summary>
    public virtual void Fire()
    {
        float effectiveFireRate = GetEffectiveFireRate();
        if (Time.time < lastFireTime + (1f / effectiveFireRate))
            return;

        lastFireTime = Time.time;
        PerformAttack();
    }

    protected float RollDamage(out bool isCrit)
    {
        isCrit = critChance > 0f && Random.value < critChance;
        return isCrit ? damage * critMultiplier : damage;
    }

    protected virtual float GetEffectiveFireRate()
    {
        var player = owner != null ? owner.GetComponent<Player>() : null;
        float multiplier = player != null ? player.GetAttackSpeedMultiplier() : 1f;
        return fireRate * multiplier;
    }

    /// <summary>Weapon-specific attack logic implemented by subclasses.</summary>
    protected abstract void PerformAttack();

    public WeaponCombatContext CreateCombatContext() => WeaponCombatContext.FromWeapon(this);

    public bool HasElement(WeaponElement element) =>
        element != WeaponElement.None && (elements & element) != 0;

    protected Vector2 GetFireDirection()
    {
        if (owner != null)
            return AimUtility.GetDirection(owner.transform, Vector2.right);

        return AimUtility.GetDirection(transform, Vector2.right);
    }

    private void LateUpdate()
    {
        if (owner == null)
            return;

        ApplyWeaponAim();
    }

    protected void ApplyWeaponAim()
    {
        bool facesLeft = owner != null &&
                         owner.TryGetComponent(out Player player) &&
                         player.WeaponFacesLeft;

        AimUtility.ApplyWeaponSpriteAim(
            transform,
            GetComponent<SpriteRenderer>(),
            GetFireDirection(),
            facesLeft,
            aimPitchOffset);
    }

    public virtual void OnEquipped(GameObject newOwner, int slotNumber1to5)
    {
        owner = newOwner;
        handSlot = slotNumber1to5;

        var slotManager = newOwner.GetComponent<WeaponSlotManager>();
        Transform targetParent;
        if (slotManager != null)
        {
            targetParent = slotManager.GetHandSlotTransform(slotNumber1to5);
        }
        else
        {
            Transform handSlotTransform = newOwner.transform.Find($"HandSlot{slotNumber1to5}");
            targetParent = handSlotTransform != null ? handSlotTransform : newOwner.transform;
        }

        transform.SetParent(targetParent);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        ApplyWeaponAim();
    }

    public virtual void OnUnequipped()
    {
        owner = null;
        AimUtility.ResetWeaponSpriteAim(transform, GetComponent<SpriteRenderer>());
    }
}