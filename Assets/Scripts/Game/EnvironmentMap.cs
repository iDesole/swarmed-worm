using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Marker component on environment prefabs that exposes the ground Tilemap.
/// </summary>
/// <remarks>
/// Added automatically at spawn time if missing. GameManager uses GroundTilemap for enemy spawn validation.
/// </remarks>
public class EnvironmentMap : MonoBehaviour
{
    [SerializeField] private Tilemap groundTilemap;

    public Tilemap GroundTilemap
    {
        get
        {
            if (groundTilemap == null)
                groundTilemap = GetComponentInChildren<Tilemap>();

            return groundTilemap;
        }
    }
}