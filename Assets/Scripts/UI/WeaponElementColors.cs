using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Shared element colors for inventory slots, tooltips, and combat UI.
/// </summary>
public static class WeaponElementColors
{
    public static readonly Color Fire = new(1f, 0.55f, 0.2f);
    public static readonly Color Ice = new(0.55f, 0.85f, 1f);
    public static readonly Color Poison = new(0.45f, 0.9f, 0.3f);
    public static readonly Color Electric = new(1f, 0.95f, 0.35f);
    public static readonly Color Void = new(0.55f, 0.25f, 0.85f);
    public static readonly Color DefaultBorder = new(0.22f, 0.24f, 0.32f, 1f);

    public static Color GetColor(WeaponElement element) =>
        element switch
        {
            WeaponElement.Fire => Fire,
            WeaponElement.Ice => Ice,
            WeaponElement.Poison => Poison,
            WeaponElement.Electric => Electric,
            WeaponElement.Void => Void,
            _ => DefaultBorder
        };

    public static bool TryGetActiveElements(WeaponElementProfile profile, out List<WeaponElement> elements)
    {
        elements = new List<WeaponElement>();
        if (profile == null || profile.elements == WeaponElement.None)
            return false;

        foreach (WeaponElement element in System.Enum.GetValues(typeof(WeaponElement)))
        {
            if (element == WeaponElement.None)
                continue;

            if (profile.HasElement(element))
                elements.Add(element);
        }

        return elements.Count > 0;
    }

    public static Color GetSlotBorderColor(WeaponElementProfile profile)
    {
        if (!TryGetActiveElements(profile, out List<WeaponElement> elements))
            return DefaultBorder;

        if (elements.Count == 1)
            return GetColor(elements[0]);

        Color blended = Color.black;
        foreach (WeaponElement element in elements)
            blended = blended + GetColor(element);

        blended = blended / elements.Count;
        blended.a = 1f;
        return blended;
    }

    public static Color GetSlotBackgroundTint(WeaponElementProfile profile)
    {
        Color border = GetSlotBorderColor(profile);
        if (border == DefaultBorder)
            return new Color(0.18f, 0.2f, 0.28f, 0.95f);

        return Color.Lerp(new Color(0.18f, 0.2f, 0.28f, 0.95f), border, 0.28f);
    }

    public static string FormatRichTextElements(WeaponElementProfile profile)
    {
        if (!TryGetActiveElements(profile, out List<WeaponElement> elements))
            return string.Empty;

        var builder = new StringBuilder();
        for (int i = 0; i < elements.Count; i++)
        {
            if (i > 0)
                builder.Append(", ");

            Color color = GetColor(elements[i]);
            builder.Append("<color=#");
            builder.Append(ColorUtility.ToHtmlStringRGB(color));
            builder.Append('>');
            builder.Append(elements[i]);
            builder.Append("</color>");
        }

        return builder.ToString();
    }
}