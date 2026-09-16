using System;
using UnityEngine;

public enum InteractableCharacterKind
{
    Generic,
    Dialogue,
    Shop
}

[Serializable]
public class InteractableCharacterSpawnPlacement
{
    [Tooltip("Map from Wave Database (Lobby, Beach, Training, etc.).")]
    public string environmentName = "Lobby";

    [Tooltip("Fallback index if the name is blank or not found.")]
    public int environmentIndex = -1;

    [Tooltip("World position on the chosen map where this character stands.")]
    public Vector3 worldPosition;

    [Header("Optional Condition Area")]
    [Tooltip("When enabled, the character stays hidden until the player enters the condition zone.")]
    public bool requireConditionArea;

    [Tooltip("Offset from world position to the center of the condition box.")]
    public Vector2 conditionAreaOffset;

    public Vector2 conditionAreaSize = new(5f, 4f);
}

/// <summary>
/// NPC the player can walk up to and press E to interact (shop/dialogue hooks come later).
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class InteractableCharacter : MonoBehaviour, IInteractable
{
    [Header("Character")]
    [SerializeField] private string characterId;
    [SerializeField] private string displayName = "Shopkeeper";
    [SerializeField] private string interactVerb = "Talk";
    [SerializeField] private InteractableCharacterKind characterKind = InteractableCharacterKind.Shop;

    [Header("Spawn Placement")]
    [SerializeField] private bool useSpawnPlacement = true;
    [SerializeField] private InteractableCharacterSpawnPlacement spawnPlacement = new();
    [SerializeField] private bool hideWhenWrongMap = true;

    [Header("Interaction")]
    [SerializeField] private float interactRadius = 1.35f;
    [SerializeField] private bool startsEnabled = true;
    [SerializeField] private bool facePlayerOnInteract = true;
    [SerializeField] private bool showGizmoInSceneView = true;
    [SerializeField] private InteractableInteractionRules interactionRules = new();
    [SerializeField] private ShopOffer[] shopOffers = System.Array.Empty<ShopOffer>();

    private bool isEnabled = true;
    private int interactionCount;
    private int purchaseCount;
    private bool conditionAreaSatisfied;
    private bool isVisibleOnCurrentMap;
    private int playersInRange;
    private SpriteRenderer spriteRenderer;
    private CircleCollider2D interactCollider;
    private InteractableConditionArea conditionArea;

    public event Action<Player> Interacted;
    public event Action<Player> PlayerEnteredRange;
    public event Action<Player> PlayerExitedRange;

    public string CharacterId => characterId;
    public string DisplayName => displayName;
    public InteractableCharacterKind CharacterKind => characterKind;
    public InteractableCharacterSpawnPlacement SpawnPlacement => spawnPlacement;
    public ShopOffer[] ShopOffers => shopOffers;
    public InteractableInteractionRules InteractionRules => interactionRules;
    public bool IsPlayerInRange => playersInRange > 0;
    public bool IsInteractionEnabled => isEnabled && isVisibleOnCurrentMap && HasInteractionsRemaining();
    public bool IsVisibleOnCurrentMap => isVisibleOnCurrentMap;

    public string InteractPrompt => string.IsNullOrWhiteSpace(interactVerb)
        ? $"Talk to {displayName}"
        : $"{interactVerb} {displayName}";

    private void Reset()
    {
        ConfigureCollider();
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        interactCollider = GetComponent<CircleCollider2D>();
        ConfigureCollider();
        isEnabled = startsEnabled;
        RenderVisibilityUtility.ConfigureSpriteRenderer(spriteRenderer, sortingOrder: 0);
    }

    private void OnEnable()
    {
        EnvironmentEvents.EnvironmentChanged += HandleEnvironmentChanged;
        ApplySpawnPlacement();
    }

    private void OnDisable()
    {
        EnvironmentEvents.EnvironmentChanged -= HandleEnvironmentChanged;
        playersInRange = 0;
        PlayerInteractionController.NotifyExitedRange(this, PlayerCatalog.ActivePlayer);
    }

    private void HandleEnvironmentChanged(WaveEnvironmentDefinition environment) => ApplySpawnPlacement();

    private void ConfigureCollider()
    {
        if (interactCollider == null)
            interactCollider = GetComponent<CircleCollider2D>();

        interactCollider.isTrigger = true;
        interactCollider.radius = Mathf.Max(0.25f, interactRadius);
    }

    public void ApplyDefinition(InteractableCharacterDefinition definition)
    {
        if (definition == null)
            return;

        characterId = definition.characterId;
        displayName = definition.displayName;
        interactVerb = definition.interactVerb;
        characterKind = definition.characterKind;
        useSpawnPlacement = definition.useSpawnPlacement;
        spawnPlacement = definition.CloneSpawnPlacement();
        hideWhenWrongMap = definition.hideWhenWrongMap;
        interactRadius = definition.interactRadius;
        startsEnabled = definition.startsEnabled;
        facePlayerOnInteract = definition.facePlayerOnInteract;
        interactionRules = CloneInteractionRules(definition.interactionRules);
        shopOffers = definition.shopOffers ?? System.Array.Empty<ShopOffer>();
        isEnabled = startsEnabled;
        ResetSessionCounters();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && definition.sprite != null)
            spriteRenderer.sprite = definition.sprite;

        gameObject.name = definition.ResolvedName;
        ConfigureCollider();

        if (isActiveAndEnabled)
            ApplySpawnPlacement();
    }

    public void SetInteractionEnabled(bool enabled) => isEnabled = enabled;

    public void RecordShopPurchase() => purchaseCount++;

    public void EnsurePlayerTracked(Player player)
    {
        if (player == null || !isVisibleOnCurrentMap)
            return;

        if (playersInRange <= 0)
        {
            playersInRange = 1;
            PlayerEnteredRange?.Invoke(player);
        }

        PlayerInteractionController.NotifyEnteredRange(this, player);
    }

    public static void ResetAllSessionState()
    {
        InteractableCharacter[] characters = UnityEngine.Object.FindObjectsByType<InteractableCharacter>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < characters.Length; i++)
            characters[i]?.ResetSessionCounters();
    }

    public static void ClearAllPlayerPresence()
    {
        InteractableCharacter[] characters = UnityEngine.Object.FindObjectsByType<InteractableCharacter>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < characters.Length; i++)
            characters[i]?.ClearPlayerPresence();
    }

    public void SetConditionAreaSatisfied(bool satisfied)
    {
        conditionAreaSatisfied = satisfied;
        RefreshVisibility();
    }

    public bool CanInteract(Player player)
    {
        if (!IsInteractionEnabled || player == null || player.IsDead)
            return false;

        return playersInRange > 0;
    }

    public void Interact(Player player)
    {
        if (!CanInteract(player))
            return;

        if (facePlayerOnInteract)
            FaceTowards(player.transform.position);

        interactionCount++;
        Interacted?.Invoke(player);
        HandleInteractionKind(player);
    }

    protected virtual void HandleInteractionKind(Player player)
    {
        switch (characterKind)
        {
            case InteractableCharacterKind.Shop:
                CharacterShopUI.Open(this, player);
                break;

            case InteractableCharacterKind.Dialogue:
                GameLog.Info($"{displayName}: \"Hello, {player.CharacterName}.\"");
                break;

            default:
                GameLog.Info($"{player.CharacterName} interacted with {displayName}.");
                break;
        }
    }

    public void ApplySpawnPlacement()
    {
        if (!useSpawnPlacement)
        {
            isVisibleOnCurrentMap = true;
            gameObject.SetActive(true);
            RefreshVisibility();
            return;
        }

        if (!IsConfiguredForActiveMap())
        {
            isVisibleOnCurrentMap = false;
            playersInRange = 0;
            PlayerInteractionController.NotifyExitedRange(this, PlayerCatalog.ActivePlayer);

            if (hideWhenWrongMap)
            {
                gameObject.SetActive(false);
            }
            else
            {
                RefreshVisibility();
            }

            return;
        }

        gameObject.SetActive(true);
        transform.position = spawnPlacement.worldPosition;
        conditionAreaSatisfied = !spawnPlacement.requireConditionArea;
        EnsureConditionArea();
        RefreshVisibility();
    }

    private bool IsConfiguredForActiveMap()
    {
        WaveDatabase database = WaveCatalog.Database;
        if (database == null)
            return true;

        if (!TryResolveConfiguredEnvironment(database, out int configuredIndex))
            return true;

        WaveEnvironmentDefinition activeEnvironment = database.GetActiveEnvironment();
        if (activeEnvironment == null)
            return false;

        return database.ActiveEnvironmentIndex == configuredIndex ||
               string.Equals(
                   activeEnvironment.EnvironmentName,
                   database.Environments[configuredIndex].EnvironmentName,
                   StringComparison.OrdinalIgnoreCase);
    }

    private bool TryResolveConfiguredEnvironment(WaveDatabase database, out int environmentIndex)
    {
        environmentIndex = -1;

        if (!string.IsNullOrWhiteSpace(spawnPlacement.environmentName) &&
            database.TryGetEnvironmentIndex(spawnPlacement.environmentName, out environmentIndex))
            return true;

        if (spawnPlacement.environmentIndex >= 0 && spawnPlacement.environmentIndex < database.Environments.Count)
        {
            environmentIndex = spawnPlacement.environmentIndex;
            return true;
        }

        return false;
    }

    private void EnsureConditionArea()
    {
        if (!spawnPlacement.requireConditionArea)
        {
            if (conditionArea != null)
                conditionArea.gameObject.SetActive(false);

            return;
        }

        if (conditionArea == null)
        {
            Transform existing = transform.Find("Condition Area");
            if (existing != null)
                conditionArea = existing.GetComponent<InteractableConditionArea>();

            if (conditionArea == null)
            {
                var areaGo = new GameObject("Condition Area", typeof(RectTransform));
                areaGo.transform.SetParent(transform, false);
                conditionArea = areaGo.AddComponent<InteractableConditionArea>();
            }
        }

        Vector3 areaPosition = transform.position + (Vector3)spawnPlacement.conditionAreaOffset;
        areaPosition.z = 0f;
        conditionArea.transform.position = areaPosition;
        conditionArea.gameObject.SetActive(true);
        conditionArea.Configure(this, spawnPlacement.conditionAreaSize);
    }

    private void RefreshVisibility()
    {
        bool shouldShow = !useSpawnPlacement || (IsConfiguredForActiveMap() &&
            (!spawnPlacement.requireConditionArea || conditionAreaSatisfied));

        isVisibleOnCurrentMap = shouldShow;

        if (spriteRenderer != null)
            spriteRenderer.enabled = shouldShow;

        if (interactCollider != null)
            interactCollider.enabled = shouldShow;

        if (!shouldShow)
        {
            playersInRange = 0;
            PlayerInteractionController.NotifyExitedRange(this, PlayerCatalog.ActivePlayer);
        }
    }

    private bool HasInteractionsRemaining()
    {
        InteractableInteractionRules rules = interactionRules ?? new InteractableInteractionRules();

        return rules.repeatPolicy switch
        {
            InteractableRepeatPolicy.Always => true,
            InteractableRepeatPolicy.MaxInteractions => interactionCount < Mathf.Max(1, rules.maxInteractions),
            InteractableRepeatPolicy.UntilPurchases => purchaseCount < Mathf.Max(1, rules.maxPurchases),
            _ => true
        };
    }

    private void ResetSessionCounters()
    {
        interactionCount = 0;
        purchaseCount = 0;
    }

    private void ClearPlayerPresence()
    {
        if (playersInRange <= 0)
            return;

        playersInRange = 0;
        PlayerInteractionController.NotifyExitedRange(this, null);
    }

    private static InteractableInteractionRules CloneInteractionRules(InteractableInteractionRules source)
    {
        if (source == null)
            return new InteractableInteractionRules();

        return new InteractableInteractionRules
        {
            repeatPolicy = source.repeatPolicy,
            maxInteractions = source.maxInteractions,
            maxPurchases = source.maxPurchases
        };
    }

    private void FaceTowards(Vector3 worldPosition)
    {
        if (spriteRenderer == null)
            return;

        float direction = worldPosition.x - transform.position.x;
        if (Mathf.Abs(direction) <= 0.05f)
            return;

        spriteRenderer.flipX = direction < 0f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Player player = other.GetComponent<Player>();
        if (player == null || player.IsDead || !isVisibleOnCurrentMap)
            return;

        playersInRange++;
        PlayerEnteredRange?.Invoke(player);
        PlayerInteractionController.NotifyEnteredRange(this, player);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Player player = other.GetComponent<Player>();
        if (player == null)
            return;

        playersInRange = Mathf.Max(0, playersInRange - 1);
        PlayerExitedRange?.Invoke(player);
        PlayerInteractionController.NotifyExitedRange(this, player);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmoInSceneView)
            return;

        if (useSpawnPlacement)
        {
            Gizmos.color = new Color(0.95f, 0.82f, 0.35f, 0.9f);
            Vector3 spawnPos = spawnPlacement.worldPosition;
            Gizmos.DrawWireSphere(spawnPos, 0.3f);
            Gizmos.DrawLine(spawnPos, spawnPos + Vector3.up * 0.8f);

            if (spawnPlacement.requireConditionArea)
            {
                Gizmos.color = new Color(0.45f, 0.75f, 1f, 0.35f);
                Vector3 areaCenter = spawnPos + (Vector3)spawnPlacement.conditionAreaOffset;
                Gizmos.DrawWireCube(areaCenter, spawnPlacement.conditionAreaSize);
            }
        }

        CircleCollider2D collider = interactCollider != null ? interactCollider : GetComponent<CircleCollider2D>();
        float radius = collider != null ? collider.radius : interactRadius;

        Gizmos.color = new Color(0.95f, 0.82f, 0.35f, 0.35f);
        Vector3 center = transform.position;
        if (collider != null)
            center = transform.TransformPoint(collider.offset);

        Gizmos.DrawWireSphere(center, radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y));
    }
}