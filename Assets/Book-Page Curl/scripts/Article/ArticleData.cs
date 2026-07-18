using UnityEngine;

[CreateAssetMenu(fileName = "ArticleData", menuName = "Newspaper/Article Data")]
public sealed class ArticleData : ScriptableObject
{
    [SerializeField] private string title;
    [SerializeField] private Sprite image;
    [SerializeField, TextArea(3, 12)] private string body;

    public string Title => title;
    public Sprite Image => image;
    public string Body => body;

    public bool HasTitle => !string.IsNullOrWhiteSpace(title);
    public bool HasImage => image != null;
    public bool HasBody => !string.IsNullOrWhiteSpace(body);
}
