using UnityEngine;

/// <summary>
/// Smooth orthographic camera that tracks the player with optional scroll zoom.
/// </summary>
/// <remarks>
/// GameManager assigns target on spawn and calls SnapToTarget for instant alignment after transitions.
/// </remarks>
public class CameraFollow : MonoBehaviour
{
    [Header("Follow")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 0, -10);
    [SerializeField] private float positionSmoothTime = 0.15f;

    [Header("Zoom")]
    [SerializeField] private float defaultOrthographicSize = 8f;
    [SerializeField] private float zoomSmoothTime = 0.4f;
    [SerializeField] private bool enableMouseZoom = true;
    [SerializeField] private float mouseZoomSensitivity = 2f;
    [SerializeField] private float minOrthographicSize = 4f;
    [SerializeField] private float maxOrthographicSize = 20f;

    private Camera cam;
    private Vector3 currentVelocity;
    private float currentZoomVelocity;
    private float targetOrthographicSize;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            enabled = false;
            return;
        }

        targetOrthographicSize = defaultOrthographicSize;
        cam.orthographicSize = defaultOrthographicSize;
    }

    private void LateUpdate()
    {
        if (target == null)
            target = PlayerCatalog.ActiveTransform;

        if (target == null) return;

        Vector3 targetPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(
            transform.position, targetPosition, ref currentVelocity, positionSmoothTime);

        if (!cam.orthographic) return;

        if (enableMouseZoom && !GameUIState.BlocksGameplay)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                targetOrthographicSize -= scroll * mouseZoomSensitivity;
                targetOrthographicSize = Mathf.Clamp(targetOrthographicSize, minOrthographicSize, maxOrthographicSize);
            }
        }

        cam.orthographicSize = Mathf.SmoothDamp(
            cam.orthographicSize, targetOrthographicSize, ref currentZoomVelocity, zoomSmoothTime);
    }

    public void SetZoom(float newSize, bool instant = false)
    {
        targetOrthographicSize = Mathf.Clamp(newSize, minOrthographicSize, maxOrthographicSize);
        if (instant && cam != null)
        {
            cam.orthographicSize = targetOrthographicSize;
            currentZoomVelocity = 0f;
        }
    }

    public void ZoomOut(float amount, bool instant = false) => SetZoom(targetOrthographicSize + amount, instant);
    public void ZoomIn(float amount, bool instant = false) => SetZoom(targetOrthographicSize - amount, instant);
    public void ResetZoom(bool instant = false) => SetZoom(defaultOrthographicSize, instant);

    public void SnapToTarget()
    {
        if (target == null)
            target = PlayerCatalog.ActiveTransform;

        if (target == null)
            return;

        transform.position = target.position + offset;
        currentVelocity = Vector3.zero;
    }
}