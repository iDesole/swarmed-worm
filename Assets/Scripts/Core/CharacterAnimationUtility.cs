using System;
using UnityEngine;

/// <summary>
/// Shared helpers for data-driven sprite animation clips.
/// </summary>
public static class CharacterAnimationUtility
{
    public static int ResolveWalkStartFrameIndex(CharacterAnimationClipEntry clip)
    {
        if (clip?.frames == null || clip.frames.Length <= 1)
            return 0;

        return 1;
    }

    public static float GetFrameDuration(CharacterAnimationClipEntry clip)
    {
        if (clip == null)
            return 0f;

        return 1f / Mathf.Max(1f, clip.framesPerSecond);
    }

    public static bool TryFindClip(
        CharacterAnimationClipEntry[] clips,
        out CharacterAnimationClipEntry clip,
        params string[] titles)
    {
        clip = null;
        if (clips == null || titles == null)
            return false;

        for (int t = 0; t < titles.Length; t++)
        {
            string title = titles[t];
            if (string.IsNullOrWhiteSpace(title))
                continue;

            for (int i = 0; i < clips.Length; i++)
            {
                CharacterAnimationClipEntry candidate = clips[i];
                if (candidate == null || !candidate.HasFrames)
                    continue;

                if (string.Equals(candidate.title, title, StringComparison.OrdinalIgnoreCase))
                {
                    clip = candidate;
                    return true;
                }
            }
        }

        return false;
    }
}