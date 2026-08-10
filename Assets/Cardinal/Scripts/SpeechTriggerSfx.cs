using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public sealed class SpeechTriggerSfx : MonoBehaviour
{
    private const string ConversationSfxName = "15 인게임- NPC대화";
    private const float MinVolume = 0.2f;
    private const float MaxVolume = 1f;

    private CircleCollider2D speechCollider;
    private StateController ownerState;
    private Transform playerTransform;
    private EventInstance conversationInstance;

    private void Awake()
    {
        speechCollider = GetComponent<CircleCollider2D>();
        ownerState = GetComponentInParent<StateController>();
    }

    private void Update()
    {
        if (playerTransform == null || ownerState == null || !IsConversationState())
        {
            StopConversationSfx();
            return;
        }

        float volume = GetCurrentVolume();
        if (!conversationInstance.isValid())
        {
            StartConversationSfx(volume);
            return;
        }

        conversationInstance.getPlaybackState(out PLAYBACK_STATE playbackState);
        if (playbackState == PLAYBACK_STATE.STOPPED)
        {
            conversationInstance.release();
            conversationInstance.clearHandle();
            StartConversationSfx(volume);
            return;
        }

        conversationInstance.setVolume(volume);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerTransform = other.transform;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerTransform = null;
            StopConversationSfx();
        }
    }

    private bool IsConversationState()
    {
        return ownerState.CurrentState == CardinalState.ChatMaster ||
            ownerState.CurrentState == CardinalState.Chatting;
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

    private void StartConversationSfx(float volume)
    {
        conversationInstance = RuntimeManager.CreateInstance("event:/SFX/" + ConversationSfxName);
        conversationInstance.setVolume(volume);
        conversationInstance.start();
    }

    private void StopConversationSfx()
    {
        if (!conversationInstance.isValid())
        {
            return;
        }

        conversationInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        conversationInstance.release();
        conversationInstance.clearHandle();
    }

    private void OnDisable()
    {
        playerTransform = null;
        StopConversationSfx();
    }
}
