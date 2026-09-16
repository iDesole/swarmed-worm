using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [SerializeField] private float baseIntensity = 1f;

    private float currentShakeIntensity;
    private float shakeTimer;
    private Vector3 shakeOffset;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void LateUpdate()
    {
        if (shakeTimer <= 0) return;

        shakeTimer -= Time.deltaTime;

        float shakeAmount = Mathf.PerlinNoise(Time.time * 20f, 0f) * 2f - 1f;
        shakeAmount *= currentShakeIntensity;

        float shakeAmountY = Mathf.PerlinNoise(0f, Time.time * 20f) * 2f - 1f;
        shakeAmountY *= currentShakeIntensity;

        shakeOffset = new Vector3(shakeAmount, shakeAmountY, 0);
        transform.localPosition += shakeOffset;

        if (shakeTimer <= 0)
            shakeOffset = Vector3.zero;
    }

    public void Shake(float intensity, float duration)
    {
        if (duration <= 0 || intensity <= 0) return;

        currentShakeIntensity = intensity * baseIntensity;
        shakeTimer = duration;
    }

    public void Shake(float intensity, float duration, float scale) =>
        Shake(intensity * scale, duration);

    public void StopShake()
    {
        shakeTimer = 0;
        shakeOffset = Vector3.zero;
    }
}