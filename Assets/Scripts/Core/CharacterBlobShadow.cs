using UnityEngine;

/// <summary>
/// Adds a soft blob shadow under a 2D character sprite.
/// Layout tracks sprite bounds each frame; work is skipped when nothing changed.
/// </summary>
[DisallowMultipleComponent]
public class CharacterBlobShadow : MonoBehaviour
{
    private const string ShadowChildName = "BlobShadow";
    private const int ShadowTextureSize = 48;

    [SerializeField] private float widthFactor = 0.7f;
    [SerializeField] private float heightFactor = 0.22f;
    [SerializeField] private float feetGap = 0.012f;
    [SerializeField, Range(0f, 1f)] private float alpha = 0.36f;
    [SerializeField] private int sortingOrder = CharacterPresentationConstants.EnemyShadowSortingOrder;

    private SpriteRenderer hostRenderer;
    private SpriteRenderer shadowRenderer;
    private Transform shadowTransform;
    private IBlobShadowHost shadowHost;

    private Sprite trackedSprite;
    private bool trackedFlipX;
    private float trackedHostScaleX = 1f;
    private float trackedHostScaleY = 1f;
    private int trackedSortingOrder = int.MinValue;
    private float trackedAlpha = -1f;

    private Vector3 shadowLocalPosition;
    private Vector3 shadowLocalScale = Vector3.one;
    private Color shadowTint = Color.black;

    private static Sprite cachedShadowSprite;

    public static CharacterBlobShadow Ensure(GameObject host, int sortingOrder = -1)
    {
        if (host == null)
            return null;

        if (!host.TryGetComponent(out CharacterBlobShadow existing))
        {
            existing = host.AddComponent<CharacterBlobShadow>();
        }

        existing.sortingOrder = sortingOrder;
        existing.Initialize();
        return existing;
    }

    private void Awake() => Initialize();

    private void Initialize()
    {
        hostRenderer = GetComponent<SpriteRenderer>();
        shadowHost = GetComponent<IBlobShadowHost>();
        shadowTint.r = 0f;
        shadowTint.g = 0f;
        shadowTint.b = 0f;
        shadowTint.a = alpha;
        EnsureShadowRenderer();
        RefreshShadow(force: true);
    }

    private void LateUpdate()
    {
        if (shadowRenderer == null)
            return;

        bool hidden = ShouldHideShadow();
        if (shadowRenderer.enabled != !hidden)
            shadowRenderer.enabled = !hidden;

        if (hidden)
            return;

        if (trackedAlpha != alpha)
        {
            trackedAlpha = alpha;
            shadowTint.a = alpha;
            shadowRenderer.color = shadowTint;
        }

        if (trackedSortingOrder != sortingOrder)
        {
            trackedSortingOrder = sortingOrder;
            shadowRenderer.sortingOrder = sortingOrder;
        }

        RefreshShadow(force: false);

        if (shadowTransform != null)
            shadowTransform.rotation = Quaternion.identity;
    }

    private bool ShouldHideShadow()
    {
        if (hostRenderer == null || !hostRenderer.enabled)
            return true;

        return shadowHost != null && shadowHost.HideBlobShadow;
    }

    private void EnsureShadowRenderer()
    {
        Transform existing = transform.Find(ShadowChildName);
        if (existing != null && existing.TryGetComponent(out shadowRenderer))
        {
            shadowTransform = shadowRenderer.transform;
            return;
        }

        var shadowObject = new GameObject(ShadowChildName);
        shadowObject.transform.SetParent(transform, false);
        shadowTransform = shadowObject.transform;

        shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
        shadowRenderer.sprite = GetShadowSprite();
        shadowRenderer.color = shadowTint;
        shadowRenderer.flipX = false;
        RenderVisibilityUtility.ConfigureSpriteRenderer(shadowRenderer, sortingOrder);
    }

    private void RefreshShadow(bool force)
    {
        if (shadowRenderer == null || shadowTransform == null)
            return;

        Sprite sprite = hostRenderer != null ? hostRenderer.sprite : null;
        bool flipX = hostRenderer != null && hostRenderer.flipX;
        float hostScaleX = Mathf.Abs(transform.localScale.x);
        float hostScaleY = Mathf.Abs(transform.localScale.y);

        if (!force
            && sprite == trackedSprite
            && flipX == trackedFlipX
            && Mathf.Approximately(hostScaleX, trackedHostScaleX)
            && Mathf.Approximately(hostScaleY, trackedHostScaleY))
        {
            return;
        }

        trackedSprite = sprite;
        trackedFlipX = flipX;
        trackedHostScaleX = hostScaleX;
        trackedHostScaleY = hostScaleY;

        if (sprite == null)
        {
            shadowLocalScale.x = CharacterPresentationConstants.ShadowFallbackWidth;
            shadowLocalScale.y = CharacterPresentationConstants.ShadowFallbackHeight;
            shadowLocalScale.z = 1f;
            shadowLocalPosition.x = 0f;
            shadowLocalPosition.y = CharacterPresentationConstants.ShadowFallbackYOffset;
            shadowLocalPosition.z = 0f;
            shadowTransform.localScale = shadowLocalScale;
            shadowTransform.localPosition = shadowLocalPosition;
            return;
        }

        Bounds bounds = sprite.bounds;
        float spriteWidth = bounds.size.x * hostScaleX;
        float shadowWidth = Mathf.Max(CharacterPresentationConstants.ShadowMinWidth, spriteWidth * widthFactor);
        float shadowHeight = Mathf.Max(CharacterPresentationConstants.ShadowMinHeight, shadowWidth * heightFactor);

        float centerX = bounds.center.x * hostScaleX;
        if (flipX)
            centerX = (bounds.max.x + bounds.min.x) * hostScaleX - centerX;

        float feetY = bounds.min.y * hostScaleY;
        shadowLocalScale.x = shadowWidth;
        shadowLocalScale.y = shadowHeight;
        shadowLocalScale.z = 1f;
        shadowLocalPosition.x = centerX;
        shadowLocalPosition.y = feetY - feetGap - shadowHeight * 0.5f;
        shadowLocalPosition.z = 0f;
        shadowTransform.localScale = shadowLocalScale;
        shadowTransform.localPosition = shadowLocalPosition;
    }

    private static Sprite GetShadowSprite()
    {
        if (cachedShadowSprite != null)
            return cachedShadowSprite;

        var texture = new Texture2D(ShadowTextureSize, ShadowTextureSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        var pixels = new Color32[ShadowTextureSize * ShadowTextureSize];
        float center = (ShadowTextureSize - 1) * 0.5f;
        float radiusX = center;
        float radiusY = center * 0.68f;

        for (int y = 0; y < ShadowTextureSize; y++)
        {
            for (int x = 0; x < ShadowTextureSize; x++)
            {
                float dx = (x - center) / radiusX;
                float dy = (y - center) / radiusY;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alphaSample = Mathf.Clamp01(1f - distance);
                alphaSample = alphaSample * alphaSample * alphaSample;
                byte sample = (byte)(alphaSample * 255f);
                pixels[y * ShadowTextureSize + x] = new Color32(255, 255, 255, sample);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

        cachedShadowSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, ShadowTextureSize, ShadowTextureSize),
            new Vector2(0.5f, 0.5f),
            ShadowTextureSize);
        cachedShadowSprite.hideFlags = HideFlags.HideAndDontSave;
        return cachedShadowSprite;
    }
}