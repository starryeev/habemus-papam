using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "E40300", menuName = "Events/까마귀 소동")]
public class E40300 : Event
{
    void Reset()
    {
        eventID = "E40300";
        eventName = "까마귀 소동";
        eventDescription = "(후보 n)이 선물을 보내왔다. 안에 든 것은 반짝이는 목걸이였다.\n그런데 그 때...! 왠 미친 까마귀 떼가 당신을 향해 날아오는 것을 발견했다!";
        maxAppear = 3;

        eventWeightBase = 20f;
        eventWeightMultiplier = 0f;

        option1 = "훠이 훠이! 물렀거라!";
        option1Chance = 0.65f;
        option1Requirement = "-";
        option1SuccessDescription = "까악 까악! 까마귀들은 한바탕 난동을 피운 뒤 저 멀리 날아갔다.\n가방 안이 영 허전해 보이는데....\n\n수중에 있는 모든 아이템 소멸!";
        option1SuccessResult = "모든 아이템 소멸";
        option1FailDescription = "까악! 당신은 까마귀들을 쥐어박고 내쫓았다. 방금이 마지막 놈이었다.\n하얗게 불태웠어....\n\n체력 30 감소!";
        option1FailResult = "체력 - 30";
        option2 = "";
    }

    public override bool CanChoiceOption1(Cardinal performer)
    {
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
        {
            var items = new List<Item>(InventoryManager.Instance.InventoryItems);
            foreach(var item in items)
            {
                InventoryManager.Instance.RemoveItem(item);
            }

            return true;
        }

        performer.ChangeHp(-30f);
        return false;
    }

    public override bool OnChoiceOption2(Cardinal performer)
    {
        return true;
    }
}
