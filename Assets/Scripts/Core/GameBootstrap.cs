using UnityEngine;

/// <summary>
/// Application-level settings applied before any scene loads.
/// </summary>
/// <remarks>
/// Runs via <see cref="RuntimeInitializeOnLoadMethodAttribute"/> so frame rate and background behavior
/// are consistent even if this component is not placed in a scene.
/// </remarks>
[DefaultExecutionOrder(-200)]
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private int targetFrameRate = 60;
    [SerializeField] private bool runInBackground = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ConfigureApplication()
    {
        Application.targetFrameRate = 60;
        Application.runInBackground = true;
        QualitySettings.vSyncCount = 1;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    private void Awake()
    {
        Application.targetFrameRate = Mathf.Max(30, targetFrameRate);
        Application.runInBackground = runInBackground;
    }
}