using UnityEngine;

/// <summary>Melee enemy variant that uses <see cref="TrilobiteBehavior"/> for rush attacks.</summary>
public class TrilobiteEnemy : MeleeEnemy
{
    protected override void Awake()
    {
        base.Awake();

        if (GetComponent<TrilobiteBehavior>() == null)
            gameObject.AddComponent<TrilobiteBehavior>();
    }
}