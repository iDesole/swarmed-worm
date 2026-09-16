using UnityEngine;

/// <summary>
/// Optional trigger zone that must be entered before an <see cref="InteractableCharacter"/> appears.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class InteractableConditionArea : MonoBehaviour
{
    [SerializeField] private InteractableCharacter owner;
    [SerializeField] private Vector2 areaSize = new(5f, 4f);
    [SerializeField] private bool showGizmoInSceneView = true;

    public Vector2 AreaSize
    {
        get => areaSize;
        set
        {
            areaSize = value;
            ConfigureCollider();
        }
    }

    private void Reset()
    {
        owner = GetComponentInParent<InteractableCharacter>();
        ConfigureCollider();
    }

    private void Awake()
    {
        if (owner == null)
            owner = GetComponentInParent<InteractableCharacter>();

        ConfigureCollider();
    }

    public void Configure(InteractableCharacter character, Vector2 size)
    {
        owner = character;
        areaSize = size;
        ConfigureCollider();
    }

    private void ConfigureCollider()
    {
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(
            Mathf.Max(0.5f, areaSize.x),
            Mathf.Max(0.5f, areaSize.y));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Player player = other.GetComponent<Player>();
        if (player == null || player.IsDead)
            return;

        owner?.SetConditionAreaSatisfied(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Player player = other.GetComponent<Player>();
        if (player == null)
            return;

        owner?.SetConditionAreaSatisfied(false);
    }

    private void OnDrawGizmos()
    {
        if (!showGizmoInSceneView)
            return;

        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        Vector2 size = collider != null ? collider.size : areaSize;
        Vector3 center = transform.position;
        if (collider != null)
            center = transform.TransformPoint(collider.offset);

        Gizmos.color = new Color(0.45f, 0.75f, 1f, 0.28f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(collider != null ? (Vector3)collider.offset : Vector3.zero, size);
        Gizmos.matrix = Matrix4x4.identity;
    }
}