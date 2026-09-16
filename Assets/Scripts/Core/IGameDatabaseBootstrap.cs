using UnityEngine;

/// <summary>
/// Implemented by scene bootstrap components that register a ScriptableObject database at runtime.
/// </summary>
/// <typeparam name="T">Database asset type, e.g. <see cref="WeaponDatabase"/>.</typeparam>
/// <remarks>
/// Lets <see cref="CatalogResolver"/> find databases without hard-coding every bootstrap class name.
/// </remarks>
public interface IGameDatabaseBootstrap<T> where T : ScriptableObject
{
    T Database { get; }
}