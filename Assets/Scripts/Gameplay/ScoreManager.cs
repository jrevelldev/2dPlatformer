using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    // playerId -> score
    private readonly Dictionary<int, int> scores = new();

    // When true, Add(...) will be ignored (no more score changes).
    private bool scoresLocked = false;

    // (playerId, newScore)
    public event Action<int, int> OnScoreChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

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

    // When a scene loads/reloads, zero out known player IDs (same behaviour as before)
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Option 1: hard clear everything
        // scores.Clear();

        // Option 2 (current): zero known players and notify UI
        var ids = new List<int>(scores.Keys);
        for (int i = 0; i < ids.Count; i++)
        {
            scores[ids[i]] = 0;
            OnScoreChanged?.Invoke(ids[i], 0);
        }

        // If you want scores to stay locked across scenes, leave this as-is.
        // If you want a new scene to allow scoring again, uncomment:
        // scoresLocked = false;
    }

    public int GetScore(int playerId) =>
        scores.TryGetValue(playerId, out var s) ? s : 0;

    public void Add(int playerId, int amount)
    {
        // ❌ If scores are locked (timer reached 0), ignore any adds.
        if (scoresLocked) return;

        if (!scores.ContainsKey(playerId))
            scores[playerId] = 0;

        scores[playerId] += amount;
        OnScoreChanged?.Invoke(playerId, scores[playerId]);
    }

    // ===== Lock / Unlock public API =====

    /// <summary>
    /// Call this when the round ends (timer hits 0).
    /// After this, Add(...) does nothing.
    /// </summary>
    public void LockScores()
    {
        scoresLocked = true;
    }

    /// <summary>
    /// Call this when a new round starts.
    /// After this, Add(...) works again.
    /// </summary>
    public void UnlockScores()
    {
        scoresLocked = false;
    }

    // Optional helpers you can call from RoundManager or UI

    public void ResetPlayers(IEnumerable<int> playerIds, bool notify = true)
    {
        foreach (var id in playerIds)
        {
            scores[id] = 0;
            if (notify)
                OnScoreChanged?.Invoke(id, 0);
        }
    }

    public void NotifyAll()
    {
        foreach (var kv in scores)
            OnScoreChanged?.Invoke(kv.Key, kv.Value);
    }
}
