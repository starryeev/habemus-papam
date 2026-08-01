using System;

public enum VolumeControlTarget
{
    Slider,
    Mute
}

public sealed class VolumeNavigationState
{
    private readonly int _rowCount;

    public int SelectedRow { get; private set; }
    public VolumeControlTarget SelectedTarget { get; private set; }
    public bool IsEditing { get; private set; }

    public VolumeNavigationState(int rowCount)
    {
        if (rowCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rowCount));
        }

        _rowCount = rowCount;
        Reset();
    }

    public void Reset()
    {
        SelectedRow = 0;
        SelectedTarget = VolumeControlTarget.Slider;
        IsEditing = false;
    }

    public bool MoveVertical(int direction)
    {
        if (IsEditing || direction == 0)
        {
            return false;
        }

        int nextRow = Math.Max(0, Math.Min(SelectedRow + Math.Sign(direction), _rowCount - 1));
        if (nextRow == SelectedRow)
        {
            return false;
        }

        SelectedRow = nextRow;
        return true;
    }

    public bool MoveHorizontal(int direction)
    {
        if (IsEditing || direction == 0)
        {
            return false;
        }

        VolumeControlTarget nextTarget = direction > 0
            ? VolumeControlTarget.Mute
            : VolumeControlTarget.Slider;

        if (nextTarget == SelectedTarget)
        {
            return false;
        }

        SelectedTarget = nextTarget;
        return true;
    }

    public bool ToggleEditing(bool canStartEditing)
    {
        if (SelectedTarget != VolumeControlTarget.Slider)
        {
            return false;
        }

        if (!IsEditing && !canStartEditing)
        {
            return false;
        }

        IsEditing = !IsEditing;
        return true;
    }

    public void ExitEditing()
    {
        IsEditing = false;
    }
}
