using UnityEngine;

public class IngameUI : MonoBehaviour
{
    [Header("인게임 UI")]
    [Tooltip("상단 UI")]public TimeUI Time;
    [Tooltip("우측 UI")]public StatsUI Stats;
    [Tooltip("이벤트창")]public EventUI Event;
    [Tooltip("아이템창")]public ItemUI Item;
    [Tooltip("공작 UI")]public SchemeUI Scheme;
    [Tooltip("기도/연설 단축키")]public Hotkeys Hotkeys;
}