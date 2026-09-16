using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Expanding shockwave damage triggered when an exploding projectile hits or expires.
/// </summary>
public static class ProjectileExplosionSystem
{
    private const float PulseDelay = 0.04f;
    private const float RingThickness = 0.25f;

    public static void Trigger(
        Vector2 origin,
        bool explodeOnHit,
        float explosionRadius,
        float initialDamage,
        float radiusDamage,
        int pulseCount,
        float pulseSpacing,
        WeaponCombatContext combatContext,
        string ownerTag,
        LayerMask hitLayers,
        Collider2D excludeFromCenterPulse = null)
    {
        if (!explodeOnHit || explosionRadius <= 0f || pulseCount <= 0)
            return;

        pulseSpacing = Mathf.Max(0.1f, pulseSpacing);
        pulseCount = Mathf.Max(1, pulseCount);

        var runner = new GameObject("ProjectileExplosion").AddComponent<ExplosionRunner>();
        runner.StartExplosion(
            origin,
            explosionRadius,
            initialDamage,
            radiusDamage,
            pulseCount,
            pulseSpacing,
            combatContext,
            ownerTag,
            hitLayers,
            excludeFromCenterPulse);
    }

    private sealed class ExplosionRunner : MonoBehaviour
    {
        public void StartExplosion(
            Vector2 origin,
            float explosionRadius,
            float initialDamage,
            float radiusDamage,
            int pulseCount,
            float pulseSpacing,
            WeaponCombatContext combatContext,
            string ownerTag,
            LayerMask hitLayers,
            Collider2D excludeFromCenterPulse)
        {
            StartCoroutine(RunExplosion(
                origin,
                explosionRadius,
                initialDamage,
                radiusDamage,
                pulseCount,
                pulseSpacing,
                combatContext,
                ownerTag,
                hitLayers,
                excludeFromCenterPulse));
        }

        private IEnumerator RunExplosion(
            Vector2 origin,
            float explosionRadius,
            float initialDamage,
            float radiusDamage,
            int pulseCount,
            float pulseSpacing,
            WeaponCombatContext combatContext,
            string ownerTag,
            LayerMask hitLayers,
            Collider2D excludeFromCenterPulse)
        {
            var damaged = new HashSet<int>();
            if (excludeFromCenterPulse != null)
                damaged.Add(excludeFromCenterPulse.GetInstanceID());

            int pulsesToRun = Mathf.Max(
                pulseCount,
                Mathf.CeilToInt(explosionRadius / pulseSpacing));

            for (int pulse = 0; pulse < pulsesToRun; pulse++)
            {
                if (pulse > 0)
                    yield return new WaitForSeconds(PulseDelay);

                float innerRadius = pulse * pulseSpacing;
                float outerRadius = Mathf.Min((pulse + 1) * pulseSpacing, explosionRadius);
                if (outerRadius <= innerRadius + 0.001f)
                    break;

                float pulseDamage = pulse == 0 ? initialDamage : radiusDamage;
                if (pulseDamage <= 0f)
                    continue;

                DamageTargetsInRing(
                    origin,
                    innerRadius,
                    outerRadius,
                    pulseDamage,
                    combatContext,
                    ownerTag,
                    hitLayers,
                    damaged);
            }

            if (radiusDamage > 0f)
            {
                DamageTargetsInRadius(
                    origin,
                    explosionRadius,
                    radiusDamage,
                    combatContext,
                    ownerTag,
                    hitLayers,
                    damaged);
            }

            Destroy(gameObject);
        }

        private static void DamageTargetsInRing(
            Vector2 origin,
            float innerRadius,
            float outerRadius,
            float damage,
            WeaponCombatContext combatContext,
            string ownerTag,
            LayerMask hitLayers,
            HashSet<int> damaged)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, outerRadius, hitLayers);
            foreach (Collider2D hit in hits)
            {
                if (!IsValidTarget(hit, ownerTag))
                    continue;

                float distance = DistanceToCollider(origin, hit);
                if (distance > outerRadius + RingThickness)
                    continue;

                if (distance < innerRadius - RingThickness && innerRadius > 0f)
                    continue;

                if (!damaged.Add(hit.GetInstanceID()))
                    continue;

                ApplyDamage(hit, damage, combatContext);
            }
        }

        private static void DamageTargetsInRadius(
            Vector2 origin,
            float radius,
            float damage,
            WeaponCombatContext combatContext,
            string ownerTag,
            LayerMask hitLayers,
            HashSet<int> damaged)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, radius, hitLayers);
            foreach (Collider2D hit in hits)
            {
                if (!IsValidTarget(hit, ownerTag))
                    continue;

                if (DistanceToCollider(origin, hit) > radius + RingThickness)
                    continue;

                if (!damaged.Add(hit.GetInstanceID()))
                    continue;

                ApplyDamage(hit, damage, combatContext);
            }
        }

        private static float DistanceToCollider(Vector2 origin, Collider2D collider) =>
            Vector2.Distance(origin, collider.ClosestPoint(origin));

        private static bool IsValidTarget(Collider2D hit, string ownerTag)
        {
            if (hit == null || hit.CompareTag(ownerTag) || hit.CompareTag("Projectile"))
                return false;

            if (hit.GetComponent<WaveTarget>() != null ||
                hit.GetComponent<Hostage>() != null ||
                hit.GetComponent<IDamageable>() != null ||
                hit.GetComponentInParent<IDamageable>() != null)
                return true;

            return false;
        }

        private static void ApplyDamage(Collider2D hit, float amount, WeaponCombatContext combatContext)
        {
            Collider2D effectCollider = hit.GetComponent<Enemy>() != null
                ? hit
                : hit.GetComponentInParent<Enemy>()?.GetComponent<Collider2D>() ?? hit;

            if (hit.GetComponent<WaveTarget>() is WaveTarget target)
                target.TakeDamage(amount);
            else if (hit.GetComponent<Hostage>() is Hostage hostage)
                hostage.TakeDamage(amount);
            else
                (hit.GetComponent<IDamageable>() ?? hit.GetComponentInParent<IDamageable>())?.TakeDamage(amount);

            ElementalCombatSystem.ApplyHitEffects(
                combatContext,
                effectCollider,
                amount,
                ElementalHitDelivery.Projectile);
        }
    }
}