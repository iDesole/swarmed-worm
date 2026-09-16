using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Inventory overlay: weapon row, item grid, artifact row, and stats panel.
/// Toggle with I. Pauses gameplay while open.
/// </summary>
public class PlayerInventoryUI : MonoBehaviour
{
    private const float SlotSize = 72f;
    private const float SlotGap = 8f;
    private const float SectionGap = 20f;
    private const float SectionHeaderHeight = 34f;
    private const float ContentPadding = 28f;
    private const float ColumnGap = 20f;
    private const float StatsColumnWidth = 400f;
    private const float SectionHeaderGap = 10f;
    private const float SectionPanelPadding = 14f;
    private const float SlotBorderInset = 2f;
    private const float PlayerCardHeight = 280f;
    private const float CurrencyBarHeight = 44f;
    private const KeyCode ToggleKey = KeyCode.I;
    private const KeyCode CloseKey = KeyCode.Escape;
    private const KeyCode DeleteKey = KeyCode.E;

    private static PlayerInventoryUI instance;

    /// <summary>True while the inventory overlay is open (blocks camera scroll zoom).</summary>
    public static bool BlocksCameraZoom => instance != null && instance.isOpen;

    private static readonly float UnifiedGridWidth =
        PlayerInventory.InventoryColumns * SlotSize + (PlayerInventory.InventoryColumns - 1) * SlotGap;

    private static readonly Color EmptySlotBackground = new(0.08f, 0.09f, 0.13f, 0.92f);
    private static readonly Color EmptySlotFrame = new(0.16f, 0.17f, 0.24f, 1f);
    private static readonly Color FilledSlotBackground = new(0.14f, 0.16f, 0.22f, 0.98f);
    private static readonly Color SectionPanelColor = new(0.05f, 0.06f, 0.09f, 0.72f);
    private static readonly Color SectionAccentColor = new(0.22f, 0.38f, 0.58f, 0.45f);

    private Player player;
    private GameObject rootPanel;
    private bool isOpen;
    private float savedTimeScale = 1f;

    private readonly List<SlotView> weaponSlotViews = new();
    private readonly List<SlotView> inventorySlotViews = new();
    private readonly List<SlotView> artifactSlotViews = new();
    private ScrollRect statsScrollRect;
    private RectTransform statsContent;
    private TextMeshProUGUI statsScrollText;
    private string lastStatsText = string.Empty;

    private ScrollRect hoverDetailsScrollRect;
    private RectTransform hoverDetailsContent;
    private TextMeshProUGUI hoverDetailsTitle;
    private TextMeshProUGUI hoverDetailsSubtitle;
    private TextMeshProUGUI hoverDetailsBody;
    private Image hoverDetailsAccent;
    private string lastHoverDetailsKey = string.Empty;

    private TextMeshProUGUI characterNameLabel;
    private TextMeshProUGUI currencyLabel;
    private TextMeshProUGUI currencyAmountLabel;
    private int lastCurrencyTotal = -1;
    private Image characterPortrait;
    private Image characterPortraitFrame;

    private InventorySlotCategory? hoveredCategory;
    private int hoveredSlotIndex = -1;

    private GameObject deleteDialogRoot;
    private TextMeshProUGUI deleteDialogMessage;
    private GameObject deleteDialogYesButton;
    private GameObject deleteDialogNoButton;
    private InventorySlotCategory? pendingDeleteCategory;
    private int pendingDeleteSlotIndex = -1;

    private TMP_FontAsset font;
    private Sprite whiteSprite;
    private int toggleInputCooldownFrames;

    private struct SlotView
    {
        public Image background;
        public Image border;
        public Image icon;
        public TextMeshProUGUI abbreviation;
        public TextMeshProUGUI quantity;
    }

    private void Awake()
    {
        instance = this;
        EnsureEventSystem();
        font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        whiteSprite = CreateWhiteSprite();
        BuildUI();
        ForceClosed();
    }

    private void Start()
    {
        toggleInputCooldownFrames = 2;
        BindPlayer(PlayerCatalog.ActivePlayer);
    }

    private void OnEnable() => PlayerCatalog.PlayerSpawned += BindPlayer;

    private void OnDisable()
    {
        PlayerCatalog.PlayerSpawned -= BindPlayer;
        RestoreGameplayTime();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;

        BindPlayer(null);
        RestoreGameplayTime();
    }

    private void Update()
    {
        if (toggleInputCooldownFrames > 0)
        {
            toggleInputCooldownFrames--;
        }
        else if (Input.GetKeyDown(ToggleKey) && !CharacterShopUI.IsOpen)
        {
            SetOpen(!isOpen);
        }
        else if (isOpen && Input.GetKeyDown(CloseKey))
        {
            if (IsDeleteDialogOpen())
                HideDeleteDialog();
            else
                SetOpen(false);
        }

        if (!isOpen)
            return;

        if (!IsDeleteDialogOpen() && Input.GetKeyDown(DeleteKey))
            TryRequestDeleteHoveredItem();

        RefreshStats();
        RefreshCurrency();
        UpdateInventoryHover();
    }

    private void BindPlayer(Player newPlayer)
    {
        if (player?.inventory != null)
            player.inventory.OnChanged -= RefreshSlots;

        player = newPlayer;
        if (player == null)
            return;

        if (player.inventory == null)
            player.inventory = new PlayerInventory();

        player.inventory.OnChanged += RefreshSlots;
        lastCurrencyTotal = -1;
        RefreshSlots();
        RefreshStats();
        RefreshCurrency();
        RefreshPortrait();
    }

    private void ForceClosed()
    {
        isOpen = false;
        if (rootPanel != null)
            rootPanel.SetActive(false);

        ClearHoverState();
        HideDeleteDialog();
    }

    private void SetOpen(bool open)
    {
        if (isOpen == open)
            return;

        if (open)
        {
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            isOpen = true;
            ClearHoverState();
            RefreshSlots();
            RefreshStats();
            RefreshCurrency();
            RefreshPortrait();
            Canvas.ForceUpdateCanvases();
            RefreshStatsScrollContent();
        }
        else
        {
            Time.timeScale = savedTimeScale;
            isOpen = false;
            ClearHoverState();
            HideDeleteDialog();
        }

        ApplyOpenState();
    }

    private void ApplyOpenState()
    {
        if (rootPanel != null)
            rootPanel.SetActive(isOpen);
    }

    private void RestoreGameplayTime()
    {
        if (!isOpen)
            return;

        Time.timeScale = savedTimeScale;
        isOpen = false;
        ClearHoverState();
        ApplyOpenState();
    }

    private void UpdateInventoryHover()
    {
        if (!TryGetSlotUnderPointer(out InventorySlotCategory category, out int index))
        {
            if (hoveredCategory.HasValue)
                ClearHoverState();
            return;
        }

        if (hoveredCategory == category && hoveredSlotIndex == index)
            return;

        SetHoveredSlot(category, index);
    }

    private void SetHoveredSlot(InventorySlotCategory category, int index)
    {
        HighlightHoveredSlot(false);

        hoveredCategory = category;
        hoveredSlotIndex = index;

        InventorySlotData slot = GetSlot(category, index);
        if (slot.IsEmpty)
        {
            ClearHoverDetails();
            return;
        }

        ShowHoverDetails(slot);
        HighlightHoveredSlot(true);
    }

    private void ClearHoverState()
    {
        HighlightHoveredSlot(false);
        hoveredCategory = null;
        hoveredSlotIndex = -1;
        ClearHoverDetails();
    }

    private bool IsDeleteDialogOpen() => deleteDialogRoot != null && deleteDialogRoot.activeSelf;

    private void TryRequestDeleteHoveredItem()
    {
        if (!hoveredCategory.HasValue || hoveredSlotIndex < 0 || player == null)
            return;

        InventorySlotData slot = GetSlot(hoveredCategory.Value, hoveredSlotIndex);
        if (slot.IsEmpty)
            return;

        if (slot.kind == InventoryItemKind.Weapon && player.GetTotalWeaponCount() <= 1)
        {
            ShowDeleteDialog("You can't delete your only weapon.", blocked: true);
            return;
        }

        string itemName = ResolveDeleteItemName(slot);
        pendingDeleteCategory = hoveredCategory;
        pendingDeleteSlotIndex = hoveredSlotIndex;
        ShowDeleteDialog($"Are you sure you want to delete \"{itemName}\"?", blocked: false);
    }

    private void ConfirmDeleteHoveredItem()
    {
        if (!pendingDeleteCategory.HasValue || pendingDeleteSlotIndex < 0 || player == null)
        {
            HideDeleteDialog();
            return;
        }

        InventorySlotData slot = GetSlot(pendingDeleteCategory.Value, pendingDeleteSlotIndex);
        if (!slot.IsEmpty &&
            slot.kind == InventoryItemKind.Weapon &&
            player.GetTotalWeaponCount() <= 1)
        {
            ShowDeleteDialog("You can't delete your only weapon.", blocked: true);
            return;
        }

        player.TryDeleteInventorySlot(pendingDeleteCategory.Value, pendingDeleteSlotIndex);
        HideDeleteDialog();
        ClearHoverState();
        RefreshSlots();
        RefreshStats();
        RefreshCurrency();
    }

    private void HideDeleteDialog()
    {
        pendingDeleteCategory = null;
        pendingDeleteSlotIndex = -1;

        if (deleteDialogRoot != null)
            deleteDialogRoot.SetActive(false);
    }

    private void ShowDeleteDialog(string message, bool blocked)
    {
        if (deleteDialogRoot == null || deleteDialogMessage == null)
            return;

        deleteDialogMessage.text = message;
        deleteDialogRoot.SetActive(true);

        if (deleteDialogYesButton != null)
            deleteDialogYesButton.SetActive(!blocked);

        if (deleteDialogNoButton != null)
        {
            TextMeshProUGUI noLabel = deleteDialogNoButton.GetComponentInChildren<TextMeshProUGUI>();
            if (noLabel != null)
                noLabel.text = blocked ? "OK" : "No";
        }
    }

    private static string ResolveDeleteItemName(InventorySlotData slot)
    {
        if (!string.IsNullOrWhiteSpace(slot.displayName))
            return slot.displayName;

        if (slot.kind == InventoryItemKind.Weapon)
        {
            WeaponDatabase database = WeaponCatalog.Database;
            if (database != null && slot.weapon.IsValid)
            {
                string weaponName = database.GetWeaponName(slot.weapon);
                if (!string.IsNullOrWhiteSpace(weaponName))
                    return weaponName;
            }
        }

        if (!string.IsNullOrWhiteSpace(slot.itemId))
            return slot.itemId;

        return slot.kind.ToString();
    }

    private bool TryGetSlotUnderPointer(out InventorySlotCategory category, out int index)
    {
        category = default;
        index = -1;

        if (EventSystem.current == null)
            return false;

        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            InventorySlotHover hover = result.gameObject.GetComponentInParent<InventorySlotHover>();
            if (hover == null)
                continue;

            category = hover.Category;
            index = hover.SlotIndex;
            return true;
        }

        return false;
    }

    private InventorySlotData GetSlot(InventorySlotCategory category, int index)
    {
        if (player?.inventory == null)
            return InventorySlotData.EmptySlot;

        return category switch
        {
            InventorySlotCategory.Weapon => player.inventory.GetWeaponSlot(index),
            InventorySlotCategory.Artifact => player.inventory.GetArtifactSlot(index),
            _ => player.inventory.GetInventorySlot(index)
        };
    }

    private void RefreshSlots()
    {
        if (player?.inventory == null)
            return;

        PlayerInventory inventory = player.inventory;
        RefreshSlotGroup(weaponSlotViews, index => inventory.GetWeaponSlot(index));
        RefreshSlotGroup(inventorySlotViews, index => inventory.GetInventorySlot(index));
        RefreshSlotGroup(artifactSlotViews, index => inventory.GetArtifactSlot(index));

        if (hoveredCategory.HasValue && hoveredSlotIndex >= 0)
            HighlightHoveredSlot(true);
    }

    private void RefreshSlotGroup(List<SlotView> views, System.Func<int, InventorySlotData> getSlot)
    {
        WeaponDatabase database = WeaponCatalog.Database;

        for (int i = 0; i < views.Count; i++)
        {
            SlotView view = views[i];
            InventorySlotData slot = getSlot(i);
            bool hasItem = !slot.IsEmpty;

            ApplySlotElementStyle(view, slot, database, hasItem);

            Sprite icon = ResolveSlotSprite(slot, database);
            bool hasIcon = icon != null;
            view.icon.enabled = hasIcon;
            view.icon.sprite = icon;
            view.icon.color = ResolveSlotIconTint(slot, database, hasIcon);

            if (!hasItem)
            {
                view.abbreviation.text = string.Empty;
                view.abbreviation.enabled = false;
                view.quantity.text = string.Empty;
                view.quantity.enabled = false;
                continue;
            }

            if (!hasIcon)
            {
                view.abbreviation.text = GetAbbreviation(slot);
                view.abbreviation.color = KindAccentColor(slot.kind);
                view.abbreviation.enabled = true;
            }
            else
            {
                view.abbreviation.text = string.Empty;
                view.abbreviation.enabled = false;
            }

            if (slot.quantity > 1)
            {
                view.quantity.text = slot.quantity.ToString();
                view.quantity.enabled = true;
            }
            else
            {
                view.quantity.text = string.Empty;
                view.quantity.enabled = false;
            }
        }
    }

    private void RefreshCurrency()
    {
        if (currencyAmountLabel == null)
            return;

        int total = player?.inventory != null ? player.inventory.GetCurrencyTotal() : 0;
        if (total == lastCurrencyTotal)
            return;

        lastCurrencyTotal = total;
        currencyAmountLabel.text = total.ToString("N0");

        if (currencyLabel != null && player?.inventory != null)
            currencyLabel.text = player.inventory.GetCurrencyDisplayName().ToUpperInvariant();
    }

    private void RefreshStats()
    {
        if (player == null)
            return;

        PlayerCoreStats core = player.ActiveCore;
        if (characterNameLabel != null)
            characterNameLabel.text = player.CharacterName;

        string nextStatsText = string.Join("\n", BuildStatLines(core));
        if (statsScrollText == null || nextStatsText == lastStatsText)
            return;

        lastStatsText = nextStatsText;
        statsScrollText.text = nextStatsText;
        RefreshStatsScrollContent();
    }

    private List<string> BuildStatLines(PlayerCoreStats core)
    {
        return new List<string>
        {
            $"Health: {Mathf.CeilToInt(player.CurrentHealth)} / {Mathf.CeilToInt(player.MaxHealth)}",
            $"Shield: {Mathf.CeilToInt(player.CurrentShield)} / {Mathf.CeilToInt(GetMaxShield())}",
            $"Move Speed: {FormatStat(core?.moveSpeed ?? 0f)}",
            $"Damage: x{FormatStat(player.GetDamageMultiplier())}",
            $"Attack Speed: x{FormatStat(player.GetAttackSpeedMultiplier())}",
            $"Crit Chance: {FormatPercent(player.GetCritChance())}",
            $"Crit Damage: x{FormatStat(player.GetCritDamageMultiplier())}",
            $"Shield Regen: {FormatStat(core?.shieldRegenRate ?? 0f)}/s",
            $"Dodge Speed: x{FormatStat(core?.dodgeSpeedMultiplier ?? 1f)}",
            $"Lifesteal: {FormatPercent(core?.lifesteal ?? 0f)}",
            $"Knockback Resist: {FormatPercent(core?.knockbackResistance ?? 0f)}",
            $"Burrow Duration: {FormatStat(core?.burrowDuration ?? 0f)}s",
            $"Burrow Cooldown: {FormatStat(core?.burrowCooldown ?? 0f)}s",
            $"Burrow Damage Reduction: {FormatPercent(core?.burrowDamageReduction ?? 0f)}",
            $"Invulnerability Frames: {FormatStat(core?.invulnerabilityFrames ?? 0f)}s",
            $"Equipped Weapons: {player.GetEquippedWeaponCount()} / {PlayerInventory.WeaponSlotCount}"
        };
    }

    private void RefreshStatsScrollContent()
    {
        if (statsScrollText == null || statsContent == null)
            return;

        statsScrollText.ForceMeshUpdate();
        float textWidth = statsScrollText.rectTransform.rect.width;
        if (textWidth <= 0f)
            textWidth = 320f;

        Vector2 preferred = statsScrollText.GetPreferredValues(statsScrollText.text, textWidth, 0f);
        float contentHeight = Mathf.Max(preferred.y + 16f, statsScrollRect != null
            ? statsScrollRect.viewport.rect.height
            : preferred.y + 16f);

        statsContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
        statsScrollRect.verticalNormalizedPosition = 1f;
    }

    private void RefreshPortrait()
    {
        if (characterPortrait == null)
            return;

        Sprite sprite = ResolveActivePlayerSprite();
        characterPortrait.enabled = sprite != null;
        characterPortrait.sprite = sprite;
        characterPortrait.color = Color.white;
    }

    private static Sprite ResolveActivePlayerSprite()
    {
        GameManager gameManager = GameManager.Instance ?? Object.FindFirstObjectByType<GameManager>();
        if (gameManager != null)
            return gameManager.GetActivePlayerSprite();

        Player activePlayer = PlayerCatalog.ActivePlayer;
        if (activePlayer == null)
            return null;

        SpriteRenderer spriteRenderer = activePlayer.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
            return spriteRenderer.sprite;

        return activePlayer.ActiveCore?.sprite;
    }

    private void ShowHoverDetails(InventorySlotData slot)
    {
        if (hoverDetailsTitle == null || hoverDetailsBody == null)
            return;

        if (!InventoryTooltipBuilder.TryBuild(slot, out string title, out string subtitle, out string body))
        {
            ClearHoverDetails();
            return;
        }

        string detailsKey = $"{slot.kind}|{title}|{subtitle}|{body}";
        if (detailsKey == lastHoverDetailsKey)
            return;

        lastHoverDetailsKey = detailsKey;
        hoverDetailsTitle.text = title;
        if (hoverDetailsSubtitle != null)
        {
            hoverDetailsSubtitle.text = subtitle;
            hoverDetailsSubtitle.enabled = !string.IsNullOrWhiteSpace(subtitle);
        }

        hoverDetailsBody.text = body;
        ApplyHoverDetailsStyle(slot, WeaponCatalog.Database);
        RefreshHoverDetailsScrollContent();
    }

    private void ClearHoverDetails()
    {
        lastHoverDetailsKey = string.Empty;

        if (hoverDetailsTitle != null)
        {
            hoverDetailsTitle.text = "Item Details";
            hoverDetailsTitle.color = new Color(0.75f, 0.78f, 0.85f);
        }

        if (hoverDetailsSubtitle != null)
        {
            hoverDetailsSubtitle.text = string.Empty;
            hoverDetailsSubtitle.enabled = false;
        }

        if (hoverDetailsBody != null)
            hoverDetailsBody.text = "Hover a weapon, item, or artifact to view its stats.";

        if (hoverDetailsAccent != null)
            hoverDetailsAccent.color = new Color(0.35f, 0.62f, 0.9f, 0.35f);

        RefreshHoverDetailsScrollContent();
    }

    private void RefreshHoverDetailsScrollContent()
    {
        if (hoverDetailsBody == null || hoverDetailsContent == null || hoverDetailsScrollRect == null)
            return;

        hoverDetailsTitle.ForceMeshUpdate();
        if (hoverDetailsSubtitle != null && hoverDetailsSubtitle.enabled)
            hoverDetailsSubtitle.ForceMeshUpdate();
        hoverDetailsBody.ForceMeshUpdate();

        float textWidth = hoverDetailsContent.rect.width - 16f;
        if (textWidth <= 0f)
            textWidth = 320f;

        float yOffset = 0f;
        float titleHeight = hoverDetailsTitle.preferredHeight + 4f;

        var titleRect = hoverDetailsTitle.rectTransform;
        titleRect.anchoredPosition = new Vector2(0f, yOffset);
        titleRect.sizeDelta = new Vector2(-16f, titleHeight);
        yOffset -= titleHeight;

        float subtitleHeight = 0f;
        if (hoverDetailsSubtitle != null && hoverDetailsSubtitle.enabled)
        {
            subtitleHeight = hoverDetailsSubtitle.preferredHeight + 6f;
            var subtitleRect = hoverDetailsSubtitle.rectTransform;
            subtitleRect.anchoredPosition = new Vector2(0f, yOffset);
            subtitleRect.sizeDelta = new Vector2(-16f, subtitleHeight);
            yOffset -= subtitleHeight;
        }

        Vector2 bodyPreferred = hoverDetailsBody.GetPreferredValues(hoverDetailsBody.text, textWidth, 0f);
        var bodyRect = hoverDetailsBody.rectTransform;
        bodyRect.anchoredPosition = new Vector2(0f, yOffset - 4f);
        bodyRect.sizeDelta = new Vector2(-16f, bodyPreferred.y);

        float contentHeight = Mathf.Max(-yOffset + bodyPreferred.y + 20f, hoverDetailsScrollRect.viewport.rect.height);
        hoverDetailsContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
        hoverDetailsScrollRect.verticalNormalizedPosition = 1f;
    }

    private void HighlightHoveredSlot(bool highlighted)
    {
        if (!hoveredCategory.HasValue || hoveredSlotIndex < 0)
            return;

        SlotView? view = GetHoveredSlotView();
        if (!view.HasValue)
            return;

        SlotView slotView = view.Value;
        InventorySlotData slot = GetSlot(hoveredCategory.Value, hoveredSlotIndex);
        WeaponDatabase database = WeaponCatalog.Database;
        ApplySlotElementStyle(slotView, slot, database, !slot.IsEmpty, highlighted);
    }

    private SlotView? GetHoveredSlotView()
    {
        if (!hoveredCategory.HasValue || hoveredSlotIndex < 0)
            return null;

        List<SlotView> views = hoveredCategory.Value switch
        {
            InventorySlotCategory.Weapon => weaponSlotViews,
            InventorySlotCategory.Artifact => artifactSlotViews,
            _ => inventorySlotViews
        };

        if (hoveredSlotIndex >= views.Count)
            return null;

        return views[hoveredSlotIndex];
    }

    private static void ApplySlotElementStyle(SlotView view, InventorySlotData slot, WeaponDatabase database, bool hasItem,
        bool highlighted = false)
    {
        if (!hasItem)
        {
            view.background.color = EmptySlotBackground;
            view.border.color = highlighted
                ? Color.Lerp(EmptySlotFrame, Color.white, 0.1f)
                : EmptySlotFrame;
            return;
        }

        Color baseBackground = ResolveSlotBackgroundColor(slot, database);
        Color baseBorder = ResolveSlotBorderColor(slot, database);

        view.background.color = highlighted
            ? Color.Lerp(baseBackground, Color.white, 0.14f)
            : baseBackground;
        view.border.color = highlighted
            ? Color.Lerp(baseBorder, Color.white, 0.18f)
            : baseBorder;
    }

    private static Color ResolveSlotBackgroundColor(InventorySlotData slot, WeaponDatabase database)
    {
        if (slot.kind == InventoryItemKind.Weapon && database != null && slot.weapon.IsValid)
            return WeaponElementColors.GetSlotBackgroundTint(database.GetWeaponElementProfile(slot.weapon));

        return FilledSlotBackground;
    }

    private static Color ResolveSlotBorderColor(InventorySlotData slot, WeaponDatabase database)
    {
        if (slot.kind == InventoryItemKind.Weapon && database != null && slot.weapon.IsValid)
            return WeaponElementColors.GetSlotBorderColor(database.GetWeaponElementProfile(slot.weapon));

        return KindAccentColor(slot.kind);
    }

    private static Color ResolveSlotIconTint(InventorySlotData slot, WeaponDatabase database, bool hasIcon)
    {
        if (!hasIcon)
            return Color.white;

        if (slot.kind == InventoryItemKind.Weapon && database != null && slot.weapon.IsValid)
        {
            Color elementColor = WeaponElementColors.GetSlotBorderColor(database.GetWeaponElementProfile(slot.weapon));
            if (elementColor != WeaponElementColors.DefaultBorder)
                return Color.Lerp(Color.white, elementColor, 0.22f);
        }

        return Color.white;
    }

    private void ApplyHoverDetailsStyle(InventorySlotData slot, WeaponDatabase database)
    {
        Color defaultAccent = new Color(0.35f, 0.62f, 0.9f, 0.35f);
        Color defaultTitle = new Color(0.95f, 0.82f, 0.45f);

        if (slot.kind == InventoryItemKind.Weapon && database != null && slot.weapon.IsValid)
        {
            Color elementColor = WeaponElementColors.GetSlotBorderColor(database.GetWeaponElementProfile(slot.weapon));
            bool hasElement = elementColor != WeaponElementColors.DefaultBorder;

            if (hoverDetailsAccent != null)
            {
                hoverDetailsAccent.color = hasElement
                    ? Color.Lerp(elementColor, Color.white, 0.15f)
                    : defaultAccent;
            }

            if (hoverDetailsTitle != null)
                hoverDetailsTitle.color = new Color(0.96f, 0.97f, 1f);

            if (hoverDetailsSubtitle != null)
            {
                hoverDetailsSubtitle.color = hasElement
                    ? Color.Lerp(elementColor, Color.white, 0.1f)
                    : new Color(0.65f, 0.7f, 0.8f);
            }

            return;
        }

        if (hoverDetailsAccent != null)
            hoverDetailsAccent.color = defaultAccent;

        if (hoverDetailsTitle != null)
            hoverDetailsTitle.color = new Color(0.96f, 0.97f, 1f);

        if (hoverDetailsSubtitle != null)
            hoverDetailsSubtitle.color = KindAccentColor(slot.kind);
    }

    private float GetMaxShield()
    {
        PlayerCoreStats core = player?.ActiveCore;
        return core != null ? core.maxShield : 0f;
    }

    private static string FormatStat(float value) => value.ToString("0.##");

    private static string FormatPercent(float value) => $"{(value * 100f):0.#}%";

    private static Sprite ResolveSlotSprite(InventorySlotData slot, WeaponDatabase database)
    {
        if (slot.IsEmpty)
            return null;

        if (slot.kind == InventoryItemKind.Weapon && database != null && slot.weapon.IsValid)
            return database.GetWeaponSprite(slot.weapon);

        return null;
    }

    private static string GetAbbreviation(InventorySlotData slot)
    {
        string source = !string.IsNullOrWhiteSpace(slot.displayName)
            ? slot.displayName
            : slot.itemId;

        if (string.IsNullOrWhiteSpace(source))
            return slot.kind.ToString()[..1];

        return source.Length <= 2
            ? source.ToUpperInvariant()
            : source[..2].ToUpperInvariant();
    }

    private static Color KindAccentColor(InventoryItemKind kind) =>
        kind switch
        {
            InventoryItemKind.Weapon => new Color(0.95f, 0.75f, 0.35f),
            InventoryItemKind.Artifact => new Color(0.65f, 0.45f, 0.95f),
            InventoryItemKind.Currency => new Color(0.45f, 0.9f, 0.55f),
            _ => new Color(0.55f, 0.75f, 0.95f)
        };

    private void BuildUI()
    {
        var canvasGo = new GameObject("Inventory Canvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        rootPanel = CreatePanel(canvasGo.transform, "Inventory Panel", Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);

        var backdrop = CreateImage(rootPanel.transform, "Backdrop", new Color(0.02f, 0.03f, 0.06f, 0.88f));
        Stretch(backdrop.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var mainContent = CreatePanel(rootPanel.transform, "Main Content", Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);
        Stretch(mainContent.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
            new Vector2(32f, 32f), new Vector2(-32f, -32f));

        var contentBg = CreateImage(mainContent.transform, "Content Background", new Color(0.05f, 0.06f, 0.09f, 0.97f));
        Stretch(contentBg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var leftColumn = CreatePanel(mainContent.transform, "Left Column", Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);
        ConfigureColumn(leftColumn.GetComponent<RectTransform>(), ColumnAnchor.Left);

        var centerColumn = CreatePanel(mainContent.transform, "Center Column", Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);
        ConfigureColumn(centerColumn.GetComponent<RectTransform>(), ColumnAnchor.Center);

        var rightColumn = CreatePanel(mainContent.transform, "Right Column", Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);
        ConfigureColumn(rightColumn.GetComponent<RectTransform>(), ColumnAnchor.Right);

        BuildLeftColumn(leftColumn.transform);
        BuildHoverDetailsSection(centerColumn.transform);
        ClearHoverDetails();
        BuildStatsPanel(rightColumn.transform);
        BuildCloseButton(mainContent.transform);
        BuildDeleteDialog(rootPanel.transform);

        rootPanel.SetActive(false);
    }

    private void BuildDeleteDialog(Transform parent)
    {
        deleteDialogRoot = CreatePanel(parent, "Delete Dialog", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Stretch(deleteDialogRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var overlay = CreateImage(deleteDialogRoot.transform, "Delete Dialog Overlay", new Color(0.02f, 0.03f, 0.06f, 0.72f));
        Stretch(overlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        overlay.raycastTarget = true;

        var panel = CreatePanel(deleteDialogRoot.transform, "Delete Dialog Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(500f, 220f));
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.pivot = new Vector2(0.5f, 0.5f);

        var panelBg = CreateImage(panel.transform, "Delete Dialog Background", new Color(0.08f, 0.09f, 0.13f, 0.98f));
        Stretch(panelBg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var panelAccent = CreateImage(panel.transform, "Delete Dialog Accent", new Color(0.82f, 0.35f, 0.35f, 0.45f));
        Stretch(panelAccent.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, -3f));
        panelAccent.rectTransform.sizeDelta = new Vector2(0f, 3f);

        deleteDialogMessage = CreateText(panel.transform, "Delete Dialog Message", 22, TextAlignmentOptions.Center);
        Stretch(deleteDialogMessage.rectTransform, Vector2.zero, Vector2.one, new Vector2(24f, 72f), new Vector2(-24f, -24f));
        deleteDialogMessage.color = new Color(0.9f, 0.91f, 0.95f);
        deleteDialogMessage.textWrappingMode = TextWrappingModes.Normal;

        deleteDialogYesButton = CreateDialogButton(panel.transform, "Delete Yes Button", "Yes",
            new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(24f, 20f), new Vector2(-8f, 64f),
            new Color(0.55f, 0.18f, 0.2f, 0.96f), ConfirmDeleteHoveredItem);

        deleteDialogNoButton = CreateDialogButton(panel.transform, "Delete No Button", "No",
            new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(8f, 20f), new Vector2(-24f, 64f),
            new Color(0.18f, 0.2f, 0.28f, 0.96f), HideDeleteDialog);

        deleteDialogRoot.SetActive(false);
    }

    private GameObject CreateDialogButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color,
        UnityEngine.Events.UnityAction onClick)
    {
        var buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);

        var rect = buttonGo.GetComponent<RectTransform>();
        Stretch(rect, anchorMin, anchorMax, offsetMin, offsetMax);

        var image = buttonGo.GetComponent<Image>();
        image.sprite = whiteSprite;
        image.color = color;

        var button = buttonGo.GetComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.12f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.12f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        var text = CreateText(buttonGo.transform, $"{name} Label", 20, TextAlignmentOptions.Center);
        Stretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(0.95f, 0.96f, 1f);
        text.text = label;
        text.raycastTarget = false;

        return buttonGo;
    }

    private void BuildCloseButton(Transform parent)
    {
        var buttonGo = new GameObject("Close Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);

        var rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(2f, -2f);
        rect.sizeDelta = new Vector2(30f, 30f);

        var image = buttonGo.GetComponent<Image>();
        image.sprite = whiteSprite;
        image.color = new Color(0.14f, 0.16f, 0.22f, 0.96f);
        image.raycastTarget = true;

        var button = buttonGo.GetComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.22f, 0.24f, 0.32f, 0.98f);
        colors.pressedColor = new Color(0.1f, 0.11f, 0.16f, 0.98f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        button.onClick.AddListener(() => SetOpen(false));

        var label = CreateText(buttonGo.transform, "Close Label", 20, TextAlignmentOptions.Center);
        Stretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(0.88f, 0.9f, 0.95f);
        label.text = "X";
        label.raycastTarget = false;
    }

    private void BuildCurrencyBar(Transform parent)
    {
        var bar = CreatePanel(parent, "Currency Bar", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch(bar.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1f, 0f),
            new Vector2(12f, 12f), new Vector2(-12f, 12f + CurrencyBarHeight));

        var barBg = CreateImage(bar.transform, "Currency Bar Background", new Color(0.08f, 0.1f, 0.13f, 0.96f));
        Stretch(barBg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var barAccent = CreateImage(bar.transform, "Currency Bar Accent", new Color(0.35f, 0.82f, 0.48f, 0.4f));
        Stretch(barAccent.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, -3f));
        barAccent.rectTransform.sizeDelta = new Vector2(0f, 3f);

        var iconBg = CreateImage(bar.transform, "Currency Icon", new Color(0.12f, 0.28f, 0.16f, 0.95f));
        Stretch(iconBg.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, -14f), new Vector2(42f, 14f));

        var iconText = CreateText(iconBg.transform, "Currency Icon Text", 18, TextAlignmentOptions.Center);
        Stretch(iconText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        iconText.fontStyle = FontStyles.Bold;
        iconText.color = new Color(0.55f, 0.95f, 0.62f);
        iconText.text = "$";

        currencyLabel = CreateText(bar.transform, "Currency Label", 18, TextAlignmentOptions.MidlineLeft);
        Stretch(currencyLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(50f, 0f), new Vector2(-120f, 0f));
        currencyLabel.fontStyle = FontStyles.Bold;
        currencyLabel.color = new Color(0.7f, 0.74f, 0.8f);
        currencyLabel.text = "CREDITS";

        currencyAmountLabel = CreateText(bar.transform, "Currency Amount", 24, TextAlignmentOptions.MidlineRight);
        Stretch(currencyAmountLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(50f, 0f), new Vector2(-12f, 0f));
        currencyAmountLabel.fontStyle = FontStyles.Bold;
        currencyAmountLabel.color = new Color(0.5f, 0.95f, 0.58f);
        currencyAmountLabel.text = "0";
    }

    private void BuildLeftColumn(Transform parent)
    {
        float topOffset = 0f;

        topOffset = BuildSlotSection(parent, "Weapons", PlayerInventory.WeaponSlotCount, 1,
            weaponSlotViews, InventorySlotCategory.Weapon, topOffset);

        topOffset += SectionGap;
        topOffset = BuildSlotSection(parent, "Inventory", PlayerInventory.InventoryColumns,
            PlayerInventory.InventoryRows, inventorySlotViews, InventorySlotCategory.Inventory, topOffset);

        topOffset += SectionGap;
        BuildSlotSection(parent, "Artifacts", PlayerInventory.ArtifactSlotCount, 1,
            artifactSlotViews, InventorySlotCategory.Artifact, topOffset);
    }

    private enum ColumnAnchor
    {
        Left,
        Center,
        Right
    }

    private void ConfigureColumn(RectTransform rect, ColumnAnchor anchor)
    {
        const float verticalPadding = 24f;
        float leftEdge = ContentPadding + UnifiedGridWidth;
        float rightEdge = ContentPadding + StatsColumnWidth;

        switch (anchor)
        {
            case ColumnAnchor.Left:
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.offsetMin = new Vector2(ContentPadding, verticalPadding);
                rect.offsetMax = new Vector2(leftEdge, -verticalPadding);
                break;

            case ColumnAnchor.Center:
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = new Vector2(leftEdge + ColumnGap, verticalPadding);
                rect.offsetMax = new Vector2(-(rightEdge + ColumnGap), -verticalPadding);
                break;

            case ColumnAnchor.Right:
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 0.5f);
                rect.offsetMin = new Vector2(-rightEdge, verticalPadding);
                rect.offsetMax = new Vector2(-ContentPadding, -verticalPadding);
                break;
        }
    }

    private float BuildSlotSection(Transform parent, string title, int columns, int rows,
        List<SlotView> slotViews, InventorySlotCategory category, float topOffset)
    {
        float gridWidth = columns * SlotSize + (columns - 1) * SlotGap;
        float gridHeight = rows * SlotSize + (rows - 1) * SlotGap;
        float panelHeight = SectionHeaderHeight + SectionHeaderGap + gridHeight + SectionPanelPadding * 2f;
        float panelTop = topOffset;

        var sectionPanel = CreatePanel(parent, $"{title} Section", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, -panelTop), new Vector2(UnifiedGridWidth, panelHeight));
        var sectionRect = sectionPanel.GetComponent<RectTransform>();
        sectionRect.pivot = new Vector2(0f, 1f);

        var sectionBg = CreateImage(sectionPanel.transform, "Section Background", SectionPanelColor);
        Stretch(sectionBg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        sectionBg.raycastTarget = false;

        var sectionAccent = CreateImage(sectionPanel.transform, "Section Accent", SectionAccentColor);
        Stretch(sectionAccent.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, -3f));
        sectionAccent.rectTransform.sizeDelta = new Vector2(0f, 3f);
        sectionAccent.raycastTarget = false;

        var header = CreateText(sectionPanel.transform, $"{title} Header", 26, TextAlignmentOptions.TopLeft);
        var headerRect = header.rectTransform;
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0f, 1f);
        headerRect.anchoredPosition = new Vector2(SectionPanelPadding, -SectionPanelPadding);
        headerRect.sizeDelta = new Vector2(-SectionPanelPadding * 2f, SectionHeaderHeight);
        header.fontStyle = FontStyles.Bold;
        header.color = new Color(0.92f, 0.93f, 0.98f);
        header.text = title.ToUpperInvariant();

        float gridTop = SectionPanelPadding + SectionHeaderHeight + SectionHeaderGap;
        float gridOffsetX = SectionPanelPadding + (UnifiedGridWidth - SectionPanelPadding * 2f - gridWidth) * 0.5f;

        var grid = CreatePanel(sectionPanel.transform, $"{title} Grid", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(gridOffsetX, -gridTop), new Vector2(gridWidth, gridHeight));
        var gridRect = grid.GetComponent<RectTransform>();
        gridRect.pivot = new Vector2(0f, 1f);

        slotViews.Clear();
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                int index = row * columns + col;
                slotViews.Add(CreateSlot(grid.transform, $"{title} Slot {index}", col, row, category, index));
            }
        }

        return panelTop + panelHeight;
    }

    private SlotView CreateSlot(Transform parent, string name, int column, int row,
        InventorySlotCategory category, int slotIndex)
    {
        float x = column * (SlotSize + SlotGap);
        float y = -row * (SlotSize + SlotGap);
        var inset = new Vector2(SlotBorderInset, SlotBorderInset);

        var slotGo = CreatePanel(parent, name, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(x, y), new Vector2(SlotSize, SlotSize));
        var slotRect = slotGo.GetComponent<RectTransform>();
        slotRect.pivot = new Vector2(0f, 1f);

        var border = CreateImage(slotGo.transform, "Border", EmptySlotFrame);
        Stretch(border.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        border.raycastTarget = true;

        var background = CreateImage(slotGo.transform, "Background", EmptySlotBackground);
        Stretch(background.rectTransform, Vector2.zero, Vector2.one, inset, -inset);
        background.raycastTarget = false;

        var icon = CreateImage(slotGo.transform, "Icon", Color.white);
        Stretch(icon.rectTransform, Vector2.zero, Vector2.one, inset + new Vector2(6f, 6f), -(inset + new Vector2(6f, 6f)));
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;

        var abbreviation = CreateText(slotGo.transform, "Abbreviation", 20, TextAlignmentOptions.Center);
        Stretch(abbreviation.rectTransform, Vector2.zero, Vector2.one, inset + new Vector2(4f, 4f), -(inset + new Vector2(4f, 4f)));
        abbreviation.fontStyle = FontStyles.Bold;
        abbreviation.enabled = false;

        var quantity = CreateText(slotGo.transform, "Quantity", 17, TextAlignmentOptions.BottomRight);
        Stretch(quantity.rectTransform, Vector2.zero, Vector2.one, inset + new Vector2(4f, 2f), -(inset + new Vector2(4f, 2f)));
        quantity.fontStyle = FontStyles.Bold;
        quantity.color = new Color(0.95f, 0.96f, 1f, 0.95f);
        quantity.enabled = false;

        var hover = slotGo.AddComponent<InventorySlotHover>();
        hover.Initialize(category, slotIndex);

        return new SlotView
        {
            background = background,
            border = border,
            icon = icon,
            abbreviation = abbreviation,
            quantity = quantity
        };
    }

    private void BuildStatsPanel(Transform parent)
    {
        var panelBg = CreateImage(parent, "Stats Background", new Color(0.06f, 0.07f, 0.1f, 0.96f));
        Stretch(panelBg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var accent = CreateImage(parent, "Stats Accent", SectionAccentColor);
        Stretch(accent.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, -3f));
        accent.rectTransform.sizeDelta = new Vector2(0f, 3f);

        var playerCard = CreatePanel(parent, "Player Card", new Vector2(0f, 1f), new Vector2(1f, 1f),
            Vector2.zero, Vector2.zero);
        var playerCardRect = playerCard.GetComponent<RectTransform>();
        playerCardRect.pivot = new Vector2(0.5f, 1f);
        playerCardRect.offsetMin = new Vector2(0f, -PlayerCardHeight);
        playerCardRect.offsetMax = Vector2.zero;

        var playerCardBg = CreateImage(playerCard.transform, "Player Card Background", new Color(0.08f, 0.09f, 0.13f, 0.98f));
        Stretch(playerCardBg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var playerCardAccent = CreateImage(playerCard.transform, "Player Card Accent", SectionAccentColor);
        Stretch(playerCardAccent.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, -3f));
        playerCardAccent.rectTransform.sizeDelta = new Vector2(0f, 3f);

        const float nameBandHeight = 34f;
        float portraitBottom = 12f + CurrencyBarHeight + 8f + nameBandHeight;

        characterPortraitFrame = CreateImage(playerCard.transform, "Portrait Frame", new Color(0.14f, 0.16f, 0.22f, 1f));
        Stretch(characterPortraitFrame.rectTransform, Vector2.zero, Vector2.one,
            new Vector2(16f, portraitBottom), new Vector2(-16f, -16f));

        characterPortrait = CreateImage(playerCard.transform, "Portrait", Color.white);
        Stretch(characterPortrait.rectTransform, Vector2.zero, Vector2.one,
            new Vector2(24f, portraitBottom + 8f), new Vector2(-24f, -24f));
        characterPortrait.preserveAspect = true;
        characterPortrait.enabled = false;

        characterNameLabel = CreateText(playerCard.transform, "Character Name", 26, TextAlignmentOptions.Center);
        Stretch(characterNameLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(16f, 12f + CurrencyBarHeight + 4f), new Vector2(-16f, 12f + CurrencyBarHeight + 4f + nameBandHeight));
        characterNameLabel.fontStyle = FontStyles.Bold;
        characterNameLabel.color = new Color(0.95f, 0.96f, 1f);

        BuildCurrencyBar(playerCard.transform);

        var statsHeader = CreateText(parent, "Stats Header", 24, TextAlignmentOptions.TopLeft);
        var statsHeaderRect = statsHeader.rectTransform;
        Stretch(statsHeaderRect, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(24f, -(PlayerCardHeight + 40f)), new Vector2(-24f, -(PlayerCardHeight + 12f)));
        statsHeader.fontStyle = FontStyles.Bold;
        statsHeader.color = new Color(0.55f, 0.82f, 1f);
        statsHeader.text = "STATS";

        BuildStatsScrollView(parent);
    }

    private void BuildStatsScrollView(Transform parent)
    {
        var scrollRoot = CreatePanel(parent, "Stats Scroll Root", new Vector2(0f, 0f), new Vector2(1f, 1f),
            Vector2.zero, Vector2.zero);
        Stretch(scrollRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
            new Vector2(24f, 24f), new Vector2(-24f, -(PlayerCardHeight + 48f)));

        var viewport = CreatePanel(scrollRoot.transform, "Stats Viewport", Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);
        var viewportRect = viewport.GetComponent<RectTransform>();
        Stretch(viewportRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0.08f, 0.09f, 0.12f, 0.35f);
        viewportImage.raycastTarget = true;
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        statsContent = CreatePanel(viewport.transform, "Stats Content", new Vector2(0f, 1f), new Vector2(1f, 1f),
            Vector2.zero, Vector2.zero).GetComponent<RectTransform>();
        statsContent.pivot = new Vector2(0.5f, 1f);
        statsContent.anchorMin = new Vector2(0f, 1f);
        statsContent.anchorMax = new Vector2(1f, 1f);
        statsContent.anchoredPosition = Vector2.zero;
        statsContent.sizeDelta = new Vector2(0f, 400f);

        statsScrollText = CreateText(statsContent, "Stats Text", 22, TextAlignmentOptions.TopLeft);
        var textRect = statsScrollText.rectTransform;
        Stretch(textRect, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f));
        statsScrollText.color = new Color(0.86f, 0.88f, 0.93f);
        statsScrollText.lineSpacing = 2f;
        statsScrollText.richText = true;
        statsScrollText.textWrappingMode = TextWrappingModes.Normal;

        statsScrollRect = scrollRoot.AddComponent<ScrollRect>();
        statsScrollRect.viewport = viewportRect;
        statsScrollRect.content = statsContent;
        statsScrollRect.horizontal = false;
        statsScrollRect.vertical = true;
        statsScrollRect.movementType = ScrollRect.MovementType.Clamped;
        statsScrollRect.scrollSensitivity = 24f;
    }

    private void BuildHoverDetailsSection(Transform parent)
    {
        var sectionPanel = CreatePanel(parent, "Details Section", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Stretch(sectionPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var sectionBg = CreateImage(sectionPanel.transform, "Details Section Background", SectionPanelColor);
        Stretch(sectionBg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        sectionBg.raycastTarget = false;

        var sectionAccent = CreateImage(sectionPanel.transform, "Details Section Accent", SectionAccentColor);
        Stretch(sectionAccent.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, -3f));
        sectionAccent.rectTransform.sizeDelta = new Vector2(0f, 3f);
        sectionAccent.raycastTarget = false;

        var hoverHeader = CreateText(sectionPanel.transform, "Hover Details Header", 26, TextAlignmentOptions.TopLeft);
        var hoverHeaderRect = hoverHeader.rectTransform;
        hoverHeaderRect.anchorMin = new Vector2(0f, 1f);
        hoverHeaderRect.anchorMax = new Vector2(1f, 1f);
        hoverHeaderRect.pivot = new Vector2(0f, 1f);
        hoverHeaderRect.anchoredPosition = new Vector2(SectionPanelPadding, -SectionPanelPadding);
        hoverHeaderRect.sizeDelta = new Vector2(-SectionPanelPadding * 2f, SectionHeaderHeight);
        hoverHeader.fontStyle = FontStyles.Bold;
        hoverHeader.color = new Color(0.92f, 0.93f, 0.98f);
        hoverHeader.text = "DETAILS";

        float headerArea = SectionPanelPadding + SectionHeaderHeight + SectionHeaderGap;

        var scrollRoot = CreatePanel(sectionPanel.transform, "Hover Details Scroll Root", Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);
        Stretch(scrollRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
            new Vector2(SectionPanelPadding, SectionPanelPadding),
            new Vector2(-SectionPanelPadding, -headerArea));
        var scrollRootRect = scrollRoot.GetComponent<RectTransform>();

        hoverDetailsAccent = CreateImage(scrollRoot.transform, "Hover Details Accent", new Color(0.35f, 0.62f, 0.9f, 0.35f));
        Stretch(hoverDetailsAccent.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, -3f));
        hoverDetailsAccent.rectTransform.sizeDelta = new Vector2(0f, 3f);

        var panelBg = CreateImage(scrollRoot.transform, "Hover Details Background", new Color(0.07f, 0.08f, 0.11f, 0.95f));
        Stretch(panelBg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var viewport = CreatePanel(scrollRoot.transform, "Hover Details Viewport", Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);
        var viewportRect = viewport.GetComponent<RectTransform>();
        Stretch(viewportRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0.08f, 0.09f, 0.12f, 0.2f);
        viewportImage.raycastTarget = true;
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        hoverDetailsContent = CreatePanel(viewport.transform, "Hover Details Content", new Vector2(0f, 1f), new Vector2(1f, 1f),
            Vector2.zero, Vector2.zero).GetComponent<RectTransform>();
        hoverDetailsContent.pivot = new Vector2(0.5f, 1f);
        hoverDetailsContent.anchorMin = new Vector2(0f, 1f);
        hoverDetailsContent.anchorMax = new Vector2(1f, 1f);
        hoverDetailsContent.anchoredPosition = Vector2.zero;
        hoverDetailsContent.sizeDelta = new Vector2(0f, 400f);

        hoverDetailsTitle = CreateText(hoverDetailsContent, "Hover Details Title", 26, TextAlignmentOptions.TopLeft);
        var titleRect = hoverDetailsTitle.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = new Vector2(-16f, 36f);
        hoverDetailsTitle.margin = new Vector4(8f, 8f, 8f, 0f);
        hoverDetailsTitle.fontStyle = FontStyles.Bold;
        hoverDetailsTitle.color = new Color(0.96f, 0.97f, 1f);

        hoverDetailsSubtitle = CreateText(hoverDetailsContent, "Hover Details Subtitle", 18, TextAlignmentOptions.TopLeft);
        var subtitleRect = hoverDetailsSubtitle.rectTransform;
        subtitleRect.anchorMin = new Vector2(0f, 1f);
        subtitleRect.anchorMax = new Vector2(1f, 1f);
        subtitleRect.pivot = new Vector2(0.5f, 1f);
        subtitleRect.anchoredPosition = new Vector2(0f, -36f);
        subtitleRect.sizeDelta = new Vector2(-16f, 24f);
        hoverDetailsSubtitle.margin = new Vector4(8f, 0f, 8f, 0f);
        hoverDetailsSubtitle.fontStyle = FontStyles.Bold;
        hoverDetailsSubtitle.richText = true;
        hoverDetailsSubtitle.enabled = false;

        hoverDetailsBody = CreateText(hoverDetailsContent, "Hover Details Body", 19, TextAlignmentOptions.TopLeft);
        var bodyRect = hoverDetailsBody.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.anchoredPosition = new Vector2(0f, -64f);
        bodyRect.sizeDelta = new Vector2(-16f, 120f);
        hoverDetailsBody.margin = new Vector4(8f, 4f, 8f, 8f);
        hoverDetailsBody.color = new Color(0.86f, 0.88f, 0.93f);
        hoverDetailsBody.lineSpacing = 0f;
        hoverDetailsBody.richText = true;
        hoverDetailsBody.textWrappingMode = TextWrappingModes.Normal;

        hoverDetailsScrollRect = scrollRoot.AddComponent<ScrollRect>();
        hoverDetailsScrollRect.viewport = viewportRect;
        hoverDetailsScrollRect.content = hoverDetailsContent;
        hoverDetailsScrollRect.horizontal = false;
        hoverDetailsScrollRect.vertical = true;
        hoverDetailsScrollRect.movementType = ScrollRect.MovementType.Clamped;
        hoverDetailsScrollRect.scrollSensitivity = 20f;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        var eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<EventSystem>();
        eventSystemGo.AddComponent<StandaloneInputModule>();
    }

    private GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin == anchorMax ? anchorMin : new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return go;
    }

    private Image CreateImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = whiteSprite;
        image.color = color;
        return image;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, float fontSize, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        if (font != null)
            text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private Sprite CreateWhiteSprite()
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }
}