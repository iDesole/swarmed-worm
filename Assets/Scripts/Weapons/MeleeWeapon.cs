using UnityEngine;

/// <summary>
/// Close-range weapon that spawns a <see cref="MeleeHitbox"/> toward the mouse aim direction.
/// </summary>
public class MeleeWeapon : WeaponBase
{
    [Header("Melee")]
    public GameObject meleeHitboxPrefab;
    public float swingDuration = 0.25f;
    public float swingRadius = 2.2f;
    public int swingsPerAttack = 1;
    public float swingArcAngle = 90f;
    public int multiHitCount = 1;
    [Range(0f, 1f)] public float lifesteal;

    protected override void PerformAttack()
    {
        float swingDamage = RollDamage(out _);

        if (meleeHitboxPrefab != null)
        {
            Transform aimOrigin = owner != null ? owner.transform : transform;
            float hitboxDistance = swingRadius * 0.6f;
            float hitboxSizeScale = MeleeHitbox.RangeToSizeScale(range);

            GameObject hitbox = Instantiate(meleeHitboxPrefab);
            hitbox.transform.SetParent(null, true);

            MeleeHitbox damageDealer = hitbox.GetComponent<MeleeHitbox>() ?? hitbox.AddComponent<MeleeHitbox>();
            damageDealer.damage = swingDamage;
            damageDealer.combatContext = CreateCombatContext();
            damageDealer.owner = owner ?? gameObject;
            damageDealer.aimOrigin = aimOrigin;
            damageDealer.followDistance = hitboxDistance;
            damageDealer.sizeScale = hitboxSizeScale;
            damageDealer.attachToOwner = true;
            damageDealer.lifetime = swingDuration;
            MeleeHitbox.ApplyAimOffset(aimOrigin, hitboxDistance, hitbox.transform, hitboxSizeScale);
            return;
        }

        Vector2 fallbackDir = GetFireDirection();
        float fallbackRadius = MeleeHitbox.RangeToWorldRadius(range);
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position + (Vector3)fallbackDir * swingRadius * 0.7f, fallbackRadius);

        int hitsDealt = 0;
        foreach (var hit in hits)
        {
            if (hit.CompareTag(owner?.tag ?? "Player")) continue;

            var dmg = hit.GetComponent<IDamageable>();
            if (dmg == null) continue;

            dmg.TakeDamage(swingDamage);
            ElementalCombatSystem.ApplyHitEffects(CreateCombatContext(), hit, swingDamage);
            hitsDealt++;

            if (lifesteal > 0 && owner != null)
                owner.GetComponent<Player>()?.Heal(swingDamage * lifesteal);

            if (multiHitCount > 0 && hitsDealt >= multiHitCount)
                break;
        }
    }
}