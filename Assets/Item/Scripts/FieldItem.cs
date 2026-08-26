using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class FieldItem : MonoBehaviour
{
    [Header("아이템 데이터")]
    [Tooltip("획득할 ScriptableObject 아이템 데이터 연결")]
    [SerializeField] private Item itemData;

    public Item ItemData => itemData;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (itemData != null)
            {
                bool isAdded = InventoryManager.Instance.AddItem(itemData);

                if (isAdded)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
