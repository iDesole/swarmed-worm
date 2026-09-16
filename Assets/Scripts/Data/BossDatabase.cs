using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BossDatabase", menuName = "Game/Boss Database")]
public class BossDatabase : ScriptableObject
{
    [SerializeField] private List<BossDefinition> bosses = new();

    public IReadOnlyList<BossDefinition> Bosses => bosses;

    public string GetBossName(BossSelection selection)
    {
        BossDefinition boss = GetBoss(selection);
        return boss != null ? boss.BossName : string.Empty;
    }

    public Sprite GetBossSprite(BossSelection selection)
    {
        BossDefinition boss = GetBoss(selection);
        return boss?.core?.sprite;
    }

    public bool TryGetBossCore(BossSelection selection, out BossCoreStats core)
    {
        core = null;
        BossDefinition boss = GetBoss(selection);
        if (boss?.core == null)
            return false;

        core = boss.core;
        return true;
    }

    public BossDefinition GetBoss(BossSelection selection)
    {
        if (!selection.IsValid || !InRange(bosses, selection.index))
            return null;

        return bosses[selection.index];
    }

    public BossSelection FindSelection(string bossName)
    {
        if (string.IsNullOrWhiteSpace(bossName) || bosses == null)
            return BossSelection.None;

        for (int i = 0; i < bosses.Count; i++)
        {
            BossDefinition boss = bosses[i];
            if (boss == null)
                continue;

            if (string.Equals(boss.BossName, bossName, StringComparison.OrdinalIgnoreCase))
                return BossSelection.At(i);
        }

        return BossSelection.None;
    }

    public Boss CreateBoss(BossSelection selection, Vector3 position) =>
        BossFactory.Create(selection, this, position);

    private void OnEnable() => BossCatalog.Register(this);

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            return;
#endif

        bosses ??= new List<BossDefinition>();

        foreach (BossDefinition boss in bosses)
        {
            boss?.Normalize();
#if UNITY_EDITOR
            if (boss?.core != null)
                BossSpriteUtility.EnsureCoreSprites(boss.core);
#endif
        }
    }

    private static bool InRange<T>(IReadOnlyList<T> list, int index) =>
        list != null && index >= 0 && index < list.Count;
}