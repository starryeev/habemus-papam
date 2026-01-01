using System.Collections.Generic;
using UnityEngine;

public class CardinalManager : MonoBehaviour
{
    [Header("추기경 프리팹 설정")]
    [Tooltip("플레이어가 조종하는 추기경 프리팹")]
    [SerializeField] private GameObject cardinalPrefabPlayer;

    [Tooltip("AI가 조종하는 추기경 프리팹")]
    [SerializeField] private GameObject cardinalPrefabAI;

    [Header("추기경 설정")]
    [Tooltip("추기경 스폰 포인트 설정")]
    [SerializeField] private Transform[] spawnPoints;

    // 싱글톤
    public static CardinalManager Instance { get; private set; }

    // 기타 멤버변수
    private List<Cardinal> cardinals;

    void Awake()
    {
        // 싱글톤
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 멤버변수 초기화
        cardinals = new List<Cardinal>();
    }

    void Start()
    {
        // 임시 추기경 생성로직
        CreateCardinals();
    }

    Transform GetOrCreateCardinalsContainer()
    {
        GameObject runtimeObj = GameObject.Find("Runtime");
        if (runtimeObj == null)
            runtimeObj = new GameObject("Runtime");

        Transform cardinalsTr = runtimeObj.transform.Find("Cardinals");
        if (cardinalsTr == null)
        {
            GameObject cardinalsObj = new GameObject("Cardinals");
            cardinalsTr = cardinalsObj.transform;
            cardinalsTr.SetParent(runtimeObj.transform, false);
        }

        return cardinalsTr;
    }

    void SpawnCardinal(GameObject prefab, Transform spawnPoint, string objName)
    {
        GameObject cardinalObj = Instantiate(prefab, spawnPoint.position, Quaternion.identity, GetOrCreateCardinalsContainer());
        cardinalObj.name = objName;

        Cardinal cardinal = cardinalObj.GetComponent<Cardinal>();
        cardinals.Add(cardinal);
    }

    void CreateCardinals()
    {
        SpawnCardinal(cardinalPrefabPlayer, spawnPoints[0], "Cardinal_0(Player)");

        for(int i = 1; i < 4; i++)
        {
            SpawnCardinal(cardinalPrefabAI, spawnPoints[i], $"Cardinal_{i}(AI)");
        }
    }

}
