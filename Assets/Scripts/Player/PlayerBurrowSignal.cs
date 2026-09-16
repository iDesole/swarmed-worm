using UnityEngine;

/// <summary>
/// Broadcasts when the player is burrowed. Anything within <see cref="Radius"/> can detect it.
/// </summary>
public class PlayerBurrowSignal : MonoBehaviour
{
    public const float Radius = 8f;

    public static PlayerBurrowSignal Active { get; private set; }

    public bool IsBroadcasting { get; private set; }

    private void Awake()
    {
        if (Active == null)
            Active = this;
    }

    private void OnDestroy()
    {
        if (Active == this)
            Active = null;
    }

    public void SetBroadcasting(bool broadcasting) => IsBroadcasting = broadcasting;

    public static bool IsNearby(Vector2 position)
    {
        if (Active == null || !Active.IsBroadcasting)
            return false;

        return Vector2.Distance(position, Active.transform.position) <= Radius;
    }
}