using UnityEngine;

public enum InventorySlotCategory
{
    Weapon,
    Inventory,
    Artifact
}

/// <summary>
/// Marks an inventory slot for hover lookup via raycast in <see cref="PlayerInventoryUI"/>.
/// </summary>
public class InventorySlotHover : MonoBehaviour
{
    public InventorySlotCategory Category { get; private set; }
    public int SlotIndex { get; private set; }

    public void Initialize(InventorySlotCategory slotCategory, int index)
    {
        Category = slotCategory;
        SlotIndex = index;
    }
}