using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class DeadPopeText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI descriptionLeft;
    [SerializeField] private TextMeshProUGUI descriptionRight;
    private int randomYear = 0;
    private string[] titleText = new string[4];
    private string[] descriptionText = new string[4];

    public void ResetPope()
    {
        PapalElectionRecordSaveData latestPope = ActionRecordManager.Instance?.GetLatestPope();

        if (title == null || descriptionLeft == null)
        {
            Debug.LogError("DeadPopeText title or descriptionLeft is not assigned.", this);
            return;
        }

        //이전 교황에 맞는 텍스트 선정, 없으면 랜덤
        ConfigureDescriptionColumns();

        int textIndex = GetTextIndex(latestPope);
        title.text = titleText[textIndex];
        descriptionLeft.text = descriptionText[textIndex];
        descriptionLeft.ForceMeshUpdate();
        Debug.Log($"DeadPopeText GetLatestPope: {latestPope?.popeName}");
    }

    void Awake()
    {
        randomYear = Random.Range(50, 280);
        PapalElectionRecordSaveData latestPope = ActionRecordManager.Instance?.GetLatestPope();
        string popeName = latestPope?.popeName ?? "";
        int popeGeneration = latestPope?.generation ?? 1;

        titleText[0] = $"태양교의 위대한 성인, {randomYear}년의 생애를 마치고 선종";
        titleText[1] = $"\"태양의 힘\", {randomYear}년의 생애를 마치고 선종";
        titleText[2] = $"\"가장 낮은 곳의 성자\", {randomYear}년의 봉사를 마치고 선종";
        titleText[3] = $"{randomYear}년간 밝혀온 태양의 에너지, 평안히 사그라들다";

        descriptionText[0] =
        $"태양교 교단은 오늘 새벽 교주 {popeName}가 관저 집무실에서 서거했다고 공식 발표했다. 향년 {randomYear}세이다.\n"+
        $"교단 대변인은 발표를 통해 “성하께서 오늘 {System.DateTime.Now:HH시 mm분}, 가까운 성직자들이 지켜보는 가운데 평화롭게 선종하셨다”고 밝혔다."
        + "교주는 최근 수 개월간 건강 악화로 일부 공식 일정을 취소해 왔으며, 지난주 중앙 성전 광장에서 열린 축복 행사가 마지막 공개 일정이 되었다.\n"
        + $"{(popeName=="" ? "교주" : popeName)}는 {randomYear - 30}년 전 제 {popeGeneration}대 교주로 선출된 이후, 잦은 성전 선포로 실추되었던 교단의 이미지를 극적으로 회복하고 높은 대중적 인지도를 유지해 왔다."
        + "특히 그의 인자한 태도와 봉사하는 삶은, 교단 신도는 물론 전 세계적으로 많은 이들에게 깊은 인상을 주었다고 평가받고 있다.\n";
        descriptionText[1] =
        $"태양교 교단은 오늘 새벽 교주 {popeName}가 관저 집무실에서 서거했다고 공식 발표했다. 향년 {randomYear}세이다.\n"+
        $"교단 대변인은 발표를 통해 “성하께서 오늘 {System.DateTime.Now:HH시 mm분}, 가까운 성직자들이 지켜보는 가운데 평화롭게 선종하셨다”고 밝혔다."
        + "교주는 최근 수 개월간 건강 악화로 일부 공식 일정을 취소해 왔으며, 지난주 고위 장로단과의 접견이 마지막 공개 일정이 되었다.\n"
        + $"{(popeName=="" ? "교주" : popeName)}는 {randomYear - 64}년 전 제 {popeGeneration}대 교주로 선출된 이후, 이단으로 인해 무너졌던 교단의 위상을 회복하고, 교단의 전통과 교리를 재정립하는 데 큰 역할을 해 왔다. 특히 그의 강력한 지도력과 결단력은 교단 신도는 물론 전 세계적으로 많은 이들에게 깊은 인상을 주었다고 평가받고 있다.\n";

        descriptionText[2] =
        $"태양교 교단은 오늘 새벽 교주 {popeName}가 관저 집무실에서 서거했다고 공식 발표했다. 향년 {randomYear}세이다.\n"+
        $"교단 대변인은 발표를 통해 “성하께서 오늘 {System.DateTime.Now:HH시 mm분}, 가까운 성직자들이 지켜보는 가운데 평화롭게 선종하셨다”고 밝혔다."
        + "교주는 최근 수 개월간 건강 악화에도 불구하고 공식 일정을 취소하지 않고 강행하였으며, 지난 주 해외 빈민구호소 순방이 마지막 공개 일정이 되었다.\n"
        + $"{(popeName=="" ? "교주" : popeName)}는 {randomYear - 72}년 전 제 {popeGeneration}대 교주로 선출된 이후, 교단의 사회적 책임을 강조하며 빈민 구호와 사회 봉사에 앞장서 왔다. 특히 그의 인자한 태도와 봉사하는 삶은, 교단 신도는 물론 전 세계적으로 많은 이들에게 깊은 인상을 주었다고 평가받고 있다.\n";
        descriptionText[3] =
        $"태양교 교단은 오늘 새벽 교주 {popeName}가 관저 집무실에서 서거했다고 공식 발표했다. 향년 {randomYear}세이다.\n"+
        $"교단 대변인은 발표를 통해 “성하께서 오늘 {System.DateTime.Now:HH시 mm분}, 집무 중 급작스러운 심장 마비로 선종하셨다”고 밝혔다."
        + "이는 교주가 평생 동안 지병 없이 건강한 삶을 살아온 점을 고려할 때, 교단 내부에서도 예상치 못한 소식으로 받아들여지고 있다.\n"
        + $"{(popeName=="" ? "교주" : popeName)}는 {randomYear - 5}년 전 제 {popeGeneration}대 교주로 선출된 이후 현재까지 교주직을 수행하였으며, 특히 교단 수도회의 발전은 그의 중요한 공로로 꼽히고 있다. 그는 교주의 자리에도 불구하고 그가 건립한 숲속 수도원을 직접 찾아다녔기에, 세간에서 '곰'이라는 별칭으로 불리기도 했다.\n";

    }

    void OnEnable()
    {
        ResetPope();
    }

    private void OnValidate()
    {
        ConfigureDescriptionColumns();
    }

    private void ConfigureDescriptionColumns()
    {
        if (descriptionLeft == null)
            return;

        if (descriptionRight == null)
        {
            descriptionLeft.linkedTextComponent = null;
            return;
        }

        descriptionRight.text = string.Empty;
        descriptionRight.linkedTextComponent = null;
        descriptionRight.overflowMode = TextOverflowModes.Truncate;

        descriptionLeft.overflowMode = TextOverflowModes.Linked;
        descriptionLeft.linkedTextComponent = descriptionRight;
    }

    private int GetTextIndex(PapalElectionRecordSaveData latestPope)
    {
        if (latestPope == null)
            return Random.Range(0, titleText.Length);

        int candidateIndex = (int)latestPope.candidateSlot - 1;
        if (candidateIndex < 0 || candidateIndex >= titleText.Length)
            return Random.Range(0, titleText.Length);

        return candidateIndex;
    }
}
