#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

[RequireComponent(typeof(EnemySpawner))]
public class EnemySpawnRingVisualizer : MonoBehaviour
{
    [SerializeField] private bool showInGame = true;
    [SerializeField] private int segments = 72;
    [SerializeField] private float lineWidth = 0.12f;
    [SerializeField] private Color innerRingColor = new(1f, 0.75f, 0.2f, 0.5f);
    [SerializeField] private Color outerRingColor = new(0.2f, 0.85f, 1f, 0.35f);

    private EnemySpawner spawner;
    private LineRenderer innerRing;
    private LineRenderer outerRing;

    public static void Refresh(EnemySpawner enemySpawner)
    {
        if (enemySpawner == null)
            return;

        EnemySpawnRingVisualizer visualizer = enemySpawner.GetComponent<EnemySpawnRingVisualizer>();
        visualizer?.BuildRings();
    }

    private void Awake()
    {
        spawner = GetComponent<EnemySpawner>();
        if (showInGame)
            BuildRings();
    }

    private void LateUpdate()
    {
        if (!showInGame || spawner == null)
            return;

        RefreshRings();
    }

    public void BuildRings()
    {
        innerRing = CreateRingRenderer("Spawn Band Inner", innerRingColor, 0);
        outerRing = CreateRingRenderer("Spawn Band Outer", outerRingColor, 1);
        RefreshRings();
    }

    private void RefreshRings()
    {
        SpawnRangeBands bands = spawner.waveDatabase != null ? spawner.waveDatabase.SpawnRangeBands : null;
        float innerRadius = bands != null ? bands.mediumMin : 14f;
        float outerRadius = bands != null ? bands.mediumMax : 18f;

        if (innerRing != null)
            DrawCircle(innerRing, innerRadius);
        if (outerRing != null)
            DrawCircle(outerRing, outerRadius);
    }

    private LineRenderer CreateRingRenderer(string name, Color color, int sortingOrder)
    {
        var child = new GameObject(name);
        child.transform.SetParent(transform, false);

        var lr = child.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = segments;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
        lr.sortingOrder = sortingOrder;
        return lr;
    }

    private void DrawCircle(LineRenderer lr, float radius)
    {
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }
    }

    private void OnDestroy()
    {
        if (innerRing != null)
            Destroy(innerRing.gameObject);
        if (outerRing != null)
            Destroy(outerRing.gameObject);
    }
}
#else
using UnityEngine;

public static class EnemySpawnRingVisualizer
{
    public static void Refresh(EnemySpawner enemySpawner) { }
}
#endif