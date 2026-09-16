using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Barkeep character shop — Lego Prequel-style character pedestals with buy/select flow.
/// </summary>
public class CharacterShopUI : MonoBehaviour
{
    private const KeyCode CloseKey = KeyCode.Escape;
    private const float CardWidth = 360f;
    private const float CardHeight = 520f;
    private const float CardGap = 48f;
    private const float PortraitFrameSize = 220f;
    private const float ShopPortraitPixelsPerUnit = 16f;

    private static CharacterShopUI instance;

    private static readonly Color GoldAccent = new(0.95f, 0.82f, 0.35f, 1f);
    private static readonly Color CardBackground = new(0.08f, 0.09f, 0.13f, 0.98f);
    private static readonly Color PedestalColor = new(0.12f, 0.14f, 0.2f, 1f);
    private static readonly Color PortraitBackground = new(0.2f, 0.2f, 0.22f, 1f);

    private GameObject rootPanel;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI subtitleLabel;
    private TextMeshProUGUI creditsAmountLabel;
    private bool isOpen;
    private float savedTimeScale = 1f;

    private Player player;
    private InteractableCharacter vendor;
    private ShopOffer[] activeOffers = System.Array.Empty<ShopOffer>();
    private readonly List<CharacterCardView> cardViews = new();

    private TMP_FontAsset font;
    private Sprite whiteSprite;

    public static bool IsOpen => instance != null && instance.isOpen;

    private struct CharacterCardView
    {
        public ShopOffer offer;
        public GameObject root;
        public Image frame;
        public Image portraitFrame;
        public Image portrait;
        public Image lockOverlay;
        public TextMeshProUGUI nameLabel;
        public TextMeshProUGUI statusLabel;
        public TextMeshProUGUI priceLabel;
        public Button actionButton;
        public TextMeshProUGUI actionLabel;
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

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;

        RestoreGameplayTime();
    }

    private void Update()
    {
        if (!isOpen)
            return;

        if (Input.GetKeyDown(CloseKey))
            Close();

        RefreshCredits();
    }

    public static void Open(InteractableCharacter shopkeeper, Player activePlayer)
    {
        if (shopkeeper == null || activePlayer == null)
            return;

        EnsureInstance();
        instance.OpenShop(shopkeeper, activePlayer);
    }

    public static void Close()
    {
        if (instance == null || !instance.isOpen)
            return;

        instance.SetOpen(false);
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        var go = new GameObject("Character Shop UI");
        go.AddComponent<CharacterShopUI>();
    }

    private void OpenShop(InteractableCharacter shopkeeper, Player activePlayer)
    {
        player = activePlayer;
        vendor = shopkeeper;
        activeOffers = shopkeeper.ShopOffers ?? System.Array.Empty<ShopOffer>();

        ShopUnlockTracker.ApplyDefaultUnlocks(activeOffers);
        titleLabel.text = string.IsNullOrWhiteSpace(shopkeeper.DisplayName)
            ? "SHOP"
            : shopkeeper.DisplayName.ToUpperInvariant();
        subtitleLabel.text = "Browse the catalog. Names, portraits, and prices are pulled from your game databases.";

        RebuildCards();
        RefreshAllCards();
        RefreshCredits();
        SetOpen(true);
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
        }
        else
        {
            Time.timeScale = savedTimeScale;
            isOpen = false;
            vendor = null;
            activeOffers = System.Array.Empty<ShopOffer>();
        }

        if (rootPanel != null)
            rootPanel.SetActive(isOpen);
    }

    private void ForceClosed()
    {
        isOpen = false;
        if (rootPanel != null)
            rootPanel.SetActive(false);
    }

    private void RestoreGameplayTime()
    {
        if (!isOpen)
            return;

        Time.timeScale = savedTimeScale;
        isOpen = false;
    }

    private void RebuildCards()
    {
        foreach (CharacterCardView view in cardViews)
        {
            if (view.root != null)
                Destroy(view.root);
        }

        cardViews.Clear();

        if (activeOffers == null || activeOffers.Length == 0)
            return;

        Transform parent = rootPanel.transform.Find("Main Content/Card Row");
        if (parent == null)
            return;

        float totalWidth = activeOffers.Length * CardWidth + (activeOffers.Length - 1) * CardGap;
        float startX = -totalWidth * 0.5f + CardWidth * 0.5f;

        for (int i = 0; i < activeOffers.Length; i++)
        {
            ShopOffer offer = activeOffers[i];
            if (offer == null || ShopCatalogResolver.Resolve(offer).IsValid == false)
                continue;

            float x = startX + i * (CardWidth + CardGap);
            cardViews.Add(CreateCharacterCard(parent, offer, x));
        }
    }

    private void RefreshAllCards()
    {
        for (int i = 0; i < cardViews.Count; i++)
            RefreshCard(cardViews[i]);
    }

    private void RefreshCard(CharacterCardView view)
    {
        ShopOfferDisplay display = ShopCatalogResolver.Resolve(view.offer);
        if (!display.IsValid)
            return;

        view.nameLabel.text = display.DisplayName.ToUpperInvariant();

        Sprite portraitSprite = display.Sprite;
        bool hasPortrait = portraitSprite != null;
        view.portrait.enabled = true;
        view.portrait.sprite = portraitSprite;
        view.portrait.color = hasPortrait ? Color.white : new Color(1f, 1f, 1f, 0.12f);

        FitShopPortrait(view.portrait, portraitSprite, PortraitFrameSize);

        bool unlocked = ShopUnlockTracker.IsUnlocked(display.OfferKey);
        bool isActive = ShopPurchaseHandler.IsActiveSelection(player, display);
        int credits = player?.inventory != null ? player.inventory.GetCurrencyTotal() : 0;
        bool canAfford = ShopPurchaseHandler.IgnoreCurrency || credits >= display.Price;

        view.lockOverlay.enabled = !unlocked;
        view.statusLabel.text = isActive ? "IN USE" : unlocked ? "UNLOCKED" : "LOCKED";
        view.statusLabel.color = isActive ? GoldAccent : unlocked
            ? new Color(0.55f, 0.82f, 1f)
            : new Color(0.72f, 0.45f, 0.45f);

        view.frame.color = isActive
            ? Color.Lerp(GoldAccent, Color.white, 0.12f)
            : unlocked
                ? new Color(0.22f, 0.38f, 0.58f, 1f)
                : new Color(0.16f, 0.17f, 0.24f, 1f);

        if (view.portraitFrame != null)
        {
            view.portraitFrame.color = isActive
                ? Color.Lerp(GoldAccent, Color.white, 0.18f)
                : unlocked
                    ? GoldAccent
                    : new Color(0.35f, 0.35f, 0.38f, 1f);
        }

        if (isActive)
        {
            view.priceLabel.text = "ACTIVE";
            view.actionLabel.text = "IN USE";
            view.actionButton.interactable = false;
            SetButtonColor(view.actionButton, new Color(0.28f, 0.24f, 0.12f, 0.9f));
            return;
        }

        if (!unlocked)
        {
            view.priceLabel.text = display.Price <= 0 ? "FREE" : $"{display.Price:N0} CREDITS";
            view.priceLabel.color = canAfford || display.Price <= 0
                ? new Color(0.5f, 0.95f, 0.58f)
                : new Color(0.95f, 0.55f, 0.45f);
            view.actionLabel.text = display.Price <= 0 ? "UNLOCK" : "BUY";
            view.actionButton.interactable = canAfford || display.Price <= 0;
            SetButtonColor(view.actionButton, view.actionButton.interactable
                ? new Color(0.18f, 0.42f, 0.24f, 0.96f)
                : new Color(0.22f, 0.14f, 0.14f, 0.9f));
            return;
        }

        view.priceLabel.text = "OWNED";
        view.priceLabel.color = GoldAccent;
        view.actionLabel.text = display.IsPlayer ? "SELECT" : "GET";
        view.actionButton.interactable = true;
        SetButtonColor(view.actionButton, new Color(0.16f, 0.28f, 0.48f, 0.96f));
    }

    private void RefreshCredits()
    {
        if (creditsAmountLabel == null)
            return;

        int total = player?.inventory != null ? player.inventory.GetCurrencyTotal() : 0;
        creditsAmountLabel.text = total.ToString("N0");
    }

    private void HandleCardAction(ShopOffer offer)
    {
        if (player == null || offer == null)
            return;

        ShopOfferDisplay display = ShopCatalogResolver.Resolve(offer);
        if (!display.IsValid)
            return;

        bool unlocked = ShopUnlockTracker.IsUnlocked(display.OfferKey);
        if (!unlocked)
        {
            if (!ShopPurchaseHandler.TryPurchase(player, display, selectingOwned: false))
                return;

            vendor?.RecordShopPurchase();
            RefreshAllCards();
            RefreshCredits();
            return;
        }

        if (ShopPurchaseHandler.IsActiveSelection(player, display))
            return;

        if (!ShopPurchaseHandler.TryPurchase(player, display, selectingOwned: true))
            return;

        if (display.IsPlayer)
            Close();
        else
            RefreshAllCards();
    }

    private CharacterCardView CreateCharacterCard(Transform parent, ShopOffer offer, float anchoredX)
    {
        ShopOfferDisplay display = ShopCatalogResolver.Resolve(offer);
        var cardGo = CreatePanel(parent, $"Shop Card {display.DisplayName}",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(anchoredX, -20f), new Vector2(CardWidth, CardHeight));
        var cardRect = cardGo.GetComponent<RectTransform>();
        cardRect.pivot = new Vector2(0.5f, 0.5f);

        var frame = CreateImage(cardGo.transform, "Card Frame", new Color(0.16f, 0.17f, 0.24f, 1f));
        Stretch(frame.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var inner = CreateImage(cardGo.transform, "Card Background", CardBackground);
        Stretch(inner.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));

        var pedestal = CreateImage(cardGo.transform, "Pedestal", PedestalColor);
        Stretch(pedestal.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(24f, 18f), new Vector2(-24f, 108f));

        Image portraitFrame;
        Image portrait;
        Image lockOverlay;
        BuildShopPortrait(cardGo.transform, out portraitFrame, out portrait, out lockOverlay);

        var nameLabel = CreateText(cardGo.transform, "Name", 24, TextAlignmentOptions.Center);
        Stretch(nameLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(20f, 108f), new Vector2(-20f, 148f));
        nameLabel.fontStyle = FontStyles.Bold;
        nameLabel.color = new Color(0.96f, 0.97f, 1f);

        var statusLabel = CreateText(cardGo.transform, "Status", 16, TextAlignmentOptions.Center);
        Stretch(statusLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(20f, -24f), new Vector2(-20f, -52f));
        statusLabel.fontStyle = FontStyles.Bold;

        var priceLabel = CreateText(cardGo.transform, "Price", 20, TextAlignmentOptions.Center);
        Stretch(priceLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(20f, 156f), new Vector2(-20f, 196f));
        priceLabel.fontStyle = FontStyles.Bold;

        Button actionButton = CreateActionButton(cardGo.transform, "Action Button", out TextMeshProUGUI actionLabel);
        ShopOffer capturedOffer = offer;
        actionButton.onClick.AddListener(() => HandleCardAction(capturedOffer));

        return new CharacterCardView
        {
            offer = offer,
            root = cardGo,
            frame = frame,
            portraitFrame = portraitFrame,
            portrait = portrait,
            lockOverlay = lockOverlay,
            nameLabel = nameLabel,
            statusLabel = statusLabel,
            priceLabel = priceLabel,
            actionButton = actionButton,
            actionLabel = actionLabel
        };
    }

    private void BuildShopPortrait(Transform card, out Image portraitFrame, out Image portrait, out Image lockOverlay)
    {
        var mount = CreatePanel(card, "Portrait Mount", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 36f), new Vector2(PortraitFrameSize + 24f, PortraitFrameSize + 24f));
        var mountRect = mount.GetComponent<RectTransform>();
        mountRect.pivot = new Vector2(0.5f, 0.5f);

        portraitFrame = CreateImage(mount.transform, "Portrait Frame", GoldAccent);
        Stretch(portraitFrame.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        portraitFrame.raycastTarget = false;

        var background = CreateImage(mount.transform, "Portrait Background", PortraitBackground);
        Stretch(background.rectTransform, Vector2.zero, Vector2.one,
            new Vector2(4f, 4f), new Vector2(-4f, -4f));
        background.raycastTarget = false;

        portrait = CreateImage(mount.transform, "Portrait", Color.white);
        ConfigureCenteredPortrait(portrait.rectTransform);
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;

        lockOverlay = CreateImage(mount.transform, "Lock Overlay", new Color(0.04f, 0.04f, 0.05f, 0.68f));
        Stretch(lockOverlay.rectTransform, Vector2.zero, Vector2.one,
            new Vector2(4f, 4f), new Vector2(-4f, -4f));
        lockOverlay.raycastTarget = false;
        lockOverlay.enabled = false;
    }

    private static void ConfigureCenteredPortrait(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void FitShopPortrait(Image portrait, Sprite sprite, float frameSize)
    {
        if (portrait == null)
            return;

        RectTransform rect = portrait.rectTransform;
        ConfigureCenteredPortrait(rect);

        if (sprite == null)
        {
            rect.sizeDelta = Vector2.one * (frameSize * 0.55f);
            return;
        }

        portrait.sprite = sprite;
        portrait.SetNativeSize();

        Vector2 nativeSize = rect.sizeDelta;
        float ppuRatio = sprite.pixelsPerUnit / ShopPortraitPixelsPerUnit;
        nativeSize *= Mathf.Max(ppuRatio, 0.01f);

        float fitSize = frameSize * 0.9f;
        float largest = Mathf.Max(nativeSize.x, nativeSize.y, 0.01f);
        rect.sizeDelta = nativeSize * (fitSize / largest);
    }

    private void BuildUI()
    {
        var canvasGo = new GameObject("Character Shop Canvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 125;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        rootPanel = CreatePanel(canvasGo.transform, "Shop Panel", Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);

        var backdrop = CreateImage(rootPanel.transform, "Backdrop", new Color(0.02f, 0.03f, 0.06f, 0.9f));
        Stretch(backdrop.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var mainContent = CreatePanel(rootPanel.transform, "Main Content", Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);
        Stretch(mainContent.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
            new Vector2(32f, 32f), new Vector2(-32f, -32f));

        var contentBg = CreateImage(mainContent.transform, "Content Background", new Color(0.05f, 0.06f, 0.09f, 0.97f));
        Stretch(contentBg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var topAccent = CreateImage(mainContent.transform, "Top Accent", new Color(0.95f, 0.82f, 0.35f, 0.45f));
        Stretch(topAccent.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, -4f));
        topAccent.rectTransform.sizeDelta = new Vector2(0f, 4f);

        titleLabel = CreateText(mainContent.transform, "Title", 34, TextAlignmentOptions.Top);
        var titleRect = titleLabel.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -28f);
        titleRect.sizeDelta = new Vector2(900f, 48f);
        titleLabel.fontStyle = FontStyles.Bold;
        titleLabel.color = GoldAccent;
        titleLabel.text = "CHARACTER SHOP";

        subtitleLabel = CreateText(mainContent.transform, "Subtitle", 20, TextAlignmentOptions.Top);
        var subtitleRect = subtitleLabel.rectTransform;
        subtitleRect.anchorMin = new Vector2(0.5f, 1f);
        subtitleRect.anchorMax = new Vector2(0.5f, 1f);
        subtitleRect.pivot = new Vector2(0.5f, 1f);
        subtitleRect.anchoredPosition = new Vector2(0f, -78f);
        subtitleRect.sizeDelta = new Vector2(1100f, 32f);
        subtitleLabel.color = new Color(0.72f, 0.76f, 0.84f);
        subtitleLabel.textWrappingMode = TextWrappingModes.Normal;

        var cardRow = CreatePanel(mainContent.transform, "Card Row", Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);
        Stretch(cardRow.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
            new Vector2(48f, 120f), new Vector2(-48f, -96f));

        BuildCreditsBar(mainContent.transform);
        BuildCloseButton(mainContent.transform);

        rootPanel.SetActive(false);
    }

    private void BuildCreditsBar(Transform parent)
    {
        var bar = CreatePanel(parent, "Credits Bar", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 28f), new Vector2(420f, 52f));
        var barRect = bar.GetComponent<RectTransform>();
        barRect.pivot = new Vector2(0.5f, 0f);

        var barBg = CreateImage(bar.transform, "Credits Background", new Color(0.08f, 0.09f, 0.13f, 0.98f));
        Stretch(barBg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var barAccent = CreateImage(bar.transform, "Credits Accent", new Color(0.35f, 0.62f, 0.9f, 0.35f));
        Stretch(barAccent.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, -3f));
        barAccent.rectTransform.sizeDelta = new Vector2(0f, 3f);

        var label = CreateText(bar.transform, "Credits Label", 18, TextAlignmentOptions.MidlineLeft);
        Stretch(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 0f), new Vector2(-120f, 0f));
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(0.7f, 0.74f, 0.8f);
        label.text = "YOUR CREDITS";

        creditsAmountLabel = CreateText(bar.transform, "Credits Amount", 24, TextAlignmentOptions.MidlineRight);
        Stretch(creditsAmountLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 0f), new Vector2(-18f, 0f));
        creditsAmountLabel.fontStyle = FontStyles.Bold;
        creditsAmountLabel.color = new Color(0.5f, 0.95f, 0.58f);
        creditsAmountLabel.text = "0";
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

        var button = buttonGo.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(Close);

        var label = CreateText(buttonGo.transform, "Close Label", 20, TextAlignmentOptions.Center);
        Stretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(0.88f, 0.9f, 0.95f);
        label.text = "X";
    }

    private Button CreateActionButton(Transform parent, string name, out TextMeshProUGUI label)
    {
        var buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);

        var rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 24f);
        rect.sizeDelta = new Vector2(-48f, 52f);

        var image = buttonGo.GetComponent<Image>();
        image.sprite = whiteSprite;
        image.color = new Color(0.16f, 0.28f, 0.48f, 0.96f);

        var button = buttonGo.GetComponent<Button>();
        button.targetGraphic = image;

        label = CreateText(buttonGo.transform, "Action Label", 22, TextAlignmentOptions.Center);
        Stretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;
        label.text = "SELECT";
        label.raycastTarget = false;

        return button;
    }

    private static void SetButtonColor(Button button, Color color)
    {
        if (button == null)
            return;

        var image = button.targetGraphic as Image;
        if (image != null)
            image.color = color;

        var colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.12f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.12f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
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