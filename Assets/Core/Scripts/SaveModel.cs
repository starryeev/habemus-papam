using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveModel
{
    public int version = 5;
    public string savedAtUtc;
    public string sceneName = "GameScene";
    public SaveCheckpointType checkpointType;
    public SaveResumeStep resumeStep;
    public GameContextSaveData gameContext = new GameContextSaveData();
    public List<CardinalSaveData> cardinals = new List<CardinalSaveData>();
    public InventorySaveData inventory = new InventorySaveData();
    public EventManagerSaveData events = new EventManagerSaveData();
    public PlotManagerSaveData plots = new PlotManagerSaveData();
    public List<FieldItemSaveData> fieldItems = new List<FieldItemSaveData>();
    public GameNameSaveData names = new GameNameSaveData();
    public ActionStatsSaveData actionStats = new ActionStatsSaveData();
    public SushiSaveData sushi = new SushiSaveData();
}

public enum SaveCheckpointType
{
    ConclaveEntryCompleted = 0,
    TurnPhaseAdvanced = 1,
    JudgementResolved = 2,
    SushiRewardAcquired = 3,
    EventResolved = 4
}

public enum SaveResumeStep
{
    Gameplay = 0,
    ReopenPendingEvent = 1,
    OpenSushiSelection = 2,
    StartNextConclave = 3,
    ContinueAfterResolvedEvent = 4
}

[Serializable]
public class SavePreviewData
{
    public string playerName = string.Empty;
    public float playerHp;
    public float playerInfluence;
    public float playerPiety;
    public int day = 1;
    public int conclave;
    public string conclaveName = string.Empty;
}

[Serializable]
public class GameNameSaveData
{
    public string playerName = string.Empty;
    public List<string> npcNames = new List<string>();
}

[Serializable]
public class CompletedPlayerNameSaveData
{
    public List<string> playerInputNames = new List<string>();
}

[Serializable]
public class ActionStatsSaveData
{
    public int prayCount;
    public int speechCount;
    public int plotCount;
    public int itemAcquireTotalCount;
    public List<ItemAcquireCountSaveData> itemAcquireCounts = new List<ItemAcquireCountSaveData>();
    public float highPietyTime;
    public float highInfluenceTime;
    public float lowPietyTime;
    public float lowInfluenceTime;
    public int stunCount;
    public int healthGameOverCount;
    public int badEndingCount;
    public int happyEndingCount;
    public int papalElectionCount;
    public int papalElectionFailedCount;
    public int currentPopeGeneration;
    public int papalElectionHistoryVersion;
    public List<PapalElectionRecordSaveData> papalElectionHistory = new List<PapalElectionRecordSaveData>();
    public int conclaveCount;

    public void RecordItemAcquired(string itemId, string itemName)
    {
        itemAcquireTotalCount++;

        if (string.IsNullOrWhiteSpace(itemId))
        {
            itemId = string.IsNullOrWhiteSpace(itemName) ? "Unknown" : itemName;
        }

        ItemAcquireCountSaveData record = itemAcquireCounts.Find(item => item.itemId == itemId);

        if (record == null)
        {
            record = new ItemAcquireCountSaveData
            {
                itemId = itemId,
                itemName = string.IsNullOrWhiteSpace(itemName) ? itemId : itemName
            };
            itemAcquireCounts.Add(record);
        }

        record.itemName = string.IsNullOrWhiteSpace(itemName) ? record.itemName : itemName;
        record.count++;
    }

    public string GetMostAcquiredItemName()
    {
        ItemAcquireCountSaveData bestRecord = null;

        foreach (ItemAcquireCountSaveData record in itemAcquireCounts)
        {
            if (record == null)
            {
                continue;
            }

            if (bestRecord == null || record.count > bestRecord.count)
            {
                bestRecord = record;
            }
        }

        if (bestRecord == null)
        {
            return "없음";
        }

        return string.IsNullOrWhiteSpace(bestRecord.itemName) ? bestRecord.itemId : bestRecord.itemName;
    }

    public ActionStatsSaveData Clone()
    {
        ActionStatsSaveData clone = new ActionStatsSaveData
        {
            prayCount = prayCount,
            speechCount = speechCount,
            plotCount = plotCount,
            itemAcquireTotalCount = itemAcquireTotalCount,
            highPietyTime = highPietyTime,
            highInfluenceTime = highInfluenceTime,
            lowPietyTime = lowPietyTime,
            lowInfluenceTime = lowInfluenceTime,
            stunCount = stunCount,
            healthGameOverCount = healthGameOverCount,
            badEndingCount = badEndingCount,
            happyEndingCount = happyEndingCount,
            papalElectionCount = papalElectionCount,
            papalElectionFailedCount = papalElectionFailedCount,
            currentPopeGeneration = currentPopeGeneration,
            papalElectionHistoryVersion = papalElectionHistoryVersion,
            conclaveCount = conclaveCount
        };

        if (itemAcquireCounts != null)
        {
            foreach (ItemAcquireCountSaveData record in itemAcquireCounts)
            {
                if (record == null)
                {
                    continue;
                }

                clone.itemAcquireCounts.Add(new ItemAcquireCountSaveData
                {
                    itemId = record.itemId,
                    itemName = record.itemName,
                    count = record.count
                });
            }
        }

        if (papalElectionHistory != null)
        {
            foreach (PapalElectionRecordSaveData record in papalElectionHistory)
            {
                if (record == null)
                {
                    continue;
                }

                clone.papalElectionHistory.Add(new PapalElectionRecordSaveData
                {
                    generation = record.generation,
                    popeName = record.popeName,
                    isPlayer = record.isPlayer,
                    candidateSlot = record.candidateSlot,
                    electedAtUtc = record.electedAtUtc
                });
            }
        }

        return clone;
    }
}

public enum CandidateSlot
{
    Unknown = 0,
    Player = 1,
    Npc1 = 2,
    Npc2 = 3,
    Npc3 = 4
}

public enum PendingEffectType
{
    P021RestoreInfluence = 0,
    P033RevengeDamage = 1
}

[Serializable]
public class PendingEffectSaveData
{
    public string id = string.Empty;
    public string sourceId = string.Empty;
    public int effectType;
    public int ownerCandidateNumber;
    public int createdDay;
    public int createdConclave;
    public int triggerDay;
    public int triggerConclave;
    public float accumulatedValue;
}

[Serializable]
public class PapalElectionRecordSaveData
{
    public int generation;
    public string popeName = string.Empty;
    public bool isPlayer;
    public CandidateSlot candidateSlot;
    public string electedAtUtc = string.Empty;
}

[Serializable]
public class ItemAcquireCountSaveData
{
    public string itemId;
    public string itemName;
    public int count;
}

[Serializable]
public class GameContextSaveData
{
    public int day = 1;
    public int conclave;
    public int currentTurn = 1;
    public int completedActions;
    public int actionsThisTurn = 8;
    public int positionProgressVersion;
    public List<int> actionCountsByPosition = new List<int>();
    public List<int> unavailableActionCountsByPosition = new List<int>();
    public int currentActionPosition;
    public int completedActionsInPosition;
    public int performedActionsInPosition;
    public int completedUnavailableActions;
    public int actionEffectVersion;
    public List<PlayerActionEffectData> playerActionEffects = new List<PlayerActionEffectData>();
    public bool isEventPhase;
    public int nextTurnActionModifier;
    public bool blockNextTurn;
    public bool blockRemainingCurrentTurn;
    public bool awaitingTurnEvent;
    public bool eventBeforeActions;
    public int lastEventCheckedActionPosition = -1;
    public bool endConclaveAfterEvent;
    public string currentEventId = string.Empty;
    public bool isTimeRunning;
    public bool isFirstStart = true;
    public bool isSushiOn;
    public bool showStartButton = true;
    public bool startButtonInteractable = true;
    public bool showInventoryPanel;
    public bool hasHandledFirstPlayerHpZero;
    public bool shouldRevivePlayerOnNextConclave;
    public List<int> npcTurnBehaviours = new List<int>();
    public List<bool> npcTurnActionsExecuted = new List<bool>();
    public List<bool> npcNextTurnActionBlocked = new List<bool>();
    public List<int> npcNextTurnBlockedActionCounts = new List<int>();
    public List<int> prayerBlockedCandidateNumbers = new List<int>();
    public List<PendingEffectSaveData> pendingEffects = new List<PendingEffectSaveData>();
}

[Serializable]
public class SushiSaveData
{
    public List<string> offeredItemIds = new List<string>();
    public List<bool> selectableSlots = new List<bool>();
    public float selectionDuration;
}

[Serializable]
public class CardinalSaveData
{
    public int index;
    public string objectName;
    public bool isPlayer;
    public bool isActive;
    public float hp;
    public float influence;
    public float piety;
    public float maxHp = 10f;
    public float hpDrainMultiplier = 1f;
    public float prayDeltaHpEvent;
    public List<string> minHpOneEffectSources = new List<string>();
    public bool isKnockedOut;
    public bool isSchemer;
    public bool isConClaving;
    public int state;
    public SerializableVector3 position = new SerializableVector3();
    public float rotationZ;
}

[Serializable]
public class InventorySaveData
{
    public int maxSlots = 3;
    public List<ItemSaveData> inventoryItems = new List<ItemSaveData>();
    public List<ItemSaveData> activeBuffs = new List<ItemSaveData>();
}

[Serializable]
public class ItemSaveData
{
    public string itemId;
    public string runtimeStateJson;
}

[Serializable]
public class EventManagerSaveData
{
    public int scheduleVersion;
    public List<EventRecordSaveData> records = new List<EventRecordSaveData>();
    public List<EventChoiceSaveData> choices = new List<EventChoiceSaveData>();
    public List<EventPlotDamageBonusSaveData> plotDamageBonuses = new List<EventPlotDamageBonusSaveData>();
    public List<PendingGuaranteedEventSaveData> pendingGuaranteedEvents = new List<PendingGuaranteedEventSaveData>();
    public bool subEventOccurredThisTurn;
    public bool guaranteeNextPrayerOrSpeech;
    public bool freePlotPietyForCurrentConclave;
}

public enum PendingEventTiming
{
    AfterCurrentEvent,
    NextEnteredPosition
}

[Serializable]
public class PendingGuaranteedEventSaveData
{
    public string eventId;
    public PendingEventTiming timing;
}

[Serializable]
public class EventRecordSaveData
{
    public string eventId;
    public int appearCount;
}

[Serializable]
public class EventChoiceSaveData
{
    public string eventId;
    public int optionIndex;
    public bool succeeded;
}

[Serializable]
public class EventPlotDamageBonusSaveData
{
    public int candidateNumber;
    public float bonus;
}

[Serializable]
public class PlotManagerSaveData
{
    public int activeDay;
    public List<PlotSetSaveData> plotSets = new List<PlotSetSaveData>();
}

[Serializable]
public class PlotSetSaveData
{
    public List<string> plotIds = new List<string>();
    public List<bool> usedSlots = new List<bool>();
}

[Serializable]
public class FieldItemSaveData
{
    public string itemId;
    public SerializableVector3 position = new SerializableVector3();
    public float rotationZ;
}

[Serializable]
public class SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3()
    {
    }

    public SerializableVector3(Vector3 value)
    {
        x = value.x;
        y = value.y;
        z = value.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }

    public static SerializableVector3 FromVector3(Vector3 value)
    {
        return new SerializableVector3(value);
    }
}
