using UnityEngine;

public class MeleeEnemy : Enemy
{
    [Header("Melee")]
    public float chargeSpeedMultiplier = 1.8f;
    public float chargeDuration = 0.6f;

    private bool isCharging;
    private float chargeEndTime;

    public override void PerformAttack(Transform target)
    {
        if (target == null) return;

        float attackDamage = GetScaledDamage();
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, GetAttackRange());

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            TryDamagePlayer(hit, attackDamage);

            Rigidbody2D playerRb = hit.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                Vector2 knockbackDir = (hit.transform.position - transform.position).normalized;
                playerRb.AddForce(knockbackDir * 5f, ForceMode2D.Impulse);
            }
        }

        if (Random.value < GetAggression() * 0.4f && !isCharging)
            StartCharge(target);
    }

    private void StartCharge(Transform target)
    {
        isCharging = true;
        chargeEndTime = Time.time + chargeDuration;

        Vector2 dir = (target.position - transform.position).normalized;
        float scaled = GetScaledMoveSpeed() * chargeSpeedMultiplier;
        if (scaled <= 0.1f)
            scaled = (baseStats != null ? baseStats.MoveSpeed : 3.5f) * chargeSpeedMultiplier;

        rb.linearVelocity = dir * scaled;
    }

    protected override void Update()
    {
        base.Update();

        if (IsJolted)
        {
            isCharging = false;
            return;
        }

        if (isCharging && Time.time > chargeEndTime)
        {
            isCharging = false;
            rb.linearVelocity *= 0.3f;
        }
    }
}