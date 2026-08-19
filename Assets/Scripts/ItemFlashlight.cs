using UnityEngine;

/// <summary>
/// Toggleable flashlight inventory item.
/// </summary>
public class ItemFlashlight : MonoBehaviour, IInventoryItem
{
    [SerializeField] private string itemName = "Flashlight";
    [SerializeField] private Light flashlightLight;

    public string ItemName => itemName;
    public bool IsEnabled => flashlightLight != null && flashlightLight.enabled;

    private void Awake()
    {
        if (flashlightLight == null)
        {
            flashlightLight = GetComponentInChildren<Light>(true);
        }
    }

    public void OnEquipped()
    {
    }

    public void OnUnequipped()
    {
    }

    public void OnUse()
    {
        if (flashlightLight == null)
        {
            return;
        }

        flashlightLight.enabled = !flashlightLight.enabled;
    }
}
