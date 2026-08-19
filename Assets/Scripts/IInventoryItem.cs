public interface IInventoryItem
{
    string ItemName { get; }
    void OnEquipped();
    void OnUnequipped();
    void OnUse();
}
