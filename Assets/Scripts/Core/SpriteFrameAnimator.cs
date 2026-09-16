using System;
using UnityEngine;

/// <summary>
/// Lightweight sprite-frame animation helper shared by player and enemy characters.
/// </summary>
public class SpriteFrameAnimator
{
    private static readonly Color DeathFallbackTint = new(0.35f, 0.35f, 0.35f, 0.85f);

    private SpriteRenderer spriteRenderer;
    private Sprite idleSprite;

    private CharacterAnimationClipEntry idleClip;
    private CharacterAnimationClipEntry walkClip;
    private CharacterAnimationClipEntry injuredClip;
    private CharacterAnimationClipEntry deathClip;

    private int idleFrameIndex;
    private float idleFrameTimer;
    private float idleFrameDuration;
    private bool isPlayingIdle;

    private int walkFrameIndex;
    private float walkFrameTimer;
    private float walkFrameDuration;
    private bool isPlayingWalk;

    private CharacterAnimationClipEntry oneShotClip;
    private int oneShotFrameIndex;
    private float oneShotFrameTimer;
    private float oneShotFrameDuration;
    private Action oneShotComplete;

    public bool IsPlayingOneShot => oneShotClip != null && oneShotClip.HasFrames;

    public void Configure(SpriteRenderer renderer, Sprite idle, CharacterAnimationClipEntry[] clips)
    {
        spriteRenderer = renderer;
        idleSprite = idle;
        CacheClips(clips);
    }

    public void CacheClips(CharacterAnimationClipEntry[] clips)
    {
        idleClip = null;
        walkClip = null;
        injuredClip = null;
        deathClip = null;
        idleFrameDuration = 0f;
        walkFrameDuration = 0f;
        ResetPlaybackState();

        if (clips == null || clips.Length == 0)
            return;

        CharacterAnimationUtility.TryFindClip(clips, out idleClip, "Idle");
        CharacterAnimationUtility.TryFindClip(clips, out walkClip, "Walk", "Walking", "Move", "Moving");
        CharacterAnimationUtility.TryFindClip(clips, out injuredClip, "Injured", "Damaged", "Hurt");
        CharacterAnimationUtility.TryFindClip(clips, out deathClip, "Death", "Die");
        idleFrameDuration = CharacterAnimationUtility.GetFrameDuration(idleClip);
        walkFrameDuration = CharacterAnimationUtility.GetFrameDuration(walkClip);
    }

    public void Update(bool allowWalk, bool isMoving)
    {
        if (spriteRenderer == null)
            return;

        if (UpdateOneShotAnimation())
            return;

        if (!allowWalk)
            return;

        if (!isMoving || walkClip == null || !walkClip.HasFrames)
        {
            StopWalkPlayback();

            if (idleClip != null && idleClip.HasFrames)
            {
                UpdateIdlePlayback();
                return;
            }

            if (isPlayingIdle || NeedsIdleSpriteRestore())
                ResetToIdleSprite();

            return;
        }

        StopIdlePlayback();

        if (!isPlayingWalk)
            BeginWalkPlayback();

        if (walkFrameDuration <= 0f)
            return;

        walkFrameTimer += Time.deltaTime;
        while (walkFrameTimer >= walkFrameDuration)
        {
            walkFrameTimer -= walkFrameDuration;
            int nextFrame = walkFrameIndex + 1;

            if (nextFrame >= walkClip.frames.Length)
                nextFrame = walkClip.loop ? 0 : walkClip.frames.Length - 1;

            if (nextFrame == walkFrameIndex)
                break;

            walkFrameIndex = nextFrame;
            ApplyClipFrame(walkClip, walkFrameIndex);
        }
    }

    public void SetFacingLeft(bool facingLeft)
    {
        if (spriteRenderer != null)
            spriteRenderer.flipX = facingLeft;
    }

    public void PlayInjured() => PlayOneShot(injuredClip);

    public void PlayDeath(Action onComplete = null)
    {
        if (deathClip != null && deathClip.HasFrames)
        {
            PlayOneShot(deathClip, onComplete);
            return;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = DeathFallbackTint;

        onComplete?.Invoke();
    }

    public void ResetToIdleSprite()
    {
        StopWalkPlayback();
        StopIdlePlayback();
        oneShotClip = null;
        oneShotFrameIndex = 0;
        oneShotFrameTimer = 0f;
        oneShotFrameDuration = 0f;
        oneShotComplete = null;

        if (spriteRenderer != null && idleSprite != null)
            spriteRenderer.sprite = idleSprite;
    }

    private void BeginWalkPlayback()
    {
        isPlayingWalk = true;
        walkFrameIndex = idleClip != null && idleClip.HasFrames
            ? 0
            : CharacterAnimationUtility.ResolveWalkStartFrameIndex(walkClip);
        walkFrameTimer = 0f;
        ApplyClipFrame(walkClip, walkFrameIndex);
    }

    private void UpdateIdlePlayback()
    {
        if (!isPlayingIdle)
        {
            isPlayingIdle = true;
            idleFrameIndex = 0;
            idleFrameTimer = 0f;
            ApplyClipFrame(idleClip, idleFrameIndex);
        }

        if (idleFrameDuration <= 0f)
            return;

        idleFrameTimer += Time.deltaTime;
        while (idleFrameTimer >= idleFrameDuration)
        {
            idleFrameTimer -= idleFrameDuration;
            int nextFrame = idleFrameIndex + 1;

            if (nextFrame >= idleClip.frames.Length)
                nextFrame = idleClip.loop ? 0 : idleClip.frames.Length - 1;

            if (nextFrame == idleFrameIndex)
                break;

            idleFrameIndex = nextFrame;
            ApplyClipFrame(idleClip, idleFrameIndex);
        }
    }

    private void StopWalkPlayback()
    {
        isPlayingWalk = false;
        walkFrameIndex = 0;
        walkFrameTimer = 0f;
    }

    private void StopIdlePlayback()
    {
        isPlayingIdle = false;
        idleFrameIndex = 0;
        idleFrameTimer = 0f;
    }

    private void ResetPlaybackState()
    {
        StopWalkPlayback();
        StopIdlePlayback();
        oneShotClip = null;
        oneShotFrameIndex = 0;
        oneShotFrameTimer = 0f;
        oneShotFrameDuration = 0f;
        oneShotComplete = null;
    }

    private bool UpdateOneShotAnimation()
    {
        if (oneShotClip == null || !oneShotClip.HasFrames)
            return false;

        if (oneShotFrameDuration <= 0f)
            return false;

        oneShotFrameTimer += Time.deltaTime;
        while (oneShotFrameTimer >= oneShotFrameDuration)
        {
            oneShotFrameTimer -= oneShotFrameDuration;
            int nextFrame = oneShotFrameIndex + 1;

            if (nextFrame >= oneShotClip.frames.Length)
            {
                CompleteOneShotAnimation();
                return false;
            }

            oneShotFrameIndex = nextFrame;
            ApplyClipFrame(oneShotClip, oneShotFrameIndex);
        }

        return true;
    }

    private void PlayOneShot(CharacterAnimationClipEntry clip, Action onComplete = null)
    {
        if (clip == null || !clip.HasFrames)
        {
            onComplete?.Invoke();
            return;
        }

        oneShotClip = clip;
        oneShotFrameIndex = 0;
        oneShotFrameTimer = 0f;
        oneShotFrameDuration = CharacterAnimationUtility.GetFrameDuration(clip);
        oneShotComplete = onComplete;
        StopWalkPlayback();
        StopIdlePlayback();
        ApplyClipFrame(oneShotClip, 0);
    }

    private void CompleteOneShotAnimation()
    {
        Action callback = oneShotComplete;
        oneShotClip = null;
        oneShotFrameIndex = 0;
        oneShotFrameTimer = 0f;
        oneShotFrameDuration = 0f;
        oneShotComplete = null;
        callback?.Invoke();
    }

    private void ApplyClipFrame(CharacterAnimationClipEntry clip, int frameIndex)
    {
        if (clip?.frames == null || frameIndex < 0 || frameIndex >= clip.frames.Length)
            return;

        Sprite frame = clip.frames[frameIndex];
        if (frame != null && spriteRenderer != null)
            spriteRenderer.sprite = frame;
    }

    private bool NeedsIdleSpriteRestore()
    {
        if (spriteRenderer == null || idleSprite == null)
            return false;

        return spriteRenderer.sprite != idleSprite;
    }
}