/// <summary>
/// Characters that own a <see cref="CharacterBlobShadow"/> implement this to control visibility.
/// </summary>
public interface IBlobShadowHost
{
    bool HideBlobShadow { get; }
}