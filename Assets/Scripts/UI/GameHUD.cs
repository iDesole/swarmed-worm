using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime-built HUD: health, wave info, objectives, and game-over overlay.
/// </summary>
/// <remarks>
/// Created by <see cref="GameUIBootstrap"/> if missing from the scene. Subscribes to player events and WaveEvents.
/// </remarks>
public class GameHUD : MonoBehaviour
{
    private Player player;
    private WaveManager waveManager;

    private Image healthFill;
    private TextMeshProUGUI healthLabel;
    private GameObject shieldBarPanel;
    private Image shieldFill;
    private TextMeshProUGUI shieldLabel;
    private GameObject waveLabelPanel;
    private TextMeshProUGUI waveLabel;
    private TextMeshProUGUI objectiveLabel;
    private GameObject objectivePanel;
    private GameObject gameOverPanel;
    private TextMeshProUGUI gameOverLabel;
    private TextMeshProUGUI gameOverRestartLabel;
    private bool gameOverVisible;

    private TMP_FontAsset font;
    private Sprite whiteSprite;

    private void Awake()
    {
        font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        whiteSprite = CreateWhiteSprite();
        BuildUI();
    }

    private void Start()
    {
        waveManager = FindFirstObjectByType<WaveManager>();
        BindPlayer(PlayerCatalog.ActivePlayer);
    }

    private void OnEnable()
    {
        WaveEvents.PlayerDeathPresentationFinished += ShowGameOver;
        PlayerCatalog.PlayerSpawned += BindPlayer;
    }

    private void OnDisable()
    {
        WaveEvents.PlayerDeathPresentationFinished -= ShowGameOver;
        PlayerCatalog.PlayerSpawned -= BindPlayer;
    }

    private void OnDestroy() => BindPlayer(null);

    private void BindPlayer(Player newPlayer)
    {
        if (player != null)
        {
            player.OnHealthChanged -= RefreshHealthBar;
            player.OnShieldChanged -= RefreshShieldBar;
        }

        player = newPlayer;
        if (player == null)
            return;

        player.OnHealthChanged += RefreshHealthBar;
        player.OnShieldChanged += RefreshShieldBar;
        RefreshHealthBar();
        RefreshShieldBar();
    }

    private void Update()
    {
        RefreshWaveLabel();
        RefreshObjectiveLabel();
        HandleGameOverInput();
    }

    private void HandleGameOverInput()
    {
        if (!gameOverVisible)
            return;

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
            return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            HideGameOver();
            gameManager.RestartAfterDeath();
            return;
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            HideGameOver();
            gameManager.ReturnToLobbyAfterDeath();
        }
    }

    private void RefreshHealthBar()
    {
        if (player == null || healthFill == null) return;

        float max = Mathf.Max(1f, player.MaxHealth);
        healthFill.fillAmount = player.CurrentHealth / max;

        if (healthLabel != null)
            healthLabel.text = $"{Mathf.CeilToInt(player.CurrentHealth)} / {Mathf.CeilToInt(max)}";
    }

    private void RefreshShieldBar()
    {
        if (player == null || shieldFill == null)
            return;

        float max = player.MaxShield;
        bool showShield = max > 0f;

        if (shieldBarPanel != null)
            shieldBarPanel.SetActive(showShield);

        if (!showShield)
            return;

        float fillMax = Mathf.Max(1f, max);
        shieldFill.fillAmount = player.CurrentShield / fillMax;

        if (shieldLabel != null)
            shieldLabel.text = $"{Mathf.CeilToInt(player.CurrentShield)} / {Mathf.CeilToInt(max)}";
    }

    private void RefreshWaveLabel()
    {
        if (waveLabel == null)
            return;

        if (waveManager == null)
            waveManager = FindFirstObjectByType<WaveManager>();

        bool showWaveUi = ShouldShowWaveUi();
        if (waveLabelPanel != null)
            waveLabelPanel.SetActive(showWaveUi);

        if (!showWaveUi)
            return;

        if (waveManager.IsGameOver)
        {
            waveLabel.text = "Game Over";
            return;
        }

        if (waveManager.CanStartNextWave)
        {
            waveLabel.text = $"Wave {waveManager.WaveNumber} Complete — Press F for Wave {waveManager.NextWaveNumber}";
            return;
        }

        if (waveManager.IsWaveActive)
        {
            waveLabel.text = $"Wave {waveManager.WaveNumber} — {FormatWaveType(waveManager.CurrentWaveType)}";
            return;
        }

        waveLabel.text = $"Wave {waveManager.WaveNumber}";
    }

    private bool ShouldShowWaveUi()
    {
        if (waveManager == null)
            return false;

        if (waveManager.IsHubMode)
            return false;

        return waveManager.IsGameOver ||
               waveManager.IsWaveActive ||
               waveManager.CanStartNextWave ||
               waveManager.WaveNumber > 0;
    }

    private void RefreshObjectiveLabel()
    {
        if (objectivePanel == null || objectiveLabel == null) return;

        if (waveManager == null || waveManager.IsHubMode || !waveManager.IsWaveActive)
        {
            objectivePanel.SetActive(false);
            return;
        }

        WaveType waveType = waveManager.CurrentWaveType;
        string text = BuildObjectiveText(waveType);
        bool show = !string.IsNullOrEmpty(text);

        objectivePanel.SetActive(show);
        if (show)
            objectiveLabel.text = text;
    }

    private string BuildObjectiveText(WaveType waveType)
    {
        switch (waveType)
        {
            case WaveType.Survival:
                return $"Survive: {FormatCountdown(waveManager.TimeLeft)}";

            case WaveType.KillCount:
            {
                int kills = waveManager.KillCount;
                int target = waveManager.KillTarget;
                int remaining = Mathf.Max(0, target - kills);
                return $"Kills: {remaining} left ({kills}/{target})";
            }

            default:
                return string.Empty;
        }
    }

    private static string FormatWaveType(WaveType type)
    {
        return type switch
        {
            WaveType.KillCount => "Kill Count",
            WaveType.Survival => "Survival",
            _ => type.ToString()
        };
    }

    private static string FormatCountdown(float seconds)
    {
        int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
        int minutes = total / 60;
        int secs = total % 60;
        return $"{minutes}:{secs:00}";
    }

    private void BuildUI()
    {
        var canvasGo = new GameObject("HUD Canvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        BuildShieldBar(canvasGo.transform);
        BuildHealthBar(canvasGo.transform);
        BuildWaveLabel(canvasGo.transform);
        BuildObjectivePanel(canvasGo.transform);
        BuildGameOverPanel(canvasGo.transform);
        BuildDemoCopyright(canvasGo.transform);
    }

    private void ShowGameOver(Player _)
    {
        gameOverVisible = true;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (objectivePanel != null)
            objectivePanel.SetActive(false);
    }

    private void HideGameOver()
    {
        gameOverVisible = false;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void BuildShieldBar(Transform parent)
    {
        shieldBarPanel = CreatePanel(parent, "Shield Bar", new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(24f, 68f), new Vector2(320f, 36f));

        var bg = CreateImage(shieldBarPanel.transform, "Background", new Color(0.1f, 0.1f, 0.12f, 0.85f));
        Stretch(bg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        shieldFill = CreateImage(shieldBarPanel.transform, "Fill", new Color(0.25f, 0.7f, 1f, 1f));
        var fillRect = shieldFill.rectTransform;
        Stretch(fillRect, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
        shieldFill.type = Image.Type.Filled;
        shieldFill.fillMethod = Image.FillMethod.Horizontal;
        shieldFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        shieldFill.fillAmount = 1f;

        shieldLabel = CreateText(shieldBarPanel.transform, "Shield Label", 18, TextAlignmentOptions.MidlineLeft);
        var labelRect = shieldLabel.rectTransform;
        Stretch(labelRect, Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, 0f));
        shieldLabel.color = Color.white;
    }

    private void BuildHealthBar(Transform parent)
    {
        var panel = CreatePanel(parent, "Health Bar", new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(24f, 24f), new Vector2(320f, 36f));

        var bg = CreateImage(panel.transform, "Background", new Color(0.1f, 0.1f, 0.12f, 0.85f));
        Stretch(bg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        healthFill = CreateImage(panel.transform, "Fill", new Color(0.2f, 0.85f, 0.35f, 1f));
        var fillRect = healthFill.rectTransform;
        Stretch(fillRect, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
        healthFill.type = Image.Type.Filled;
        healthFill.fillMethod = Image.FillMethod.Horizontal;
        healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        healthFill.fillAmount = 1f;

        healthLabel = CreateText(panel.transform, "Health Label", 18, TextAlignmentOptions.MidlineLeft);
        var labelRect = healthLabel.rectTransform;
        Stretch(labelRect, Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, 0f));
        healthLabel.color = Color.white;
    }

    private void BuildWaveLabel(Transform parent)
    {
        waveLabelPanel = CreatePanel(parent, "Wave Label", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -20f), new Vector2(480f, 48f));

        var bg = CreateImage(waveLabelPanel.transform, "Background", new Color(0.08f, 0.08f, 0.1f, 0.7f));
        Stretch(bg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        waveLabel = CreateText(waveLabelPanel.transform, "Wave Text", 28, TextAlignmentOptions.Center);
        Stretch(waveLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        waveLabel.fontStyle = FontStyles.Bold;
        waveLabel.color = new Color(0.95f, 0.95f, 1f);
        waveLabel.text = string.Empty;
        waveLabelPanel.SetActive(false);
    }

    private void BuildGameOverPanel(Transform parent)
    {
        gameOverPanel = CreatePanel(parent, "Game Over Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(560f, 260f));

        var bg = CreateImage(gameOverPanel.transform, "Background", new Color(0.05f, 0.05f, 0.08f, 0.92f));
        Stretch(bg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        gameOverLabel = CreateText(gameOverPanel.transform, "Game Over Text", 56, TextAlignmentOptions.Center);
        var titleRect = gameOverLabel.rectTransform;
        Stretch(titleRect, new Vector2(0f, 0.52f), Vector2.one, Vector2.zero, Vector2.zero);
        gameOverLabel.fontStyle = FontStyles.Bold;
        gameOverLabel.color = new Color(1f, 0.35f, 0.35f);
        gameOverLabel.text = "GAME OVER";

        gameOverRestartLabel = CreateText(gameOverPanel.transform, "Restart Prompt", 24, TextAlignmentOptions.Center);
        var restartRect = gameOverRestartLabel.rectTransform;
        Stretch(restartRect, Vector2.zero, new Vector2(1f, 0.52f), Vector2.zero, Vector2.zero);
        gameOverRestartLabel.color = new Color(0.9f, 0.9f, 0.95f);
        gameOverRestartLabel.text = "Press R to play again\nPress L to return to lobby";

        gameOverPanel.SetActive(false);
    }

    private void BuildObjectivePanel(Transform parent)
    {
        objectivePanel = CreatePanel(parent, "Objective Panel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -72f), new Vector2(320f, 40f));

        var bg = CreateImage(objectivePanel.transform, "Background", new Color(0.08f, 0.08f, 0.1f, 0.75f));
        Stretch(bg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        objectiveLabel = CreateText(objectivePanel.transform, "Objective Text", 22, TextAlignmentOptions.Center);
        Stretch(objectiveLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        objectiveLabel.fontStyle = FontStyles.Bold;
        objectiveLabel.color = new Color(1f, 0.88f, 0.45f);
        objectivePanel.SetActive(false);
    }

    private void BuildDemoCopyright(Transform parent)
    {
        var panel = CreatePanel(parent, "Demo Copyright", new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-16f, 14f), new Vector2(560f, 24f));
        panel.GetComponent<RectTransform>().pivot = new Vector2(1f, 0f);

        var text = CreateText(panel.transform, "Copyright Text", 14, TextAlignmentOptions.MidlineRight);
        Stretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        text.color = new Color(1f, 1f, 1f, 0.45f);
        text.text = "SWARMED WORM  ·  PORTFOLIO DEMO  ·  © 2026 CHASE WILSON";
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