// RoundManager.cs (TMP edition: fall-until-ground, then freeze)
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;   // still needed for Button, Panels, etc.
using TMPro;           // << TextMesh Pro

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    [Header("Players & Scoring")]
    [Tooltip("Top-level GameObject of each player.")]
    public List<GameObject> players = new List<GameObject>();

    [Tooltip("Optional: exact class name of your movement script to toggle (e.g., 'PlayerController'). Leave empty to skip.")]
    public string movementScriptTypeName = "";

    [Header("Round Setup")]
    [Tooltip("Round duration (seconds).")]
    public int roundDurationSeconds = 60;

    [Tooltip("Start automatically on Play.")]
    public bool autoStart = true;

    [Header("Simple UI (TMP)")]
    public TMP_Text timeText;               // << TMP
    public GameObject setupPanel;
    public GameObject resultsPanel;
    public TMP_Text resultsText;            // << TMP

    [Tooltip("Optional: assign this, then hook the Start button to BeginRoundFromUI_NoParam()")]
    public TMP_InputField durationInput;    // << TMP (optional convenience field)

    [Header("Grounding Settings")]
    [Tooltip("Layers that count as 'ground'.")]
    public LayerMask groundMask;

    [Tooltip("Distance below the collider bottom we probe to detect ground.")]
    public float groundProbeDistance = 0.015f;

    [Tooltip("Maximum time we wait for a player to touch ground after time-up (seconds).")]
    public float groundWaitTimeout = 3f;

    [Tooltip("Small delay after ground contact before hard-freezing (seconds).")]
    public float groundSettleDelay = 1f;

    [Header("Debug")]
    public bool logDebug = false;

    enum RoundState { Idle, Playing, Finished }
    RoundState state = RoundState.Idle;

    float timeLeft;
    readonly Dictionary<GameObject, int> scores = new Dictionary<GameObject, int>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        foreach (var p in players)
            if (p != null && !scores.ContainsKey(p))
                scores[p] = 0;

        if (setupPanel != null) setupPanel.SetActive(!autoStart);
        if (resultsPanel != null) resultsPanel.SetActive(false);
    }

    void Start()
    {
        if (autoStart) BeginRound();
        else UpdateTimerUI(roundDurationSeconds);
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

    // ===== Public API =====
    // TMP version (hook the button and drag a TMP_InputField parameter)
    public void BeginRoundFromUI(TMP_InputField durationField = null)
    {
        if (durationField != null && int.TryParse(durationField.text, out var secs))
            roundDurationSeconds = Mathf.Max(5, secs);
        BeginRound();
    }

    // Convenience: if you prefer no parameters on the Button, assign 'durationInput' in Inspector and hook this one.
    public void BeginRoundFromUI_NoParam()
    {
        if (durationInput != null && int.TryParse(durationInput.text, out var secs))
            roundDurationSeconds = Mathf.Max(5, secs);
        BeginRound();
    }

    public void BeginRound()
    {
        if (players.Count == 0) { Debug.LogWarning("RoundManager: No players assigned."); return; }

        foreach (var key in new List<GameObject>(scores.Keys))
            scores[key] = 0;

        timeLeft = Mathf.Max(1, roundDurationSeconds);
        state = RoundState.Playing;

        if (setupPanel != null) setupPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);

        UnfreezePlayers(true);
        if (logDebug) Debug.Log("[RoundManager] Round started.");
    }

    public void EndRound()
    {
        if (state != RoundState.Playing) return;
        state = RoundState.Finished;

        // 1) Disable player input/movement immediately
        TogglePlayerControl(false);

        // 2) For each player: fall until grounded, then freeze solid
        foreach (var p in players)
        {
            if (p == null) continue;
            StartCoroutine(GroundAndFreeze(p));
        }

        // Compute winners
        int best = int.MinValue;
        var winners = new List<GameObject>();
        foreach (var kv in scores)
        {
            if (kv.Value > best) { best = kv.Value; winners.Clear(); winners.Add(kv.Key); }
            else if (kv.Value == best) winners.Add(kv.Key);
        }

        if (resultsPanel != null) resultsPanel.SetActive(true);
        if (resultsText != null) resultsText.text = BuildResultsText(winners, best);

        if (logDebug) Debug.Log("[RoundManager] Round ended. " + BuildResultsText(winners, best));
    }

    public void AddScore(GameObject player, int points = 1)
    {
        if (state != RoundState.Playing) return;
        if (player == null) return;
        if (!scores.ContainsKey(player)) scores[player] = 0;

        scores[player] += points;
        if (logDebug) Debug.Log($"[RoundManager] +{points} to {player.name}. Total={scores[player]}");
    }

    public void ResetToSetup()
    {
        state = RoundState.Idle;
        if (setupPanel != null) setupPanel.SetActive(true);
        if (resultsPanel != null) resultsPanel.SetActive(false);
        UpdateTimerUI(roundDurationSeconds);
        UnfreezePlayers(true);
    }

    // ===== Internals =====
    void UpdateTimerUI(float seconds)
    {
        if (timeText == null) return;
        int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int m = s / 60;
        int r = s % 60;
        timeText.text = $"{m:00}:{r:00}";
    }

    string BuildResultsText(List<GameObject> winners, int best)
    {
        var sb = new StringBuilder();

        if (winners.Count == 0) sb.AppendLine("No winners.");
        else if (winners.Count == 1) sb.AppendLine($"Winner: {winners[0].name} ({best} pts)");
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
            scores.TryGetValue(p, out var sc);
            sb.AppendLine($"- {p.name}: {sc}");
        }
        return sb.ToString();
    }

    void TogglePlayerControl(bool allow)
    {
        foreach (var p in players)
        {
            if (p == null) continue;

#if ENABLE_INPUT_SYSTEM
            var pi = p.GetComponentInChildren<PlayerInput>();
            if (pi != null) pi.enabled = allow;
#endif
            if (!string.IsNullOrEmpty(movementScriptTypeName))
            {
                var t = FindTypeByName(movementScriptTypeName);
                if (t != null)
                {
                    var comp = p.GetComponentInChildren(t) as Behaviour;
                    if (comp != null) comp.enabled = allow;
                }
            }
        }
    }

    System.Collections.IEnumerator GroundAndFreeze(GameObject playerRoot)
    {
        if (playerRoot == null) yield break;

        var rb = playerRoot.GetComponentInChildren<Rigidbody2D>();
        var col = playerRoot.GetComponentInChildren<Collider2D>();
        if (rb == null || col == null) yield break;

        if (rb.bodyType != RigidbodyType2D.Dynamic) rb.bodyType = RigidbodyType2D.Dynamic;
        if (rb.gravityScale <= 0f) rb.gravityScale = 1f;

        rb.simulated = true;
#if UNITY_6000_0_OR_NEWER
        var v = rb.linearVelocity;
        rb.linearVelocity = new Vector2(0f, v.y);
#else
        var v = rb.velocity;
        rb.velocity = new Vector2(0f, v.y);
#endif
        rb.angularVelocity = 0f;

        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;

        float t = 0f;
        while (!IsGrounded(col) && t < groundWaitTimeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (groundSettleDelay > 0f) yield return new WaitForSecondsRealtime(groundSettleDelay);

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector2.zero;
#else
        rb.velocity = Vector2.zero;
#endif
        rb.angularVelocity = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        rb.simulated = false;

        if (logDebug)
            Debug.Log($"[RoundManager] Grounded & frozen: {playerRoot.name} (waited {t:0.00}s)");
    }

    bool IsGrounded(Collider2D col)
    {
        Bounds b = col.bounds;
        Vector2 origin = new Vector2(b.center.x, b.min.y + 0.01f);
        float dist = Mathf.Max(0.01f, groundProbeDistance);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, dist, groundMask);
#if UNITY_EDITOR
        Debug.DrawRay(origin, Vector2.down * dist, hit.collider ? Color.green : Color.red, 0.05f);
#endif
        return hit.collider != null;
    }

    void UnfreezePlayers(bool allowMove)
    {
        foreach (var p in players)
        {
            if (p == null) continue;

#if ENABLE_INPUT_SYSTEM
            var pi = p.GetComponentInChildren<PlayerInput>();
            if (pi != null) pi.enabled = allowMove;
#endif
            if (!string.IsNullOrEmpty(movementScriptTypeName))
            {
                var t = FindTypeByName(movementScriptTypeName);
                if (t != null)
                {
                    var comp = p.GetComponentInChildren(t) as Behaviour;
                    if (comp != null) comp.enabled = allowMove;
                }
            }

            var rb = p.GetComponentInChildren<Rigidbody2D>();
            if (rb != null)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector2.zero;
#else
                rb.velocity = Vector2.zero;
#endif
                rb.angularVelocity = 0f;

                if (allowMove)
                {
                    rb.simulated = true;
                    rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                }
                else
                {
                    rb.constraints = RigidbodyConstraints2D.FreezeAll;
                    rb.simulated = false;
                }
            }
        }
    }

    Type FindTypeByName(string name)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(name);
            if (t != null) return t;
        }
        return null;
    }
}
