using System.Collections.Generic;
using UnityEngine;

public class VoidBlackHoleSystem : MonoBehaviour
{
    private sealed class ActiveVoid
    {
        public Enemy Anchor;
        public Vector2 AnchorPosition;
        public float Timer;
        public VoidElementStats Stats;
        public WeaponCombatContext Context;
        public readonly HashSet<Enemy> Pulled = new();
        public float DotTickTimer;
    }

    public static VoidBlackHoleSystem Instance { get; private set; }

    private readonly List<ActiveVoid> activeVoids = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureSystem()
    {
        if (Instance != null)
            return;

        var go = new GameObject("Void Black Hole System");
        go.AddComponent<VoidBlackHoleSystem>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void ApplyVoidHit(Enemy anchor, WeaponCombatContext context)
    {
        if (Instance == null || anchor == null || !anchor.gameObject.activeInHierarchy || anchor.IsBurrowed)
            return;

        VoidElementStats stats = context.voidStats ?? new VoidElementStats();
        stats.Normalize();
        Instance.CreateOrRefreshVoid(anchor, stats, context);
    }

    public static void NotifyEnemyRemoved(Enemy enemy)
    {
        if (Instance == null || enemy == null)
            return;

        Instance.HandleEnemyRemoved(enemy);
    }

    public static float GetAnchorTimeRemaining(Enemy anchor)
    {
        if (Instance == null || anchor == null)
            return 0f;

        for (int i = 0; i < Instance.activeVoids.Count; i++)
        {
            ActiveVoid hole = Instance.activeVoids[i];
            if (hole.Anchor == anchor)
                return Mathf.Max(0f, hole.Timer);
        }

        return 0f;
    }

    private void CreateOrRefreshVoid(Enemy anchor, VoidElementStats stats, WeaponCombatContext context)
    {
        for (int i = 0; i < activeVoids.Count; i++)
        {
            ActiveVoid existing = activeVoids[i];
            if (existing.Anchor != anchor || existing.Context.sourceWeaponId != context.sourceWeaponId)
                continue;

            existing.Timer = stats.collapseDuration;
            existing.Stats = stats.Clone();
            existing.Context = context;
            if (existing.DotTickTimer <= 0f)
                existing.DotTickTimer = stats.voidTickInterval;
            PinAnchor(existing);
            anchor.ClearVoidPull();
            return;
        }

        bool ownsVoid = anchor.IsVoidAnchored && anchor.VoidAnchorWeaponId == context.sourceWeaponId;
        if (!ownsVoid && VoidTracker.GetAvailableSlots(context.sourceWeaponId, stats.maxVoidCount) <= 0)
            return;

        if (!ownsVoid && !VoidTracker.TryRegister(anchor, context.sourceWeaponId, stats.maxVoidCount))
            return;

        anchor.ReleaseVoidEffects();
        anchor.SetVoidAnchor(true, context.sourceWeaponId);

        Vector2 contactPosition = anchor.transform.position;

        activeVoids.Add(new ActiveVoid
        {
            Anchor = anchor,
            AnchorPosition = contactPosition,
            Timer = stats.collapseDuration,
            Stats = stats,
            Context = context,
            DotTickTimer = stats.voidTickInterval
        });

        PinAnchor(activeVoids[activeVoids.Count - 1]);
    }

    private void Update()
    {
        for (int i = activeVoids.Count - 1; i >= 0; i--)
        {
            ActiveVoid hole = activeVoids[i];
            if (!IsAnchorValid(hole))
            {
                ReleaseVoid(hole);
                activeVoids.RemoveAt(i);
                continue;
            }

            ProcessVoidDot(hole);

            hole.Timer -= Time.deltaTime;
            if (hole.Timer <= 0f)
            {
                TriggerVoidBoom(hole);
                activeVoids.RemoveAt(i);
            }
        }
    }

    private static void ProcessVoidDot(ActiveVoid hole)
    {
        VoidElementStats stats = hole.Stats;
        stats.Normalize();

        float damagePerTick = Mathf.Max(0f, hole.Context.baseDamage * stats.voidDamageMultiplier);
        if (damagePerTick <= 0f)
            return;

        hole.DotTickTimer -= Time.deltaTime;
        if (hole.DotTickTimer > 0f)
            return;

        hole.DotTickTimer = stats.voidTickInterval;

        if (IsAnchorValid(hole))
            hole.Anchor.TakeDamage(damagePerTick);

        foreach (Enemy pulled in hole.Pulled)
        {
            if (pulled == null || !pulled.gameObject.activeInHierarchy || pulled.IsBurrowed)
                continue;

            pulled.TakeDamage(damagePerTick);
        }
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < activeVoids.Count; i++)
        {
            ActiveVoid hole = activeVoids[i];
            PinAnchor(hole);
            ApplyPull(hole);
        }
    }

    private static void PinAnchor(ActiveVoid hole)
    {
        if (!IsAnchorValid(hole))
            return;

        Rigidbody2D anchorBody = hole.Anchor.GetComponent<Rigidbody2D>();
        if (anchorBody == null)
            return;

        anchorBody.MovePosition(hole.AnchorPosition);
        anchorBody.linearVelocity = Vector2.zero;
    }

    private void ApplyPull(ActiveVoid hole)
    {
        if (!IsAnchorValid(hole))
            return;

        Enemy anchor = hole.Anchor;
        anchor.SetVoidAnchor(true, hole.Context.sourceWeaponId);

        Vector2 anchorPos = hole.AnchorPosition;
        VoidElementStats stats = hole.Stats;
        Collider2D[] hits = Physics2D.OverlapCircleAll(anchorPos, stats.pullRadius);
        var candidates = new List<(Enemy enemy, float distance)>(hits.Length);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
                continue;

            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy == null || enemy == anchor || !enemy.gameObject.activeInHierarchy || enemy.IsBurrowed)
                continue;

            if (enemy.IsVoidAnchored)
                continue;

            float distance = Vector2.Distance(anchorPos, enemy.transform.position);
            if (distance > stats.pullRadius)
                continue;

            candidates.Add((enemy, distance));
        }

        candidates.Sort((a, b) => a.distance.CompareTo(b.distance));

        var stillPulling = new HashSet<Enemy>();
        int pullCount = 0;

        for (int i = 0; i < candidates.Count && pullCount < stats.maxPullTargets; i++)
        {
            Enemy pulled = candidates[i].enemy;
            hole.Pulled.Add(pulled);
            stillPulling.Add(pulled);
            pulled.SetVoidPullFrom(anchor);
            pullCount++;

            Rigidbody2D body = pulled.GetComponent<Rigidbody2D>();
            if (body == null)
                continue;

            Vector2 offset = anchorPos - body.position;
            if (offset.sqrMagnitude <= 0.01f)
                continue;

            body.linearVelocity = offset.normalized * stats.pullStrength;
        }

        var stale = new List<Enemy>();
        foreach (Enemy pulled in hole.Pulled)
        {
            if (pulled == null || !pulled.gameObject.activeInHierarchy || pulled.IsBurrowed || !stillPulling.Contains(pulled))
                stale.Add(pulled);
        }

        for (int i = 0; i < stale.Count; i++)
        {
            Enemy removed = stale[i];
            hole.Pulled.Remove(removed);
            removed?.ClearVoidPull();
        }
    }

    private void TriggerVoidBoom(ActiveVoid hole)
    {
        if (!IsAnchorValid(hole))
        {
            ReleaseVoid(hole);
            return;
        }

        float critDamage = hole.Context.GetCritDamage();
        Enemy anchor = hole.Anchor;

        if (critDamage > 0f)
            anchor.TakeDamage(critDamage);

        foreach (Enemy pulled in hole.Pulled)
        {
            if (pulled == null || !pulled.gameObject.activeInHierarchy || pulled.IsBurrowed)
                continue;

            if (critDamage > 0f)
                pulled.TakeDamage(critDamage);
        }

        ReleaseVoid(hole);
    }

    private void ReleaseVoid(ActiveVoid hole)
    {
        if (hole.Anchor != null)
        {
            VoidTracker.Unregister(hole.Anchor, hole.Context.sourceWeaponId);
            hole.Anchor.ReleaseVoidEffects();
        }

        foreach (Enemy pulled in hole.Pulled)
            pulled?.ClearVoidPull();

        hole.Pulled.Clear();
    }

    private void HandleEnemyRemoved(Enemy enemy)
    {
        for (int i = activeVoids.Count - 1; i >= 0; i--)
        {
            ActiveVoid hole = activeVoids[i];
            if (hole.Anchor == enemy)
            {
                ReleaseVoid(hole);
                activeVoids.RemoveAt(i);
                continue;
            }

            if (hole.Pulled.Remove(enemy))
                enemy.ClearVoidPull();
        }
    }

    private static bool IsAnchorValid(ActiveVoid hole) =>
        hole?.Anchor != null
        && hole.Anchor.gameObject.activeInHierarchy
        && !hole.Anchor.IsBurrowed
        && hole.Anchor.CurrentHealth > 0f;
}