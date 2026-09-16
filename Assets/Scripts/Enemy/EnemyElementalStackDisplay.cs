using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyElementalStackDisplay : MonoBehaviour
{
    private static TMP_FontAsset sharedFont;

    private static readonly Color FireColor = new(1f, 0.55f, 0.2f);
    private static readonly Color IceColor = new(0.55f, 0.85f, 1f);
    private static readonly Color PoisonColor = new(0.45f, 0.9f, 0.3f);
    private static readonly Color ElectricColor = new(1f, 0.95f, 0.35f);
    private static readonly Color VoidColor = new(0.55f, 0.25f, 0.85f);

    [SerializeField] private float verticalOffset = 1.1f;
    [SerializeField] private float rowSpacing = 0.5f;
    [SerializeField] private float labelScale = 1.35f;
    [SerializeField] private float rowFontSize = 14f;
    [SerializeField] private float popupFontSize = 16f;
    [SerializeField] private float popupLifetime = 0.55f;
    [SerializeField] private float popupRiseSpeed = 1.4f;

    private Enemy enemy;
    private Transform visualRoot;
    private TextMeshPro fireLabel;
    private TextMeshPro iceLabel;
    private TextMeshPro poisonLabel;
    private TextMeshPro electricLabel;
    private TextMeshPro voidLabel;
    private Camera mainCamera;

    private int lastBurnStacks;
    private int lastIceStacks;
    private int lastPoisonStacks;
    private int lastShockStacks;

    private readonly List<StackPopup> activePopups = new();

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        mainCamera = Camera.main;
        sharedFont ??= Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        BuildVisuals();
    }

    private void LateUpdate()
    {
        CleanupPopups();
        UpdatePopups();

        if (!ShouldShow())
        {
            visualRoot.gameObject.SetActive(false);
            ResetTracking();
            return;
        }

        visualRoot.gameObject.SetActive(true);
        PositionVisualRoot();
        FaceCamera();

        UpdateFireRow();
        UpdateIceRow();
        UpdatePoisonRow();
        UpdateElectricRow();
        UpdateVoidRow();
    }

    private bool ShouldShow() =>
        ElementalStackVisualSettings.ShowStackVisuals
        && enemy != null
        && enemy.gameObject.activeInHierarchy
        && !enemy.IsBurrowed;

    private void BuildVisuals()
    {
        var rootGo = new GameObject("Elemental Stack Visuals");
        rootGo.transform.SetParent(transform, false);
        visualRoot = rootGo.transform;

        fireLabel = CreateRowLabel("Fire Stacks", FireColor, 0f);
        iceLabel = CreateRowLabel("Ice Stacks", IceColor, -rowSpacing);
        poisonLabel = CreateRowLabel("Poison Stacks", PoisonColor, -rowSpacing * 2f);
        electricLabel = CreateRowLabel("Electric Stacks", ElectricColor, -rowSpacing * 3f);
        voidLabel = CreateRowLabel("Void Timer", VoidColor, -rowSpacing * 4f);
        visualRoot.gameObject.SetActive(false);
    }

    private TextMeshPro CreateRowLabel(string name, Color color, float yOffset)
    {
        var go = new GameObject(name, typeof(TextMeshPro));
        go.transform.SetParent(visualRoot, false);
        go.transform.localPosition = new Vector3(0f, yOffset, 0f);

        var label = go.GetComponent<TextMeshPro>();
        if (sharedFont != null)
            label.font = sharedFont;
        label.fontSize = rowFontSize;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.text = string.Empty;
        label.outlineWidth = 0.2f;
        label.outlineColor = new Color(0f, 0f, 0f, 0.85f);
        label.sortingOrder = 20;
        return label;
    }

    private void PositionVisualRoot()
    {
        float enemyScale = Mathf.Max(0.75f, enemy.transform.lossyScale.y);
        visualRoot.localScale = Vector3.one * labelScale * enemyScale;
        visualRoot.position = transform.position + Vector3.up * (verticalOffset * enemyScale);
    }

    private void FaceCamera()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        visualRoot.rotation = mainCamera.transform.rotation;
    }

    private void UpdateFireRow()
    {
        if (!enemy.IsBurning)
        {
            fireLabel.gameObject.SetActive(false);
            lastBurnStacks = 0;
            return;
        }

        int current = enemy.BurnStacks;
        int max = Mathf.Max(1, enemy.BurnStackThreshold);
        fireLabel.gameObject.SetActive(true);
        fireLabel.text = BuildRowText("Fire", current, max, FireColor);

        if (current > lastBurnStacks)
            SpawnPopup("Fire", current - lastBurnStacks, FireColor, fireLabel.transform.localPosition);

        lastBurnStacks = current;
    }

    private void UpdateIceRow()
    {
        if (!enemy.IsSlowed)
        {
            iceLabel.gameObject.SetActive(false);
            lastIceStacks = 0;
            return;
        }

        int current = enemy.IceSlowStacks;
        int max = Mathf.Max(1, enemy.IceStackThreshold);
        iceLabel.gameObject.SetActive(true);
        iceLabel.text = BuildRowText("Ice", current, max, IceColor);

        if (current > lastIceStacks)
            SpawnPopup("Ice", current - lastIceStacks, IceColor, iceLabel.transform.localPosition);

        lastIceStacks = current;
    }

    private void UpdatePoisonRow()
    {
        if (!enemy.IsPoisoned)
        {
            poisonLabel.gameObject.SetActive(false);
            lastPoisonStacks = 0;
            return;
        }

        int current = enemy.PoisonStacks;
        int max = Mathf.Max(1, enemy.PoisonStackThreshold);
        poisonLabel.gameObject.SetActive(true);
        poisonLabel.text = BuildRowText("Poison", current, max, PoisonColor);

        if (current > lastPoisonStacks)
            SpawnPopup("Poison", current - lastPoisonStacks, PoisonColor, poisonLabel.transform.localPosition);

        lastPoisonStacks = current;
    }

    private void UpdateElectricRow()
    {
        if (!enemy.IsElectrocuted)
        {
            electricLabel.gameObject.SetActive(false);
            lastShockStacks = 0;
            return;
        }

        int current = enemy.ShockStacks;
        int max = Mathf.Max(1, enemy.ShockStackThreshold);
        electricLabel.gameObject.SetActive(true);
        electricLabel.text = BuildRowText("Shock", current, max, ElectricColor);

        if (current > lastShockStacks)
            SpawnPopup("Shock", current - lastShockStacks, ElectricColor, electricLabel.transform.localPosition);

        lastShockStacks = current;
    }

    private void UpdateVoidRow()
    {
        if (!enemy.IsVoidAnchored)
        {
            voidLabel.gameObject.SetActive(false);
            return;
        }

        float remaining = enemy.VoidCollapseTimeRemaining;
        voidLabel.gameObject.SetActive(true);
        string hex = ColorUtility.ToHtmlStringRGB(VoidColor);
        voidLabel.text = $"<color=#{hex}>Void {remaining:0.0}s</color>";
    }

    private static string BuildRowText(string prefix, int current, int max, Color color)
    {
        string filled = new string('\u25cf', current);
        string empty = new string('\u25cb', Mathf.Max(0, max - current));
        string hex = ColorUtility.ToHtmlStringRGB(color);
        return $"<color=#{hex}>{prefix} {current}/{max}</color> <color=#{hex}>{filled}</color><color=#666666>{empty}</color>";
    }

    private void SpawnPopup(string prefix, int amount, Color color, Vector3 localOffset)
    {
        if (amount <= 0)
            return;

        var go = new GameObject($"{prefix} Stack Popup", typeof(TextMeshPro));
        go.transform.SetParent(visualRoot, false);
        go.transform.localPosition = localOffset + Vector3.up * 0.15f;

        var text = go.GetComponent<TextMeshPro>();
        if (sharedFont != null)
            text.font = sharedFont;
        text.fontSize = popupFontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.outlineWidth = 0.25f;
        text.outlineColor = new Color(0f, 0f, 0f, 0.9f);
        text.sortingOrder = 25;
        text.text = amount == 1 ? "+1" : $"+{amount}";

        activePopups.Add(new StackPopup
        {
            Transform = go.transform,
            Text = text,
            BaseColor = color,
            Elapsed = 0f
        });
    }

    private void UpdatePopups()
    {
        for (int i = 0; i < activePopups.Count; i++)
        {
            StackPopup popup = activePopups[i];
            popup.Elapsed += Time.deltaTime;
            popup.Transform.localPosition += Vector3.up * (popupRiseSpeed * Time.deltaTime);

            float alpha = 1f - Mathf.Clamp01(popup.Elapsed / popupLifetime);
            Color c = popup.BaseColor;
            popup.Text.color = new Color(c.r, c.g, c.b, alpha);
        }
    }

    private void CleanupPopups()
    {
        for (int i = activePopups.Count - 1; i >= 0; i--)
        {
            if (activePopups[i].Elapsed < popupLifetime)
                continue;

            if (activePopups[i].Transform != null)
                Destroy(activePopups[i].Transform.gameObject);

            activePopups.RemoveAt(i);
        }
    }

    private void ResetTracking()
    {
        lastBurnStacks = 0;
        lastIceStacks = 0;
        lastPoisonStacks = 0;
        lastShockStacks = 0;
    }

    private void OnDisable()
    {
        if (visualRoot != null)
            visualRoot.gameObject.SetActive(false);

        ResetTracking();

        for (int i = activePopups.Count - 1; i >= 0; i--)
        {
            if (activePopups[i].Transform != null)
                Destroy(activePopups[i].Transform.gameObject);
        }

        activePopups.Clear();
    }

    private class StackPopup
    {
        public Transform Transform;
        public TextMeshPro Text;
        public Color BaseColor;
        public float Elapsed;
    }
}