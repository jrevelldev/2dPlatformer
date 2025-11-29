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

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = gameObject.AddComponent<SpriteRenderer>();

        sr.enabled = false;

        if (winnerText != null)
            winnerText.gameObject.SetActive(false);
    }

    public void ShowWinner(int playerId)
    {
        // --- Show sprite ---
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

        // --- Show text ---
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
    }

    public void Hide()
    {
        if (sr != null)
            sr.enabled = false;

        if (winnerText != null)
            winnerText.gameObject.SetActive(false);
    }
}
