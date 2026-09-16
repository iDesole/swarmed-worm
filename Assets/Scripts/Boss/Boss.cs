using UnityEngine;

/// <summary>
/// Boss enemy with optional custom behavior from BossDatabase.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Boss : Enemy
{
    public BossBehavior ActiveBehavior { get; private set; }
    public bool UsesCustomBehavior => ActiveBehavior != null;
    public BossCoreStats VisualCore => visualCore;

    private BossCoreStats visualCore;

    public void ApplyBossCore(BossCoreStats core)
    {
        if (core == null)
            return;

        visualCore = core.Clone();
        BossSpriteUtility.EnsureCoreSprites(visualCore);
        ApplyCore(BossStatsUtility.ToEnemyCore(visualCore));
    }

    public void AttachBehavior(BossBehavior behavior)
    {
        ActiveBehavior = behavior;
        if (ActiveBehavior == null)
            return;

        ActiveBehavior.Bind(this);

        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null)
            ai.enabled = false;
    }

    protected override void ApplyVisualsFromStats()
    {
        base.ApplyVisualsFromStats();
        BossSpriteUtility.ApplyRendererSprite(this, visualCore);

        CharacterBlobShadow.Ensure(
            gameObject,
            sortingOrder: CharacterPresentationConstants.PlayerShadowSortingOrder);
    }

    private void Start() => BossSpriteUtility.ApplyRendererSprite(this, visualCore);

    public override void TakeDamage(float damage)
    {
        float previousMax = ResolveMaxHealth() * HealthMultiplier;
        float previousHealth = CurrentHealth;

        base.TakeDamage(damage);

        if (ActiveBehavior == null || previousMax <= 0f)
            return;

        float previousPercent = previousHealth / previousMax;
        float currentPercent = CurrentHealth / previousMax;
        if (!Mathf.Approximately(previousPercent, currentPercent))
            ActiveBehavior.NotifyHealthChanged(currentPercent);
    }
}