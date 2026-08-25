using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("슬롯 설정")]
    [SerializeField] private ItemSlotUI[] slots;

    [Header("상세 정보창")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private Image detailImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private TextMeshProUGUI effectText;
    [SerializeField] private Button useButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button dropButton;

    private Item currentDetailItem;
    private bool isInitialized = false;

    void Awake()
    {
        InitializeSlots();
    }

    void Start()
    {
        InitializeSlots();
        RefreshFromInventoryManager();
        CloseDetailPanel();
    }

    void OnEnable()
    {
        InitializeSlots();
        RefreshFromInventoryManager();
    }

    private void InitializeSlots()
    {
        if (isInitialized)
        {
            return;
        }

        foreach (var slot in slots)
        {
            slot.Setup(this);
        }

        if (useButton != null)
        {
            useButton.onClick.AddListener(OnUseButtonClicked);
        }

        if (dropButton != null)
        {
            dropButton.onClick.AddListener(OnDropButtonClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseDetailPanel);
        }

        isInitialized = true;
    }

    private void RefreshFromInventoryManager()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RefreshUI();
        }
    }

    public void UpdateSlotUI(List<Item> items)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < items.Count)
            {
                slots[i].SetItem(items[i]);
            }
            else
            {
                slots[i].ClearSlot();
            }
        }
    }

    public void ShowDetailPanel(Item item)
    {
        if (item == null)
        {
            return;
        }

        currentDetailItem = item;
        detailPanel.SetActive(true);

        if (dropButton != null)
        {
            dropButton.gameObject.SetActive(item.CanDrop());
        }

        detailImage.sprite = item.itemImage;
        titleText.text = item.itemName;

        if (effectText != null)
        {
            effectText.text = item.itemEffectDescription;
        }

        if (descText != null)
        {
            descText.text = item.itemDescription;
        }

        string gradeStr = item.GetColoredGrade();
        string typeStr = item.usageType == ItemUsageType.Active
            ? "<color=green>[수동 사용]</color>"
            : "<color=yellow>[자동 적용]</color>";

        typeText.text = $"{gradeStr}  {typeStr}";

        if (item.usageType == ItemUsageType.Active)
        {
            typeText.text = $"{gradeStr} <color=green>[수동 사용]</color>";
            if (useButton != null)
            {
                useButton.gameObject.SetActive(true);
            }
        }
        else
        {
            typeText.text = $"{gradeStr} <color=yellow>[자동 적용]</color>";
            if (useButton != null)
            {
                useButton.gameObject.SetActive(false);
            }
        }
    }

    public void CloseDetailPanel()
    {
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }

        currentDetailItem = null;
    }

    private void OnUseButtonClicked()
    {
        if (currentDetailItem != null && currentDetailItem.usageType == ItemUsageType.Active)
        {
            InventoryManager.Instance.UseItem(currentDetailItem);
            CloseDetailPanel();
        }
    }

    private void OnDropButtonClicked()
    {
        if (currentDetailItem != null)
        {
            InventoryManager.Instance.DropItem(currentDetailItem);
            CloseDetailPanel();
        }
    }
}
