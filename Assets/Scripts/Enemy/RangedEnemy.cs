using UnityEngine;

public class RangedEnemy : Enemy
{
    [Header("Ranged")]
    public GameObject projectilePrefab;
    public Sprite projectileSprite;
    public float projectileSpeed = 12f;
    public float projectileLifetime = 3f;
    public string projectileLayerName = "Player";

    public override void PerformAttack(Transform target)
    {
        if (target == null || projectilePrefab == null) return;

        Vector2 direction = (target.position - transform.position).normalized;
        Vector3 spawnPos = transform.position + (Vector3)direction * 0.8f;
        GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        if (projectileSprite != null)
        {
            var projectileRenderer = proj.GetComponent<SpriteRenderer>();
            if (projectileRenderer != null)
                projectileRenderer.sprite = projectileSprite;
        }

        int targetLayer = LayerMask.NameToLayer(projectileLayerName);
        if (targetLayer != -1)
            proj.layer = targetLayer;

        Projectile projectile = proj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Initialize(direction, GetScaledDamage(), projectileSpeed, projectileLifetime, gameObject);
        }
        else
        {
            Rigidbody2D projRb = proj.GetComponent<Rigidbody2D>();
            if (projRb != null)
                projRb.linearVelocity = direction * projectileSpeed;
            Destroy(proj, projectileLifetime);
        }
    }
}