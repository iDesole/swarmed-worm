#if UNITY_EDITOR || DEVELOPMENT_BUILD
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ElementalStackVisualDebug : MonoBehaviour
{
    [SerializeField] private KeyCode toggleKey = KeyCode.F6;

    private Toggle hudToggle;
    private TextMeshProUGUI hudToggleLabel;

    private void Awake()
    {
        ElementalStackVisualSettings.Changed += HandleSettingsChanged;
        BuildToggleUi();
        RefreshToggleUi();
    }

    private void OnDestroy() =>
        ElementalStackVisualSettings.Changed -= HandleSettingsChanged;

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            ElementalStackVisualSettings.Toggle();
    }

    private void HandleSettingsChanged(bool enabled) =>
        RefreshToggleUi();

    private void BuildToggleUi()
    {
        var uiRoot = new GameObject("Stack Visual Debug UI");
        uiRoot.transform.SetParent(transform, false);

        var canvas = uiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;

        var scaler = uiRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        uiRoot.AddComponent<GraphicRaycaster>();

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        Sprite whiteSprite = CreateWhiteSprite();

        var panel = new GameObject("Stack Visual Toggle", typeof(RectTransform));
        panel.transform.SetParent(uiRoot.transform, false);

        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-24f, 24f);
        panelRect.sizeDelta = new Vector2(220f, 36f);

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(panel.transform, false);
        var bgImage = bg.GetComponent<Image>();
        bgImage.sprite = whiteSprite;
        bgImage.color = new Color(0.08f, 0.08f, 0.1f, 0.82f);
        Stretch(bg.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var toggleGo = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
        toggleGo.transform.SetParent(panel.transform, false);
        var toggleRect = toggleGo.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0f, 0.5f);
        toggleRect.anchorMax = new Vector2(0f, 0.5f);
        toggleRect.pivot = new Vector2(0f, 0.5f);
        toggleRect.anchoredPosition = new Vector2(10f, 0f);
        toggleRect.sizeDelta = new Vector2(24f, 24f);

        var checkBg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        checkBg.transform.SetParent(toggleGo.transform, false);
        var checkBgImage = checkBg.GetComponent<Image>();
        checkBgImage.sprite = whiteSprite;
        checkBgImage.color = new Color(0.2f, 0.2f, 0.24f, 1f);
        Stretch(checkBg.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkmark.transform.SetParent(checkBg.transform, false);
        var checkmarkImage = checkmark.GetComponent<Image>();
        checkmarkImage.sprite = whiteSprite;
        checkmarkImage.color = new Color(0.35f, 0.9f, 0.45f, 1f);
        Stretch(checkmark.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));

        hudToggle = toggleGo.GetComponent<Toggle>();
        hudToggle.targetGraphic = checkBgImage;
        hudToggle.graphic = checkmarkImage;
        hudToggle.onValueChanged.AddListener(ElementalStackVisualSettings.SetEnabled);

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(panel.transform, false);
        hudToggleLabel = labelGo.GetComponent<TextMeshProUGUI>();
        if (font != null)
            hudToggleLabel.font = font;
        hudToggleLabel.fontSize = 16f;
        hudToggleLabel.alignment = TextAlignmentOptions.MidlineLeft;
        hudToggleLabel.raycastTarget = false;
        Stretch(labelGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(40f, 0f), new Vector2(-8f, 0f));
    }

    private void RefreshToggleUi()
    {
        bool enabled = ElementalStackVisualSettings.ShowStackVisuals;

        if (hudToggle != null)
            hudToggle.SetIsOnWithoutNotify(enabled);

        if (hudToggleLabel != null)
            hudToggleLabel.text = enabled ? "Stack Visuals On" : "Stack Visuals Off";
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static Sprite CreateWhiteSprite()
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }
}
#endif