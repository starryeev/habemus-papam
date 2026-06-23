using UnityEngine;

public sealed class NewsBookInitializationTrigger : MonoBehaviour
{
    [SerializeField] private NewsBookInitializer initializer;
    [SerializeField] private bool invokeOnStart;

    private void Start()
    {
        if (invokeOnStart)
            Trigger();
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
}
