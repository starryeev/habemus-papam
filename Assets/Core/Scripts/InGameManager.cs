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

    public void Tick(float deltaTime)
    {
        if(remainingTime > 0)
        {
            remainingTime -= deltaTime;
        }
        else
        {
            AdvanceConclave();
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

    // 프로퍼티
    public GameBalance Balance => balance;
    public GameContext Context => gameContext;

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
        // 콘클라베 타이머
        gameContext.Tick(Time.deltaTime);

        // 추기경 자동 체력감소
        CardinalManager cardinalManager = CardinalManager.Instance;
        cardinalManager.DrainAllCardinalHp(balance.HpDeltaPerSec * Time.deltaTime);
    }

    void InitGame()
    {
        gameContext.InitGameContext();
    }

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
