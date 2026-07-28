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

    [Header("이동 관련 설정")]
    [SerializeField] private float baseMoveSpeed;

    private float speedMultiplier = 1f;

    public float prayDeltaHpEvent = 0f;

    private List<Item> items;
    private NavMeshAgent agent;
    private bool isKnockedOut = false;
    private bool hasMinHpOneEffect = false;
    private bool isInitialized = false;
    private Coroutine indicatorRestoreCoroutine;

    public float Hp => hp;
    public float HpDrainMultiplier => hpDrainMultiplier;
    public float Influence => influence;
    public float Piety => piety;
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
        if (!isInitialized)
        {
            InitCardinal();
        }
    }

    void Update()
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

    private void ApplyBalanceDefaults()
    {
        if (InGameManager.Instance == null)
        {
            return;
        }

        GameBalance balance = InGameManager.Instance.Balance;
        hp = balance.InitialHp;
        influence = balance.InitialInfluence;
        piety = balance.InitialPiety;
        baseMoveSpeed = balance.InitialMoveSpeed;
        speedMultiplier = 1f;
        prayDeltaHpEvent = 0f;
        isKnockedOut = false;

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

        return new CardinalSaveData
        {
            index = index,
            objectName = gameObject.name,
            isPlayer = CompareTag("Player"),
            isActive = gameObject.activeSelf,
            hp = GetBalanceFallback(hp, balance => balance.InitialHp),
            influence = GetBalanceFallback(influence, balance => balance.InitialInfluence),
            piety = GetBalanceFallback(piety, balance => balance.InitialPiety),
            hpDrainMultiplier = hpDrainMultiplier,
            prayDeltaHpEvent = prayDeltaHpEvent,
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

        hp = saveData.hp;
        influence = saveData.influence;
        piety = saveData.piety;
        hpDrainMultiplier = saveData.hpDrainMultiplier;
        prayDeltaHpEvent = saveData.prayDeltaHpEvent;
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
        hasMinHpOneEffect = active;
    }

    public void ChangeHp(float delta)
    {
        float nextHp = hp + delta;

        if (hasMinHpOneEffect && delta < 0f)
        {
            hp = Mathf.Clamp(nextHp, 1f, 100f);
        }
        else
        {
            hp = Mathf.Clamp(nextHp, 0f, 100f);
        }
    }

    public void ChangeInfluence(float delta)
    {
        influence = Mathf.Clamp(influence + delta, 0f, 100f);
    }

    public void ChangePiety(float delta)
    {
        piety = Mathf.Clamp(piety + delta, 0f, 100f);
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
        if (InGameManager.Instance == null)
        {
            return;
        }

        GameBalance balance = InGameManager.Instance.Balance;
        bool guaranteedSuccess = InGameManager.Instance.EventManager != null &&
            InGameManager.Instance.EventManager.TryConsumeGuaranteedPrayerOrSpeech(this);
        bool rolledSuccess = Random.value < balance.PraySuccessChance;

        if (guaranteedSuccess || rolledSuccess)
        {
            ChangePiety(balance.PraySuccessDeltaPiety);
            ChangeHp(balance.PraySuccessDeltaHp + prayDeltaHpEvent);
        }
        else
        {
            ChangePiety(balance.PrayFailDeltaPiety);
            ChangeHp(balance.PrayFailDeltaHp);
        }

        foreach (var item in items)
        {
            item?.OnPray(this);
        }

        if (ActionRecordManager.Instance != null)
        {
            ActionRecordManager.Instance.RecordPray(this);
        }
    }

    public void Speech()
    {
        if (InGameManager.Instance == null)
        {
            return;
        }

        GameBalance balance = InGameManager.Instance.Balance;
        bool guaranteedSuccess = InGameManager.Instance.EventManager != null &&
            InGameManager.Instance.EventManager.TryConsumeGuaranteedPrayerOrSpeech(this);
        bool rolledSuccess = Random.value < balance.SpeechSuccessChance;

        Animation_Controller anim = GetComponent<Animation_Controller>();
        if (anim == null)
        {
            anim = GetComponentInChildren<Animation_Controller>();
        }

        if (guaranteedSuccess || rolledSuccess)
        {
            if (anim != null)
            {
                anim.SetSpeechAnimation(2);
            }

            float speechSuccessDeltaInfluence = Random.Range(balance.SpeechSuccessDeltaInfluenceMin, balance.SpeechSuccessDeltaInfluenceMax + 1);

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
            if (anim != null)
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

        foreach (var item in items)
        {
            item?.OnSpeech(this);
        }

        if (ActionRecordManager.Instance != null)
        {
            ActionRecordManager.Instance.RecordSpeech(this);
        }
    }

    public void OnPlotExecuted()
    {
        foreach (var item in items)
        {
            item?.OnPlot(this);
        }
    }

    public void Plot()
    {
        PlotManager.Instance.InitializePlotSession(this);
    }
}
