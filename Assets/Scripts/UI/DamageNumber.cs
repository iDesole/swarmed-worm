using TMPro;
using UnityEngine;

public class DamageNumber : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.85f;
    [SerializeField] private float floatSpeed = 1.6f;
    [SerializeField] private float drift = 0.35f;

    private TextMeshPro text;
    private float elapsed;
    private Vector3 velocity;
    private Color baseColor;
    private Camera mainCamera;

    private void Awake()
    {
        text = GetComponent<TextMeshPro>();
        mainCamera = Camera.main;
    }

    public void Play(float amount, Vector3 worldPosition, DamageNumberStyle style)
    {
        if (text == null)
            text = GetComponent<TextMeshPro>();

        if (text == null)
            return;

        transform.position = worldPosition + new Vector3(Random.Range(-drift, drift), 0.4f, 0f);
        elapsed = 0f;
        velocity = Vector3.up * floatSpeed;

        baseColor = style == DamageNumberStyle.PlayerHit
            ? new Color(1f, 0.35f, 0.35f)
            : new Color(1f, 0.92f, 0.45f);

        text.text = Mathf.RoundToInt(amount).ToString();
        text.color = baseColor;
        text.fontSize = style == DamageNumberStyle.PlayerHit ? 4.5f : 4f;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (text == null)
            return;

        elapsed += Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        float alpha = 1f - Mathf.Clamp01(elapsed / lifetime);
        text.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

        if (mainCamera != null)
            transform.rotation = mainCamera.transform.rotation;

        if (elapsed >= lifetime)
            DamageNumberSpawner.Instance?.Release(this);
    }
}