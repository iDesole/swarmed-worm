using UnityEngine;

/// <summary>
/// Trilobite melee AI: stalks the player, telegraphs, then bull-rushes in a straight line.
/// </summary>
[DefaultExecutionOrder(50)]
[DisallowMultipleComponent]
public class TrilobiteBehavior : MonoBehaviour
{
    private enum TrilobiteState
    {
        Stalking,
        WindUp,
        Rushing,
        Recovering
    }

    [Header("Melee Rush")]
    [SerializeField] private float rushSpeedMultiplier = 2f;
    [SerializeField] private float rushDuration = 1.1f;
    [SerializeField] private float rushCooldown = 4.5f;
    [SerializeField] private float rushWindUpDuration = 1.05f;
    [SerializeField] private float rushAccelDuration = 0.45f;
    [SerializeField] private float windBackDurationRatio = 0.4f;
    [SerializeField] private float windBackSpeedMultiplier = 0.4f;
    [SerializeField] private float recoveryDuration = 0.6f;
    [SerializeField] private float rushMinRange = 2f;
    [SerializeField] private float stalkSpeedMultiplier = 1f;

    private static readonly Color WindUpTint = new(1f, 0.55f, 0.2f);

    private Enemy enemy;
    private EnemyAI enemyAI;
    private Rigidbody2D enemyBody;
    private Transform player;
    private TrilobiteState state = TrilobiteState.Stalking;
    private Vector2 rushDirection = Vector2.right;
    private float stateTimer;
    private float rushElapsed;
    private float rushCooldownTimer;
    private float lastMeleeDamageTime;
    private Vector3 baseScale = Vector3.one;
    private Color baseTint = Color.white;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        enemyAI = GetComponent<EnemyAI>();
        enemyBody = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (enemy == null)
            return;

        enemy.EnableRotationalFacing(90f);
        baseScale = enemy.transform.localScale;
        baseTint = spriteRenderer != null ? spriteRenderer.color : Color.white;

        if (enemyAI != null)
            enemyAI.enabled = false;
    }

    private void FixedUpdate()
    {
        if (enemy == null || enemy.IsDefeated || enemyBody == null)
            return;

        ResolvePlayerTarget();
        if (player == null)
            return;

        if (enemy.IsJolted || enemy.IsFrozen || enemy.IsVoidAnchored)
        {
            enemyBody.linearVelocity = Vector2.zero;
            if (state == TrilobiteState.WindUp || state == TrilobiteState.Rushing)
            {
                ResetTelegraphVisuals();
                state = TrilobiteState.Stalking;
            }

            return;
        }

        if (rushCooldownTimer > 0f)
            rushCooldownTimer -= Time.fixedDeltaTime;

        switch (state)
        {
            case TrilobiteState.Stalking:
                UpdateStalking();
                break;
            case TrilobiteState.WindUp:
                UpdateWindUp();
                break;
            case TrilobiteState.Rushing:
                UpdateRush();
                break;
            case TrilobiteState.Recovering:
                UpdateRecovery();
                break;
        }
    }

    private void LateUpdate()
    {
        if (enemy == null || enemy.IsDefeated)
            return;

        UpdateFacing();
    }

    private void ResolvePlayerTarget()
    {
        if (player != null)
            return;

        player = PlayerCatalog.ActiveTransform;
        if (player != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
    }

    private void UpdateFacing()
    {
        if (state == TrilobiteState.WindUp || state == TrilobiteState.Rushing)
        {
            enemy.RotateInDirection(rushDirection);
            return;
        }

        if (player != null)
            enemy.FaceToward(player.position);
    }

    private void UpdateStalking()
    {
        float distance = DistanceToPlayer();
        float detectionRange = enemy.GetDetectionRange();

        if (CanStartRush(distance, detectionRange))
        {
            BeginWindUp();
            return;
        }

        if (distance <= detectionRange)
            MoveTowardPlayer(stalkSpeedMultiplier);
        else
            enemyBody.linearVelocity = Vector2.zero;
    }

    private void UpdateWindUp()
    {
        stateTimer -= Time.fixedDeltaTime;
        float progress = 1f - Mathf.Clamp01(stateTimer / rushWindUpDuration);

        if (progress < windBackDurationRatio)
        {
            float backProgress = progress / Mathf.Max(0.01f, windBackDurationRatio);
            float backSpeed = enemy.GetScaledMoveSpeed() * windBackSpeedMultiplier * (1f - backProgress);
            enemyBody.linearVelocity = -rushDirection * backSpeed;
        }
        else
        {
            enemyBody.linearVelocity = Vector2.zero;
        }

        ApplyWindUpTelegraph(progress);

        if (stateTimer > 0f)
            return;

        BeginRush();
    }

    private void UpdateRush()
    {
        stateTimer -= Time.fixedDeltaTime;
        rushElapsed += Time.fixedDeltaTime;

        float maxSpeed = enemy.GetScaledMoveSpeed() * rushSpeedMultiplier;
        float accelT = rushAccelDuration <= 0f
            ? 1f
            : Mathf.Clamp01(rushElapsed / rushAccelDuration);
        float speed = maxSpeed * Mathf.SmoothStep(0f, 1f, accelT);
        enemyBody.linearVelocity = rushDirection * speed;

        TryMeleeDamage();

        if (stateTimer <= 0f)
            BeginRecovery();
    }

    private void UpdateRecovery()
    {
        enemyBody.linearVelocity = Vector2.Lerp(enemyBody.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 8f);
        stateTimer -= Time.fixedDeltaTime;

        if (stateTimer <= 0f)
            state = TrilobiteState.Stalking;
    }

    private void BeginWindUp()
    {
        state = TrilobiteState.WindUp;
        stateTimer = rushWindUpDuration;
        rushDirection = DirectionToPlayer();
        enemyBody.linearVelocity = Vector2.zero;
    }

    private void BeginRush()
    {
        state = TrilobiteState.Rushing;
        stateTimer = rushDuration;
        rushElapsed = 0f;
        rushCooldownTimer = rushCooldown;
        rushDirection = rushDirection.sqrMagnitude > 0.0001f ? rushDirection.normalized : DirectionToPlayer();
        ResetTelegraphVisuals();
    }

    private void BeginRecovery()
    {
        state = TrilobiteState.Recovering;
        stateTimer = recoveryDuration;
        enemyBody.linearVelocity *= 0.25f;
        ResetTelegraphVisuals();
    }

    private void ApplyWindUpTelegraph(float progress)
    {
        float intensity = Mathf.SmoothStep(0f, 1f, progress);
        float pulse = 1f + Mathf.Sin(progress * Mathf.PI * 4f) * 0.03f * intensity;
        enemy.transform.localScale = baseScale * (1f + intensity * 0.07f) * pulse;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.Lerp(baseTint, WindUpTint, intensity * 0.7f);
    }

    private void ResetTelegraphVisuals()
    {
        enemy.transform.localScale = baseScale;

        if (spriteRenderer != null)
            spriteRenderer.color = baseTint;
    }

    private bool CanStartRush(float distance, float detectionRange)
    {
        if (rushCooldownTimer > 0f)
            return false;

        return distance <= detectionRange && distance >= rushMinRange;
    }

    private void MoveTowardPlayer(float speedMultiplier)
    {
        Vector2 direction = DirectionToPlayer();
        if (direction.sqrMagnitude <= 0.0001f)
        {
            enemyBody.linearVelocity = Vector2.zero;
            return;
        }

        enemyBody.linearVelocity = direction * (enemy.GetScaledMoveSpeed() * speedMultiplier);
    }

    private void TryMeleeDamage()
    {
        if (Time.time - lastMeleeDamageTime < enemy.GetAttackCooldown())
            return;

        float attackRange = enemy.GetAttackRange();
        Collider2D[] hits = Physics2D.OverlapCircleAll(enemy.transform.position, attackRange);
        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Player"))
                continue;

            Player target = hit.GetComponent<Player>();
            if (target == null)
                continue;

            target.TakeDamage(enemy.GetScaledDamage(), attackerCanHitUnderground: false);
            ApplyMeleeKnockback(hit);
            lastMeleeDamageTime = Time.time;
            return;
        }
    }

    private void ApplyMeleeKnockback(Collider2D hit)
    {
        Rigidbody2D playerRb = hit.GetComponent<Rigidbody2D>();
        if (playerRb == null)
            return;

        Vector2 knockbackDir = ((Vector2)hit.transform.position - (Vector2)enemy.transform.position).normalized;
        if (knockbackDir.sqrMagnitude <= 0.0001f)
            knockbackDir = rushDirection;

        playerRb.AddForce(knockbackDir * 6f, ForceMode2D.Impulse);
    }

    private float DistanceToPlayer() =>
        Vector2.Distance(enemy.transform.position, player.position);

    private Vector2 DirectionToPlayer()
    {
        Vector2 offset = (Vector2)player.position - (Vector2)enemy.transform.position;
        return offset.sqrMagnitude > 0.0001f ? offset.normalized : Vector2.right;
    }
}