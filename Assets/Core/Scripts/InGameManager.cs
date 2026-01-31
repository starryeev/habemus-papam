using System;
using UnityEngine;

public class GameContext
{
    public enum Conclave
    {
        Dawn, Morning, Afternoon, Evening
    }

    public enum GameContextEvent
    {
        ConclaveStart, ConclaveEnd
    }

    int currentDay;
    Conclave currentConclave;
    float remainingTime;

    public event Action<GameContextEvent> OnGameContextEvent;
    public int CurrentDay => currentDay;
    public Conclave CurrentConclave => currentConclave;
    public float RemainingTime => remainingTime;

    public void InitGameContext(int day=1, Conclave conclave=Conclave.Dawn)
    {
        currentDay = day;
        currentConclave = conclave;
        remainingTime = InGameManager.Instance.Balance.MaxConclaveTime;
    }

    public void AdvanceConclave()
    {
        if(currentConclave == Conclave.Evening)
        {
            currentConclave = Conclave.Dawn;
            currentDay++;
        }
        else
        {
            currentConclave++;
        }

        remainingTime = InGameManager.Instance.Balance.MaxConclaveTime;

        // 이벤트 알림
        OnGameContextEvent?.Invoke(GameContextEvent.ConclaveStart);
    }

    // 퇴장 연출과 공동선택을 위해 시간을 0이 되면 시간이 0 으로 고정하도록 수정했습니다.
    public void Tick(float deltaTime)
    {
        if (remainingTime > 0)
        {
            remainingTime -= deltaTime;
            if (remainingTime < 0) remainingTime = 0;
        }
    }
}

public class InGameManager : MonoBehaviour
{
    // 싱글톤
    public static InGameManager Instance { get; private set; }

    // 멤버변수
    [SerializeField] private GameBalance balance;
    private GameContext gameContext;


    // 시간 흐름 제어 플래그
    private bool isTimeRunning = false;

    // 프로퍼티
    public GameBalance Balance => balance;
    public GameContext Context => gameContext;
    public bool IsTimeRunning => isTimeRunning;

    void Awake()
    {
        // 싱글톤
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Awake 함수에 들어가야할 로직은 이 아래에
        gameContext = new GameContext();
        gameContext.OnGameContextEvent += HandleGameContextEvent;
    }

    void Start()
    {
        InitGame();

    }

    void Update()
    {
        if (isTimeRunning)
        {
            // 콘클라베 타이머
            gameContext.Tick(Time.deltaTime);

            // 추기경 자동 체력감소 입장하자마자 피가 까이는 버그가 있어서 입장 완료 한 후 시간이 흐를때 체력 감소하도록 수정했습니다!
            CardinalManager cardinalManager = CardinalManager.Instance;
            if (cardinalManager != null)
            {
                cardinalManager.DrainAllCardinalHp(balance.HpDeltaPerSec * Time.deltaTime);
            }
        }
    }

    // 입장 완료시 타이머가 흐르도록
    public void StartTimer()
    {
        // 만약 시간이 0이거나 그 이하라면, 새로운 라운드가 시작된 것으로 간주하고
        // 다음 시간대로 넘긴 후 시간을 리필한다.
        if (gameContext.RemainingTime <= 0)
        {
            gameContext.AdvanceConclave();
            Debug.Log($"New Conclave Started: Day {gameContext.CurrentDay} - {gameContext.CurrentConclave}");
        }

        isTimeRunning = true;
    }

    // 0초가 된다면 타이머 정지
    public void StopTimer()
    {
        isTimeRunning = false;
    }

    void InitGame()
    {
        isTimeRunning = false;

        gameContext.InitGameContext();
        // 게임 시작 시 바로 타이머가 돌지 않게 함
        
    }

    //여기서는 아직 게임을 조작하지 않고 디버깅을 위해서 버튼을 통해 StartConClave 를 호출하도록 합니다.
    void HandleGameContextEvent(GameContext.GameContextEvent eventType)
    {
        switch(eventType)
        {
            // 임시 로직
            case GameContext.GameContextEvent.ConclaveStart:
                Debug.Log("콘클라베 시작");
                //CardinalManager.Instance.StartConClave();
                break;
            case GameContext.GameContextEvent.ConclaveEnd:
                //이 부분도 0초가 되면 자동으로 StopConClave() 를 호출하도록 해서 특별한 경우가 아니면 쓰일것 같지는 않습니다.
                Debug.Log("콘클라베 끝");
                //CardinalManager.Instance.StopConClave();
                break;
        }
    }

    public int GetProgress()
    {
        CardinalManager cardinalManager = CardinalManager.Instance;

        int result = 0;

        int dayFactor = (gameContext.CurrentDay - 1) * 10;
        int hpFactor = Mathf.RoundToInt(Mathf.Clamp((400 - cardinalManager.GetCardinalHpSum()) * 0.025f, 0, 10));
        int polFactor = Mathf.RoundToInt(Mathf.Clamp(cardinalManager.GetCardinalPolSum() * 0.075f, 0, 30));

        result = dayFactor + hpFactor + polFactor;

        return result;
    }
    public int GetCurrentDay()
    {
        return gameContext.CurrentDay;
    }
    public GameContext.Conclave GetCurrentConclave()
    {
        return gameContext.CurrentConclave;
    }
    public float GetRemainingTime()
    {
        return gameContext.RemainingTime;
    }
}
