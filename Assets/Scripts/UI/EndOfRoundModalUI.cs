using TMPro;
using UnityEngine;

public class EndOfRoundModalUI : MonoBehaviour
{
    public GameObject playerNameHeadingRow;
    public GameObject roundScoreRow;

    public TMP_Text playerNameCellPrefab;
    public TMP_Text scoreCellPrefab;

    public RoundScoreState RoundScore
    {
        set => InitTable(value);
    }

    private void InitTable(RoundScoreState roundScore)
    {
        for (var i = playerNameHeadingRow.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(playerNameHeadingRow.transform.GetChild(i).gameObject);
        }

        for (var i = roundScoreRow.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(roundScoreRow.transform.GetChild(i).gameObject);
        }
        
        foreach (var playerName in NetworkedDataStore.Instance.PlayerNames)
        {
            var playerNameText = Instantiate(playerNameCellPrefab, playerNameHeadingRow.transform);
            playerNameText.text = playerName;
        }

        foreach (var score in roundScore.Scores)
        {
            var scoreText = Instantiate(scoreCellPrefab, roundScoreRow.transform);
            scoreText.text = score.ToString();
        }
    }
}