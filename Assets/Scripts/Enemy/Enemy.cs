using UnityEngine;

/// <summary>
/// Base enemy: health, elemental status effects, loot drops, and death handling.
/// </summary>
/// <remarks>
/// Subtypes (MeleeEnemy, RangedEnemy, etc.) add attack patterns. Stats come from EnemyDatabase via EnemyFactory.
/// Elemental stacks (burn, poison, shock, etc.) live here and are driven by <see cref="ElementalCombatSystem"/>.
/// On death, notifies <see cref="WaveEvents"/> and may spawn loot via <see cref="LootDropper"/>.
/// </remarks>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyAI))]
public class Enemy : MonoBehaviour, IDamageable, IBlobShadowHost
{
    [Header("Stats")]
    [SerializeField] protected EnemyStats baseStats;
    private EnemyCoreStats activeCore;

    public EnemyStats BaseStats => baseStats;
    public bool UsesDatabaseCore => activeCore != null;

    public void AssignStats(EnemyStats stats)
    {
        baseStats = stats;
        activeCore = null;
        ApplyBaseStats();
        ApplyLootPool(stats?.LootPool);
    }

    public void ApplyLootPool(EnemyLootPool pool) =>
        activeLootPool = pool?.Clone();

    public void ApplyCore(EnemyCoreStats core)
    {
        activeCore = core?.Clone();
        ApplyBaseStats();
    }

    [Header("Runtime State")]
    public float CurrentHealth { get; private set; }
    public bool IsBurrowed { get; protected set; }
    public bool IsJolted => joltTimer > 0f;
    public bool IsBurning => burnWindowTimer > 0f;
    public bool HasBurnDot => burnTimer > 0f;
    public bool IsElectrocuted => shockTimer > 0f;
    public bool IsSlowed => slowTimer > 0f && !IsFrozen;
    public bool IsFrozen { get; private set; }
    public float FreezeTimeRemaining => freezeTimer;
    public int IceSlowStacks => iceSlowStacks;
    public int BurnStacks => burnStacks;
    public int ShockStacks => shockStacks;
    public bool IsPoisoned => poisonWindowTimer > 0f;
    public int PoisonStacks => poisonStacks;
    public int BurnStackThreshold =>
        activeBurnStackStats?.stacksToIncinerate ?? activeBurnStats?.stacksToIncinerate ?? 0;
    public int ShockStackThreshold => activeShockStackStats?.stacksToJolt ?? 0;
    public int IceStackThreshold => activeIceStats?.stacksToFreeze ?? 0;
    public int PoisonStackThreshold =>
        activePoisonStackStats?.stacksToSpread ?? activePoisonStats?.stacksToSpread ?? 0;
    public bool IsVoidAnchored { get; private set; }
    public bool IsVoidPulled => voidPullAnchor != null;
    public int VoidAnchorWeaponId { get; private set; }
    public float VoidCollapseTimeRemaining => VoidBlackHoleSystem.GetAnchorTimeRemaining(this);

    public float HealthMultiplier { get; private set; } = 1f;
    public float DamageMultiplier { get; private set; } = 1f;
    public float SpeedMultiplier { get; private set; } = 1f;

    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected float lastBurrowTime;
    private bool isDead;
    private bool isDying;
    private bool facingLeft;
    private bool usesRotationalFacing;
    private float rotationalFacingOffset;
    private float locomotionSpeedSqr;
    private readonly SpriteFrameAnimator spriteAnimator = new();
    private float joltTimer;
    private float shockTimer;
    private float slowTimer;
    private float freezeTimer;
    private float burnWindowTimer;
    private float burnTimer;
    private float burnTickTimer;
    private float burnTickInterval;
    private float burnDamagePerTick;
    private FireElementStats activeBurnStats;
    private WeaponCombatContext activeBurnContext;
    private int burnSourceWeaponId;
    private int iceSlowStacks;
    private IceElementStats activeIceStats;
    private int burnStacks;
    private FireElementStats activeBurnStackStats;
    private int shockStacks;
    private ElectricElementStats activeShockStackStats;
    private float poisonWindowTimer;
    private int poisonStacks;
    private int poisonSourceWeaponId;
    private PoisonElementStats activePoisonStats;
    private PoisonElementStats activePoisonStackStats;
    private Enemy voidPullAnchor;
    private EnemyLootPool activeLootPool;

    public EnemyLootPool LootPool => activeLootPool;
    public bool IsDefeated => isDead || isDying;
    public bool HideBlobShadow => IsBurrowed || IsDefeated;

    protected virtual bool SupportsBurrow =>
        baseStats != null && baseStats.CanBurrow;

    public bool CanBurrowNow()
    {
        if (!SupportsBurrow)
            return false;

        if (baseStats == null)
            return true;

        return Time.time >= lastBurrowTime + baseStats.BurrowCooldown;
    }

    public virtual bool CanAttackUnderground =>
        baseStats != null && baseStats.CanAttackUnderground;

    protected void TryDamagePlayer(Collider2D hit, float damage)
    {
        if (hit == null)
            return;

        Player player = hit.GetComponent<Player>();
        if (player != null)
        {
            player.TakeDamage(damage, CanAttackUnderground);
            return;
        }

        hit.GetComponent<IDamageable>()?.TakeDamage(damage);
    }

    protected void RecordBurrowTime() => lastBurrowTime = Time.time;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (GetComponent<EnemyElementalStackDisplay>() == null)
            gameObject.AddComponent<EnemyElementalStackDisplay>();

        if (activeCore != null || baseStats != null)
            ApplyBaseStats();
        else
            CurrentHealth = 30f;
    }

    protected virtual void Update()
    {
        UpdateJolt();
        UpdateBurnTicks();
    }

    protected virtual void FixedUpdate()
    {
        locomotionSpeedSqr = rb != null ? rb.linearVelocity.sqrMagnitude : 0f;
    }

    protected virtual void LateUpdate()
    {
        locomotionSpeedSqr = rb != null ? rb.linearVelocity.sqrMagnitude : 0f;
        UpdateSpriteAnimation();
        UpdateShockWindow();
        UpdateIceWindow();
        UpdatePoisonWindow();
        UpdateBurnWindow();
    }

    private void UpdateJolt()
    {
        if (joltTimer <= 0f)
            return;

        joltTimer -= Time.deltaTime;
        if (joltTimer <= 0f && rb != null && !IsBurrowed)
            rb.linearVelocity = Vector2.zero;
    }

    private void UpdateShockWindow()
    {
        if (IsBurrowed)
        {
            if (IsElectrocuted)
                ClearElectrocute();
            return;
        }

        if (shockTimer <= 0f)
            return;

        shockTimer -= Time.deltaTime;
        if (shockTimer <= 0f)
            ClearElectrocute();
    }

    private void UpdateIceWindow()
    {
        if (IsBurrowed)
        {
            if (IsFrozen)
                ClearIce();
            else if (IsSlowed)
                ClearSlowWindow();
            return;
        }

        if (IsFrozen)
        {
            if (freezeTimer <= 0f)
                return;

            freezeTimer -= Time.deltaTime;
            if (freezeTimer <= 0f)
                Thaw();
            return;
        }

        if (slowTimer <= 0f)
            return;

        slowTimer -= Time.deltaTime;
        if (slowTimer <= 0f)
            ClearSlowWindow();
    }

    private void UpdatePoisonWindow()
    {
        if (IsBurrowed)
        {
            if (IsPoisoned)
                ClearAllPoison();
            return;
        }

        if (poisonWindowTimer <= 0f)
            return;

        poisonWindowTimer -= Time.deltaTime;
        if (poisonWindowTimer <= 0f)
            ClearPoisonWindow();
    }

    private void UpdateBurnTicks()
    {
        if (IsBurrowed || !HasBurnDot)
            return;

        burnTickTimer -= Time.deltaTime;
        if (burnTickTimer > 0f)
            return;

        burnTickTimer = burnTickInterval;
        ElementalCombatSystem.ProcessBurnTick(this, burnDamagePerTick, activeBurnStats, activeBurnContext);
    }

    private void UpdateBurnWindow()
    {
        if (IsBurrowed)
        {
            if (IsBurning)
                ClearFireWindow();
            if (HasBurnDot)
                ClearBurnDot();
            if (IsFrozen || IsSlowed || iceSlowStacks > 0)
                ClearIce();
            if (IsElectrocuted)
                ClearElectrocute();
            if (IsPoisoned)
                ClearAllPoison();
            return;
        }

        bool visualsDirty = false;

        if (burnWindowTimer > 0f)
        {
            burnWindowTimer -= Time.deltaTime;
            if (burnWindowTimer <= 0f)
            {
                ClearFireWindow();
                visualsDirty = true;
            }
        }

        if (burnTimer > 0f)
        {
            burnTimer -= Time.deltaTime;
            if (burnTimer <= 0f)
            {
                ClearBurnDot();
                visualsDirty = true;
            }
        }

        if (visualsDirty || IsBurning || HasBurnDot)
            RefreshStatusVisual();
    }

    public virtual void ApplyJolt(float duration)
    {
        if (duration <= 0f || IsBurrowed)
            return;

        joltTimer = Mathf.Max(joltTimer, duration);
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    public bool IsBurningFromWeapon(int weaponId) =>
        HasBurnDot && burnSourceWeaponId == weaponId;

    public virtual void ApplyFireWindow(FireElementStats stats)
    {
        if (stats == null || IsBurrowed || !gameObject.activeInHierarchy)
            return;

        stats.Normalize();
        activeBurnStackStats = stats.Clone();
        burnWindowTimer = Mathf.Max(burnWindowTimer, stats.burnDuration);
        RefreshStatusVisual();
    }

    public virtual bool ApplyBurn(FireElementStats stats, WeaponCombatContext context)
    {
        if (stats == null || IsBurrowed || !gameObject.activeInHierarchy)
            return false;

        bool ownsBurn = IsBurningFromWeapon(context.sourceWeaponId);
        if (!ownsBurn && !FireBurnTracker.TryRegister(this, context))
            return false;

        stats.Normalize();
        activeBurnStats = stats.Clone();
        activeBurnContext = context;
        burnSourceWeaponId = context.sourceWeaponId;
        burnTimer = Mathf.Max(burnTimer, stats.burnDuration);
        burnTickInterval = stats.burnTickInterval;
        burnDamagePerTick = Mathf.Max(0f, context.baseDamage * stats.burnDamageMultiplier);

        if (burnTickTimer <= 0f)
            burnTickTimer = burnTickInterval;

        RefreshStatusVisual();
        return true;
    }

    public void ClearBurnDot()
    {
        if (burnSourceWeaponId != 0)
            FireBurnTracker.Unregister(this, burnSourceWeaponId);

        burnSourceWeaponId = 0;
        burnTimer = 0f;
        burnTickTimer = 0f;
        burnDamagePerTick = 0f;
        activeBurnStats = null;
        activeBurnContext = default;
        RefreshStatusVisual();
    }

    public void ClearFireWindow()
    {
        burnWindowTimer = 0f;
        ClearBurnStacks();
        RefreshStatusVisual();
    }

    public void ClearAllFire()
    {
        ClearBurnDot();
        ClearFireWindow();
    }

    public virtual void ApplyBurnStacks(FireElementStats stats, int stackCount = -1)
    {
        if (stats == null || IsBurrowed || !gameObject.activeInHierarchy || !IsBurning)
            return;

        stats.Normalize();
        activeBurnStackStats = stats.Clone();
        burnWindowTimer = Mathf.Max(burnWindowTimer, stats.burnDuration);

        int stacksToAdd = stackCount >= 0 ? stackCount : stats.burnStacksPerHit;
        burnStacks = Mathf.Min(burnStacks + stacksToAdd, stats.stacksToIncinerate);
        RefreshStatusVisual();
    }

    public void ClearBurnStacks()
    {
        burnStacks = 0;
        activeBurnStackStats = null;
    }

    public virtual void ApplyElectrocute(ElectricElementStats stats)
    {
        if (stats == null || IsBurrowed || !gameObject.activeInHierarchy)
            return;

        stats.Normalize();
        activeShockStackStats = stats.Clone();
        shockTimer = Mathf.Max(shockTimer, stats.shockDuration);
        RefreshStatusVisual();
    }

    public void ClearElectrocute()
    {
        shockTimer = 0f;
        ClearShockStacks();
        RefreshStatusVisual();
    }

    public virtual void ApplyShockStacks(ElectricElementStats stats, int stackCount = -1)
    {
        if (stats == null || IsBurrowed || !gameObject.activeInHierarchy || !IsElectrocuted)
            return;

        stats.Normalize();
        activeShockStackStats = stats.Clone();

        shockTimer = Mathf.Max(shockTimer, stats.shockDuration);

        int stacksToAdd = stackCount >= 0 ? stackCount : stats.shockStacksPerHit;
        shockStacks = Mathf.Min(shockStacks + stacksToAdd, stats.stacksToJolt);
        RefreshStatusVisual();
    }

    public void ClearShockStacks()
    {
        shockStacks = 0;
        if (!IsElectrocuted)
            activeShockStackStats = null;
    }

    public bool IsPoisonedFromWeapon(int weaponId) =>
        IsPoisoned && poisonSourceWeaponId == weaponId;

    public virtual void ApplyPoisonWindow(PoisonElementStats stats, WeaponCombatContext context)
    {
        if (stats == null || IsBurrowed || !gameObject.activeInHierarchy)
            return;

        stats.Normalize();
        activePoisonStats = stats.Clone();
        activePoisonStackStats = stats.Clone();
        poisonWindowTimer = Mathf.Max(poisonWindowTimer, stats.poisonDuration);

        bool ownsPoison = IsPoisonedFromWeapon(context.sourceWeaponId);
        if (!ownsPoison)
            PoisonTracker.TryRegister(this, context);

        poisonSourceWeaponId = context.sourceWeaponId;
        RefreshStatusVisual();
    }

    public void ClearPoisonWindow()
    {
        if (poisonSourceWeaponId != 0)
            PoisonTracker.Unregister(this, poisonSourceWeaponId);

        poisonSourceWeaponId = 0;
        poisonWindowTimer = 0f;
        activePoisonStats = null;
        ClearPoisonStacks();
        RefreshStatusVisual();
    }

    public void ClearAllPoison()
    {
        if (poisonSourceWeaponId != 0)
            PoisonTracker.Unregister(this, poisonSourceWeaponId);

        poisonSourceWeaponId = 0;
        poisonWindowTimer = 0f;
        activePoisonStats = null;
        ClearPoisonStacks();
        RefreshStatusVisual();
    }

    public virtual void ApplyPoisonStacks(PoisonElementStats stats, int stackCount = -1)
    {
        if (stats == null || IsBurrowed || !gameObject.activeInHierarchy || !IsPoisoned)
            return;

        stats.Normalize();
        activePoisonStats = stats.Clone();
        activePoisonStackStats = stats.Clone();
        poisonWindowTimer = Mathf.Max(poisonWindowTimer, stats.poisonDuration);

        int stacksToAdd = stackCount >= 0 ? stackCount : stats.poisonStacksPerHit;
        poisonStacks = Mathf.Min(poisonStacks + stacksToAdd, stats.stacksToSpread);
        RefreshStatusVisual();
    }

    public void ClearPoisonStacks()
    {
        poisonStacks = 0;
        activePoisonStackStats = null;
    }

    public virtual void ApplySlowWindow(IceElementStats stats)
    {
        if (stats == null || IsBurrowed || !gameObject.activeInHierarchy || IsFrozen)
            return;

        stats.Normalize();
        activeIceStats = stats.Clone();
        slowTimer = Mathf.Max(slowTimer, stats.slowDuration);
        RefreshStatusVisual();
    }

    public void ClearSlowWindow()
    {
        slowTimer = 0f;
        iceSlowStacks = 0;
        activeIceStats = null;
        RefreshStatusVisual();
    }

    public virtual void ApplySlowStacks(IceElementStats stats, int stackCount = -1)
    {
        if (stats == null || IsBurrowed || !gameObject.activeInHierarchy || IsFrozen || !IsSlowed)
            return;

        stats.Normalize();
        activeIceStats = stats.Clone();

        slowTimer = Mathf.Max(slowTimer, stats.slowDuration);

        int stacksToAdd = stackCount >= 0 ? stackCount : stats.slowStacksPerHit;
        iceSlowStacks = Mathf.Min(iceSlowStacks + stacksToAdd, stats.stacksToFreeze);

        if (iceSlowStacks >= stats.stacksToFreeze)
            ApplyFreeze(stats);
        else
            RefreshStatusVisual();
    }

    public virtual void ApplyFreeze(IceElementStats stats)
    {
        if (IsBurrowed || stats == null)
            return;

        stats.Normalize();
        activeIceStats = stats.Clone();
        slowTimer = 0f;
        iceSlowStacks = 0;
        IsFrozen = true;
        freezeTimer = stats.freezeDuration;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        RefreshStatusVisual();
    }

    public void Thaw()
    {
        IsFrozen = false;
        freezeTimer = 0f;
        iceSlowStacks = 0;
        slowTimer = 0f;
        RefreshStatusVisual();
    }

    public void ClearIce()
    {
        IsFrozen = false;
        freezeTimer = 0f;
        slowTimer = 0f;
        iceSlowStacks = 0;
        activeIceStats = null;
        RefreshStatusVisual();
    }

    public float GetIceMoveSpeedMultiplier()
    {
        if (IsFrozen || IsBurrowed)
            return 0f;

        if (iceSlowStacks <= 0 || activeIceStats == null)
            return 1f;

        float slow = iceSlowStacks * activeIceStats.slowPerStack;
        return Mathf.Clamp01(1f - slow);
    }

    public float GetPoisonMoveSpeedMultiplier()
    {
        if (IsBurrowed)
            return 1f;

        if (!IsPoisoned || poisonStacks <= 0 || activePoisonStats == null)
            return 1f;

        float slow = poisonStacks * activePoisonStats.slowPerStack;
        return Mathf.Clamp01(1f - slow);
    }

    public void SetVoidAnchor(bool active, int weaponId)
    {
        IsVoidAnchored = active;
        VoidAnchorWeaponId = active ? weaponId : 0;

        if (active && rb != null)
            rb.linearVelocity = Vector2.zero;

        RefreshStatusVisual();
    }

    public void SetVoidPullFrom(Enemy anchor)
    {
        voidPullAnchor = anchor;
        RefreshStatusVisual();
    }

    public void ClearVoidPull()
    {
        voidPullAnchor = null;
        RefreshStatusVisual();
    }

    public void ReleaseVoidEffects()
    {
        if (IsVoidAnchored && VoidAnchorWeaponId != 0)
            VoidTracker.Unregister(this, VoidAnchorWeaponId);

        IsVoidAnchored = false;
        VoidAnchorWeaponId = 0;
        voidPullAnchor = null;
        RefreshStatusVisual();
    }

    private void RefreshStatusVisual()
    {
        if (spriteRenderer == null)
            return;

        if (IsBurrowed)
            return;

        Color baseColor = ResolveTintColor();

        if (IsFrozen)
        {
            spriteRenderer.color = Color.Lerp(baseColor, new Color(0.55f, 0.85f, 1f), 0.65f);
            return;
        }

        if (IsVoidAnchored)
        {
            spriteRenderer.color = Color.Lerp(baseColor, new Color(0.15f, 0.05f, 0.2f), 0.75f);
            return;
        }

        if (IsVoidPulled)
        {
            spriteRenderer.color = Color.Lerp(baseColor, new Color(0.25f, 0.1f, 0.35f), 0.55f);
            return;
        }

        if (IsBurning)
        {
            float burnRatio = activeBurnStackStats != null
                ? burnStacks / (float)Mathf.Max(1, activeBurnStackStats.stacksToIncinerate)
                : 0f;
            spriteRenderer.color = Color.Lerp(
                baseColor,
                new Color(1f, 0.45f, 0.15f),
                0.35f + burnRatio * 0.35f);
            return;
        }

        if (IsElectrocuted && activeShockStackStats != null)
        {
            float shockRatio = shockStacks / (float)Mathf.Max(1, activeShockStackStats.stacksToJolt);
            spriteRenderer.color = Color.Lerp(
                baseColor,
                new Color(1f, 0.95f, 0.35f),
                0.3f + shockRatio * 0.45f);
            return;
        }

        if (IsPoisoned && activePoisonStackStats != null)
        {
            float poisonRatio = poisonStacks / (float)Mathf.Max(1, activePoisonStackStats.stacksToSpread);
            spriteRenderer.color = Color.Lerp(
                baseColor,
                new Color(0.45f, 0.9f, 0.3f),
                0.25f + poisonRatio * 0.5f);
            return;
        }

        if (IsSlowed && activeIceStats != null)
        {
            float slowRatio = iceSlowStacks / (float)Mathf.Max(1, activeIceStats.stacksToFreeze);
            spriteRenderer.color = Color.Lerp(
                baseColor,
                new Color(0.65f, 0.85f, 1f),
                0.25f + slowRatio * 0.5f);
            return;
        }

        spriteRenderer.color = baseColor;
    }

    protected virtual void ApplyBaseStats()
    {
        if (activeCore == null && baseStats == null)
        {
            CurrentHealth = 30f;
            return;
        }

        CurrentHealth = ResolveMaxHealth() * HealthMultiplier;
        ApplyVisualsFromStats();
    }

    protected virtual void ApplyVisualsFromStats()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            return;

        Sprite sprite = ResolveSprite();
        if (sprite != null)
            spriteRenderer.sprite = sprite;

        spriteRenderer.color = ResolveTintColor();
        RenderVisibilityUtility.ConfigureSpriteRenderer(spriteRenderer, sortingOrder: 0);

        float scale = ResolveDisplayScale();
        if (scale > 0f)
            transform.localScale = Vector3.one * scale;

        spriteAnimator.Configure(spriteRenderer, ResolveSprite(), activeCore?.animationClips);
        CharacterBlobShadow.Ensure(gameObject, sortingOrder: CharacterPresentationConstants.EnemyShadowSortingOrder);
    }

    private void UpdateSpriteAnimation()
    {
        if (spriteRenderer == null)
            return;

        bool allowAnimation = !isDying && !isDead && !IsJolted && !IsFrozen && !IsVoidAnchored;
        bool allowWalkMotion = allowAnimation && !IsBurrowed;
        bool isMoving = allowWalkMotion && locomotionSpeedSqr > CharacterPresentationConstants.LocomotionSpeedThresholdSqr;
        spriteAnimator.Update(allowWalkMotion, isMoving);

        if (allowWalkMotion && isMoving && rb != null && !usesRotationalFacing)
            UpdateFacingFromVelocity(rb.linearVelocity);
    }

    private void UpdateFacingFromVelocity(Vector2 velocity)
    {
        if (Mathf.Abs(velocity.x) < CharacterPresentationConstants.FacingVelocityThreshold)
            return;

        bool wantLeft = velocity.x < 0f;
        if (wantLeft == facingLeft)
            return;

        facingLeft = wantLeft;
        spriteAnimator.SetFacingLeft(facingLeft);
    }

    /// <summary>Uses Z rotation instead of flipX. Offset corrects sprites drawn facing up at 0 degrees.</summary>
    public void EnableRotationalFacing(float angleOffsetDegrees = -90f)
    {
        usesRotationalFacing = true;
        rotationalFacingOffset = angleOffsetDegrees;
        facingLeft = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = false;
            spriteRenderer.flipY = false;
        }
    }

    /// <summary>Points the sprite toward a world position (rotation or horizontal flip).</summary>
    public void FaceToward(Vector2 worldPosition)
    {
        Vector2 direction = worldPosition - (Vector2)transform.position;
        RotateInDirection(direction);
    }

    /// <summary>Points the sprite along a direction vector (rotation or horizontal flip).</summary>
    public void RotateInDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        if (usesRotationalFacing)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + rotationalFacingOffset;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = false;
                spriteRenderer.flipY = false;
            }

            return;
        }

        float deltaX = direction.x;
        if (Mathf.Abs(deltaX) < CharacterPresentationConstants.FacingVelocityThreshold)
            return;

        bool wantLeft = deltaX < 0f;
        if (wantLeft == facingLeft)
            return;

        facingLeft = wantLeft;
        spriteAnimator.SetFacingLeft(facingLeft);
    }

    public virtual void ApplyWaveScaling(float healthMult, float damageMult, float speedMult = 1f)
    {
        HealthMultiplier = Mathf.Max(0.1f, healthMult);
        DamageMultiplier = Mathf.Max(0.1f, damageMult);
        SpeedMultiplier = Mathf.Max(0.1f, speedMult);

        float baseMax = ResolveMaxHealth();
        if (baseMax > 0f)
        {
            float newMax = baseMax * HealthMultiplier;
            float previousMax = baseMax * (HealthMultiplier / healthMult);
            float healthRatio = CurrentHealth > 0 && previousMax > 0
                ? CurrentHealth / previousMax
                : 1f;
            CurrentHealth = Mathf.Clamp(newMax * healthRatio, 0, newMax);
        }
        else
        {
            CurrentHealth *= HealthMultiplier;
        }
    }

    public virtual void TakeDamage(float damage)
    {
        if (IsDefeated || IsBurrowed) return;

        damage *= 1f - ResolveDamageResistance();

        if (damage > 0f && !spriteAnimator.IsPlayingOneShot)
            spriteAnimator.PlayInjured();

        DamageEvents.Notify(damage, transform.position, DamageNumberStyle.EnemyHit);

        CurrentHealth -= damage;
        CurrentHealth = Mathf.Max(CurrentHealth, 0);

        if (CurrentHealth <= 0)
            Die();
    }

    protected virtual void Die()
    {
        if (IsDefeated)
            return;

        isDying = true;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null)
            ai.enabled = false;

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = false;

        spriteAnimator.PlayDeath(FinishDeath);
    }

    private void FinishDeath()
    {
        if (isDead)
            return;

        isDead = true;
        isDying = false;

        VoidBlackHoleSystem.NotifyEnemyRemoved(this);
        ReleaseVoidEffects();
        ClearAllFire();
        ClearElectrocute();
        ClearAllPoison();
        ClearIce();
        LootDropper.DropFrom(this);
        WaveEvents.NotifyEnemyKilled(this);
        Destroy(gameObject);
    }

    public virtual void PerformAttack(Transform target) { }

    public virtual void ToggleBurrow()
    {
        if (!SupportsBurrow)
            return;

        IsBurrowed = !IsBurrowed;

        if (IsBurrowed)
        {
            RecordBurrowTime();
            VoidBlackHoleSystem.NotifyEnemyRemoved(this);
            ReleaseVoidEffects();
            ClearIce();
            ClearElectrocute();
            ClearAllPoison();
            if (spriteRenderer != null)
                spriteRenderer.color = new Color(0.4f, 0.4f, 0.4f, 0.5f);
            rb.linearVelocity = Vector2.zero;
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.color = ResolveTintColor();
        }
    }

    public float GetAttackRange() => ResolveAttackRange();
    public float GetDetectionRange() => ResolveDetectionRange();
    public float GetAggression() => ResolveAggression();
    public float GetAttackCooldown() => ResolveAttackCooldown();

    public float GetScaledMoveSpeed() =>
        ResolveMoveSpeed() * SpeedMultiplier * GetIceMoveSpeedMultiplier() * GetPoisonMoveSpeedMultiplier();
    public float GetScaledDamage() => ResolveDamage() * DamageMultiplier;

    protected float ResolveMaxHealth() =>
        activeCore?.maxHealth ?? (baseStats != null ? baseStats.MaxHealth : 30f);

    protected float ResolveMoveSpeed() =>
        activeCore?.moveSpeed ?? (baseStats != null ? baseStats.MoveSpeed : 3.5f);

    protected float ResolveDamage() =>
        activeCore?.damage ?? (baseStats != null ? baseStats.Damage : 8f);

    protected float ResolveAttackCooldown() =>
        activeCore?.attackCooldown ?? (baseStats != null ? baseStats.AttackCooldown : 1.2f);

    protected float ResolveDetectionRange() =>
        activeCore?.detectionRange ?? (baseStats != null ? baseStats.DetectionRange : 8f);

    protected float ResolveAttackRange() =>
        activeCore?.attackRange ?? (baseStats != null ? baseStats.AttackRange : 2f);

    protected float ResolveAggression() =>
        activeCore?.aggression ?? (baseStats != null ? baseStats.Aggression : 0.5f);

    protected float ResolveDamageResistance() =>
        activeCore?.damageResistance ?? (baseStats != null ? baseStats.DamageResistance : 0f);

    protected Sprite ResolveSprite() =>
        activeCore?.sprite ?? (baseStats != null ? baseStats.Sprite : null);

    protected Color ResolveTintColor() =>
        activeCore?.tintColor ?? (baseStats != null ? baseStats.TintColor : Color.white);

    protected float ResolveDisplayScale() =>
        activeCore?.displayScale ?? (baseStats != null ? baseStats.DisplayScale : 1f);
}