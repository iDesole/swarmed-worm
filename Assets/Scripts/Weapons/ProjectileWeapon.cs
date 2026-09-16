using UnityEngine;

/// <summary>
/// Ranged weapon that spawns <see cref="Projectile"/> instances toward the mouse aim direction.
/// </summary>
/// <remarks>
/// Each shot rolls damage (with crit), applies spread, then calls Projectile.Initialize with combat context
/// for elemental effects on hit.
/// </remarks>
public class ProjectileWeapon : WeaponBase
{
    [Header("Projectile")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 18f;
    public int projectileCount = 1;
    public float spreadAngle;

    [Header("Projectile Behavior")]
    public int pierceCount;
    public int bounceCount;
    [Range(0f, 1f)] public float homingStrength;
    public float projectileSize = 1f;
    public string projectileLayerName = "Player";
    public Sprite projectileSprite;

    [Header("Projectile Aim Alignment")]
    [Tooltip("Nudge spawned bullets up/down perpendicular to aim.")]
    [Range(-1f, 1f)] public float projectileVerticalOffset;
    [Tooltip("Rotate spawned bullet sprites relative to travel (degrees).")]
    [Range(-45f, 45f)] public float projectilePitchOffset;

    [Header("Explosion")]
    public bool explodeOnHit;
    public float explosionRadius = 2f;
    public float explosionInitialDamage = 10f;
    public float explosionRadiusDamage = 5f;
    public int explosionPulseCount = 3;
    public float explosionPulseSpacing = 0.75f;

    [Header("Physics")]
    public float projectileGravityScale;

    protected override void PerformAttack()
    {
        if (projectilePrefab == null)
            return;

        float shotDamage = RollDamage(out _);
        Vector2 baseDirection = GetFireDirection();

        for (int i = 0; i < projectileCount; i++)
        {
            Vector2 dir = baseDirection;
            if (spreadAngle > 0.01f)
            {
                float randomSpread = Random.Range(-spreadAngle, spreadAngle);
                dir = Quaternion.Euler(0f, 0f, randomSpread) * dir;
            }

            Vector3 spawnPos = transform.position + (Vector3)dir * 0.6f;
            GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

            if (projectileSprite != null)
            {
                var projectileRenderer = projObj.GetComponent<SpriteRenderer>();
                if (projectileRenderer != null)
                    projectileRenderer.sprite = projectileSprite;
            }

            int targetLayer = LayerMask.NameToLayer(projectileLayerName);
            if (targetLayer != -1)
                projObj.layer = targetLayer;

            Projectile proj = projObj.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.Initialize(dir, shotDamage, projectileSpeed, null, owner ?? gameObject,
                    pierceCount, bounceCount, homingStrength, CreateCombatContext(),
                    projectileVerticalOffset, projectilePitchOffset,
                    explodeOnHit, explosionRadius, explosionInitialDamage, explosionRadiusDamage,
                    explosionPulseCount, explosionPulseSpacing, projectileGravityScale);

                if (projectileSize != 1f)
                    projObj.transform.localScale = Vector3.one * projectileSize;
            }
            else
            {
                Rigidbody2D rb = projObj.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.linearVelocity = dir * projectileSpeed;
                Destroy(projObj, range / projectileSpeed + 1f);
            }
        }
    }
}