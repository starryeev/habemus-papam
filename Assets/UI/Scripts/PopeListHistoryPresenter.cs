using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopeListHistoryPresenter : MonoBehaviour
{
    private const int CenterFrameNumber = 3;
    private const string PortraitCatalogResourcePath = "UI/PopePortraitCatalog";
    private static readonly Regex RegnalSuffixPattern = new Regex(@"\s*\d+\s*세\s*$");

    private readonly List<FrameBinding> frames = new List<FrameBinding>();
    private readonly List<PapalElectionRecordSaveData> history = new List<PapalElectionRecordSaveData>();
    private readonly List<string> displayNames = new List<string>();

    private PopePortraitCatalog portraitCatalog;
    private Button leftButton;
    private Button rightButton;
    private int centerIndex = -1;

    public bool IsBrowsing { get; private set; }
    public Sprite CurrentCenterPortrait { get; private set; }

    public void Initialize(
        IReadOnlyList<Button> frameButtons,
        Button resolvedLeftButton,
        Button resolvedRightButton)
    {
        if (portraitCatalog == null)
        {
            portraitCatalog = Resources.Load<PopePortraitCatalog>(PortraitCatalogResourcePath);
        }

        leftButton = resolvedLeftButton;
        rightButton = resolvedRightButton;
        ResolveFrames(frameButtons);
        ResetToLatest();
        ExitBrowseMode();
    }

    public void ResetToLatest()
    {
        RefreshHistory();
        centerIndex = history.Count - 1;
        Render();
    }

    public void EnterBrowseMode()
    {
        if (history.Count == 0)
        {
            ExitBrowseMode();
            return;
        }

        IsBrowsing = true;
        SetArrowObjectsActive(true);
        UpdateArrowState();
    }

    public void ExitBrowseMode()
    {
        IsBrowsing = false;
        SetArrowObjectsActive(false);
    }

    public void MoveLeft()
    {
        if (!IsBrowsing || centerIndex >= history.Count - 1)
        {
            return;
        }

        centerIndex++;
        Render();
    }

    public void MoveRight()
    {
        if (!IsBrowsing || centerIndex <= 0)
        {
            return;
        }

        centerIndex--;
        Render();
    }

    private void RefreshHistory()
    {
        history.Clear();

        if (ActionRecordManager.Instance != null)
        {
            IReadOnlyList<PapalElectionRecordSaveData> records =
                ActionRecordManager.Instance.GetPersistentPapalElectionHistory();

            foreach (PapalElectionRecordSaveData record in records)
            {
                if (record != null)
                {
                    history.Add(record);
                }
            }
        }

        BuildDisplayNames();
    }

    private void ResolveFrames(IReadOnlyList<Button> frameButtons)
    {
        frames.Clear();

        for (int frameNumber = 1; frameNumber <= 5; frameNumber++)
        {
            Transform frameTransform = transform.Find($"Frame{frameNumber}");
            Button frameButton = frameButtons != null && frameNumber - 1 < frameButtons.Count
                ? frameButtons[frameNumber - 1]
                : null;

            if (frameButton == null && frameTransform != null)
            {
                frameButton = frameTransform.GetComponent<Button>();
            }

            Transform imageTransform = frameTransform != null ? frameTransform.Find("Image") : null;
            Image portraitImage = imageTransform != null ? imageTransform.GetComponent<Image>() : null;
            TMP_Text nameText = frameTransform != null
                ? frameTransform.GetComponentInChildren<TMP_Text>(true)
                : null;

            frames.Add(new FrameBinding(frameNumber, frameButton, portraitImage, nameText));
        }
    }

    private void BuildDisplayNames()
    {
        displayNames.Clear();

        var baseNames = new List<string>(history.Count);
        var hadSuffixes = new List<bool>(history.Count);
        var totals = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (PapalElectionRecordSaveData record in history)
        {
            string rawName = record != null && record.popeName != null
                ? record.popeName.Trim()
                : string.Empty;
            bool hadSuffix = RegnalSuffixPattern.IsMatch(rawName);
            string baseName = RegnalSuffixPattern.Replace(rawName, string.Empty).Trim();

            baseNames.Add(baseName);
            hadSuffixes.Add(hadSuffix);

            if (!string.IsNullOrEmpty(baseName))
            {
                totals.TryGetValue(baseName, out int total);
                totals[baseName] = total + 1;
            }
        }

        var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < baseNames.Count; i++)
        {
            string baseName = baseNames[i];
            if (string.IsNullOrEmpty(baseName))
            {
                displayNames.Add(string.Empty);
                continue;
            }

            occurrences.TryGetValue(baseName, out int occurrence);
            occurrence++;
            occurrences[baseName] = occurrence;

            bool showRegnalNumber = hadSuffixes[i] || totals[baseName] > 1;
            displayNames.Add(showRegnalNumber ? $"{baseName} {occurrence}세" : baseName);
        }
    }

    private void Render()
    {
        CurrentCenterPortrait = null;

        foreach (FrameBinding frame in frames)
        {
            int recordIndex = centerIndex + (frame.FrameNumber - CenterFrameNumber);
            bool hasRecord = recordIndex >= 0 && recordIndex < history.Count;

            if (!hasRecord)
            {
                ClearFrame(frame);
                continue;
            }

            PapalElectionRecordSaveData record = history[recordIndex];
            Sprite portrait = portraitCatalog != null
                ? portraitCatalog.GetPortrait(record.candidateSlot)
                : null;

            if (frame.FrameNumber == CenterFrameNumber)
            {
                CurrentCenterPortrait = portrait;
            }

            if (frame.PortraitImage != null)
            {
                frame.PortraitImage.sprite = portrait;
                frame.PortraitImage.enabled = portrait != null;
                frame.PortraitImage.preserveAspect = true;
            }

            if (frame.NameText != null)
            {
                frame.NameText.text = displayNames[recordIndex];
            }

            if (frame.Button != null)
            {
                frame.Button.interactable = true;
            }
        }

        UpdateArrowState();
    }

    private static void ClearFrame(FrameBinding frame)
    {
        if (frame.PortraitImage != null)
        {
            frame.PortraitImage.sprite = null;
            frame.PortraitImage.enabled = false;
        }

        if (frame.NameText != null)
        {
            frame.NameText.text = string.Empty;
        }

        if (frame.Button != null)
        {
            frame.Button.interactable = false;
        }
    }

    private void SetArrowObjectsActive(bool isActive)
    {
        if (leftButton != null)
        {
            leftButton.gameObject.SetActive(isActive);
        }

        if (rightButton != null)
        {
            rightButton.gameObject.SetActive(isActive);
        }
    }

    private void UpdateArrowState()
    {
        if (leftButton != null)
        {
            leftButton.interactable = IsBrowsing && centerIndex < history.Count - 1;
        }

        if (rightButton != null)
        {
            rightButton.interactable = IsBrowsing && centerIndex > 0;
        }
    }

    private sealed class FrameBinding
    {
        public int FrameNumber { get; }
        public Button Button { get; }
        public Image PortraitImage { get; }
        public TMP_Text NameText { get; }

        public FrameBinding(int frameNumber, Button button, Image portraitImage, TMP_Text nameText)
        {
            FrameNumber = frameNumber;
            Button = button;
            PortraitImage = portraitImage;
            NameText = nameText;
        }
    }
}
