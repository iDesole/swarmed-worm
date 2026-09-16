using UnityEngine;

/// <summary>
/// World pickup that grants loot to the player on contact (weapons go to hand slots or inventory).
/// </summary>
/// <remarks>
/// Spawned by <see cref="LootDropper"/> when enemies die. Player.CollectLootReward handles the reward logic.
/// </remarks>
[RequireComponent(typeof(CircleCollider2D))]
public class WorldLootPickup : MonoBehaviour
{
    public const float DisplayPixelSize = 16f;
    public const float PickupRadius = 0.28f;

    private LootReward reward;
    private SpriteRenderer spriteRenderer;
    private float bobPhase;
    private Vector3 basePosition;

    public void Initialize(LootReward lootReward, Sprite icon)
    {
        reward = lootReward;
        basePosition = transform.position;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        spriteRenderer.sprite = icon;
        RenderVisibilityUtility.ConfigureSpriteRenderer(spriteRenderer, sortingOrder: 1);

        float targetScale = GetDisplayScale(icon) * GetKindScaleMultiplier(lootReward.kind);
        transform.localScale = Vector3.one * (targetScale * 0.35f);
        bobPhase = Random.Range(0f, Mathf.PI * 2f);

        var collider = GetComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = PickupRadius;
    }

    private void Start()
    {
        float targetScale = transform.localScale.x / 0.35f;
        transform.localScale = Vector3.one * targetScale;
    }

    private static float GetKindScaleMultiplier(LootRewardKind kind) =>
        kind switch
        {
            LootRewardKind.Currency => 0.85f,
            LootRewardKind.Artifact => 1.1f,
            _ => 1f
        };

    private void Update()
    {
        bobPhase += Time.deltaTime * 4f;
        float bob = Mathf.Sin(bobPhase) * 0.04f;
        transform.position = basePosition + new Vector3(0f, bob, 0f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Player player = other.GetComponent<Player>();
        if (player == null || player.IsDead)
            return;

        if (!player.CollectLootReward(reward))
            return;

        Destroy(gameObject);
    }

    public static float GetDisplayScale(Sprite icon)
    {
        if (icon == null)
            return 1f;

        return DisplayPixelSize / Mathf.Max(1f, icon.rect.width);
    }
}