// RoundManager.cs
// Simple version: disables PlayerController + stops x-velocity when time hits 0.

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

using Platformer.Mechanics; // <-- required for PlayerController

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    [Header("Players & Scoring")]
    [Tooltip("Top-level GameObject of each player (e.g. Player1, Player2...).")]
    public List<GameObject> players = new List<GameObject>();

    [Header("Round Setup")]
    public int roundDurationSeconds = 60;
    public bool autoStart = true;

    [Header("UI")]
    public TMP_Text timeText;
    public GameObject setupPanel;
    public GameObject resultsPanel;
    public TMP_Text resultsText;
    public TMP_InputField durationInput;

    [Header("Debug")]
    public bool logDebug = false;

    enum RoundState { Idle, Playing, Finished }
    RoundState state = RoundState.Idle;

    float timeLeft;
    readonly Dictionary<GameObject, int> scores = new Dictionary<GameObject, int>();

    // Reset singleton before scene loads
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ResetStatics_BeforeSceneLoad()
    {
        Instance = null;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Init scores
        scores.Clear();
        foreach (var p in players)
            if (p != null)
                scores[p] = 0;

        if (setupPanel != null) setupPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);
    }

    void Start()
    {
        if (autoStart)
            BeginRound();
        else
            UpdateTimerUI(roundDurationSeconds);
    }

    void Update()
    {
        if (state != RoundState.Playing) return;

        timeLeft -= Time.deltaTime;
        if (timeLeft < 0f) timeLeft = 0f;

        UpdateTimerUI(timeLeft);

        if (timeLeft <= 0f)
            EndRound();
    }

    // ===== Round Control =====

    public void BeginRoundFromUI_NoParam()
    {
        if (durationInput != null && int.TryParse(durationInput.text, out int secs))
            roundDurationSeconds = Mathf.Max(5, secs);

        BeginRound();
    }

    public void BeginRound()
    {
        if (players.Count == 0)
        {
            Debug.LogWarning("RoundManager: No players assigned.");
            return;
        }

        // Reset scores
        foreach (var k in new List<GameObject>(scores.Keys))
            scores[k] = 0;

        timeLeft = Mathf.Max(1, roundDurationSeconds);
        state = RoundState.Playing;

        if (setupPanel != null) setupPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);

        SetPlayerControllersEnabled(true);

        UpdateTimerUI(timeLeft);
        if (logDebug) Debug.Log("[RoundManager] Round started.");
    }

    public void EndRound()
    {
        if (state != RoundState.Playing) return;
        state = RoundState.Finished;

        // 🔥 KEY BEHAVIOR:
        // Disable PlayerController + stop horizontal movement
        SetPlayerControllersEnabled(false);

        // ----- Winner (optional) -----
        int best = int.MinValue;
        List<GameObject> winners = new List<GameObject>();

        foreach (var kv in scores)
        {
            if (kv.Value > best)
            {
                best = kv.Value;
                winners.Clear();
                winners.Add(kv.Key);
            }
            else if (kv.Value == best)
            {
                winners.Add(kv.Key);
            }
        }

        if (resultsPanel != null) resultsPanel.SetActive(true);
        if (resultsText != null) resultsText.text = BuildResultsText(winners, best);

        if (logDebug) Debug.Log("[RoundManager] Round ended.");
    }

    public void ResetToSetup()
    {
        state = RoundState.Idle;

        if (setupPanel != null) setupPanel.SetActive(true);
        if (resultsPanel != null) resultsPanel.SetActive(false);

        timeLeft = roundDurationSeconds;
        UpdateTimerUI(timeLeft);

        SetPlayerControllersEnabled(true);
    }

    // ===== UI =====

    void UpdateTimerUI(float seconds)
    {
        if (timeText == null) return;

        int s = Mathf.FloorToInt(seconds);
        int m = s / 60;
        int r = s % 60;

        timeText.text = $"{m:00}:{r:00}";
    }

    string BuildResultsText(List<GameObject> winners, int best)
    {
        var sb = new StringBuilder();

        if (winners.Count == 0)
            sb.AppendLine("No winners.");
        else if (winners.Count == 1)
            sb.AppendLine($"Winner: {winners[0].name} ({best} pts)");
        else
        {
            sb.Append("Tie: ");
            for (int i = 0; i < winners.Count; i++)
            {
                sb.Append(winners[i].name);
                if (i < winners.Count - 1) sb.Append(", ");
            }
            sb.AppendLine($" ({best} pts)");
        }

        sb.AppendLine();
        sb.AppendLine("Scores:");

        foreach (var p in players)
        {
            if (p == null) continue;
            scores.TryGetValue(p, out int sc);
            sb.AppendLine($"- {p.name}: {sc}");
        }

        return sb.ToString();
    }

    // ===== Movement Control =====

    void SetPlayerControllersEnabled(bool enabled)
    {
        foreach (var p in players)
        {
            if (p == null) continue;

            // Movement script
            var pc = p.GetComponentInChildren<PlayerController>(true);
            if (pc != null)
                pc.enabled = enabled;

            // Optional: Input System
#if ENABLE_INPUT_SYSTEM
            var pi = p.GetComponentInChildren<PlayerInput>(true);
            if (pi != null)
                pi.enabled = enabled;
#endif

            // Rigidbody horizontal stop
            var rb = p.GetComponentInChildren<Rigidbody2D>(true);
            if (rb != null)
            {
#if UNITY_6000_0_OR_NEWER
                var v = rb.linearVelocity;
                if (!enabled) v.x = 0f;
                rb.linearVelocity = v;
#else
                var v = rb.velocity;
                if (!enabled) v.x = 0f;
                rb.velocity = v;
#endif
            }
        }

        if (logDebug)
            Debug.Log($"[RoundManager] PlayerController enabled = {enabled}");
    }
}
