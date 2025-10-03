using System.Collections;
using TMPro;
using UnityEngine;

public class ScoreboardHUD : MonoBehaviour
{
    [Header("Assign your 4 TMP labels (P1..P4)")]
    public TextMeshProUGUI p1Label;
    public TextMeshProUGUI p2Label;
    public TextMeshProUGUI p3Label;
    public TextMeshProUGUI p4Label;

    TextMeshProUGUI[] labels;

    void Awake()
    {
        labels = new[] { p1Label, p2Label, p3Label, p4Label };
    }

    void OnEnable()
    {
        StartCoroutine(SubscribeWhenReady());
    }

    IEnumerator SubscribeWhenReady()
    {
        // Wait until ScoreManager exists (e.g., created in another scene object)
        while (ScoreManager.Instance == null) yield return null;

        ScoreManager.Instance.OnScoreChanged += OnScoreChanged;
        RefreshAll();
    }

    void OnDisable()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged -= OnScoreChanged;
    }

    void OnScoreChanged(int playerId, int newScore)
    {
        // playerId expected 1..4
        int idx = playerId - 1;
        if (idx < 0 || idx >= labels.Length) return;
        if (labels[idx] != null) labels[idx].text = $"P{playerId}: {newScore}";
    }

    public void RefreshAll()
    {
        if (ScoreManager.Instance == null) return;
        for (int i = 0; i < labels.Length; i++)
        {
            int pid = i + 1;
            if (labels[i] != null)
                labels[i].text = $"P{pid}: {ScoreManager.Instance.GetScore(pid)}";
        }
    }
}
