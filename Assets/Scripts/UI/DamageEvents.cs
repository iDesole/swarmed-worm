using System;
using UnityEngine;

public enum DamageNumberStyle
{
    EnemyHit,
    PlayerHit
}

public static class DamageEvents
{
    public static event Action<float, Vector3, DamageNumberStyle> DamageDealt;

    public static void Notify(float amount, Vector3 worldPosition, DamageNumberStyle style)
    {
        if (amount <= 0f) return;
        DamageDealt?.Invoke(amount, worldPosition, style);
    }
}