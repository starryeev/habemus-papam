using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "E31212", menuName = "Events/디아나의 보물")]
public class E31212 : Event
{
    void Reset()
    {
        eventID = "E31212";
        eventName = "디아나의 보물";
        eventDescription = "교단 직속 탐험가인 디아나 존스가 고대 유적지 도굴... 아니 탐험을 마치고 돌아왔다!\n수십 마리의 코끼리와 기린들, 금은보화로 가득찬 수레들...\n그 중에서도 가장 빛나는 것은 고대 태양교 경전이 적힌 커다란 황금 판이다.\n황금 경판을 읽어 볼까?";
        maxAppear = 3;

        eventWeightBase = 10f;
        eventWeightMultiplier = 0f;

        option1 = "황금 경판을 낭독한다!";
        option1Chance = 0.02f;
        option1Requirement = "경건함 30 이상";
        option1SuccessDescription = "당신은 숨을 크게 들이쉬고, 태양의 기운이 담긴 판을 낭독한다.\n경전의 오묘한 신비에 취해 당신은 무아지경에 빠진다. 당신의 몸에 태양의 힘이 깃든다.\n당신의 목소리는 낮고 평안하면서도, 대신전 전체에 우레처럼 울러퍼진다.\n이윽고 하늘에서 밝은 빛줄기가 내려온다. 당신의 영혼이 대지를 떠나 태양을 향해 떠오른다.\n\n승리!";
        option1SuccessResult = "게임 오버\n\"승천\" 엔딩";
        option1FailDescription = "당신은 태양의 기운이 담긴 황금 경판을 집어든다.\n당연한 소리지만, 고대의 사제들만이 사용하던 비밀스러운 상형 문자로 적혀 있다.\n읽을 수는 없을 것 같다.\n\n아이템 '황금 경판' 획득!";
        option1FailResult = "황금 경판 획득";

        option2 = "주변의 보물 중 챙길 게 있는지 확인해 본다!";
        option2Chance = 1f;
        option2SuccessDescription = "오... 좋은 아이템을 찾았다!\n\n(아이템 이름) 획득!";
        option2SuccessResult = "랜덤 일반 아이템 획득";
    }

    public override bool CanChoiceOption1(Cardinal performer)
    {
        if(performer.Piety < 30f) return false;
        return true;
    }

    public override bool CanChoiceOption2(Cardinal performer)
    {
        return true;
    }

    public override bool OnChoiceOption1(Cardinal performer)
    {
        if(!CanChoiceOption1(performer)) return false;

        if(Random.value <= option1Chance)
        {  // 성공했을 때 로직
            // 승천 엔딩 처리 필요

            return true;
        }
        else
        {  // 실패했을 때 로직
            GiveItemById("I013");

            return false;
        }
    }


    public override bool OnChoiceOption2(Cardinal performer)
    {
        if(!CanChoiceOption2(performer)) return false;

        if(Random.value <= option2Chance)
        {  // 성공했을 때 로직
            Item item = GiveRandomCommonItem();

            if(item != null)
            {
                option2SuccessDescription = $"오... 좋은 아이템을 찾았다!\n\n{item.itemName} 획득!";
            }

            return true;
        }
        else
        {  // 실패했을 때 로직
            

            return false;
        }
    }

    private void GiveItemById(string itemId)
    {
        GameObject itemPrefab = InGameManager.Instance.GetFieldItemPrefabByItemId(itemId);

        if(itemPrefab != null)
        {
            FieldItem rewardItem = itemPrefab.GetComponent<FieldItem>();

            if(rewardItem != null && rewardItem.ItemData != null)
            {
                InventoryManager.Instance.AddItem(rewardItem.ItemData);
                return;
            }
        }

        Item item = Resources.FindObjectsOfTypeAll<Item>()
            .FirstOrDefault(foundItem => foundItem != null && foundItem.itemID == itemId);

        if(item != null)
        {
            InventoryManager.Instance.AddItem(item);
        }
    }

    private Item GiveRandomCommonItem()
    {
        var commonItems = Resources.FindObjectsOfTypeAll<Item>()
            .Where(item => item != null && item.itemGrade == ItemGrade.Common && item.itemID != "I013")
            .GroupBy(item => item.itemID)
            .Select(group => group.First())
            .ToList();

        if(commonItems.Count == 0) return null;

        Item selectedItem = commonItems[Random.Range(0, commonItems.Count)];
        InventoryManager.Instance.AddItem(selectedItem);

        return selectedItem;
    }
}
