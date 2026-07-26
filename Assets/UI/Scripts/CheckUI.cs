using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
//여기에 교황 판정 화면을 구현
public class CheckUI : MonoBehaviour
{
    private Image img;
    [SerializeField] private Button SkipButton;
    [SerializeField] private Button Vote;
    [SerializeField] private Sprite[] sprites; //코루틴 돌릴 스프라이트.
    [SerializeField] private Animator anim;
    private int currentSprite = 0;
    private bool isClicked = false;
    
    private enum AnimState
    {
        None = 0,
        Enter,
        ElectWait,
        Elect,
        ElectEnd
    }
    [SerializeField] private AnimState animState;
    [SerializeField] private string ElectionMessage = "운명의 순간이다...\n제발 나만 아니면 돼.";
    [SerializeField] private string ElectionSubMessage = "투표함을 눌러 투표 결과를 확인하세요!";
    [SerializeField] private string[] JudgeMessage;
    [SerializeField] private string JudgeSubMessage = "투표함을 눌러 판정 결과를 확인하세요!";
    [SerializeField] private string miscMessage = "신탁 성공 확률";
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private TextMeshProUGUI subText;
    [SerializeField] private TextMeshProUGUI miscText;
    [SerializeField] private TextMeshProUGUI probabilityText;
    [SerializeField] private float jackpotDuration = 3f;
    [SerializeField] private VideoPlayer vp;
    private Coroutine jackpotCoroutine;
    private float winProbability = 0; //ElectionManager에서 정해진 승리 확률.
    private int winner = -1;
    public void SetWinner(int i) {winner = i;}
    public void SetProbability(float f) {winProbability = f;}
    void Start()
    {
        img = GetComponent<Image>();
        if(SkipButton != null) SkipButton.onClick.AddListener(Skip);
        if(Vote != null)
        {
            Vote.onClick.AddListener(OnVote);
        }
        animState = 0;
        SetSprite(0);
        isClicked = false;
        text.text = "";
        subText.text = "";
        probabilityText.text = "";
        miscText.text = "";
    }
    public void SetSprite(int i)
    {
        if(currentSprite == i) return;
        currentSprite = i;
        img.sprite = sprites[i];
    }
    private void Skip() //스킵 버튼을 누르면 스킵.
    {
        if(jackpotCoroutine != null) StopCoroutine(jackpotCoroutine);
        if(animState == AnimState.None)
        {
            Debug.Log("투표 화면 오류!");
        }
        if(animState == AnimState.Enter || animState == AnimState.ElectWait || animState == AnimState.Elect)
        OnElectAnimFinished();
    }
    private void OnVote()
    {
        Debug.Log("투표함 클릭됨");
        if(animState == AnimState.ElectWait)
        {
            text.text = ElectionMessage;
            anim.Play("Elect", 0, 0f);
            animState = AnimState.Elect;
            return;
        }
        if(animState == AnimState.Elect)
        {
            Skip();
            return;
        }
        if(animState==AnimState.ElectEnd)
        {
            ElectionManager.Instance.GetNextScenes();
        }
    }
    private void OnEnable()
    {
        animState = AnimState.Enter;
        text.text = ElectionMessage;
        subText.text = ElectionSubMessage;
        probabilityText.text = "";
        anim.Play("Enter", 0, 0f);
        Vote.gameObject.SetActive(false);
    }
    public void OnEnterAnimFinished()
    {
        text.text = ElectionMessage;
        subText.text = ElectionSubMessage;
        animState = AnimState.ElectWait;
        Vote.gameObject.SetActive(true);
        anim.Play("Idle", 0, 0f);
    }
    public void OnElectAnimFinished()
    {
        animState = AnimState.ElectEnd;
        SetSprite(4+(winner%4));

        string s = JudgeMessage[winner];
        text.text = s.Replace("NAME", ElectionManager.Instance.CurrentWinnerCandidate.name); //이름 찾아야 함. StatsUI에서 찾는 방식은 좀 그럼.
        miscText.text = miscMessage;

        jackpotCoroutine = StartCoroutine(JackpotRoutine(winProbability));
    }
    private IEnumerator JackpotRoutine(float finalProb)
    {
        float elapsed = 0f;

        while (elapsed < jackpotDuration)
        {
            elapsed += Time.deltaTime;

            float randomTick = Random.Range(0f, 100f);

            if (probabilityText != null)
            {
                probabilityText.text = $" <color=white>{randomTick:F1}%</color>";
            }

            yield return null;
        }

        if (probabilityText != null)
        {
            probabilityText.text = $" <color=white>{finalProb:F1}%</color>";
        }

        jackpotCoroutine = null;
    }
    public void DoNothing()
    {
    }//애니메이션을 위한 더미함수.
}