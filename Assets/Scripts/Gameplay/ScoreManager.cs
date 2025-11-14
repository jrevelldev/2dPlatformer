using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private readonly Dictionary<int, int> scores = new();

    // (playerId, newScore)
    public event Action<int, int> OnScoreChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ✅ When a scene loads/reloads, zero out known player IDs (or clear if you prefer)
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Option 1: hard clear everything
        // scores.Clear();

        // Option 2 (recommended): zero known players and notify UI
        var ids = new List<int>(scores.Keys);
        for (int i = 0; i < ids.Count; i++)
        {
            scores[ids[i]] = 0;
            OnScoreChanged?.Invoke(ids[i], 0);
        }
    }

    public int GetScore(int playerId) => scores.TryGetValue(playerId, out var s) ? s : 0;

    public void Add(int playerId, int amount)
    {
        if (!scores.ContainsKey(playerId)) scores[playerId] = 0;
        scores[playerId] += amount;
        OnScoreChanged?.Invoke(playerId, scores[playerId]);
    }

    // Optional helpers you can call from RoundManager or UI
    public void ResetPlayers(IEnumerable<int> playerIds, bool notify = true)
    {
        foreach (var id in playerIds)
        {
            scores[id] = 0;
            if (notify) OnScoreChanged?.Invoke(id, 0);
        }
    }

    public void NotifyAll()
    {
        foreach (var kv in scores)
            OnScoreChanged?.Invoke(kv.Key, kv.Value);
    }
}
