using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

public sealed class SettingsKeyboardNavigationBindings
{
    public GameObject SettingsPanel;
    public GameObject ConfirmPopup;
    public Button PopupConfirmButton;
    public Button PopupCancelButton;
    public GameObject HowToPlayPanel;
    public ScrollRect SettingsScrollRect;
    public Sprite SelectionArrowSprite;
    public Image SelectionLeftArrow;
    public Image SelectionRightArrow;
    public Button CloseSettingsButton;
    public VolumeSet MasterVolume;
    public VolumeSet BgmVolume;
    public VolumeSet SfxVolume;
    public Button UpKey;
    public Button LeftKey;
    public Button DownKey;
    public Button RightKey;
    public Button ResetHotKeysButton;
    public Button NewGameButton;
    public Button QuitGameButton;
    public Button HowToPlayButton;
    public Button CloseHowToPlayButton;
}

public sealed class SettingsKeyboardNavigator
{
    private const float VolumeHoldDelay = 0.2f;
    private const float VolumeRepeatInterval = 0.025f;
    private const float PulseDarkDuration = 1f;
    private const float PulseCycleDuration = 2f;
    private const float PulseDarkenAmount = 0.35f;
    private const float RebindBlinkCycle = 1f;

    private static readonly SettingsNavigationTarget[] VerticalTargets =
    {
        SettingsNavigationTarget.CloseSettings,
        SettingsNavigationTarget.MasterSlider,
        SettingsNavigationTarget.BgmSlider,
        SettingsNavigationTarget.SfxSlider,
        SettingsNavigationTarget.MoveUp,
        SettingsNavigationTarget.MoveLeft,
        SettingsNavigationTarget.MoveDown,
        SettingsNavigationTarget.MoveRight,
        SettingsNavigationTarget.ResetHotKeys,
        SettingsNavigationTarget.HowToPlay
    };

    private static readonly SettingsNavigationTarget[] BottomTargets =
    {
        SettingsNavigationTarget.NewGame,
        SettingsNavigationTarget.HowToPlay,
        SettingsNavigationTarget.QuitGame
    };

    private sealed class NavigationItem
    {
        public SettingsNavigationTarget Target;
        public RectTransform Rect;
        public Button Button;
        public VolumeSet VolumeSet;
        public VolumeControlTarget VolumeTarget;
        public bool UsesPulse;
        public bool IsAvailable => Rect != null && Rect.gameObject.activeInHierarchy;
    }

    private readonly SettingsUI _owner;
    private readonly SettingsKeyboardNavigationBindings _bindings;
    private readonly SettingsNavigationState _state = new SettingsNavigationState();
    private readonly DirectionalRepeatState _volumeRepeatState =
        new DirectionalRepeatState(VolumeHoldDelay, VolumeRepeatInterval);
    private readonly Dictionary<SettingsNavigationTarget, NavigationItem> _items =
        new Dictionary<SettingsNavigationTarget, NavigationItem>();
    private readonly SettingsSelectionIndicator _selectionIndicator;

    private Graphic _pulseGraphic;
    private Color _pulseBaseColor;
    private float _pulseStartedAt;
    private Graphic _rebindGraphic;
    private Color _rebindBaseColor;
    private float _candidateBlinkStartedAt;
    private int _activatedFrame = -1;
    private bool _shouldUpdateArrowPosition;

    public bool DidConsumeEscapeThisFrame { get; private set; }
    public bool IsRebinding => _state.IsRebinding;

    public SettingsKeyboardNavigator(
        SettingsUI owner,
        SettingsKeyboardNavigationBindings bindings)
    {
        _owner = owner;
        _bindings = bindings;

        RectTransform indicatorLayer = bindings.SettingsPanel != null
            ? bindings.SettingsPanel.transform as RectTransform
            : null;
        _selectionIndicator = new SettingsSelectionIndicator(
            indicatorLayer,
            bindings.SelectionArrowSprite,
            bindings.SelectionLeftArrow,
            bindings.SelectionRightArrow);

        RegisterItems();
    }

    public void Activate()
    {
        CancelInteraction();
        SettingsNavigationTarget initialTarget = IsAvailable(SettingsNavigationTarget.CloseSettings)
            ? SettingsNavigationTarget.CloseSettings
            : FindFirstAvailableTarget();
        _state.Activate(initialTarget);
        _activatedFrame = Time.frameCount;
        _shouldUpdateArrowPosition = true;
        _volumeRepeatState.Reset();
        ClearEventSystemSelection();
        EnsureVisible(GetItem(initialTarget));
        UpdateSelectionVisual();
    }

    public void Deactivate()
    {
        CancelInteraction();
        _state.Deactivate();
        _volumeRepeatState.Reset();
        SetPulseGraphic(null);
        _selectionIndicator.Hide();
        SetAllVolumeEditingVisuals(null);
    }

    public void Tick()
    {
        DidConsumeEscapeThisFrame = false;

        if (_bindings.SettingsPanel == null || !_bindings.SettingsPanel.activeInHierarchy)
        {
            if (_state.IsActive)
            {
                Deactivate();
            }

            return;
        }

        if (_activatedFrame == Time.frameCount)
        {
            UpdateSelectionVisual();
            return;
        }

        if (IsConfirmPopupOpen())
        {
            HandleConfirmPopup();
            return;
        }

        if (_state.IsPopupNavigating)
        {
            _state.ReturnToNavigation();
            SetPulseGraphic(null);
        }

        if (_bindings.HowToPlayPanel != null && _bindings.HowToPlayPanel.activeInHierarchy)
        {
            HandleHowToPlayPanel();
            return;
        }

        if (!_state.IsActive)
        {
            Activate();
        }
        else if (_state.SelectedTarget == SettingsNavigationTarget.CloseHowToPlay)
        {
            _state.Activate(SettingsNavigationTarget.HowToPlay);
            _shouldUpdateArrowPosition = true;
            EnsureVisible(GetItem(SettingsNavigationTarget.HowToPlay));
        }

        EnsureCurrentTargetIsAvailable();

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            UpdateSelectionVisual();
            return;
        }

        switch (_state.Mode)
        {
            case SettingsNavigationMode.VolumeEditing:
                HandleVolumeEditing(keyboard);
                break;
            case SettingsNavigationMode.HotKeyRebinding:
                HandleHotKeyRebinding(keyboard);
                break;
            default:
                HandleNavigation(keyboard);
                break;
        }

        if (_bindings.SettingsPanel != null && _bindings.SettingsPanel.activeInHierarchy)
        {
            UpdateSelectionVisual();
        }
    }

    public void BeginHotKeyRebind(HotKeyAction action)
    {
        if (_bindings.SettingsPanel == null || !_bindings.SettingsPanel.activeInHierarchy)
        {
            return;
        }

        SettingsNavigationTarget target = GetTarget(action);
        NavigationItem item = GetItem(target);
        if (item == null || !item.IsAvailable)
        {
            return;
        }

        CancelInteraction();
        _owner.PrepareHotKeyRebind();
        _state.BeginHotKeyRebinding(target, action);
        _volumeRepeatState.Reset();
        ClearEventSystemSelection();

        _rebindGraphic = item.Button != null ? item.Button.targetGraphic : null;
        if (_rebindGraphic != null)
        {
            _rebindBaseColor = _rebindGraphic.color;
            _rebindGraphic.color = Color.Lerp(_rebindBaseColor, Color.black, PulseDarkenAmount);
        }

        _owner.SetHotKeyButtonPreview(action, SettingsManager.Instance?.GetHotKey(action) ?? Key.None, 0f);
        EnsureVisible(item);
    }

    public void CancelCurrentInteraction()
    {
        CancelInteraction();
        UpdateSelectionVisual();
    }

    private void RegisterItems()
    {
        AddButton(SettingsNavigationTarget.CloseSettings, _bindings.CloseSettingsButton, true);
        AddVolume(SettingsNavigationTarget.MasterSlider, _bindings.MasterVolume, VolumeControlTarget.Slider);
        AddVolume(SettingsNavigationTarget.MasterMute, _bindings.MasterVolume, VolumeControlTarget.Mute);
        AddVolume(SettingsNavigationTarget.BgmSlider, _bindings.BgmVolume, VolumeControlTarget.Slider);
        AddVolume(SettingsNavigationTarget.BgmMute, _bindings.BgmVolume, VolumeControlTarget.Mute);
        AddVolume(SettingsNavigationTarget.SfxSlider, _bindings.SfxVolume, VolumeControlTarget.Slider);
        AddVolume(SettingsNavigationTarget.SfxMute, _bindings.SfxVolume, VolumeControlTarget.Mute);
        AddButton(SettingsNavigationTarget.MoveUp, _bindings.UpKey, false);
        AddButton(SettingsNavigationTarget.MoveLeft, _bindings.LeftKey, false);
        AddButton(SettingsNavigationTarget.MoveDown, _bindings.DownKey, false);
        AddButton(SettingsNavigationTarget.MoveRight, _bindings.RightKey, false);
        AddButton(SettingsNavigationTarget.ResetHotKeys, _bindings.ResetHotKeysButton, false);
        AddButton(SettingsNavigationTarget.NewGame, _bindings.NewGameButton, true);
        AddButton(SettingsNavigationTarget.HowToPlay, _bindings.HowToPlayButton, true);
        AddButton(SettingsNavigationTarget.QuitGame, _bindings.QuitGameButton, true);
        AddButton(SettingsNavigationTarget.CloseHowToPlay, _bindings.CloseHowToPlayButton, true);
    }

    private void AddButton(SettingsNavigationTarget target, Button button, bool usesPulse)
    {
        _items[target] = new NavigationItem
        {
            Target = target,
            Rect = button != null ? button.transform as RectTransform : null,
            Button = button,
            UsesPulse = usesPulse
        };
    }

    private void AddVolume(
        SettingsNavigationTarget target,
        VolumeSet volumeSet,
        VolumeControlTarget volumeTarget)
    {
        _items[target] = new NavigationItem
        {
            Target = target,
            Rect = volumeSet != null ? volumeSet.transform as RectTransform : null,
            VolumeSet = volumeSet,
            VolumeTarget = volumeTarget
        };
    }

    private void HandleHowToPlayPanel()
    {
        if (!_state.IsActive || _state.SelectedTarget != SettingsNavigationTarget.CloseHowToPlay)
        {
            CancelInteraction();
            _state.Activate(SettingsNavigationTarget.CloseHowToPlay);
            _shouldUpdateArrowPosition = true;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && IsEnterPressed(keyboard))
        {
            InvokeSelectedButton();
        }

        if (_bindings.HowToPlayPanel != null && _bindings.HowToPlayPanel.activeInHierarchy)
        {
            UpdateSelectionVisual();
        }
    }

    private void HandleConfirmPopup()
    {
        if (!_state.IsPopupNavigating)
        {
            CancelInteraction();
            if (!_state.IsActive)
            {
                _state.Activate(FindFirstAvailableTarget());
            }

            SettingsPopupTarget initialTarget = IsPopupButtonAvailable(_bindings.PopupCancelButton)
                ? SettingsPopupTarget.Cancel
                : SettingsPopupTarget.Confirm;
            _state.BeginPopupNavigation(initialTarget);
            ClearEventSystemSelection();
        }

        EnsurePopupTargetIsAvailable();
        _selectionIndicator.Hide();
        SetAllVolumeEditingVisuals(null);

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                DidConsumeEscapeThisFrame = true;
                InvokePopupButton(SettingsPopupTarget.Cancel);
                return;
            }

            if (keyboard.leftArrowKey.wasPressedThisFrame &&
                IsPopupButtonAvailable(_bindings.PopupConfirmButton))
            {
                _state.SelectPopupTarget(SettingsPopupTarget.Confirm);
            }
            else if (keyboard.rightArrowKey.wasPressedThisFrame &&
                     IsPopupButtonAvailable(_bindings.PopupCancelButton))
            {
                _state.SelectPopupTarget(SettingsPopupTarget.Cancel);
            }
            else if (IsEnterPressed(keyboard))
            {
                InvokePopupButton(_state.PopupTarget);
                return;
            }
        }

        UpdatePopupSelectionVisual();
    }

    private void InvokePopupButton(SettingsPopupTarget target)
    {
        Button button = GetPopupButton(target);
        if (!IsPopupButtonAvailable(button))
        {
            return;
        }

        SetPulseGraphic(null);
        ClearEventSystemSelection();
        button.onClick.Invoke();

        if (!IsConfirmPopupOpen())
        {
            _state.ReturnToNavigation();
            UpdateSelectionVisual();
        }
    }

    private void EnsurePopupTargetIsAvailable()
    {
        if (IsPopupButtonAvailable(GetPopupButton(_state.PopupTarget)))
        {
            return;
        }

        SettingsPopupTarget fallback = IsPopupButtonAvailable(_bindings.PopupCancelButton)
            ? SettingsPopupTarget.Cancel
            : SettingsPopupTarget.Confirm;
        _state.SelectPopupTarget(fallback);
    }

    private void UpdatePopupSelectionVisual()
    {
        Button selectedButton = GetPopupButton(_state.PopupTarget);
        SetPulseGraphic(selectedButton != null ? selectedButton.targetGraphic : null);
        UpdatePulseColor();
    }

    private Button GetPopupButton(SettingsPopupTarget target)
    {
        return target == SettingsPopupTarget.Confirm
            ? _bindings.PopupConfirmButton
            : _bindings.PopupCancelButton;
    }

    private bool IsConfirmPopupOpen()
    {
        return _bindings.ConfirmPopup != null && _bindings.ConfirmPopup.activeInHierarchy;
    }

    private static bool IsPopupButtonAvailable(Button button)
    {
        return button != null && button.gameObject.activeInHierarchy && button.interactable;
    }

    private void HandleNavigation(Keyboard keyboard)
    {
        if (IsEnterPressed(keyboard))
        {
            ActivateSelectedItem();
            return;
        }

        SettingsNavigationTarget nextTarget = _state.SelectedTarget;
        bool hasTarget = false;
        if (keyboard.upArrowKey.wasPressedThisFrame)
        {
            hasTarget = TryGetVerticalTarget(-1, out nextTarget);
        }
        else if (keyboard.downArrowKey.wasPressedThisFrame)
        {
            hasTarget = TryGetVerticalTarget(1, out nextTarget);
        }
        else if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            hasTarget = TryGetHorizontalTarget(-1, out nextTarget);
        }
        else if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            hasTarget = TryGetHorizontalTarget(1, out nextTarget);
        }

        if (hasTarget && _state.Select(nextTarget))
        {
            _shouldUpdateArrowPosition = true;
            ClearEventSystemSelection();
            EnsureVisible(GetItem(nextTarget));
        }
    }

    private void ActivateSelectedItem()
    {
        NavigationItem item = GetItem(_state.SelectedTarget);
        if (item == null || !item.IsAvailable)
        {
            return;
        }

        ClearEventSystemSelection();

        if (item.VolumeSet != null)
        {
            if (item.VolumeTarget == VolumeControlTarget.Mute)
            {
                item.VolumeSet.ToggleMute();
                return;
            }

            if (_state.BeginVolumeEditing(item.VolumeSet.CanEditVolume))
            {
                _volumeRepeatState.Reset();
            }

            return;
        }

        if (TryGetHotKeyAction(item.Target, out HotKeyAction action))
        {
            BeginHotKeyRebind(action);
            return;
        }

        InvokeSelectedButton();
    }

    private void InvokeSelectedButton()
    {
        NavigationItem item = GetItem(_state.SelectedTarget);
        if (item?.Button != null && item.Button.interactable)
        {
            ClearEventSystemSelection();
            item.Button.onClick.Invoke();
        }
    }

    private void HandleVolumeEditing(Keyboard keyboard)
    {
        NavigationItem item = GetItem(_state.SelectedTarget);
        if (item?.VolumeSet == null || !item.VolumeSet.CanEditVolume)
        {
            _state.ReturnToNavigation();
            _volumeRepeatState.Reset();
            return;
        }

        if (IsEnterPressed(keyboard))
        {
            _state.ReturnToNavigation();
            _volumeRepeatState.Reset();
            return;
        }

        int direction =
            (keyboard.rightArrowKey.isPressed ? 1 : 0) -
            (keyboard.leftArrowKey.isPressed ? 1 : 0);
        bool wasPressedThisFrame =
            keyboard.rightArrowKey.wasPressedThisFrame ||
            keyboard.leftArrowKey.wasPressedThisFrame;
        int adjustment = _volumeRepeatState.Tick(
            direction,
            wasPressedThisFrame,
            Time.unscaledDeltaTime);

        if (adjustment != 0)
        {
            ClearEventSystemSelection();
            item.VolumeSet.AdjustVolume(adjustment);
        }
    }

    private void HandleHotKeyRebinding(Keyboard keyboard)
    {
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            DidConsumeEscapeThisFrame = true;
            EndHotKeyRebind(false);
            return;
        }

        if (IsEnterPressed(keyboard))
        {
            EndHotKeyRebind(true);
            return;
        }

        if (TryGetSupportedPressedKey(keyboard, out Key pressedKey))
        {
            _state.SetCandidateKey(pressedKey);
            _candidateBlinkStartedAt = Time.unscaledTime;
        }

        Key displayKey = _state.CandidateKey != Key.None
            ? _state.CandidateKey
            : SettingsManager.Instance?.GetHotKey(_state.RebindingAction) ?? Key.None;
        float blinkAlpha = _state.CandidateKey == Key.None
            ? 0f
            : (Mathf.Repeat(Time.unscaledTime - _candidateBlinkStartedAt, RebindBlinkCycle) <
               RebindBlinkCycle * 0.5f ? 1f : 0.25f);
        _owner.SetHotKeyButtonPreview(_state.RebindingAction, displayKey, blinkAlpha);
    }

    private void EndHotKeyRebind(bool shouldCommit)
    {
        if (!_state.IsRebinding)
        {
            return;
        }

        HotKeyAction action = _state.RebindingAction;
        Key candidateKey = _state.CandidateKey;
        RestoreRebindGraphic();
        _state.ReturnToNavigation();
        _owner.SyncHotKeyButtonsFromManager();

        if (shouldCommit && candidateKey != Key.None)
        {
            _owner.CommitHotKeyChange(action, candidateKey);
        }
    }

    private void CancelInteraction()
    {
        if (_state.IsRebinding)
        {
            RestoreRebindGraphic();
            _state.ReturnToNavigation();
            _owner.SyncHotKeyButtonsFromManager();
        }

        if (_state.Mode == SettingsNavigationMode.VolumeEditing)
        {
            _state.ReturnToNavigation();
        }

        if (_state.IsPopupNavigating)
        {
            _state.ReturnToNavigation();
            SetPulseGraphic(null);
        }

        _volumeRepeatState.Reset();
        SetAllVolumeEditingVisuals(null);
    }

    private void RestoreRebindGraphic()
    {
        if (_rebindGraphic != null)
        {
            _rebindGraphic.color = _rebindBaseColor;
            _rebindGraphic = null;
        }
    }

    private bool TryGetVerticalTarget(int direction, out SettingsNavigationTarget target)
    {
        if (TryGetVolumePosition(_state.SelectedTarget, out int row, out VolumeControlTarget control))
        {
            int nextRow = row + direction;
            if (nextRow < 0)
            {
                target = SettingsNavigationTarget.CloseSettings;
                return IsAvailable(target);
            }

            if (nextRow > 2)
            {
                target = SettingsNavigationTarget.MoveUp;
                return IsAvailable(target);
            }

            target = GetVolumeTarget(nextRow, control);
            return IsAvailable(target);
        }

        SettingsNavigationTarget currentTarget = IsBottomTarget(_state.SelectedTarget)
            ? SettingsNavigationTarget.HowToPlay
            : _state.SelectedTarget;
        int currentIndex = -1;
        for (int index = 0; index < VerticalTargets.Length; index++)
        {
            if (VerticalTargets[index] == currentTarget)
            {
                currentIndex = index;
                break;
            }
        }

        if (currentIndex < 0)
        {
            target = _state.SelectedTarget;
            return false;
        }

        for (int index = currentIndex + direction;
             index >= 0 && index < VerticalTargets.Length;
             index += direction)
        {
            if (IsAvailable(VerticalTargets[index]))
            {
                target = VerticalTargets[index];
                return true;
            }
        }

        target = _state.SelectedTarget;
        return false;
    }

    private bool TryGetHorizontalTarget(int direction, out SettingsNavigationTarget target)
    {
        if (TryGetVolumePosition(_state.SelectedTarget, out _, out _))
        {
            return TryGetHorizontalVolumeTarget(direction, out target);
        }

        return TryGetBottomTarget(direction, out target);
    }

    private bool TryGetBottomTarget(int direction, out SettingsNavigationTarget target)
    {
        int currentIndex = GetBottomTargetIndex(_state.SelectedTarget);
        if (currentIndex < 0)
        {
            target = _state.SelectedTarget;
            return false;
        }

        int step = direction > 0 ? 1 : -1;
        for (int index = currentIndex + step;
             index >= 0 && index < BottomTargets.Length;
             index += step)
        {
            if (IsAvailable(BottomTargets[index]))
            {
                target = BottomTargets[index];
                return true;
            }
        }

        target = _state.SelectedTarget;
        return false;
    }

    private bool TryGetHorizontalVolumeTarget(int direction, out SettingsNavigationTarget target)
    {
        if (!TryGetVolumePosition(_state.SelectedTarget, out int row, out VolumeControlTarget control))
        {
            target = _state.SelectedTarget;
            return false;
        }

        VolumeControlTarget nextControl = direction > 0
            ? VolumeControlTarget.Mute
            : VolumeControlTarget.Slider;
        if (nextControl == control)
        {
            target = _state.SelectedTarget;
            return false;
        }

        target = GetVolumeTarget(row, nextControl);
        return IsAvailable(target);
    }

    private void EnsureCurrentTargetIsAvailable()
    {
        if (IsAvailable(_state.SelectedTarget))
        {
            return;
        }

        if (IsBottomTarget(_state.SelectedTarget) && IsAvailable(SettingsNavigationTarget.HowToPlay))
        {
            _state.Select(SettingsNavigationTarget.HowToPlay);
            _shouldUpdateArrowPosition = true;
            EnsureVisible(GetItem(SettingsNavigationTarget.HowToPlay));
            return;
        }

        SettingsNavigationTarget fallback = FindFirstAvailableTarget();
        _state.Select(fallback);
        _shouldUpdateArrowPosition = true;
        EnsureVisible(GetItem(fallback));
    }

    private static bool IsBottomTarget(SettingsNavigationTarget target)
    {
        return GetBottomTargetIndex(target) >= 0;
    }

    private static int GetBottomTargetIndex(SettingsNavigationTarget target)
    {
        for (int index = 0; index < BottomTargets.Length; index++)
        {
            if (BottomTargets[index] == target)
            {
                return index;
            }
        }

        return -1;
    }

    private SettingsNavigationTarget FindFirstAvailableTarget()
    {
        for (int index = 0; index < VerticalTargets.Length; index++)
        {
            if (IsAvailable(VerticalTargets[index]))
            {
                return VerticalTargets[index];
            }
        }

        return SettingsNavigationTarget.MasterSlider;
    }

    private void UpdateSelectionVisual()
    {
        bool shouldUpdateArrowPosition = _shouldUpdateArrowPosition;
        _shouldUpdateArrowPosition = false;

        if (!_state.IsActive)
        {
            _selectionIndicator.Hide();
            SetPulseGraphic(null);
            SetAllVolumeEditingVisuals(null);
            return;
        }

        if (IsConfirmPopupOpen() || _state.IsPopupNavigating)
        {
            _selectionIndicator.Hide();
            SetPulseGraphic(null);
            SetAllVolumeEditingVisuals(null);
            return;
        }

        NavigationItem item = GetItem(_state.SelectedTarget);
        if (item == null || !item.IsAvailable || _state.IsRebinding)
        {
            _selectionIndicator.Hide();
            SetPulseGraphic(null);
            SetAllVolumeEditingVisuals(null);
            return;
        }

        SetAllVolumeEditingVisuals(
            _state.Mode == SettingsNavigationMode.VolumeEditing ? item.VolumeSet : null);

        if (item.UsesPulse)
        {
            _selectionIndicator.Hide();
            SetPulseGraphic(item.Button != null ? item.Button.targetGraphic : null);
            UpdatePulseColor();
            return;
        }

        SetPulseGraphic(null);
        if (item.VolumeSet != null)
        {
            _selectionIndicator.Show(
                item.VolumeSet,
                item.VolumeTarget,
                shouldUpdateArrowPosition);
        }
        else
        {
            _selectionIndicator.Show(item.Rect, shouldUpdateArrowPosition);
        }
    }

    private void SetAllVolumeEditingVisuals(VolumeSet editingVolume)
    {
        _bindings.MasterVolume?.SetEditingVisual(_bindings.MasterVolume == editingVolume);
        _bindings.BgmVolume?.SetEditingVisual(_bindings.BgmVolume == editingVolume);
        _bindings.SfxVolume?.SetEditingVisual(_bindings.SfxVolume == editingVolume);
    }

    private void SetPulseGraphic(Graphic graphic)
    {
        if (_pulseGraphic == graphic)
        {
            return;
        }

        if (_pulseGraphic != null)
        {
            _pulseGraphic.color = _pulseBaseColor;
        }

        _pulseGraphic = graphic;
        if (_pulseGraphic != null)
        {
            _pulseBaseColor = _pulseGraphic.color;
            _pulseStartedAt = Time.unscaledTime;
        }
    }

    private void UpdatePulseColor()
    {
        if (_pulseGraphic == null)
        {
            return;
        }

        _pulseGraphic.color = SettingsPulseColorEvaluator.Evaluate(
            _pulseBaseColor,
            Time.unscaledTime - _pulseStartedAt,
            PulseDarkDuration,
            PulseCycleDuration - PulseDarkDuration,
            PulseDarkenAmount);
    }

    private void EnsureVisible(NavigationItem item)
    {
        ScrollRect scrollRect = _bindings.SettingsScrollRect;
        if (item?.Rect == null || scrollRect?.content == null || scrollRect.viewport == null ||
            !item.Rect.IsChildOf(scrollRect.content))
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        Bounds targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
            scrollRect.viewport,
            item.Rect);
        Rect viewportRect = scrollRect.viewport.rect;
        Vector2 contentPosition = scrollRect.content.anchoredPosition;

        if (targetBounds.max.y > viewportRect.yMax)
        {
            contentPosition.y -= targetBounds.max.y - viewportRect.yMax;
        }
        else if (targetBounds.min.y < viewportRect.yMin)
        {
            contentPosition.y += viewportRect.yMin - targetBounds.min.y;
        }

        scrollRect.StopMovement();
        scrollRect.content.anchoredPosition = contentPosition;
    }

    private NavigationItem GetItem(SettingsNavigationTarget target)
    {
        return _items.TryGetValue(target, out NavigationItem item) ? item : null;
    }

    private bool IsAvailable(SettingsNavigationTarget target)
    {
        NavigationItem item = GetItem(target);
        return item != null && item.IsAvailable;
    }

    private static bool IsEnterPressed(Keyboard keyboard)
    {
        return keyboard.enterKey.wasPressedThisFrame ||
               keyboard.numpadEnterKey.wasPressedThisFrame;
    }

    private static bool TryGetSupportedPressedKey(Keyboard keyboard, out Key pressedKey)
    {
        if (keyboard == null)
        {
            pressedKey = Key.None;
            return false;
        }

        foreach (KeyControl keyControl in keyboard.allKeys)
        {
            if (keyControl == null)
            {
                continue;
            }

            if (keyControl.wasPressedThisFrame &&
                keyControl.keyCode >= Key.A &&
                keyControl.keyCode <= Key.Z)
            {
                pressedKey = keyControl.keyCode;
                return true;
            }
        }

        pressedKey = Key.None;
        return false;
    }

    private static bool TryGetVolumePosition(
        SettingsNavigationTarget target,
        out int row,
        out VolumeControlTarget control)
    {
        switch (target)
        {
            case SettingsNavigationTarget.MasterSlider:
                row = 0;
                control = VolumeControlTarget.Slider;
                return true;
            case SettingsNavigationTarget.MasterMute:
                row = 0;
                control = VolumeControlTarget.Mute;
                return true;
            case SettingsNavigationTarget.BgmSlider:
                row = 1;
                control = VolumeControlTarget.Slider;
                return true;
            case SettingsNavigationTarget.BgmMute:
                row = 1;
                control = VolumeControlTarget.Mute;
                return true;
            case SettingsNavigationTarget.SfxSlider:
                row = 2;
                control = VolumeControlTarget.Slider;
                return true;
            case SettingsNavigationTarget.SfxMute:
                row = 2;
                control = VolumeControlTarget.Mute;
                return true;
            default:
                row = -1;
                control = VolumeControlTarget.Slider;
                return false;
        }
    }

    private static SettingsNavigationTarget GetVolumeTarget(int row, VolumeControlTarget control)
    {
        if (row == 0)
        {
            return control == VolumeControlTarget.Slider
                ? SettingsNavigationTarget.MasterSlider
                : SettingsNavigationTarget.MasterMute;
        }

        if (row == 1)
        {
            return control == VolumeControlTarget.Slider
                ? SettingsNavigationTarget.BgmSlider
                : SettingsNavigationTarget.BgmMute;
        }

        return control == VolumeControlTarget.Slider
            ? SettingsNavigationTarget.SfxSlider
            : SettingsNavigationTarget.SfxMute;
    }

    private static SettingsNavigationTarget GetTarget(HotKeyAction action)
    {
        switch (action)
        {
            case HotKeyAction.MoveUp:
                return SettingsNavigationTarget.MoveUp;
            case HotKeyAction.MoveLeft:
                return SettingsNavigationTarget.MoveLeft;
            case HotKeyAction.MoveDown:
                return SettingsNavigationTarget.MoveDown;
            default:
                return SettingsNavigationTarget.MoveRight;
        }
    }

    private static bool TryGetHotKeyAction(
        SettingsNavigationTarget target,
        out HotKeyAction action)
    {
        switch (target)
        {
            case SettingsNavigationTarget.MoveUp:
                action = HotKeyAction.MoveUp;
                return true;
            case SettingsNavigationTarget.MoveLeft:
                action = HotKeyAction.MoveLeft;
                return true;
            case SettingsNavigationTarget.MoveDown:
                action = HotKeyAction.MoveDown;
                return true;
            case SettingsNavigationTarget.MoveRight:
                action = HotKeyAction.MoveRight;
                return true;
            default:
                action = default;
                return false;
        }
    }

    private static void ClearEventSystemSelection()
    {
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
