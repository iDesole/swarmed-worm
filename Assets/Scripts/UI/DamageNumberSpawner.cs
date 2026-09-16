using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DamageNumberSpawner : MonoBehaviour
{
    public static DamageNumberSpawner Instance { get; private set; }

    [SerializeField] private int poolSize = 24;

    private readonly Queue<DamageNumber> pool = new Queue<DamageNumber>();
    private TMP_FontAsset font;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        BuildPool();
    }

    private void OnEnable() => DamageEvents.DamageDealt += HandleDamage;
    private void OnDisable() => DamageEvents.DamageDealt -= HandleDamage;

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void BuildPool()
    {
        for (int i = 0; i < poolSize; i++)
            pool.Enqueue(CreateInstance());
    }

    private DamageNumber CreateInstance()
    {
        var go = new GameObject("DamageNumber");
        go.transform.SetParent(transform, false);

        var text = go.AddComponent<TextMeshPro>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.sortingOrder = 200;
        if (font != null)
            text.font = font;

        var number = go.AddComponent<DamageNumber>();
        go.SetActive(false);
        return number;
    }

    private void HandleDamage(float amount, Vector3 worldPosition, DamageNumberStyle style)
    {
        DamageNumber number = RentFromPool();
        if (number == null)
            return;

        number.Play(amount, worldPosition, style);
    }

    public void Release(DamageNumber number)
    {
        if (number == null)
            return;

        number.gameObject.SetActive(false);
        pool.Enqueue(number);
    }

    public void RecycleAll()
    {
        DamageNumber[] numbers = GetComponentsInChildren<DamageNumber>(true);
        for (int i = 0; i < numbers.Length; i++)
        {
            DamageNumber number = numbers[i];
            if (number != null && number.gameObject.activeSelf)
                Release(number);
        }

        PurgeDestroyedFromPool();
    }

    private DamageNumber RentFromPool()
    {
        PurgeDestroyedFromPool();

        while (pool.Count > 0)
        {
            DamageNumber number = pool.Dequeue();
            if (number != null)
                return number;
        }

        return CreateInstance();
    }

    private void PurgeDestroyedFromPool()
    {
        if (pool.Count == 0)
            return;

        int count = pool.Count;
        for (int i = 0; i < count; i++)
        {
            DamageNumber number = pool.Dequeue();
            if (number != null)
                pool.Enqueue(number);
        }
    }
}