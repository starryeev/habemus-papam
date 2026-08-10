using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Cardinal : MonoBehaviour
{
    [Header("추기경 기본 설정")]
    [Tooltip("추기경 기본 체력")]
    [SerializeField] private float hp;
    [SerializeField] public float hpDrainMultiplier = 1f;

    [Tooltip("추기경 기본 정치력")]
    [SerializeField] private float influence;

    [Tooltip("추기경 기본 경건함")]
    [SerializeField] private float piety;
    [SerializeField] private float maxHp = 10f;

    [Header("이동 관련 설정")]
    [SerializeField] private float baseMoveSpeed;

    private float speedMultiplier = 1f;

    public float prayDeltaHpEvent = 0f;

    private List<Item> items;
    private NavMeshAgent agent;
    private bool isKnockedOut = false;
    private readonly HashSet<string> minHpOneEffectSources = new HashSet<string>();
    private GameContext subscribedGameContext;
    private bool isInitialized = false;
    private Coroutine indicatorRestoreCoroutine;

    public float Hp => hp;
    public float HpDrainMultiplier => hpDrainMultiplier;
    public float Influence => influence;
    public float Piety => piety;
    public float MaxHp => maxHp;
    public float MoveSpeed => baseMoveSpeed * speedMultiplier;
    public bool IsKnockedOut => isKnockedOut;

    void Awake()
    {
        items = new List<Item>();
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }
    }

    void Start()
    {
        SubscribeToGameContext();
        if (!isInitialized)
        {
            InitCardinal();
        }
    }

    void Update()
    {
        ResolveHpState();
    }

    public void ResolveHpState()
    {
        if (hp <= 0f && !isKnockedOut)
        {
            bool isRevived = false;

            foreach (var item in items)
            {
                if (item != null && item.OnHpReachedZero(this))
                {
                    isRevived = true;
                    break;
                }
            }

            if (!isRevived)
            {
                isKnockedOut = true;
                hp = 0f;

                if (ActionRecordManager.Instance != null)
                {
                    ActionRecordManager.Instance.RecordKnockOut(this);
                }

                if (CompareTag("Player") && InGameManager.Instance != null)
                {
                    InGameManager.Instance.HandlePlayerHpReachedZero(this);
                }

                Debug.Log($"[{gameObject.name}] 체력이 0이 되어 기절했습니다!");
            }
        }
        else if (hp > 0f)
        {
            isKnockedOut = false;
        }
    }

    void InitCardinal()
    {
        ApplyBalanceDefaults();
        isInitialized = true;
        RegisterPlayerIfNeeded();
    }

    void OnEnable()
    {
        SubscribeToGameContext();
        if (items != null)
        {
            foreach (var item in items)
            {
                if (item != null)
                {
                    item.OnReapply(this);
                }
            }
        }
    }

    void OnDisable()
    {
        UnsubscribeFromGameContext();
    }

    private void ApplyBalanceDefaults()
    {
        if (InGameManager.Instance == null)
        {
            return;
        }

        GameBalance balance = InGameManager.Instance.Balance;
        hp = balance.InitialHp;
        maxHp = 10f;
        influence = balance.InitialInfluence;
        piety = balance.InitialPiety;
        baseMoveSpeed = balance.InitialMoveSpeed;
        speedMultiplier = 1f;
        prayDeltaHpEvent = 0f;
        isKnockedOut = false;
        minHpOneEffectSources.Clear();

        if (agent != null)
        {
            agent.speed = MoveSpeed;
        }
    }

    private void RegisterPlayerIfNeeded()
    {
        if (!CompareTag("Player"))
        {
            return;
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SetPlayer(this);
        }
    }

    private float GetBalanceFallback(float currentValue, System.Func<GameBalance, float> selector)
    {
        if (isInitialized || InGameManager.Instance == null)
        {
            return currentValue;
        }

        return selector(InGameManager.Instance.Balance);
    }

    public CardinalSaveData CaptureSaveData(int index)
    {
        StateController stateController = GetComponent<StateController>();
        List<string> savedMinHpSources = new List<string>(minHpOneEffectSources);
        savedMinHpSources.Sort(System.StringComparer.Ordinal);

        return new CardinalSaveData
        {
            index = index,
            objectName = gameObject.name,
            isPlayer = CompareTag("Player"),
            isActive = gameObject.activeSelf,
            hp = GetBalanceFallback(hp, balance => balance.InitialHp),
            influence = GetBalanceFallback(influence, balance => balance.InitialInfluence),
            piety = GetBalanceFallback(piety, balance => balance.InitialPiety),
            maxHp = maxHp,
            hpDrainMultiplier = hpDrainMultiplier,
            prayDeltaHpEvent = prayDeltaHpEvent,
            minHpOneEffectSources = savedMinHpSources,
            isKnockedOut = isKnockedOut,
            isSchemer = stateController != null && stateController.IsSchemer,
            isConClaving = stateController != null && stateController.ConClaving,
            state = stateController != null ? (int)stateController.CurrentState : (int)CardinalState.CutScene,
            position = SerializableVector3.FromVector3(transform.position),
            rotationZ = transform.eulerAngles.z
        };
    }

    public void ApplySaveData(CardinalSaveData saveData)
    {
        ApplyBalanceDefaults();

        maxHp = Mathf.Max(1f, saveData.maxHp);
        hp = Mathf.Clamp(saveData.hp, 0f, maxHp);
        influence = Mathf.Clamp(saveData.influence, 0f, 10f);
        piety = Mathf.Clamp(saveData.piety, 0f, 10f);
        hpDrainMultiplier = saveData.hpDrainMultiplier;
        prayDeltaHpEvent = saveData.prayDeltaHpEvent;
        minHpOneEffectSources.Clear();
        if (saveData.minHpOneEffectSources != null)
        {
            foreach (string sourceId in saveData.minHpOneEffectSources)
            {
                if (!string.IsNullOrWhiteSpace(sourceId)) minHpOneEffectSources.Add(sourceId);
            }
        }
        isKnockedOut = saveData.isKnockedOut;
        isInitialized = true;

        if (agent != null)
        {
            agent.speed = MoveSpeed;
        }

        RegisterPlayerIfNeeded();
    }

    public void RestorePlayerIndicatorAfterLoad()
    {
        if (!CompareTag("Player") || !gameObject.activeInHierarchy)
        {
            return;
        }

        Animation_Controller animCtrl = GetComponentInChildren<Animation_Controller>(true);
        if (animCtrl == null)
        {
            return;
        }

        if (indicatorRestoreCoroutine != null)
        {
            StopCoroutine(indicatorRestoreCoroutine);
        }

        indicatorRestoreCoroutine = StartCoroutine(ApplyPlayerIndicatorAfterLoad(animCtrl));
    }

    private System.Collections.IEnumerator ApplyPlayerIndicatorAfterLoad(Animation_Controller animCtrl)
    {
        yield return null;

        if (animCtrl != null)
        {
            animCtrl.SetIndicatorActive(true);
        }

        indicatorRestoreCoroutine = null;
    }

    public void ChangeSpeed(float delta)
    {
        speedMultiplier += delta;

        if (agent != null)
        {
            agent.speed = MoveSpeed;
        }
    }

    public void RestoreMoveSpeed()
    {
        speedMultiplier = 1f;
        if (agent != null)
        {
            agent.speed = baseMoveSpeed;
        }
    }

    public void SetAgentSize(float newRadius, float newHeight)
    {
        if (agent != null)
        {
            agent.radius = newRadius;
            agent.height = newHeight;
        }
    }

    public void SetMinHpOneEffect(bool active)
    {
        SetMinHpOneEffect("Legacy", active);
    }

    public void SetMinHpOneEffect(string sourceId, bool active)
    {
        if (string.IsNullOrWhiteSpace(sourceId)) return;

        if (active) minHpOneEffectSources.Add(sourceId);
        else minHpOneEffectSources.Remove(sourceId);
    }

    public void ChangeHp(float delta)
    {
        float previousHp = hp;
        float nextHp = hp + delta;

        if (minHpOneEffectSources.Count > 0 && delta < 0f)
        {
            hp = Mathf.Clamp(nextHp, 1f, maxHp);
        }
        else
        {
            hp = Mathf.Clamp(nextHp, 0f, maxHp);
        }

        float actualLoss = previousHp - hp;
        if (actualLoss > 0f && InGameManager.Instance != null)
        {
            InGameManager.Instance.RecordPendingHpLoss(this, actualLoss);
        }
    }

    public void SetMaxHp(float value)
    {
        maxHp = Mathf.Max(1f, value);
        hp = Mathf.Min(hp, maxHp);
    }

    public void ChangeInfluence(float delta)
    {
        influence = Mathf.Clamp(influence + delta, 0f, 10f);
    }

    public void ChangePiety(float delta)
    {
        piety = Mathf.Clamp(piety + delta, 0f, 10f);
    }

    private void SubscribeToGameContext()
    {
        GameContext context = InGameManager.Instance != null ? InGameManager.Instance.Context : null;
        if (context == null || subscribedGameContext == context) return;

        UnsubscribeFromGameContext();
        subscribedGameContext = context;
        subscribedGameContext.OnGameContextEvent += HandleGameContextEvent;
    }

    private void UnsubscribeFromGameContext()
    {
        if (subscribedGameContext == null) return;
        subscribedGameContext.OnGameContextEvent -= HandleGameContextEvent;
        subscribedGameContext = null;
    }

    private void HandleGameContextEvent(GameContext.GameContextEvent eventType)
    {
        if (eventType == GameContext.GameContextEvent.ConclaveEnd)
        {
            minHpOneEffectSources.Clear();
        }
    }

    public void AddPassiveItem(Item item)
    {
        if (item == null)
        {
            return;
        }

        if (!items.Contains(item))
        {
            items.Add(item);
        }
    }

    public void RemovePassiveItem(Item item)
    {
        if (item != null && items.Contains(item))
        {
            items.Remove(item);
        }
    }

    public void ClearPassiveItems()
    {
        items.Clear();
    }

    public void Pray()
    {
        if (InGameManager.Instance == null || !InGameManager.Instance.CanPerformPlayerAction(this))
        {
            return;
        }

        if (!InGameManager.Instance.CanPerformPrayer(this, out _, out _))
        {
            return;
        }

        if (CompareTag("Player")) InGameManager.Instance.ExecuteNpcActionsBeforePlayerAction(this);
        ResolvePrayer(InGameManager.Instance.Balance.PraySuccessChance, CompareTag("Player"));
    }

    public void PerformNpcPrayer(float successChance)
    {
        ResolvePrayer(Mathf.Clamp01(successChance), false);
    }

    private void ResolvePrayer(float successChance, bool completePlayerAction)
    {
        if (InGameManager.Instance == null) return;

        GameBalance balance = InGameManager.Instance.Balance;
        bool guaranteedSuccess = completePlayerAction && InGameManager.Instance.EventManager != null &&
            InGameManager.Instance.EventManager.TryConsumeGuaranteedPrayerOrSpeech(this);
        bool rolledSuccess = Random.value < successChance;
        bool success = guaranteedSuccess || rolledSuccess;

        float hpBeforePrayer = Hp;
        bool blocksPrayerHealing = items.Exists(item => item is I001);

        if (success)
        {
            ChangePiety(balance.PraySuccessDeltaPiety);
            ChangeHp(balance.PraySuccessDeltaHp + prayDeltaHpEvent);
        }
        else
        {
            ChangePiety(balance.PrayFailDeltaPiety);
            ChangeHp(balance.PrayFailDeltaHp);
        }

        if (completePlayerAction)
        {
            SoundManager.Instance.PlaySFX(success ? "20 인게임- 기도성공" : "21 인게임- 기도실패");
        }

        foreach (var item in items)
        {
            item?.OnPray(this);
        }

        if (blocksPrayerHealing && Hp > hpBeforePrayer)
        {
            ChangeHp(hpBeforePrayer - Hp);
        }

        if (ActionRecordManager.Instance != null)
        {
            ActionRecordManager.Instance.RecordPray(this);
        }
        if (completePlayerAction) InGameManager.Instance.CompletePlayerAction(this);
    }

    public void Speech()
    {
        if (InGameManager.Instance == null || !InGameManager.Instance.CanPerformPlayerAction(this))
        {
            return;
        }

        if (CompareTag("Player")) InGameManager.Instance.ExecuteNpcActionsBeforePlayerAction(this);
        ResolveSpeech(InGameManager.Instance.GetSpeechSuccessChance(this), CompareTag("Player"));
    }

    public void PerformNpcSpeech(float successChance)
    {
        ResolveSpeech(Mathf.Clamp01(successChance), false);
    }

    private void ResolveSpeech(float successChance, bool completePlayerAction)
    {
        if (InGameManager.Instance == null) return;

        GameBalance balance = InGameManager.Instance.Balance;
        bool guaranteedSuccess = completePlayerAction && InGameManager.Instance.EventManager != null &&
            InGameManager.Instance.EventManager.TryConsumeGuaranteedPrayerOrSpeech(this);
        bool rolledSuccess = Random.value < successChance;
        bool success = guaranteedSuccess || rolledSuccess;
        StateController stateController = GetComponent<StateController>();
        bool playSpeechAnimation = completePlayerAction ||
            stateController != null && stateController.CurrentState == CardinalState.InSpeech;

        Animation_Controller anim = GetComponent<Animation_Controller>();
        if (anim == null)
        {
            anim = GetComponentInChildren<Animation_Controller>();
        }

        if (success)
        {
            if (playSpeechAnimation && anim != null)
            {
                anim.SetSpeechAnimation(2);
            }

            float speechSuccessDeltaInfluence = balance.SpeechSuccessDeltaInfluenceMin;
            if (!Mathf.Approximately(balance.SpeechSuccessDeltaInfluenceMin, balance.SpeechSuccessDeltaInfluenceMax))
            {
                speechSuccessDeltaInfluence = Random.Range(
                    balance.SpeechSuccessDeltaInfluenceMin,
                    balance.SpeechSuccessDeltaInfluenceMax);
            }

            foreach (var item in items)
            {
                if (item != null)
                {
                    speechSuccessDeltaInfluence = item.ModifySpeechInfluence(speechSuccessDeltaInfluence, balance, true);
                }
            }

            ChangeInfluence(speechSuccessDeltaInfluence);
            ChangeHp(balance.SpeechSuccessDeltaHp);
        }
        else
        {
            if (playSpeechAnimation && anim != null)
            {
                anim.SetSpeechAnimation(3);
            }

            float speechFailDeltaInfluence = balance.SpeechFailDeltaInfluence;

            foreach (var item in items)
            {
                if (item != null)
                {
                    speechFailDeltaInfluence = item.ModifySpeechInfluence(speechFailDeltaInfluence, balance, false);
                }
            }

            ChangeInfluence(speechFailDeltaInfluence);
            ChangeHp(balance.SpeechFailDeltaHp);
        }

        if (completePlayerAction)
        {
            SoundManager.Instance.PlaySFX(success ? "22 인게임- 연설성공" : "23 인게임- 연설실패");
        }

        foreach (var item in items)
        {
            item?.OnSpeech(this);
        }

        if (ActionRecordManager.Instance != null)
        {
            ActionRecordManager.Instance.RecordSpeech(this);
        }
        if (completePlayerAction) InGameManager.Instance.CompletePlayerAction(this);
    }

    public void OnPlotExecuted()
    {
        foreach (var item in items)
        {
            item?.OnPlot(this);
        }
    }

    public void Plot(StateController schemerState)
    {
        PlotManager.Instance.InitializePlotSession(this, schemerState);
    }
}
