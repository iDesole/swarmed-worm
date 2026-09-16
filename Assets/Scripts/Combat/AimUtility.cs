using UnityEngine;

/// <summary>
/// Shared mouse-aim math used by the player, weapons, and melee hitboxes.
/// </summary>
/// <remarks>
/// Uses screen-space offset (not raw world mouse position) so aim stays correct when the player sprite flips.
/// Weapon sprites rotate via pitch + horizontal flip; see <see cref="ApplyWeaponSpriteAim"/>.
/// </remarks>
public static class AimUtility
{
    /// <summary>Normalized direction from origin toward the mouse cursor.</summary>
    public static Vector2 GetDirection(Transform origin, Vector2 fallbackDirection)
    {
        Camera camera = Camera.main;
        if (camera == null || origin == null)
            return NormalizeOrDefault(fallbackDirection, Vector2.right);

        Vector2 originWorld = GetOriginWorldPosition(origin);
        Vector3 originScreen = camera.WorldToScreenPoint(originWorld);
        Vector2 screenOffset = (Vector2)Input.mousePosition - (Vector2)originScreen;

        if (screenOffset.sqrMagnitude <= 1f)
            return NormalizeOrDefault(fallbackDirection, Vector2.right);

        return screenOffset.normalized;
    }

    public static Vector2 GetOriginWorldPosition(Transform origin)
    {
        if (origin == null)
            return Vector2.zero;

        Rigidbody2D body = origin.GetComponent<Rigidbody2D>();
        return body != null ? body.position : (Vector2)origin.position;
    }

    /// <summary>True when the mouse is to the left of the origin (used for sprite flipX).</summary>
    public static bool IsAimingLeft(Vector2 aimDirection, bool currentFacingLeft)
    {
        if (Mathf.Abs(aimDirection.x) < 0.01f)
            return currentFacingLeft;

        return aimDirection.x < 0f;
    }

    /// <summary>Rotates and flips a weapon sprite to track aim without turning it upside-down.</summary>
    public static void ApplyWeaponSpriteAim(
        Transform weaponTransform,
        SpriteRenderer spriteRenderer,
        Vector2 aimDirection,
        bool facesLeft,
        float pitchOffset)
    {
        if (weaponTransform == null)
            return;

        weaponTransform.localPosition = Vector3.zero;

        if (aimDirection.sqrMagnitude < 0.0001f)
            aimDirection = Vector2.right;

        float pitch = Mathf.Atan2(aimDirection.y, Mathf.Abs(aimDirection.x)) * Mathf.Rad2Deg + pitchOffset;
        if (facesLeft)
            pitch = -pitch;

        weaponTransform.localRotation = Quaternion.Euler(0f, 0f, pitch);

        if (spriteRenderer == null)
            return;

        spriteRenderer.flipX = facesLeft;
        spriteRenderer.flipY = false;
    }

    public static void ResetWeaponSpriteAim(Transform weaponTransform, SpriteRenderer spriteRenderer)
    {
        if (weaponTransform != null)
            weaponTransform.localRotation = Quaternion.identity;

        if (spriteRenderer == null)
            return;

        spriteRenderer.flipX = false;
        spriteRenderer.flipY = false;
    }

    private static Vector2 NormalizeOrDefault(Vector2 direction, Vector2 defaultDirection) =>
        direction.sqrMagnitude > 0.0001f ? direction.normalized : defaultDirection;
}