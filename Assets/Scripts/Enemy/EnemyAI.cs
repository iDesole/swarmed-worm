using UnityEngine;

/// <summary>
/// Simple state machine that moves enemies toward the player and triggers attacks.
/// </summary>
/// <remarks>
/// States: Idle → Chase → Attack (and Retreat/Burrow for supported enemy types).
/// Reads ranges and speeds from the parent <see cref="Enemy"/> stats component.
/// </remarks>
[RequireComponent(typeof(Enemy))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    public enum AIState
    {
        Idle,
        Chase,
        Attack,
        Retreat,
        Burrow
    }

    [Header("Behavior")]
    [SerializeField] private float wanderRadius = 4f;
    [SerializeField] private float wanderInterval = 2f;

    [Header("State")]
    public AIState CurrentState { get; private set; } = AIState.Idle;

    private Enemy enemy;
    private Rigidbody2D rb;
    private Transform player;
    private Vector2 wanderTarget;
    private float lastWanderTime;
    private float lastAttackTime;
    private float burrowEnterTime;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start() => ResolvePlayerTarget();

    private void ResolvePlayerTarget()
    {
        if (player != null)
            return;

        player = PlayerCatalog.ActiveTransform;
        if (player != null)
            return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    private void Update()
    {
        if (player == null)
            ResolvePlayerTarget();

        if (player == null || enemy == null || enemy.IsDefeated || enemy.IsBurrowed || enemy.IsJolted || enemy.IsFrozen
            || enemy.IsVoidAnchored || enemy.IsVoidPulled)
        {
            if (enemy != null && (enemy.IsJolted || enemy.IsFrozen || enemy.IsVoidAnchored) && rb != null)
                rb.linearVelocity = Vector2.zero;
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        float detectionRange = enemy.GetDetectionRange();
        float attackRange = enemy.GetAttackRange();
        float aggression = enemy.GetAggression();
        if (enemy.BaseStats != null && enemy.BaseStats.IsSeeker)
            aggression = Mathf.Min(1f, aggression + 0.35f);

        switch (CurrentState)
        {
            case AIState.Idle:
                HandleIdle(distanceToPlayer, detectionRange, aggression);
                break;
            case AIState.Chase:
                HandleChase(distanceToPlayer, attackRange, aggression);
                break;
            case AIState.Attack:
                HandleAttack(distanceToPlayer, attackRange, aggression);
                break;
            case AIState.Retreat:
                HandleRetreat(distanceToPlayer);
                break;
            case AIState.Burrow:
                HandleBurrow();
                break;
        }
    }

    private void FixedUpdate()
    {
        if (player == null || enemy == null || enemy.IsDefeated || enemy.IsBurrowed || enemy.IsJolted || enemy.IsFrozen
            || enemy.IsVoidAnchored || enemy.IsVoidPulled)
        {
            if (enemy != null && (enemy.IsJolted || enemy.IsFrozen || enemy.IsVoidAnchored) && rb != null)
                rb.linearVelocity = Vector2.zero;
            return;
        }

        switch (CurrentState)
        {
            case AIState.Chase:
                MoveTowards(player.position, 1f);
                break;
            case AIState.Retreat:
                Vector2 away = (transform.position - player.position).normalized;
                MoveTowards(transform.position + (Vector3)away * 5f, 1.2f);
                break;
            case AIState.Idle:
                Wander();
                break;
        }
    }

    private void HandleIdle(float distToPlayer, float detectionRange, float aggression)
    {
        if (distToPlayer < detectionRange * (0.7f + aggression * 0.3f))
            CurrentState = AIState.Chase;
    }

    private void HandleChase(float distToPlayer, float attackRange, float aggression)
    {
        if (distToPlayer <= attackRange && CanAttackPlayer())
            CurrentState = AIState.Attack;
        else if (distToPlayer > enemy.GetDetectionRange() * 1.3f)
            CurrentState = AIState.Idle;

        if (Random.value < aggression * 0.05f && distToPlayer > attackRange * 1.5f && enemy.CanBurrowNow())
            CurrentState = AIState.Burrow;
    }

    private void HandleAttack(float distToPlayer, float attackRange, float aggression)
    {
        if (!CanAttackPlayer())
        {
            CurrentState = AIState.Chase;
            return;
        }

        float attackCooldown = enemy.GetAttackCooldown();
        float effectiveCooldown = attackCooldown * (1.5f - aggression);

        if (Time.time >= lastAttackTime + effectiveCooldown)
        {
            enemy.PerformAttack(player);
            lastAttackTime = Time.time;
        }

        if (distToPlayer > attackRange * 1.3f)
            CurrentState = AIState.Chase;
    }

    private bool CanAttackPlayer()
    {
        if (player == null)
            return false;

        Player target = player.GetComponent<Player>();
        if (target == null || !target.IsBurrowed)
            return true;

        if (!PlayerBurrowSignal.IsNearby(enemy.transform.position))
            return false;

        return enemy.CanAttackUnderground;
    }

    private void HandleRetreat(float distToPlayer)
    {
        if (distToPlayer > enemy.GetDetectionRange() * 0.8f)
            CurrentState = AIState.Chase;
    }

    private void HandleBurrow()
    {
        if (!enemy.IsBurrowed)
        {
            enemy.ToggleBurrow();
            burrowEnterTime = Time.time;
        }

        if (player != null)
        {
            Vector2 newPos = (Vector2)player.position + Random.insideUnitCircle * 3f;
            float scaledSpeed = enemy.GetScaledMoveSpeed();
            Vector2 desiredVelocity = (newPos - (Vector2)transform.position).normalized * scaledSpeed * 2f;
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, desiredVelocity, Time.deltaTime * 10f);
        }

        float duration = enemy.BaseStats != null ? enemy.BaseStats.BurrowDuration : 2f;
        if (Time.time > burrowEnterTime + duration)
        {
            if (enemy.IsBurrowed)
                enemy.ToggleBurrow();
            CurrentState = AIState.Chase;
        }
    }

    private void MoveTowards(Vector3 target, float speedMultiplier = 1f)
    {
        if (enemy == null) return;

        float speed = enemy.GetScaledMoveSpeed() * speedMultiplier;
        Vector2 direction = (target - transform.position).normalized;
        rb.linearVelocity = direction * speed;
    }

    private void Wander()
    {
        if (Time.time > lastWanderTime + wanderInterval)
        {
            wanderTarget = (Vector2)transform.position + Random.insideUnitCircle * wanderRadius;
            lastWanderTime = Time.time;
        }

        Vector2 direction = (wanderTarget - (Vector2)transform.position).normalized;
        float speed = enemy.GetScaledMoveSpeed() * 0.6f;
        rb.linearVelocity = direction * speed;
    }

    public void SetState(AIState newState) => CurrentState = newState;
}