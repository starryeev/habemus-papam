using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    private const string ItemAcquireSfxName = "29 인게임- 아이템 획득";
    private const string ItemUseSfxName = "30 인게임- 아이템 사용";
    private const string ItemDropSfxName = "31 인게임- 아이템 버리기";

    public static InventoryManager Instance { get; private set; }

    [SerializeField] private int maxSlots = 3;
    [SerializeField] private InventoryUI inventoryUI;

    private Cardinal playerCardinal;
    private List<Item> inventoryItems = new List<Item>();
    private List<Item> activeBuffs = new List<Item>();

    public Cardinal Player => playerCardinal;
    public IReadOnlyList<Item> InventoryItems => inventoryItems;
    public IReadOnlyList<Item> ActiveBuffs => activeBuffs;
    public int MaxSlots => maxSlots;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (InGameManager.Instance != null && InGameManager.Instance.Context != null)
        {
            InGameManager.Instance.Context.OnGameContextEvent += OnGameContextChanged;
        }

        RefreshUI();
    }

    void OnDestroy()
    {
        if (InGameManager.Instance != null && InGameManager.Instance.Context != null)
        {
            InGameManager.Instance.Context.OnGameContextEvent -= OnGameContextChanged;
        }
    }

    private void OnGameContextChanged(GameContext.GameContextEvent eventType)
    {
        if (eventType == GameContext.GameContextEvent.ConclaveEnd)
        {
            CheckAndRemoveExpiredItems();
        }
    }

    private void CheckAndRemoveExpiredItems()
    {
        if (InGameManager.Instance == null)
        {
            return;
        }

        bool isEndOfDay = InGameManager.Instance.GetCurrentConclave() == GameContext.Conclave.Evening;

        RemoveExpiredFromList(inventoryItems, isEndOfDay, true);
        RemoveExpiredFromList(activeBuffs, isEndOfDay, false);
    }

    private void RemoveExpiredFromList(List<Item> targetList, bool isNewDay, bool shouldUpdateUI)
    {
        List<Item> itemsToRemove = new List<Item>();

        foreach (var item in targetList)
        {
            if (item.itemExpirationType == ItemExpirationType.Conclave)
            {
                itemsToRemove.Add(item);
            }
            else if (item.itemExpirationType == ItemExpirationType.Day && isNewDay)
            {
                itemsToRemove.Add(item);
            }
        }

        foreach (var item in itemsToRemove)
        {
            item.OnRemove();
            targetList.Remove(item);

            if (playerCardinal != null)
            {
                playerCardinal.RemovePassiveItem(item);
            }
        }

        if (shouldUpdateUI)
        {
            RefreshUI();
        }
    }

    public void SetPlayer(Cardinal player)
    {
        playerCardinal = player;
    }

    public Item GetItemByID(string id)
    {
        foreach (var item in inventoryItems)
        {
            if (item != null && item.itemID == id)
            {
                return item;
            }
        }

        foreach (var item in activeBuffs)
        {
            if (item != null && item.itemID == id)
            {
                return item;
            }
        }

        return null;
    }

    public bool AddItem(Item newItem)
    {
        if (inventoryItems.Count >= maxSlots)
        {
            Debug.Log("[Inventory] No empty slots.");
            return false;
        }

        Item runtimeItem = CreateRuntimeItemInstance(newItem);
        if (runtimeItem == null)
        {
            return false;
        }

        inventoryItems.Add(runtimeItem);
        runtimeItem.OnAcquire();

        if (ActionRecordManager.Instance != null)
        {
            ActionRecordManager.Instance.RecordItemAcquired(runtimeItem);
        }

        if (playerCardinal != null && runtimeItem.usageType == ItemUsageType.Passive)
        {
            playerCardinal.AddPassiveItem(runtimeItem);
        }

        RefreshUI();
        SoundManager.Instance.PlaySFX(ItemAcquireSfxName);
        return true;
    }

    public void UseItem(Item item)
    {
        if (!inventoryItems.Contains(item))
        {
            return;
        }

        item.OnUse();
        SoundManager.Instance.PlaySFX(ItemUseSfxName);

        if (item.IsDurationBuff)
        {
            inventoryItems.Remove(item);
            activeBuffs.Add(item);

            if (playerCardinal != null)
            {
                playerCardinal.AddPassiveItem(item);
            }

            RefreshUI();
            Debug.Log($"[Inventory] '{item.itemName}' moved to active buffs.");
        }
        else if (item.ConsumeOnUse)
        {
            RemoveItem(item);
        }
    }

    public void DropItem(Item item)
    {
        if (!inventoryItems.Contains(item))
        {
            return;
        }

        RemoveItem(item);
        SoundManager.Instance.PlaySFX(ItemDropSfxName);
        Debug.Log($"[Inventory] Removed item: {item.itemName}");
    }

    public void RemoveItem(Item item)
    {
        if (!inventoryItems.Contains(item))
        {
            return;
        }

        item.OnRemove();
        inventoryItems.Remove(item);

        if (playerCardinal != null)
        {
            playerCardinal.RemovePassiveItem(item);
        }

        RefreshUI();
    }

    public InventorySaveData CaptureSaveData()
    {
        InventorySaveData saveData = new InventorySaveData
        {
            maxSlots = maxSlots
        };

        foreach (var item in inventoryItems)
        {
            if (item == null)
            {
                continue;
            }

            saveData.inventoryItems.Add(new ItemSaveData
            {
                itemId = item.itemID,
                runtimeStateJson = item.CaptureRuntimeState()
            });
        }

        foreach (var item in activeBuffs)
        {
            if (item == null)
            {
                continue;
            }

            saveData.activeBuffs.Add(new ItemSaveData
            {
                itemId = item.itemID,
                runtimeStateJson = item.CaptureRuntimeState()
            });
        }

        return saveData;
    }

    public void RestoreFromSave(InventorySaveData saveData, Dictionary<string, Item> itemCatalog)
    {
        inventoryItems.Clear();
        activeBuffs.Clear();

        if (playerCardinal != null)
        {
            playerCardinal.ClearPassiveItems();
        }

        if (saveData == null || itemCatalog == null)
        {
            RefreshUI();
            return;
        }

        RestoreItemList(saveData.inventoryItems, inventoryItems, itemCatalog);
        RestoreItemList(saveData.activeBuffs, activeBuffs, itemCatalog);

        ReapplyItemsAfterLoad();
        RefreshUI();
    }

    private void RestoreItemList(List<ItemSaveData> source, List<Item> target, Dictionary<string, Item> itemCatalog)
    {
        if (source == null)
        {
            return;
        }

        foreach (var itemSave in source)
        {
            if (itemSave == null || string.IsNullOrWhiteSpace(itemSave.itemId))
            {
                continue;
            }

            if (!itemCatalog.TryGetValue(itemSave.itemId, out Item item) || item == null)
            {
                Debug.LogWarning($"[Save] Missing item asset: {itemSave.itemId}");
                continue;
            }

            Item runtimeItem = CreateRuntimeItemInstance(item);
            if (runtimeItem == null)
            {
                continue;
            }

            runtimeItem.RestoreRuntimeState(itemSave.runtimeStateJson);
            target.Add(runtimeItem);
        }
    }

    private Item CreateRuntimeItemInstance(Item sourceItem)
    {
        if (sourceItem == null)
        {
            Debug.LogWarning("[Inventory] Tried to create an item instance from a null source.");
            return null;
        }

        Item runtimeItem = Instantiate(sourceItem);
        runtimeItem.name = sourceItem.name;
        runtimeItem.ResetRuntimeState();
        return runtimeItem;
    }

    private void ReapplyItemsAfterLoad()
    {
        if (playerCardinal == null)
        {
            return;
        }

        playerCardinal.ClearPassiveItems();

        foreach (var item in inventoryItems)
        {
            if (item == null)
            {
                continue;
            }

            item.OnReapply(playerCardinal);

            if (item.usageType == ItemUsageType.Passive)
            {
                playerCardinal.AddPassiveItem(item);
            }
        }

        foreach (var item in activeBuffs)
        {
            if (item == null)
            {
                continue;
            }

            item.OnReapply(playerCardinal);
            playerCardinal.AddPassiveItem(item);
        }
    }

    public void RefreshUI()
    {
        if (inventoryUI != null)
        {
            inventoryUI.UpdateSlotUI(inventoryItems);
        }
    }
}
