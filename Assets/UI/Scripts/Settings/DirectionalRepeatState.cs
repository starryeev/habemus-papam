using System;

public sealed class DirectionalRepeatState
{
    private readonly float _holdDelay;
    private readonly float _repeatInterval;

    private int _direction;
    private float _heldDuration;
    private float _repeatAccumulator;
    private bool _hasEnteredRepeat;

    public DirectionalRepeatState(float holdDelay, float repeatInterval)
    {
        if (holdDelay < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(holdDelay));
        }

        if (repeatInterval <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(repeatInterval));
        }

        _holdDelay = holdDelay;
        _repeatInterval = repeatInterval;
    }

    public int Tick(int direction, bool wasPressedThisFrame, float unscaledDeltaTime)
    {
        int normalizedDirection = Math.Sign(direction);
        if (normalizedDirection == 0)
        {
            Reset();
            return 0;
        }

        if (wasPressedThisFrame || normalizedDirection != _direction)
        {
            _direction = normalizedDirection;
            _heldDuration = 0f;
            _repeatAccumulator = 0f;
            _hasEnteredRepeat = false;
            return _direction;
        }

        _heldDuration += Math.Max(0f, unscaledDeltaTime);
        if (_heldDuration < _holdDelay)
        {
            return 0;
        }

        if (!_hasEnteredRepeat)
        {
            _hasEnteredRepeat = true;
            _repeatAccumulator = _heldDuration - _holdDelay;
            return _direction;
        }

        _repeatAccumulator += Math.Max(0f, unscaledDeltaTime);
        int repeatCount = (int)(_repeatAccumulator / _repeatInterval);
        if (repeatCount == 0)
        {
            return 0;
        }

        _repeatAccumulator -= repeatCount * _repeatInterval;
        return _direction * repeatCount;
    }

    public void Reset()
    {
        _direction = 0;
        _heldDuration = 0f;
        _repeatAccumulator = 0f;
        _hasEnteredRepeat = false;
    }
}
