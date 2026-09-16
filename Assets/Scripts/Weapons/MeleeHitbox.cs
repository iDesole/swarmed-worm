using UnityEngine;

/// <summary>
/// Short-lived damage volume for melee swings. Follows aim direction around the player.
/// </summary>
/// <remarks>
/// Spawned by <see cref="MeleeWeapon"/>. Detaches from player in Start so parent scale flips do not invert aim.
/// Static helpers are also used by <see cref="AimUtility"/> for consistent mouse direction.
/// </remarks>
public class MeleeHitbox : MonoBehaviour
{
    public const float DefaultRangeReference = 12f;
    public const float DefaultColliderRadius = 1.2f;

    public float damage = 15f;
    public WeaponCombatContext combatContext;
    public GameObject owner;
    public Transform aimOrigin;
    public float followDistance = 1.3f;
    public float sizeScale = 1f;
    public bool attachToOwner = true;
    public float lifetime = 0.3f;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == owner) return;
        if (other.CompareTag(owner?.tag ?? "Player")) return;
        other.GetComponent<IDamageable>()?.TakeDamage(damage);
        ElementalCombatSystem.ApplyHitEffects(combatContext, other, damage);
    }

    private void Start()
    {
        // World-space follow avoids player scale flip inverting the click side.
        transform.SetParent(null, true);
    }

    private void Update()
    {
        if (attachToOwner)
            UpdateAttachedAim();

        if (lifetime <= 0f)
            return;

        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
            Destroy(gameObject);
    }

    private Transform GetAimOrigin()
    {
        if (aimOrigin != null)
            return aimOrigin;

        return owner != null ? owner.transform : null;
    }

    private void UpdateAttachedAim()
    {
        Transform origin = GetAimOrigin();
        if (origin == null)
            return;

        ApplyAimOffset(origin, followDistance, transform, sizeScale);
    }

    public static float RangeToSizeScale(float range) =>
        Mathf.Max(0.1f, range / DefaultRangeReference);

    public static float RangeToWorldRadius(float range) =>
        DefaultColliderRadius * RangeToSizeScale(range);

    public static void ApplyAimOffset(Transform origin, float distance, Transform target, float sizeScale = 1f)
    {
        if (origin == null || target == null)
            return;

        Vector2 originWorld = GetOriginWorldPosition(origin);
        Vector2 direction = GetAimDirection(origin);
        Vector2 worldPosition = originWorld + direction * distance;

        Rigidbody2D body = target.GetComponent<Rigidbody2D>();
        if (body != null)
            body.MovePosition(worldPosition);
        else
            target.position = worldPosition;

        target.rotation = Quaternion.identity;
        ApplySizeScale(target, sizeScale);
    }

    public static void ApplySizeScale(Transform target, float scale)
    {
        if (target == null)
            return;

        float clampedScale = Mathf.Max(0.1f, scale);
        target.localScale = Vector3.one * clampedScale;

        CircleCollider2D circle = target.GetComponent<CircleCollider2D>();
        if (circle != null)
            circle.radius = DefaultColliderRadius;
    }

    public static Vector2 GetOriginWorldPosition(Transform origin) =>
        AimUtility.GetOriginWorldPosition(origin);

    public static Vector2 GetAimDirection(Transform origin) =>
        AimUtility.GetDirection(origin, Vector2.right);
}