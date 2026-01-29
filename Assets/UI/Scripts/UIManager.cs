using UnityEngine;
using UnityEngine.UI;
/*
인게임 UI를 담당함
- InGameManager에서 정보를 받아옴
- 각 UI창을 연결
    - UI창의 On/Off
    - 각 UI창에서 필요한 정보의 목록
*/
public class UIManager : MonoBehaviour
{
    [Header("공동선택/인게임/교황판정 분류 오브젝트")]
    //타이틀 화면은 다른 씬에서 다루기?
    [Tooltip("공동 선택")]public SushiUI Sushi;
    [Tooltip("인게임")]public IngameUI Ingame;
    [Tooltip("교황 판정")]public CheckUI Check;
    [Space(10f)]
    [Header("환경설정")]
    [Tooltip("환경설정")]public SettingsUI Settings;

//Singleton 구현
private static UIManager instance = null;
void Awake()
    {
        if(instance==null)
        {
            instance=this;
        }
        else Destroy(gameObject);
    }

    public static UIManager Instance
    {
        get; private set;
    }

//외부 게임매니저에서 게임 전환시 UI를 일괄 조작.
    public enum UIState
    {
        SUSHI, //공동선택에서 아이템 선택하기
        INGAME, //일반적인 인게임
        SETTINGS, //환경설정 버튼 누름
        CHECK //콘클라베 종료 후 교황 판정
    }

    private UIState UIstate;

    //프로퍼티
    public UIState State => UIstate;

    // UIState : 외부 클래스에서 UI를 조절할 필요가 있다면 SetUIState로 조절
    
    public void SetUIState(UIManager.UIState state)
    {
        UIstate = state;
        switch (state)
        {
            case UIState.SUSHI:
                {
                    Sushi.gameObject.SetActive(true);
                    Ingame.gameObject.SetActive(false);
                    Check.gameObject.SetActive(false);

                    break;
                }
            case UIState.INGAME:
                {
                    Sushi.gameObject.SetActive(false);
                    Ingame.gameObject.SetActive(true);
                    Check.gameObject.SetActive(false);
                    break;
                }
            case UIState.SETTINGS: //환경설정 켤 시 모든 UI 숨김
                {
                    Sushi.gameObject.SetActive(false);
                    Ingame.gameObject.SetActive(false);
                    Check.gameObject.SetActive(false);
                    break;
                }
            case UIState.CHECK:
            {
                Sushi.gameObject.SetActive(false);
                    Ingame.gameObject.SetActive(false);
                    Check.gameObject.SetActive(true);
                    break;
            }
        }
    }
}
