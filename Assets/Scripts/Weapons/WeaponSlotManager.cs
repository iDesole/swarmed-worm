using UnityEngine;

/// <summary>
/// Creates and positions the five hand-slot transforms on the player.
/// </summary>
/// <remarks>
/// Slots mirror their X offset when the player faces left so weapons stay on the correct side of the body.
/// WeaponBase.OnEquipped parents weapons to the slot returned by <see cref="GetHandSlotTransform"/>.
/// </remarks>
public class WeaponSlotManager : MonoBehaviour
{
    public Transform[] handSlotTransforms = new Transform[5];

    private Vector3[] baseLocalPositions = new Vector3[5];
    private bool slotsConfigured;
    private bool facingLeft;

    public void ConfigureHandSlots(PlayerHandSlotDefinition[] slots)
    {
        PlayerHandSlotDefinition[] resolved = slots != null && slots.Length > 0
            ? slots
            : PlayerHandSlotDefinition.CreateDefaults();

        EnsureHandSlotArray();
        EnsureBasePositionArray();

        for (int i = 0; i < 5; i++)
        {
            PlayerHandSlotDefinition slotDefinition = i < resolved.Length && resolved[i] != null
                ? resolved[i]
                : PlayerHandSlotDefinition.Default(i + 1);

            baseLocalPositions[i] = NormalizeSlotPosition(slotDefinition.localPosition);

            Transform slotTransform = handSlotTransforms[i];
            if (slotTransform == null)
            {
                var slotObject = new GameObject($"HandSlot{i + 1}");
                slotObject.transform.SetParent(transform, false);

                int playerLayer = LayerMask.NameToLayer("Player");
                if (playerLayer != -1)
                    slotObject.layer = playerLayer;

                slotTransform = slotObject.transform;
                handSlotTransforms[i] = slotTransform;
            }

            ApplySlotLocalPosition(i, slotTransform);
        }

        slotsConfigured = true;
    }

    public void SetFacingLeft(bool left)
    {
        if (facingLeft == left)
            return;

        facingLeft = left;
        RefreshSlotPositions();
    }

    public Transform GetHandSlotTransform(int slotNumber1to5)
    {
        if (slotNumber1to5 < 1 || slotNumber1to5 > 5)
            slotNumber1to5 = 1;

        EnsureSlotsReady();

        int index = slotNumber1to5 - 1;
        Transform slotTransform = handSlotTransforms[index];
        if (slotTransform != null)
            return slotTransform;

        Transform found = transform.Find($"HandSlot{slotNumber1to5}");
        if (found != null)
        {
            handSlotTransforms[index] = found;
            return found;
        }

        return transform;
    }

    private void EnsureSlotsReady()
    {
        if (slotsConfigured && handSlotTransforms[0] != null)
            return;

        ConfigureHandSlots(PlayerHandSlotDefinition.CreateDefaults());
    }

    private void RefreshSlotPositions()
    {
        for (int i = 0; i < 5; i++)
        {
            if (handSlotTransforms[i] != null)
                ApplySlotLocalPosition(i, handSlotTransforms[i]);
        }
    }

    private void ApplySlotLocalPosition(int index, Transform slotTransform)
    {
        Vector3 basePosition = baseLocalPositions[index];
        float x = facingLeft ? -basePosition.x : basePosition.x;
        slotTransform.localPosition = new Vector3(x, basePosition.y, basePosition.z);
        slotTransform.localScale = Vector3.one;
    }

    private static Vector3 NormalizeSlotPosition(Vector3 position) =>
        new(position.x, position.y, 0f);

    private void EnsureHandSlotArray()
    {
        if (handSlotTransforms == null || handSlotTransforms.Length != 5)
            handSlotTransforms = new Transform[5];
    }

    private void EnsureBasePositionArray()
    {
        if (baseLocalPositions == null || baseLocalPositions.Length != 5)
            baseLocalPositions = new Vector3[5];
    }
}