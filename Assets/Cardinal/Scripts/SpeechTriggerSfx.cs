using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public sealed class SpeechTriggerSfx : MonoBehaviour
{
    private const string ConversationSfxName = "15 인게임- NPC대화";
    private const string SchemeProximitySfxName = "17 인게임- NPC공작";
    private const float MinVolume = 0.2f;
    private const float MaxVolume = 1f;

    private CircleCollider2D speechCollider;
    private StateController ownerState;
    private Transform playerTransform;
    private StateController playerState;
    private EventInstance proximityInstance;
    private string activeSfxName;
    private bool hasPlayedSchemeThisVisit;

    private void Awake()
    {
        speechCollider = GetComponent<CircleCollider2D>();
        ownerState = GetComponentInParent<StateController>();
    }

    private void Update()
    {
        string sfxName = GetProximitySfxName();
        if (playerTransform == null || playerState == null || ownerState == null ||
            IsConclaveTransitionInProgress() || sfxName == null ||
            sfxName == ConversationSfxName && (IsPlayerPerformingAction() ||
                ownerState.IsChatSfxInterrupted))
        {
            StopProximitySfx();
            return;
        }

        float volume = GetCurrentVolume();
        if (!proximityInstance.isValid() || activeSfxName != sfxName)
        {
            StopProximitySfx();
            if (sfxName == SchemeProximitySfxName && hasPlayedSchemeThisVisit)
            {
                return;
            }

            StartProximitySfx(sfxName, volume);
            return;
        }

        proximityInstance.getPlaybackState(out PLAYBACK_STATE playbackState);
        if (playbackState == PLAYBACK_STATE.STOPPED)
        {
            proximityInstance.release();
            proximityInstance.clearHandle();
            if (sfxName == SchemeProximitySfxName)
            {
                return;
            }

            StartProximitySfx(sfxName, volume);
            return;
        }

        proximityInstance.setVolume(volume);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (playerTransform == null)
            {
                hasPlayedSchemeThisVisit = false;
            }

            playerTransform = other.transform;
            playerState = other.GetComponent<StateController>();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerTransform = null;
            playerState = null;
            hasPlayedSchemeThisVisit = false;
            StopProximitySfx();
        }
    }

    private static bool IsConclaveTransitionInProgress()
    {
        return CardinalManager.Instance != null && CardinalManager.Instance.IsConclaveTransitionInProgress;
    }

    private bool IsPlayerPerformingAction()
    {
        return playerState.IsPerformingPrayerAction || playerState.IsPerformingSpeechAction;
    }

    private string GetProximitySfxName()
    {
        if (ownerState == null)
        {
            return null;
        }

        if (ownerState.CurrentState == CardinalState.ChatMaster ||
            ownerState.CurrentState == CardinalState.Chatting)
        {
            return ConversationSfxName;
        }

        return ownerState.CurrentState == CardinalState.Scheme || ownerState.IsSchemer
            ? SchemeProximitySfxName
            : null;
    }

    private float GetCurrentVolume()
    {
        if (speechCollider == null || speechCollider.radius <= 0f)
        {
            return MinVolume;
        }

        Vector2 localPlayerPosition = speechCollider.transform.InverseTransformPoint(playerTransform.position);
        float normalizedDistance = Vector2.Distance(localPlayerPosition, speechCollider.offset) / speechCollider.radius;
        return CalculateVolume(normalizedDistance);
    }

    private static float CalculateVolume(float normalizedDistance)
    {
        return Mathf.Lerp(MaxVolume, MinVolume, Mathf.Clamp01(normalizedDistance));
    }

    private void StartProximitySfx(string sfxName, float volume)
    {
        activeSfxName = sfxName;
        if (sfxName == SchemeProximitySfxName)
        {
            hasPlayedSchemeThisVisit = true;
        }

        proximityInstance = RuntimeManager.CreateInstance("event:/SFX/" + sfxName);
        proximityInstance.setVolume(volume);
        proximityInstance.start();
    }

    private void StopProximitySfx()
    {
        activeSfxName = null;
        if (!proximityInstance.isValid())
        {
            return;
        }

        proximityInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        proximityInstance.release();
        proximityInstance.clearHandle();
    }

    private void OnDisable()
    {
        playerTransform = null;
        playerState = null;
        hasPlayedSchemeThisVisit = false;
        StopProximitySfx();
    }
}
