using UnityEngine;

/// <summary>
/// Moving hitbox spawned by <see cref="ProjectileWeapon"/>. Handles travel, collision, and elemental hits.
/// </summary>
/// <remarks>
/// Initialize() must be called once after Instantiate. Damage and element context come from the firing weapon.
/// </remarks>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Header("Behavior")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private bool destroyOnHit = true;
    [SerializeField] private bool penetrate;
    [SerializeField] private int penetrationCount = 1;
    [SerializeField] private int bounceCount;
    [SerializeField] private float homingStrength;

    [Header("Filtering")]
    [SerializeField] private LayerMask hitLayers = ~0;
    [SerializeField] private string ownerTag = "Player";

    [Header("Effects")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private bool rotateToVelocity = true;

    [Header("Aim Alignment")]
    [Tooltip("Nudge bullet sprite up/down perpendicular to travel.")]
    [Range(-1f, 1f)] public float visualVerticalOffset;
    [Tooltip("Rotate bullet sprite relative to travel (degrees).")]
    [Range(-45f, 45f)] public float visualPitchOffset;

    private float damage;
    private WeaponCombatContext combatContext;
    private Rigidbody2D rb;
    private int hitsRemaining;
    private bool hasInitialized;
    private bool explodeOnHit;
    private float explosionRadius;
    private float explosionInitialDamage;
    private float explosionRadiusDamage;
    private int explosionPulseCount;
    private float explosionPulseSpacing;
    private bool explosionTriggered;
    private Collider2D lastHitCollider;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        var col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    public void Initialize(
        Vector2 direction,
        float weaponDamage,
        float? overrideSpeed = null,
        float? overrideLifetime = null,
        GameObject owner = null,
        int pierce = 0,
        int bounces = 0,
        float homing = 0f,
        WeaponCombatContext? combatContextOverride = null,
        float weaponVerticalOffset = 0f,
        float weaponPitchOffset = 0f,
        bool weaponExplodeOnHit = false,
        float weaponExplosionRadius = 2f,
        float weaponExplosionInitialDamage = 10f,
        float weaponExplosionRadiusDamage = 5f,
        int weaponExplosionPulseCount = 3,
        float weaponExplosionPulseSpacing = 0.75f,
        float gravityScale = 0f)
    {
        if (hasInitialized) return;
        hasInitialized = true;

        Vector2 travelDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

        explodeOnHit = weaponExplodeOnHit;
        explosionRadius = weaponExplosionRadius;
        explosionInitialDamage = weaponExplosionInitialDamage;
        explosionRadiusDamage = weaponExplosionRadiusDamage;
        explosionPulseCount = weaponExplosionPulseCount;
        explosionPulseSpacing = weaponExplosionPulseSpacing;

        damage = explodeOnHit && explosionInitialDamage > 0f
            ? explosionInitialDamage
            : weaponDamage;
        combatContext = combatContextOverride ?? default;

        if (overrideSpeed.HasValue) speed = overrideSpeed.Value;
        if (overrideLifetime.HasValue) lifetime = overrideLifetime.Value;

        if (pierce > 0)
        {
            penetrate = true;
            penetrationCount = pierce;
        }

        bounceCount = bounces;
        homingStrength = homing;

        if (owner != null)
            ownerTag = owner.tag;

        hitsRemaining = penetrate ? penetrationCount : 1;
        rb.gravityScale = gravityScale;
        rb.linearVelocity = travelDirection * speed;
        ApplyVisualAlignment(travelDirection, weaponVerticalOffset, weaponPitchOffset);

        Destroy(gameObject, lifetime);
    }

    private void ApplyVisualAlignment(Vector2 travelDirection, float weaponVerticalOffset, float weaponPitchOffset)
    {
        float totalVerticalOffset = visualVerticalOffset + weaponVerticalOffset;
        float totalPitchOffset = visualPitchOffset + weaponPitchOffset;

        if (Mathf.Abs(totalVerticalOffset) > 0.0001f)
        {
            Vector2 perpendicular = new Vector2(-travelDirection.y, travelDirection.x);
            transform.position += (Vector3)perpendicular * totalVerticalOffset;
        }

        if (!rotateToVelocity || travelDirection == Vector2.zero)
            return;

        float angle = Mathf.Atan2(travelDirection.y, travelDirection.x) * Mathf.Rad2Deg + totalPitchOffset;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(ownerTag)) return;
        if (other.CompareTag("Projectile")) return;

        if (LayerMask.LayerToName(other.gameObject.layer) == "Player" &&
            LayerMask.LayerToName(gameObject.layer) == "Enemy")
            return;

        if (((1 << other.gameObject.layer) & hitLayers) == 0) return;

        if (other.GetComponent<WaveTarget>() is WaveTarget target)
            target.TakeDamage(damage);
        else if (other.GetComponent<Hostage>() is Hostage hostage)
            hostage.TakeDamage(damage);
        else
            other.GetComponent<IDamageable>()?.TakeDamage(damage);

        ElementalCombatSystem.ApplyHitEffects(
            combatContext,
            other,
            damage,
            ElementalHitDelivery.Projectile);

        lastHitCollider = other;

        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

        hitsRemaining--;

        if (destroyOnHit && hitsRemaining <= 0)
            DestroyProjectile();
    }

    private void OnDestroy()
    {
        if (!hasInitialized || explosionTriggered)
            return;

        TriggerExplosion();
    }

    private void DestroyProjectile()
    {
        TriggerExplosion();
        Destroy(gameObject);
    }

    private void TriggerExplosion()
    {
        if (explosionTriggered || !explodeOnHit)
            return;

        explosionTriggered = true;
        ProjectileExplosionSystem.Trigger(
            transform.position,
            explodeOnHit,
            explosionRadius,
            explosionInitialDamage,
            explosionRadiusDamage,
            explosionPulseCount,
            explosionPulseSpacing,
            combatContext,
            ownerTag,
            hitLayers,
            lastHitCollider);
    }

    private void OnCollisionEnter2D(Collision2D collision) =>
        OnTriggerEnter2D(collision.collider);
}