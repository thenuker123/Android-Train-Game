using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores portable player items and tracks which one is equipped.
/// </summary>
public class InventorySystem : MonoBehaviour
{
    [SerializeField] private List<MonoBehaviour> pocketItemComponents = new List<MonoBehaviour>();
    [SerializeField] private int startEquippedIndex;

    public event Action<int, IInventoryItem> EquippedItemChanged;

    private readonly List<IInventoryItem> _items = new List<IInventoryItem>();
    private int _equippedIndex = -1;

    public int ItemCount => _items.Count;
    public int EquippedIndex => _equippedIndex;
    public IInventoryItem EquippedItem => _equippedIndex >= 0 && _equippedIndex < _items.Count ? _items[_equippedIndex] : null;

    private void Awake()
    {
        RebuildItems();
        EquipIndex(_items.Count == 0 ? -1 : Mathf.Clamp(startEquippedIndex, 0, _items.Count - 1));
    }

    public void RebuildItems()
    {
        _items.Clear();

        for (int i = 0; i < pocketItemComponents.Count; i++)
        {
            if (pocketItemComponents[i] is IInventoryItem item)
            {
                _items.Add(item);
            }
        }
    }

    public void CycleNextItem()
    {
        if (_items.Count == 0)
        {
            return;
        }

        int nextIndex = (_equippedIndex + 1) % _items.Count;
        EquipIndex(nextIndex);
    }

    public void CyclePreviousItem()
    {
        if (_items.Count == 0)
        {
            return;
        }

        int previousIndex = (_equippedIndex - 1 + _items.Count) % _items.Count;
        EquipIndex(previousIndex);
    }

    public void UseEquippedItem()
    {
        EquippedItem?.OnUse();
    }

    public T GetEquippedItem<T>() where T : class, IInventoryItem
    {
        return EquippedItem as T;
    }

    private void EquipIndex(int newIndex)
    {
        if (_equippedIndex == newIndex)
        {
            return;
        }

        if (_equippedIndex >= 0 && _equippedIndex < _items.Count)
        {
            _items[_equippedIndex].OnUnequipped();
        }

        _equippedIndex = newIndex;

        if (_equippedIndex >= 0 && _equippedIndex < _items.Count)
        {
            _items[_equippedIndex].OnEquipped();
        }

        EquippedItemChanged?.Invoke(_equippedIndex, EquippedItem);
    }
}
