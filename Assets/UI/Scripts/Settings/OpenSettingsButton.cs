using UnityEngine;

public class OpenSettingsButton : MonoBehaviour
{
    public void OnClickOpenSettings()
    {
        SoundManager.Instance.PlaySFX("Book");

        if (SettingsService.Instance == null)
        {
            Debug.LogWarning("SettingsService is not initialized.");
            return;
        }

        SettingsService.Instance.OpenSettings();
    }
}
