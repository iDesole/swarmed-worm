using UnityEngine;

public enum EnemyRole
{
    Standard,
    Miniboss,
    Boss,
    Seeker,
    Hostage,
    DestroyTarget
}

public enum EnemyBehaviorType
{
    Melee,
    Ranged,
    Burrower
}

[CreateAssetMenu(fileName = "EnemyStats", menuName = "Game/Enemy Stats")]
public class EnemyStats : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string enemyName = "Enemy";
    [TextArea] [SerializeField] private string description;
    [SerializeField] private string category = "";

    [Header("Visuals")]
    [SerializeField] private Sprite sprite;
    [SerializeField] private float displayScale = 1f;
    [SerializeField] private Color tintColor = Color.white;

    [Header("Prefab")]
    [Tooltip("Optional. Leave empty to use the spawner generic prefab for this behavior type.")]
    [SerializeField] private Enemy prefabOverride;
    [SerializeField] private EnemyBehaviorType behaviorType = EnemyBehaviorType.Melee;

    [Header("Core")]
    [SerializeField] private float maxHealth = 30f;
    [SerializeField] private float moveSpeed = 3.5f;

    [Header("Detection & Aggression")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float attackRange = 1.8f;
    [Range(0f, 1f)]
    [SerializeField] private float aggression = 0.6f;

    [Header("Combat")]
    [SerializeField] private float damage = 8f;
    [SerializeField] private float attackCooldown = 1.2f;

    [Header("Abilities")]
    [SerializeField] private bool canBurrow;
    [SerializeField] private float burrowDuration = 2f;
    [SerializeField] private float burrowCooldown = 10f;
    [Tooltip("When enabled, this enemy can damage a player who is fully underground.")]
    [SerializeField] private bool canAttackUnderground;
    [SerializeField] private bool isRanged;
    [SerializeField] private float projectileSpeed = 12f;

    [Header("Defense")]
    [SerializeField] private float knockbackResistance = 0.5f;
    [SerializeField] private float damageResistance;

    [Header("Classification")]
    [SerializeField] private EnemyRole role = EnemyRole.Standard;

    [Header("Loot")]
    [SerializeField] private EnemyLootPool lootPool = new();

    public string EnemyName => enemyName;
    public string Description => description;
    public Sprite Sprite => sprite;
    public float DisplayScale => displayScale;
    public Color TintColor => tintColor;
    public Enemy PrefabOverride => prefabOverride;
    public EnemyBehaviorType BehaviorType => behaviorType;
    public float MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
    public float DetectionRange => detectionRange;
    public float AttackRange => attackRange;
    public float Aggression => aggression;
    public float Damage => damage;
    public float AttackCooldown => attackCooldown;
    public bool CanBurrow => canBurrow;
    public float BurrowDuration => burrowDuration;
    public float BurrowCooldown => burrowCooldown;
    public bool CanAttackUnderground => canAttackUnderground;
    public bool IsRanged => isRanged;
    public float ProjectileSpeed => projectileSpeed;
    public float KnockbackResistance => knockbackResistance;
    public float DamageResistance => damageResistance;
    public EnemyRole Role => role;
    public bool IsBoss => role == EnemyRole.Boss;
    public bool IsMiniboss => role == EnemyRole.Miniboss;
    public bool IsSeeker => role == EnemyRole.Seeker;
    public string Category => category;
    public EnemyLootPool LootPool => lootPool;

    private void OnValidate()
    {
        displayScale = Mathf.Max(0.1f, displayScale);
        if (string.IsNullOrEmpty(enemyName))
            enemyName = name;

        if (behaviorType == EnemyBehaviorType.Burrower)
            canAttackUnderground = true;
    }
}