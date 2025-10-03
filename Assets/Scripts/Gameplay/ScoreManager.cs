using System;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }
    private readonly Dictionary<int, int> scores = new();
    public event Action<int, int> OnScoreChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public int GetScore(int playerId) => scores.TryGetValue(playerId, out var s) ? s : 0;

    public void Add(int playerId, int amount)
    {
        if (!scores.ContainsKey(playerId)) scores[playerId] = 0;
        scores[playerId] += amount;
        OnScoreChanged?.Invoke(playerId, scores[playerId]);
    }
}
