using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SushiSlot : MonoBehaviour
{
    [Header("UI 요소")]
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemGradeText;
    public TextMeshProUGUI itemDescText;
    public TextMeshProUGUI itemEffectText;
    public Button useButton;
    public CanvasGroup canvasGroup;
    public Image sushiGradeImage;

    private Item currentItem;
    private bool isSelectable = true;

    public string CurrentItemId => currentItem != null ? currentItem.itemID : string.Empty;
    public bool IsSelectable => isSelectable;

    public void Setup(Item item, Sprite gradeSprite)
    {
        currentItem = item;
        itemIcon.sprite = item.itemImage;
        itemNameText.text = item.itemName;
        itemGradeText.text = $"[{item.itemGrade}]";
        itemDescText.text = item.itemDescription;
        itemEffectText.text = item.itemEffectDescription;

        if (sushiGradeImage != null)
        {
            sushiGradeImage.sprite = gradeSprite;
        }

        SetSelectable(true); 

        useButton.onClick.RemoveAllListeners();
        useButton.onClick.AddListener(OnPickItem);
    }

    public void SetSelectable(bool selectable)
    {
        isSelectable = selectable;
        useButton.interactable = selectable;

        if (selectable)
        {
            itemIcon.color = Color.white;
            canvasGroup.alpha = 1.0f;
        }
        else
        {
            itemIcon.color = Color.gray;
            canvasGroup.alpha = 0.5f;
        }
    }

    private void OnPickItem()
    {
        if (currentItem == null || !isSelectable) return;

        if (!InventoryManager.Instance.AddItem(currentItem)) return;

        SushiUI.Instance.Close();
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveCheckpoint(
                SaveCheckpointType.SushiRewardAcquired,
                SaveResumeStep.StartNextConclave);
        }
        InGameManager.Instance.StartConclaveCycle();
    }
}
