using UnityEngine;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Button slotButton;

    private Item currentItem;
    private InventoryUI inventoryUI;
    private Image _slotBackgroundImage;
    private Sprite _defaultSlotSprite;
    private Sprite _emptySlotSprite;
    private Sprite _filledSlotSprite;

    public void Setup(InventoryUI ui, Sprite emptySlotSprite, Sprite filledSlotSprite)
    {
        inventoryUI = ui;
        _slotBackgroundImage = slotButton.targetGraphic as Image;
        _defaultSlotSprite = _slotBackgroundImage != null ? _slotBackgroundImage.sprite : null;
        _emptySlotSprite = emptySlotSprite;
        _filledSlotSprite = filledSlotSprite;

        ColorBlock colors = slotButton.colors;
        colors.disabledColor = Color.white;
        slotButton.colors = colors;

        slotButton.onClick.AddListener(OnSlotClicked);
        ClearSlot();
    }

    public void SetItem(Item item)
    {
        currentItem = item;
        if (item != null)
        {
            ApplySlotBackground(_filledSlotSprite);
            iconImage.sprite = item.itemImage;
            iconImage.preserveAspect = true;
            iconImage.gameObject.SetActive(true);
            slotButton.interactable = true;
        }
        else
        {
            ClearSlot();
        }
    }

    public void ClearSlot()
    {
        currentItem = null;
        ApplySlotBackground(_emptySlotSprite);
        iconImage.sprite = null;
        iconImage.gameObject.SetActive(false);
        slotButton.interactable = false;
    }

    private void ApplySlotBackground(Sprite targetSprite)
    {
        if (_slotBackgroundImage != null)
        {
            _slotBackgroundImage.sprite = targetSprite != null ? targetSprite : _defaultSlotSprite;
        }
    }

    private void OnSlotClicked()
    {
        if (currentItem != null)
        {
            inventoryUI.ShowDetailPanel(currentItem);
        }
    }
}
