using System;

/// <summary>
/// Lightweight event bus decoupling combat outcomes from wave logic and UI.
/// </summary>
/// <remarks>
/// Enemies call <see cref="NotifyEnemyKilled"/> on death; Player calls <see cref="NotifyPlayerDied"/>.
/// Subscribers: <see cref="WaveManager"/> (wave progress), <see cref="GameHUD"/> (objectives display).
/// Prefer this over direct references when many systems need the same signal.
/// </remarks>
public static class WaveEvents
{
    public static event Action<Enemy> EnemyKilled;
    public static event Action<Player> PlayerDied;
    public static event Action<Player> PlayerDeathPresentationFinished;

    public static void NotifyEnemyKilled(Enemy enemy) => EnemyKilled?.Invoke(enemy);
    public static void NotifyPlayerDied(Player player) => PlayerDied?.Invoke(player);
    public static void NotifyPlayerDeathPresentationFinished(Player player) =>
        PlayerDeathPresentationFinished?.Invoke(player);
}