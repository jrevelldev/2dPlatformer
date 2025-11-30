using UnityEngine;
using TMPro;

public class WinnerDisplay : MonoBehaviour
{
    [Header("Winner sprites by playerId (index = playerId)")]
    [Tooltip("Leave index 0 empty. Put Player 1 in [1], Player 2 in [2], etc.")]
    public Sprite[] playerWinnerSprites;

    [Header("Winner texts by playerId (index = playerId)")]
    [Tooltip("Leave index 0 empty. Put Player 1 in [1], Player 2 in [2], etc.")]
    public string[] playerWinnerTexts;

    [Header("Winner text display UI")]
    public TMP_Text winnerText;

    [Header("Pop animation")]
    [Tooltip("How long the pop animation lasts (seconds).")]
    public float popDuration = 0.25f;

    [Tooltip("Scale factor at the start of the pop (relative to base scale).")]
    public float popStartScaleFactor = 0.2f;

    [Tooltip("Scale factor at the end of the pop (relative to base scale). Usually 1 = normal size.")]
    public float popEndScaleFactor = 1f;

    [Tooltip("Curve controlling the pop. X=0..1 over time, Y=0..1 blends between start and end scale.")]
    public AnimationCurve popCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Highlight object (optional)")]
    [Tooltip("Any GameObject to highlight the winner (e.g. a Light, glow sprite, etc.).")]
    public GameObject highlightObject;

    private SpriteRenderer sr;
    private Vector3 baseScale;
    private Coroutine popRoutine;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = gameObject.AddComponent<SpriteRenderer>();

        // Remember the original scale so we can animate relative to it
        baseScale = transform.localScale;

        // Start hidden
        sr.enabled = false;

        if (winnerText != null)
            winnerText.gameObject.SetActive(false);

        if (highlightObject != null)
            highlightObject.SetActive(false);
    }

    public void ShowWinner(int playerId)
    {
        // --- Show correct sprite ---
        if (playerId > 0 &&
            playerId < playerWinnerSprites.Length &&
            playerWinnerSprites[playerId] != null)
        {
            sr.sprite = playerWinnerSprites[playerId];
            sr.enabled = true;
        }
        else
        {
            sr.enabled = false;
        }

        // --- Show correct text (custom from Inspector) ---
        if (winnerText != null)
        {
            string msg = "";

            if (playerId > 0 &&
                playerId < playerWinnerTexts.Length &&
                !string.IsNullOrEmpty(playerWinnerTexts[playerId]))
            {
                msg = playerWinnerTexts[playerId];
            }

            winnerText.text = msg;
            winnerText.gameObject.SetActive(true);
        }

        // --- Turn on highlight object (light / glow / etc.) ---
        if (highlightObject != null)
            highlightObject.SetActive(true);

        // --- Start pop animation ---
        if (popRoutine != null)
            StopCoroutine(popRoutine);

        popRoutine = StartCoroutine(PopAnimation());
    }

    public void Hide()
    {
        if (sr != null)
            sr.enabled = false;

        if (winnerText != null)
            winnerText.gameObject.SetActive(false);

        if (highlightObject != null)
            highlightObject.SetActive(false);

        // Reset scale
        transform.localScale = baseScale;

        if (popRoutine != null)
        {
            StopCoroutine(popRoutine);
            popRoutine = null;
        }
    }

    private System.Collections.IEnumerator PopAnimation()
    {
        // Start from smaller scale
        transform.localScale = baseScale * popStartScaleFactor;

        float t = 0f;

        while (t < popDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / popDuration);

            float curveValue = popCurve.Evaluate(normalized); // 0..1
            float scaleFactor = Mathf.Lerp(popStartScaleFactor, popEndScaleFactor, curveValue);

            transform.localScale = baseScale * scaleFactor;

            yield return null;
        }

        // Ensure final scale is exact
        transform.localScale = baseScale * popEndScaleFactor;
        popRoutine = null;
    }
}
