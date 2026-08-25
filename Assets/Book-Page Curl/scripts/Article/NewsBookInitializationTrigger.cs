using UnityEngine;
using UnityEngine.Video;

public sealed class NewsBookInitializationTrigger : MonoBehaviour
{
    [SerializeField] private NewsBookInitializer initializer;
    [SerializeField] private bool invokeOnStart;
    [SerializeField] private VideoPlayer cutsceneAnim;
    [SerializeField] private GameObject cutscene;
    [SerializeField] private GameObject arr1, arr2;

    private void Awake()
    {
        cutsceneAnim.loopPointReached += OnVideoFinished;
    }
    private void Start()
    {
        cutscene.SetActive(true);
        arr1.SetActive(false);
        arr2.SetActive(false);
        cutsceneAnim.Play();
    }

    public void Trigger()
    {
        if (initializer == null)
        {
            Debug.LogError("News book initializer is not assigned.", this);
            return;
        }
        initializer.InitializeNewGameNews();
    }

    private void Reset()
    {
        initializer = GetComponent<NewsBookInitializer>();
    }
    private void OnVideoFinished(VideoPlayer src)
    {
        cutscene.SetActive(false);
        if (invokeOnStart)
            Trigger();
    }
}
