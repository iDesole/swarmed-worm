using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles focusing nearby interactables and pressing E to talk/shop with NPCs.
/// </summary>
[DefaultExecutionOrder(-90)]
public class PlayerInteractionController : MonoBehaviour
{
    private const KeyCode InteractKey = KeyCode.E;

    private static PlayerInteractionController instance;

    private readonly HashSet<InteractableCharacter> candidates = new();
    private InteractableCharacter focusedInteractable;
    private Player owner;

    private Canvas promptCanvas;
    private TextMeshProUGUI promptLabel;
    private Image promptBackground;

    public static InteractableCharacter FocusedInteractable =>
        instance != null ? instance.focusedInteractable : null;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
        owner = GetComponent<Player>();
        BuildPromptUI();
        HidePrompt();
    }

    private void Start() => ResyncNearbyInteractables();

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Update()
    {
        if (owner == null || owner.IsDead)
        {
            ClearFocus();
            HidePrompt();
            return;
        }

        if (GameUIState.BlocksGameplay)
        {
            HidePrompt();
            return;
        }

        UpdateFocusedInteractable();

        if (focusedInteractable == null)
        {
            HidePrompt();
            return;
        }

        ShowPrompt(focusedInteractable.InteractPrompt);

        if (Input.GetKeyDown(InteractKey))
            TryInteract();
    }

    public static void NotifyEnteredRange(InteractableCharacter character, Player player)
    {
        if (instance == null || character == null || player == null)
            return;

        if (instance.owner != player)
            return;

        instance.candidates.Add(character);
        instance.UpdateFocusedInteractable();
    }

    public static void NotifyExitedRange(InteractableCharacter character, Player player)
    {
        if (instance == null || character == null)
            return;

        if (player != null && instance.owner != null && instance.owner != player)
            return;

        instance.candidates.Remove(character);
        if (instance.focusedInteractable == character)
            instance.focusedInteractable = null;

        instance.UpdateFocusedInteractable();
    }

    private void UpdateFocusedInteractable()
    {
        InteractableCharacter best = null;
        float bestDistance = float.MaxValue;
        Vector3 playerPosition = owner.transform.position;

        foreach (InteractableCharacter candidate in candidates)
        {
            if (candidate == null || !candidate.isActiveAndEnabled || !candidate.IsInteractionEnabled)
                continue;

            float distance = Vector3.SqrMagnitude(candidate.transform.position - playerPosition);
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            best = candidate;
        }

        focusedInteractable = best;
    }

    private void TryInteract()
    {
        if (focusedInteractable == null || owner == null)
            return;

        if (!focusedInteractable.CanInteract(owner))
            return;

        focusedInteractable.Interact(owner);
    }

    private void ClearFocus()
    {
        candidates.Clear();
        focusedInteractable = null;
    }

    private void ResyncNearbyInteractables()
    {
        if (owner == null)
            return;

        candidates.Clear();
        focusedInteractable = null;

        Collider2D playerCollider = owner.GetComponent<Collider2D>();
        if (playerCollider == null)
            return;

        ContactFilter2D filter = new()
        {
            useTriggers = true,
            useLayerMask = false
        };

        var overlaps = new Collider2D[16];
        int count = playerCollider.Overlap(filter, overlaps);
        for (int i = 0; i < count; i++)
        {
            if (overlaps[i] == null)
                continue;

            InteractableCharacter character = overlaps[i].GetComponent<InteractableCharacter>();
            if (character != null && character.IsInteractionEnabled)
                character.EnsurePlayerTracked(owner);
        }

        UpdateFocusedInteractable();
    }

    private void BuildPromptUI()
    {
        var canvasGo = new GameObject("Interact Prompt Canvas");
        canvasGo.transform.SetParent(transform, false);

        promptCanvas = canvasGo.AddComponent<Canvas>();
        promptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        promptCanvas.sortingOrder = 110;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        var panelGo = new GameObject("Interact Prompt Panel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(canvasGo.transform, false);

        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 72f);
        panelRect.sizeDelta = new Vector2(520f, 52f);

        promptBackground = panelGo.GetComponent<Image>();
        promptBackground.color = new Color(0.06f, 0.07f, 0.1f, 0.92f);
        promptBackground.raycastTarget = false;

        var accentGo = new GameObject("Prompt Accent", typeof(RectTransform), typeof(Image));
        accentGo.transform.SetParent(panelGo.transform, false);
        var accentRect = accentGo.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(0f, 3f);
        accentGo.GetComponent<Image>().color = new Color(0.95f, 0.82f, 0.35f, 0.55f);

        var labelGo = new GameObject("Prompt Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(panelGo.transform, false);

        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(16f, 8f);
        labelRect.offsetMax = new Vector2(-16f, -8f);

        promptLabel = labelGo.GetComponent<TextMeshProUGUI>();
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null)
            promptLabel.font = font;

        promptLabel.fontSize = 24f;
        promptLabel.alignment = TextAlignmentOptions.Center;
        promptLabel.color = new Color(0.92f, 0.93f, 0.98f);
        promptLabel.fontStyle = FontStyles.Bold;
        promptLabel.raycastTarget = false;
    }

    private void ShowPrompt(string message)
    {
        if (promptCanvas == null || promptLabel == null)
            return;

        promptLabel.text = $"[E] {message}";
        promptCanvas.enabled = true;
    }

    private void HidePrompt()
    {
        if (promptCanvas != null)
            promptCanvas.enabled = false;
    }
}