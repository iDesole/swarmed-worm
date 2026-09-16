using UnityEngine;

/// <summary>
/// Template boss behavior. Duplicate this file and customize for each boss.
/// Assign your new script on the boss entry in BossDatabase.
/// </summary>
public class ExampleBossBehavior : BossBehavior
{
    [SerializeField] private float patrolRadius = 3f;
    [SerializeField] private float phaseTwoHealthThreshold = 0.5f;

    private bool phaseTwoTriggered;

    protected override void OnBossStart()
    {
        if (Boss == null)
            return;

        GameLog.Info($"Example boss '{Boss.name}' started with custom behavior.");
    }

    protected override void OnHealthChanged(float healthPercent)
    {
        if (phaseTwoTriggered || healthPercent > phaseTwoHealthThreshold)
            return;

        phaseTwoTriggered = true;
        GameLog.Info($"Example boss entered phase 2 at {healthPercent:P0} health.");
    }

    private void Update()
    {
        if (Boss == null || Boss.IsDefeated || Player == null)
            return;

        Vector2 offset = (Vector2)(Player.position - Boss.transform.position);
        if (offset.sqrMagnitude > patrolRadius * patrolRadius)
            Boss.transform.position += (Vector3)(offset.normalized * Boss.GetScaledMoveSpeed() * Time.deltaTime);
    }
}