using UnityEngine;

public class BurrowerEnemy : Enemy
{
    [Header("Burrower")]
    public float ambushRange = 3f;

    public override bool CanAttackUnderground => true;

    public override void PerformAttack(Transform target)
    {
        if (target == null) return;

        float distance = Vector2.Distance(transform.position, target.position);
        float attackDamage = GetScaledDamage();

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, GetAttackRange());
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
                TryDamagePlayer(hit, attackDamage);
        }

        if (baseStats != null && baseStats.CanBurrow && !IsBurrowed)
        {
            if (distance < ambushRange || Random.value < 0.6f)
                ToggleBurrow();
        }
    }
}