using UnityEngine;

/// <summary>
/// Mobile button bridge for inventory cycling and using hand tools.
/// </summary>
public class AndroidInventoryUI : MonoBehaviour
{
    [SerializeField] private InventorySystem inventorySystem;

    private void Awake()
    {
        if (inventorySystem == null)
        {
            inventorySystem = FindObjectOfType<InventorySystem>();
        }
    }

    public void OnNextItemPressed()
    {
        inventorySystem?.CycleNextItem();
    }

    public void OnPreviousItemPressed()
    {
        inventorySystem?.CyclePreviousItem();
    }

    public void OnUseItemPressed()
    {
        inventorySystem?.UseEquippedItem();
    }

    public void OnRemoteThrottleUpPressed()
    {
        inventorySystem?.GetEquippedItem<ItemTrainRemote>()?.IncreaseThrottle();
    }

    public void OnRemoteThrottleDownPressed()
    {
        inventorySystem?.GetEquippedItem<ItemTrainRemote>()?.DecreaseThrottle();
    }

    public void OnRemoteBrakeUpPressed()
    {
        inventorySystem?.GetEquippedItem<ItemTrainRemote>()?.IncreaseBrake();
    }

    public void OnRemoteBrakeDownPressed()
    {
        inventorySystem?.GetEquippedItem<ItemTrainRemote>()?.DecreaseBrake();
    }
}
