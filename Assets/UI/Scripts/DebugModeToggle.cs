using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DebugModeToggle : MonoBehaviour
{
    private GameObject debugUi;

    public static void Attach(GameObject host)
    {
        if (host == null)
        {
            return;
        }

        DebugModeToggle toggle = host.GetComponent<DebugModeToggle>();
        if (toggle == null)
        {
            toggle = host.AddComponent<DebugModeToggle>();
        }

        toggle.Configure();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8))
        {
            SetDebugUiActive(debugUi == null || !debugUi.activeSelf);
        }
    }

    private void Configure()
    {
        debugUi = FindDebugUi();
        SetDebugUiActive(false);
    }

    private void SetDebugUiActive(bool active)
    {
        if (debugUi != null)
        {
            debugUi.SetActive(active);
        }
    }

    private static GameObject FindDebugUi()
    {
        foreach (GameObject rootObject in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (rootObject.name == "Debug_UI")
            {
                return rootObject;
            }
        }

        return null;
    }
}
