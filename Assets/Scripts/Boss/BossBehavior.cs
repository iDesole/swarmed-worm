using UnityEngine;

/// <summary>
/// Base class for boss-specific behavior scripts.
/// Create a subclass per boss and assign it on the boss entry in BossDatabase.
/// </summary>
public abstract class BossBehavior : MonoBehaviour
{
    public Boss Boss { get; private set; }
    public Transform Player => PlayerCatalog.ActiveTransform;
    protected Player ActivePlayer => Player != null ? Player.GetComponent<Player>() : null;
    protected Rigidbody2D Rigidbody => Boss != null ? Boss.GetComponent<Rigidbody2D>() : null;

    internal void Bind(Boss boss)
    {
        Boss = boss;
        OnBound();
    }

    /// <summary>Called once when the behavior is attached to a spawned boss.</summary>
    protected virtual void OnBound() { }

    /// <summary>Called on the first frame after binding.</summary>
    protected virtual void OnBossStart() { }

    /// <summary>Called when boss health changes. healthPercent is 0-1.</summary>
    protected virtual void OnHealthChanged(float healthPercent) { }

    internal void NotifyHealthChanged(float healthPercent) => OnHealthChanged(healthPercent);

    private void Start() => OnBossStart();
}