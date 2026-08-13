using UnityEngine;

public class SettingsService : MonoBehaviour
{
    private const string SettingsUIPrefabPath = "UI/SettingsUI";

    public static SettingsService Instance { get; private set; }
    public bool IsInputCaptured => settingsUI != null && settingsUI.IsInputCaptured;

    private SettingsUI settingsUI;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject serviceObject = new GameObject(nameof(SettingsService));
        DontDestroyOnLoad(serviceObject);
        serviceObject.AddComponent<SettingsService>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        EnsureSettingsUI();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void OpenSettings()
    {
        EnsureSettingsUI();

        if (settingsUI == null)
        {
            Debug.LogWarning("SettingsUI cannot be opened.");
            return;
        }

        settingsUI.RefreshSceneDependentUI();
        settingsUI.OpenSettingsPanel();
    }

    private void EnsureSettingsUI()
    {
        if (settingsUI != null)
        {
            return;
        }

        PersistentSettingsUI persistentSettingsUI = Object.FindFirstObjectByType<PersistentSettingsUI>();
        if (persistentSettingsUI != null)
        {
            settingsUI = persistentSettingsUI.GetComponentInChildren<SettingsUI>(true);
            if (settingsUI != null)
            {
                settingsUI.RefreshSceneDependentUI();
                return;
            }
        }

        GameObject prefab = Resources.Load<GameObject>(SettingsUIPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"Settings UI Prefab not found: Resources/{SettingsUIPrefabPath}");
            return;
        }

        GameObject settingsUIObject = Object.Instantiate(prefab);
        if (settingsUIObject.GetComponent<PersistentSettingsUI>() == null)
        {
            settingsUIObject.AddComponent<PersistentSettingsUI>();
        }

        settingsUI = settingsUIObject.GetComponentInChildren<SettingsUI>(true);
        if (settingsUI != null)
        {
            settingsUI.RefreshSceneDependentUI();
        }
    }
}
