using UnityEngine;
using NameData;
using System.Collections.Generic;

public class NameDB : MonoBehaviour
{
    //싱글톤
    private static NameDB instance = null;
    private static readonly List<string> playerInputNameStore = new();

    private Dictionary<string, int> PopeList = new();
    public List<string> PlayerInputName => playerInputNameStore;
    int weightSum = 0;
    void Awake()
    {
        if(instance == null) instance = this;
        else Destroy(this.gameObject);
        RefreshDB();
    }

    public static NameDB Instance
    {
        get
        {
            if (instance == null) return null;
            return instance;
        }
    }
    void RefreshDB()
    {
        PopeList.Clear();
        weightSum = 0;
        PopeName.Load();

        foreach(var v in PopeName.PopeNameList)
        {
            weightSum+=v.weight;
        }

        foreach(var v in PopeName.PopeNameList)
        {
            PopeList.Add(v.popeName, v.weight);
        }
    }

    public void SetCompletedPlayerInputNames(IEnumerable<string> names)
    {
        SetPlayerInputNames(names);
    }

    public static void SetPlayerInputNames(IEnumerable<string> names)
    {
        playerInputNameStore.Clear();

        if (names == null)
        {
            return;
        }

        foreach (string name in names)
        {
            AddCompletedPlayerInputName(name);
        }
    }

    public void AddPlayerInputName(string playerName)
    {
        AddCompletedPlayerInputName(playerName);
    }

    public static void AddCompletedPlayerInputName(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            return;
        }

        if (!playerInputNameStore.Contains(playerName))
        {
            playerInputNameStore.Add(playerName);
        }
    }

    public string GetRandomName(ICollection<string> excludedNames)
    {
        int availableWeight = 0;

        foreach (var entry in PopeList)
        {
            if (entry.Value <= 0 ||
                string.IsNullOrWhiteSpace(entry.Key) ||
                (excludedNames != null && excludedNames.Contains(entry.Key)))
                continue;

            availableWeight += entry.Value;
        }

        if (availableWeight <= 0)
            return null;

        int roll = UnityEngine.Random.Range(0, availableWeight);

        foreach (var entry in PopeList)
        {
            if (entry.Value <= 0 ||
                string.IsNullOrWhiteSpace(entry.Key) ||
                (excludedNames != null && excludedNames.Contains(entry.Key)))
                continue;

            if (roll < entry.Value)
                return entry.Key;

            roll -= entry.Value;
        }

        return null;
    }

    public string GetRandomName()
    {
        float randNum = UnityEngine.Random.Range(0, weightSum);
        int pivot = 0;
        foreach(var v in PopeList)
        {
            pivot += v.Value;
            if (pivot > (float)(randNum%weightSum) && v.Value != 0)
            {
                return v.Key;
            }
        }
        return null;
    }
    /*
    randNum이 0이라면? 첫 0이 아닌 가중치를 더하고 반환
    randNum이 weightSum이라면? 모든 가중치를 다 더하고 반환
    weight이 0인 것들이 있다면? 반환안하고 다음 0이 아닌 가중치에서 반환
    굿
    */
}
