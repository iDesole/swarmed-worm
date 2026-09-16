using UnityEngine;

/// <summary>
/// Player character controller: movement, aim-facing, burrow, weapons, loot, and damage.
/// </summary>
/// <remarks>
/// Data-driven: stats and starting weapons come from <see cref="PlayerDatabase"/> via
/// <see cref="ApplyCharacterDefinition"/>. Aim direction always beats movement for facing (shoot left while walking right).
/// Five weapon slots (index 1–5) map to <see cref="WeaponSlotManager"/> hand transforms.
/// </remarks>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[DefaultExecutionOrder(-95)]
public class Player : MonoBehaviour, IDamageable, IBlobShadowHost
{
    [Header("Character")]
    [SerializeField] private int characterIndex;

    private PlayerCoreStats activeCore;
    private PlayerHandSlotDefinition[] activeHandSlots;
    private WeaponSelection startingWeapon = WeaponSelection.None;
    private bool initializedFromData;
    private bool startingWeaponsEquipped;

    private float maxHealth = 100f;
    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    private float maxShield = 50f;
    public float CurrentShield { get; private set; }
    public float MaxShield => maxShield;
    public bool IsBurrowed { get; private set; }

    private float moveSpeed = 6f;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Vector2 moveInput;
    private bool facingLeft;
    private float initialScaleMagnitude = 1f;
    private float visualDisplayScale = 1f;

    private CharacterAnimationClipEntry walkClip;
    private CharacterAnimationClipEntry injuredClip;
    private CharacterAnimationClipEntry burrowDownClip;
    private CharacterAnimationClipEntry burrowedClip;
    private CharacterAnimationClipEntry burrowUpClip;
    private CharacterAnimationClipEntry deathClip;

    private int walkFrameIndex;
    private float walkFrameTimer;
    private bool isPlayingWalk;

    private CharacterAnimationClipEntry oneShotClip;
    private int oneShotFrameIndex;
    private float oneShotFrameTimer;
    private System.Action oneShotComplete;
    private bool isBurrowTransitioning;

    private float burrowInvincibilityDuration = 3f;
    private float burrowCooldown = 8f;
    private float burrowTimer;
    private float burrowCooldownTimer;

    [Header("Input")]
    public KeyCode burrowKey = KeyCode.Space;
    public KeyCode shootKey = KeyCode.Mouse0;

    [Header("Weapons")]
    [HideInInspector] public WeaponBase[] handSlots = new WeaponBase[5];

    [Header("Inventory")]
    public PlayerInventory inventory = new();

    public string CharacterName => activeCore?.characterName ?? "Player";
    public int CharacterIndex => characterIndex;
    public PlayerCoreStats ActiveCore => activeCore;
    public bool IsUnderground => IsBurrowed && !isBurrowTransitioning;
    public bool HideBlobShadow => IsUnderground;

    public System.Action OnHealthChanged;
    public System.Action OnShieldChanged;
    public System.Action OnBurrowToggled;

    public bool CollectLootReward(LootReward reward)
    {
        if (IsDead)
            return false;

        switch (reward.kind)
        {
            case LootRewardKind.Weapon:
                if (!reward.weapon.IsValid)
                    return false;

                if (EquipNextAvailableSlotFromLoot(reward.weapon) >= 0)
                    return true;

                return AddInventoryReward(reward);

            default:
                return AddInventoryReward(reward);
        }
    }

    private int EquipNextAvailableSlotFromLoot(WeaponSelection selection)
    {
        for (int slot = 1; slot <= 5; slot++)
        {
            if (GetWeaponInSlot(slot) != null)
                continue;

            if (EquipSelectedWeapon(selection, slot))
                return slot;
        }

        return -1;
    }

    private bool AddInventoryReward(LootReward reward)
    {
        EnsureInventory();
        return inventory.TryAddLoot(reward);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        EnsureSpriteRenderer();
        initialScaleMagnitude = Mathf.Max(
            Mathf.Abs(transform.localScale.x),
            Mathf.Abs(transform.localScale.y),
            1f);

        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (GetComponent<WeaponSlotManager>() == null)
            gameObject.AddComponent<WeaponSlotManager>();

        if (GetComponent<PlayerInteractionController>() == null)
            gameObject.AddComponent<PlayerInteractionController>();

        EnsureInventory();
        facingLeft = spriteRenderer != null && spriteRenderer.flipX;
        SetPlayerLayer();

        if (GetComponent<PlayerBurrowSignal>() == null)
            gameObject.AddComponent<PlayerBurrowSignal>();
    }

    private void Start()
    {
        if (!initializedFromData)
        {
            LoadCharacterFromDatabase();
            CurrentHealth = maxHealth;
            CurrentShield = maxShield;
            ApplyFacing();
        }

        EnsureStartingWeapons();
    }

    public void Initialize(int index, PlayerCharacterDefinition definition)
    {
        characterIndex = Mathf.Max(0, index);
        startingWeaponsEquipped = false;
        ApplyCharacterDefinition(definition);
        CurrentHealth = maxHealth;
        CurrentShield = maxShield;
        ApplyFacing();
        initializedFromData = true;
        OnHealthChanged?.Invoke();
        OnShieldChanged?.Invoke();
    }

    public void SetCharacterIndex(int index)
    {
        characterIndex = Mathf.Max(0, index);
        startingWeaponsEquipped = false;
        LoadCharacterFromDatabase();
        CurrentHealth = maxHealth;
        CurrentShield = maxShield;
        ApplyFacing();
        EnsureStartingWeapons();
        OnHealthChanged?.Invoke();
        OnShieldChanged?.Invoke();
    }

    public void ApplyCharacterDefinition(PlayerCharacterDefinition definition)
    {
        if (definition?.core == null)
            return;

        startingWeaponsEquipped = false;
        activeCore = definition.core.Clone();
        activeCore.Normalize();

        maxHealth = activeCore.maxHealth;
        maxShield = activeCore.maxShield;
        moveSpeed = activeCore.moveSpeed;
        burrowInvincibilityDuration = activeCore.burrowDuration;
        burrowCooldown = activeCore.burrowCooldown;
        startingWeapon = definition.startingWeapon;
        activeHandSlots = definition.GetResolvedHandSlots();

        EnsureInventory();

        WeaponSlotManager slotManager = GetComponent<WeaponSlotManager>();
        slotManager?.ConfigureHandSlots(activeHandSlots);

        CacheAnimationClips(definition);
        ApplyVisualsFromCore();
    }

    private void CacheAnimationClips(PlayerCharacterDefinition definition)
    {
        walkClip = null;
        injuredClip = null;
        burrowDownClip = null;
        burrowedClip = null;
        burrowUpClip = null;
        deathClip = null;
        walkFrameIndex = 0;
        walkFrameTimer = 0f;
        isPlayingWalk = false;
        oneShotClip = null;
        oneShotFrameIndex = 0;
        oneShotFrameTimer = 0f;
        oneShotComplete = null;
        isBurrowTransitioning = false;

        if (definition == null)
            return;

        definition.TryGetAnimationEntry(out walkClip, "Walk", "Walking");
        definition.TryGetAnimationEntry(out injuredClip, "Injured", "Damaged", "Hurt");
        definition.TryGetAnimationEntry(out burrowDownClip, "BurrowDown", "Burrowing");
        definition.TryGetAnimationEntry(out burrowedClip, "Burrowed");
        definition.TryGetAnimationEntry(out burrowUpClip, "BurrowUp", "Rising");
        definition.TryGetAnimationEntry(out deathClip, "Death", "Die");
    }

    private void UpdateSpriteAnimation()
    {
        EnsureSpriteRenderer();
        if (spriteRenderer == null || activeCore == null)
            return;

        if (UpdateOneShotAnimation())
            return;

        if (IsDead)
            return;

        if (IsBurrowed)
        {
            if (burrowedClip != null && burrowedClip.HasFrames)
                ApplyClipFrame(burrowedClip, 0);

            return;
        }

        bool isMoving = moveInput.sqrMagnitude > 0.01f && !isBurrowTransitioning;

        if (!isMoving || walkClip == null || !walkClip.HasFrames)
        {
            if (isPlayingWalk || NeedsIdleSpriteRestore())
                ResetToIdleSprite();

            return;
        }

        if (!isPlayingWalk)
        {
            isPlayingWalk = true;
            walkFrameIndex = 0;
            walkFrameTimer = 0f;
            ApplyClipFrame(walkClip, 0);
        }

        walkFrameTimer += Time.deltaTime;
        float frameDuration = 1f / walkClip.framesPerSecond;

        while (walkFrameTimer >= frameDuration)
        {
            walkFrameTimer -= frameDuration;
            int nextFrame = walkFrameIndex + 1;

            if (nextFrame >= walkClip.frames.Length)
                nextFrame = walkClip.loop ? 0 : walkClip.frames.Length - 1;

            if (nextFrame == walkFrameIndex)
                break;

            walkFrameIndex = nextFrame;
            ApplyClipFrame(walkClip, walkFrameIndex);
        }
    }

    private bool UpdateOneShotAnimation()
    {
        if (oneShotClip == null || !oneShotClip.HasFrames)
            return false;

        oneShotFrameTimer += Time.deltaTime;
        float frameDuration = 1f / oneShotClip.framesPerSecond;

        while (oneShotFrameTimer >= frameDuration)
        {
            oneShotFrameTimer -= frameDuration;
            int nextFrame = oneShotFrameIndex + 1;

            if (nextFrame >= oneShotClip.frames.Length)
            {
                CompleteOneShotAnimation();
                return false;
            }

            oneShotFrameIndex = nextFrame;
            ApplyClipFrame(oneShotClip, oneShotFrameIndex);
        }

        return true;
    }

    private void PlayOneShotClip(CharacterAnimationClipEntry clip, System.Action onComplete = null)
    {
        if (clip == null || !clip.HasFrames)
        {
            onComplete?.Invoke();
            return;
        }

        oneShotClip = clip;
        oneShotFrameIndex = 0;
        oneShotFrameTimer = 0f;
        oneShotComplete = onComplete;
        isPlayingWalk = false;
        ApplyClipFrame(oneShotClip, 0);
    }

    private void CompleteOneShotAnimation()
    {
        System.Action callback = oneShotComplete;
        oneShotClip = null;
        oneShotFrameIndex = 0;
        oneShotFrameTimer = 0f;
        oneShotComplete = null;
        callback?.Invoke();
    }

    private void PlayInjuredAnimation()
    {
        if (IsBurrowed || isBurrowTransitioning || IsDead)
            return;

        PlayOneShotClip(injuredClip);
    }

    private void PlayDeathAnimation(System.Action onComplete)
    {
        if (deathClip != null && deathClip.HasFrames)
        {
            PlayOneShotClip(deathClip, onComplete);
            return;
        }

        EnsureSpriteRenderer();
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(0.35f, 0.35f, 0.35f, 0.85f);

        onComplete?.Invoke();
    }

    private PlayerBurrowSignal burrowSignal;

    private bool IsActionBlockedByBurrow => IsBurrowed || isBurrowTransitioning;

    private void ApplyClipFrame(CharacterAnimationClipEntry clip, int frameIndex)
    {
        if (clip?.frames == null || frameIndex < 0 || frameIndex >= clip.frames.Length)
            return;

        Sprite frame = clip.frames[frameIndex];
        if (frame != null && spriteRenderer != null)
            spriteRenderer.sprite = frame;
    }

    private bool NeedsIdleSpriteRestore()
    {
        if (spriteRenderer == null || activeCore?.sprite == null)
            return false;

        return spriteRenderer.sprite != activeCore.sprite;
    }

    private void ResetToIdleSprite()
    {
        isPlayingWalk = false;
        walkFrameIndex = 0;
        walkFrameTimer = 0f;

        if (spriteRenderer != null && activeCore?.sprite != null)
            spriteRenderer.sprite = activeCore.sprite;
    }

    private void SetBurrowBroadcast(bool broadcasting)
    {
        if (burrowSignal == null)
            burrowSignal = GetComponent<PlayerBurrowSignal>();

        burrowSignal?.SetBroadcasting(broadcasting);
    }

    private void LoadCharacterFromDatabase()
    {
        PlayerDatabase database = PlayerCatalog.Database;
        if (database == null)
        {
            GameLog.Warning("No PlayerDatabase found. Using fallback character stats.");
            ApplyCharacterDefinition(new PlayerCharacterDefinition
            {
                core = new PlayerCoreStats { characterName = "Fallback Worm" },
                startingWeapon = WeaponSelection.Projectile(0)
            });
            return;
        }

        PlayerCharacterDefinition character = database.GetCharacter(PlayerSelection.At(characterIndex))
            ?? database.GetDefaultCharacter();

        if (character == null)
        {
            GameLog.Warning("PlayerDatabase has no characters configured.");
            return;
        }

        ApplyCharacterDefinition(character);
    }

    private void EnsureSpriteRenderer()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void ApplyVisualsFromCore()
    {
        if (activeCore == null)
            return;

        EnsureSpriteRenderer();
        if (spriteRenderer == null)
            return;

        visualDisplayScale = activeCore.displayScale > 0f
            ? activeCore.displayScale
            : initialScaleMagnitude;

        PlayerSpriteUtility.ConfigureRenderer(spriteRenderer, activeCore.sprite, sortingOrder: 1);
        if (spriteRenderer.sprite == null)
            GameLog.Warning($"Character '{activeCore.characterName}' has no sprite assigned.");

        spriteRenderer.color = IsBurrowed ? spriteRenderer.color : activeCore.tintColor;
        spriteRenderer.flipX = facingLeft;
        ApplyDisplayScale();
    }

    private void ApplyDisplayScale()
    {
        float zScale = Mathf.Approximately(transform.localScale.z, 0f) ? 1f : transform.localScale.z;
        transform.localScale = new Vector3(visualDisplayScale, visualDisplayScale, zScale);
    }

    private Color ResolveTintColor()
    {
        if (activeCore == null)
            return Color.white;

        Color tint = activeCore.tintColor;
        if (tint.a <= 0f)
            tint.a = 1f;

        return tint;
    }

    private void SetPlayerLayer()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer != -1)
            gameObject.layer = playerLayer;
    }

    private void EnsureStartingWeapons()
    {
        if (startingWeaponsEquipped)
            return;

        SyncAimBeforeEquippingWeapons();

        for (int slot = 1; slot <= 5; slot++)
        {
            WeaponSelection selection = GetStartingWeaponForSlot(slot);
            if (!selection.IsValid)
            {
                ClearSlotWeapons(slot);
                continue;
            }

            EquipSelectedWeapon(selection, slot);
        }

        startingWeaponsEquipped = true;
    }

    private WeaponSelection GetStartingWeaponForSlot(int slotNumber1to5)
    {
        int index = slotNumber1to5 - 1;
        if (activeHandSlots == null ||
            index < 0 ||
            index >= activeHandSlots.Length ||
            activeHandSlots[index] == null)
        {
            return WeaponSelection.None;
        }

        return activeHandSlots[index].startingWeapon;
    }

    public bool EquipSelectedWeapon(WeaponSelection selection, int slotNumber1to5)
    {
        WeaponDatabase database = WeaponCatalog.Database;
        if (database == null)
        {
            GameLog.Warning("Cannot equip weapon. WeaponDatabase is not loaded.");
            return false;
        }

        WeaponBase weapon = database.CreateWeapon(selection, transform);
        if (weapon == null)
        {
            GameLog.Warning($"Failed to create weapon for selection {selection.type} index {selection.index}.");
            return false;
        }

        return EquipWeapon(weapon, slotNumber1to5);
    }

    public int EquipNextAvailableSlot(WeaponBase weapon)
    {
        if (weapon == null) return -1;

        for (int slot = 1; slot <= 5; slot++)
        {
            if (GetWeaponInSlot(slot) != null)
                continue;

            EquipWeapon(weapon, slot);
            return slot;
        }

        return -1;
    }

    // Input and aim run in Update; physics movement runs in FixedUpdate for stable collisions.
    private void Update()
    {
        if (IsDead)
        {
            UpdateSpriteAnimation();
            return;
        }

        if (Time.timeScale <= 0f)
        {
            moveInput = Vector2.zero;
            return;
        }

        HandleMovementInput();
        UpdateSpriteAnimation();
        UpdateAimPresentation(); // aim direction overrides movement for sprite facing
        HandleCombatInput();
        HandleBurrowInput();
    }

    private void FixedUpdate()
    {
        if (IsDead || Time.timeScale <= 0f || isBurrowTransitioning)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = moveInput * moveSpeed;
    }

    private void HandleMovementInput()
    {
        if (isBurrowTransitioning)
        {
            moveInput = Vector2.zero;
            return;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(horizontal, vertical).normalized;
    }

    public bool WeaponFacesLeft => facingLeft;

    private void UpdateAimPresentation()
    {
        bool wantLeft = IsAimingLeft();
        if (wantLeft != facingLeft)
        {
            facingLeft = wantLeft;
            ApplyFacing();
            GetComponent<WeaponSlotManager>()?.SetFacingLeft(facingLeft);
        }
    }

    private bool IsAimingLeft() =>
        AimUtility.IsAimingLeft(GetAimDirection(), facingLeft);

    private Vector2 GetAimDirection()
    {
        Vector2 fallback = facingLeft ? Vector2.left : Vector2.right;
        return AimUtility.GetDirection(transform, fallback);
    }

    private void ApplyFacing()
    {
        EnsureSpriteRenderer();
        if (spriteRenderer != null)
            spriteRenderer.flipX = facingLeft;

        ApplyDisplayScale();
    }

    private void SyncAimBeforeEquippingWeapons()
    {
        facingLeft = IsAimingLeft();
        ApplyFacing();
        GetComponent<WeaponSlotManager>()?.SetFacingLeft(facingLeft);
    }

    private void HandleCombatInput()
    {
        if (Input.GetKey(shootKey))
            FireAllWeapons();
    }

    public void FireAllWeapons()
    {
        if (IsDead || IsActionBlockedByBurrow) return;

        foreach (WeaponBase weapon in handSlots)
            weapon?.Fire();
    }

    public void TakeDamage(float incomingDamage) => TakeDamage(incomingDamage, false);

    public void TakeDamage(float incomingDamage, bool attackerCanHitUnderground)
    {
        if (IsDead || isBurrowTransitioning)
            return;

        if (IsUnderground && !attackerCanHitUnderground)
            return;

        if (IsUnderground && activeCore != null)
            incomingDamage *= 1f - activeCore.burrowDamageReduction;

        if (incomingDamage <= 0f)
            return;

        if (incomingDamage > 0f)
            PlayInjuredAnimation();

        DamageEvents.Notify(incomingDamage, transform.position, DamageNumberStyle.PlayerHit);

        float remainingDamage = incomingDamage;

        if (CurrentShield > 0)
        {
            float shieldDamage = Mathf.Min(remainingDamage, CurrentShield);
            CurrentShield -= shieldDamage;
            remainingDamage -= shieldDamage;
            OnShieldChanged?.Invoke();
        }

        if (remainingDamage > 0)
        {
            CurrentHealth -= remainingDamage;
            CurrentHealth = Mathf.Max(CurrentHealth, 0);
            OnHealthChanged?.Invoke();

            if (CameraShake.Instance != null)
            {
                float shakeScale = Mathf.Clamp01(remainingDamage / 20f);
                CameraShake.Instance.Shake(0.6f, 0.25f, shakeScale);
            }
        }

        if (CurrentHealth <= 0)
            Die();
    }

    public void Heal(float amount)
    {
        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke();
    }

    public void RegenerateShield(float amount)
    {
        CurrentShield = Mathf.Min(CurrentShield + amount, maxShield);
        OnShieldChanged?.Invoke();
    }

    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;
        IsBurrowed = false;
        isBurrowTransitioning = false;
        SetBurrowBroadcast(false);
        moveInput = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        PlayerLootDropper.DropFrom(this);
        StripAllWeapons();
        PlayDeathAnimation(() => WaveEvents.NotifyPlayerDeathPresentationFinished(this));
        WaveEvents.NotifyPlayerDied(this);
    }

    private void StripAllWeapons()
    {
        for (int slot = 1; slot <= 5; slot++)
            ClearSlotWeapons(slot);

        if (inventory == null)
            return;

        for (int i = 0; i < PlayerInventory.WeaponSlotCount; i++)
            inventory.ClearWeaponSlot(i);

        for (int i = 0; i < PlayerInventory.InventorySlotCount; i++)
        {
            InventorySlotData slot = inventory.GetInventorySlot(i);
            if (slot.kind == InventoryItemKind.Weapon)
                inventory.ClearInventorySlot(i);
        }
    }

    private void HandleBurrowInput()
    {
        if (burrowCooldownTimer > 0)
            burrowCooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(burrowKey))
            ToggleBurrow();

        if (IsBurrowed && !isBurrowTransitioning)
        {
            burrowTimer -= Time.deltaTime;
            if (burrowTimer <= 0)
                BeginBurrowUp();
        }
    }

    public void ToggleBurrow()
    {
        if (IsDead || isBurrowTransitioning)
            return;

        if (IsBurrowed)
            BeginBurrowUp();
        else if (burrowCooldownTimer <= 0)
            BeginBurrowDown();
    }

    private void BeginBurrowDown()
    {
        if (IsDead || IsBurrowed || isBurrowTransitioning || burrowCooldownTimer > 0f)
            return;

        isBurrowTransitioning = true;
        moveInput = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        isPlayingWalk = false;

        if (burrowDownClip == null || !burrowDownClip.HasFrames)
        {
            EnterBurrowedState();
            return;
        }

        PlayOneShotClip(burrowDownClip, EnterBurrowedState);
    }

    private void EnterBurrowedState()
    {
        isBurrowTransitioning = false;
        IsBurrowed = true;
        burrowTimer = burrowInvincibilityDuration;
        SetBurrowBroadcast(true);

        if (burrowedClip != null && burrowedClip.HasFrames)
            ApplyClipFrame(burrowedClip, 0);

        OnBurrowToggled?.Invoke();
    }

    private void BeginBurrowUp()
    {
        if (IsDead || !IsBurrowed || isBurrowTransitioning)
            return;

        IsBurrowed = false;
        SetBurrowBroadcast(false);
        isBurrowTransitioning = true;
        moveInput = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        if (burrowUpClip == null || !burrowUpClip.HasFrames)
        {
            ExitBurrowedState();
            return;
        }

        PlayOneShotClip(burrowUpClip, ExitBurrowedState);
    }

    private void ExitBurrowedState()
    {
        isBurrowTransitioning = false;
        SetBurrowBroadcast(false);
        burrowCooldownTimer = burrowCooldown;
        ResetToIdleSprite();

        if (spriteRenderer != null)
            spriteRenderer.color = ResolveTintColor();

        OnBurrowToggled?.Invoke();
    }

    public bool EquipWeapon(WeaponBase weapon, int slotNumber1to5)
    {
        if (weapon == null || slotNumber1to5 < 1 || slotNumber1to5 > 5) return false;

        int index = slotNumber1to5 - 1;
        ClearSlotWeapons(slotNumber1to5);

        handSlots[index] = weapon;
        weapon.OnEquipped(gameObject, slotNumber1to5);
        SyncInventoryWeaponSlot(slotNumber1to5, weapon.sourceSelection, weapon.weaponName);
        return true;
    }

    private void ClearSlotWeapons(int slotNumber1to5)
    {
        int index = slotNumber1to5 - 1;

        if (handSlots[index] != null)
        {
            handSlots[index].OnUnequipped();
            Destroy(handSlots[index].gameObject);
            handSlots[index] = null;
        }

        inventory?.ClearWeaponSlot(index);

        Transform slotTransform = GetHandSlotTransform(slotNumber1to5);
        if (slotTransform == null)
            return;

        for (int childIndex = slotTransform.childCount - 1; childIndex >= 0; childIndex--)
        {
            Transform child = slotTransform.GetChild(childIndex);
            if (child.GetComponent<WeaponBase>() != null)
                Destroy(child.gameObject);
        }
    }

    private Transform GetHandSlotTransform(int slotNumber1to5)
    {
        WeaponSlotManager slotManager = GetComponent<WeaponSlotManager>();
        if (slotManager != null)
            return slotManager.GetHandSlotTransform(slotNumber1to5);

        return transform.Find($"HandSlot{slotNumber1to5}");
    }

    public WeaponBase GetWeaponInSlot(int slotNumber1to5)
    {
        if (slotNumber1to5 < 1 || slotNumber1to5 > 5) return null;
        return handSlots[slotNumber1to5 - 1];
    }

    public int GetEquippedWeaponCount()
    {
        int count = 0;
        foreach (WeaponBase weapon in handSlots)
        {
            if (weapon != null)
                count++;
        }

        return count;
    }

    public int GetTotalWeaponCount() => inventory?.GetWeaponCount() ?? 0;

    public bool TryDeleteInventorySlot(InventorySlotCategory category, int index)
    {
        EnsureInventory();

        InventorySlotData slot = category switch
        {
            InventorySlotCategory.Weapon => inventory.GetWeaponSlot(index),
            InventorySlotCategory.Artifact => inventory.GetArtifactSlot(index),
            _ => inventory.GetInventorySlot(index)
        };

        if (slot.IsEmpty)
            return false;

        switch (category)
        {
            case InventorySlotCategory.Weapon:
                ClearSlotWeapons(index + 1);
                break;

            case InventorySlotCategory.Artifact:
                inventory.ClearArtifactSlot(index);
                break;

            default:
                inventory.ClearInventorySlot(index);
                break;
        }

        return true;
    }

    public float GetAttackSpeedMultiplier() =>
        activeCore != null ? activeCore.attackSpeedMultiplier : 1f;

    public float GetDamageMultiplier() =>
        activeCore != null ? activeCore.damageMultiplier : 1f;

    public float GetCritChance() =>
        activeCore != null ? activeCore.critChance : 0f;

    public float GetCritDamageMultiplier() =>
        activeCore != null ? activeCore.critDamageMultiplier : 1.5f;

    private void EnsureInventory()
    {
        inventory ??= new PlayerInventory();
    }

    private void SyncInventoryWeaponSlot(int slotNumber1to5, WeaponSelection selection, string displayName)
    {
        EnsureInventory();

        WeaponDatabase database = WeaponCatalog.Database;
        string resolvedName = displayName;
        if (database != null && selection.IsValid)
        {
            string databaseName = database.GetWeaponName(selection);
            if (!string.IsNullOrWhiteSpace(databaseName))
                resolvedName = databaseName;
        }

        inventory.SyncWeaponSlotFromEquipped(slotNumber1to5, selection, resolvedName);
    }
}