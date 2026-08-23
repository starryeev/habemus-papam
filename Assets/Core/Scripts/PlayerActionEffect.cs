using System;
using System.Collections.Generic;

public enum PlayerActionEffectType
{
    Additional = 0,
    Unavailable = 1
}

public enum PlayerActionEffectSourceType
{
    Plot = 0,
    Item = 1,
    Event = 2,
    Legacy = 3
}

public enum PlayerActionEffectPersistence
{
    CurrentDay = 0,
    WhileItemOwned = 1
}

[Serializable]
public sealed class PlayerActionEffectData
{
    public string id = string.Empty;
    public int effectType;
    public int sourceType;
    public string sourceId = string.Empty;
    public string sourceName = string.Empty;
    public int totalCount;
    public int remainingCount;
    public int createdDay;
    public int targetPositionIndex = -1;
    public int persistence;
    public bool isNoticePending;
    public bool isDeferred;

    public PlayerActionEffectType EffectType => (PlayerActionEffectType)effectType;
    public PlayerActionEffectSourceType SourceType => (PlayerActionEffectSourceType)sourceType;
    public PlayerActionEffectPersistence Persistence => (PlayerActionEffectPersistence)persistence;

    public PlayerActionEffectData Clone()
    {
        return new PlayerActionEffectData
        {
            id = id,
            effectType = effectType,
            sourceType = sourceType,
            sourceId = sourceId,
            sourceName = sourceName,
            totalCount = totalCount,
            remainingCount = remainingCount,
            createdDay = createdDay,
            targetPositionIndex = targetPositionIndex,
            persistence = persistence,
            isNoticePending = isNoticePending,
            isDeferred = isDeferred
        };
    }
}

public sealed class PlayerActionEffectQueue
{
    private readonly List<PlayerActionEffectData> _effects = new List<PlayerActionEffectData>();

    public int Count => _effects.Count;
    public IReadOnlyList<PlayerActionEffectData> Effects => _effects;

    public void Clear() => _effects.Clear();

    public void Enqueue(PlayerActionEffectData effect)
    {
        if (effect == null || effect.totalCount <= 0) return;
        _effects.Add(effect);
    }

    public PlayerActionEffectData FindPendingAdditionalNotice(int positionIndex)
    {
        for (int i = 0; i < _effects.Count; i++)
        {
            PlayerActionEffectData effect = _effects[i];
            if (effect != null && effect.EffectType == PlayerActionEffectType.Additional &&
                !effect.isDeferred && effect.isNoticePending &&
                effect.targetPositionIndex == positionIndex)
            {
                return effect;
            }
        }

        return null;
    }

    public bool HasDeferredAdditionalForPosition(int positionIndex)
    {
        for (int i = 0; i < _effects.Count; i++)
        {
            PlayerActionEffectData effect = _effects[i];
            if (effect != null && effect.EffectType == PlayerActionEffectType.Additional &&
                effect.isDeferred && effect.targetPositionIndex == positionIndex)
            {
                return true;
            }
        }

        return false;
    }

    public PlayerActionEffectData FindPendingUnavailableNotice()
    {
        for (int i = 0; i < _effects.Count; i++)
        {
            PlayerActionEffectData effect = _effects[i];
            if (effect != null && effect.EffectType == PlayerActionEffectType.Unavailable &&
                !effect.isDeferred && effect.remainingCount > 0 && effect.isNoticePending)
            {
                return effect;
            }
        }

        return null;
    }

    public PlayerActionEffectData PeekUnavailable()
    {
        for (int i = 0; i < _effects.Count; i++)
        {
            PlayerActionEffectData effect = _effects[i];
            if (effect != null && effect.EffectType == PlayerActionEffectType.Unavailable &&
                !effect.isDeferred && effect.remainingCount > 0)
            {
                return effect;
            }
        }

        return null;
    }

    public void ConsumeUnavailable(PlayerActionEffectData effect)
    {
        if (effect == null || effect.remainingCount <= 0) return;
        effect.remainingCount--;
        RemoveCompletedEffects();
    }

    public void CompleteNotice(PlayerActionEffectData effect)
    {
        if (effect == null) return;
        effect.isNoticePending = false;
        RemoveCompletedEffects();
    }

    public void Remove(PlayerActionEffectData effect)
    {
        if (effect != null) _effects.Remove(effect);
    }

    public int RemoveExpired(int currentDay, Func<string, bool> hasItem)
    {
        int removedUnavailableCount = 0;
        for (int i = _effects.Count - 1; i >= 0; i--)
        {
            PlayerActionEffectData effect = _effects[i];
            bool shouldKeep = effect != null && (effect.createdDay == currentDay ||
                effect.EffectType == PlayerActionEffectType.Unavailable &&
                effect.Persistence == PlayerActionEffectPersistence.WhileItemOwned &&
                hasItem != null && hasItem(effect.sourceId));
            if (shouldKeep) continue;

            if (effect != null && effect.EffectType == PlayerActionEffectType.Unavailable)
                removedUnavailableCount += Math.Max(0, effect.remainingCount);
            _effects.RemoveAt(i);
        }

        return removedUnavailableCount;
    }

    public List<PlayerActionEffectData> Capture()
    {
        List<PlayerActionEffectData> result = new List<PlayerActionEffectData>(_effects.Count);
        for (int i = 0; i < _effects.Count; i++)
        {
            if (_effects[i] != null) result.Add(_effects[i].Clone());
        }

        return result;
    }

    public void Restore(List<PlayerActionEffectData> effects)
    {
        _effects.Clear();
        if (effects == null) return;
        for (int i = 0; i < effects.Count; i++)
        {
            PlayerActionEffectData effect = effects[i];
            if (effect == null || effect.totalCount <= 0) continue;
            _effects.Add(effect.Clone());
        }
    }

    private void RemoveCompletedEffects()
    {
        for (int i = _effects.Count - 1; i >= 0; i--)
        {
            PlayerActionEffectData effect = _effects[i];
            if (effect == null ||
                (effect.EffectType == PlayerActionEffectType.Additional && !effect.isNoticePending) ||
                (effect.EffectType == PlayerActionEffectType.Unavailable && effect.remainingCount <= 0))
            {
                _effects.RemoveAt(i);
            }
        }
    }
}
