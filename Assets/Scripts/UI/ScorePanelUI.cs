using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class ScorePanelUI : UIPanelBase
{
    public GameObject playerNameHeadingRow;
    public GameObject roundScoreRowsCanvas;

    public TMP_Text playerNameCellPrefab;
    public TMP_Text scoreCellPrefab;
    public GameObject roundScoreRowPrefab;

    public override void Start()
    {
        base.Start();
        
        foreach (var playerName in NetworkedDataStore.Instance.PlayerNames)
        {
            var playerNameText = Instantiate(playerNameCellPrefab, playerNameHeadingRow.transform);
            playerNameText.text = playerName;
        }
        
        // todo listen for round completions to get scores
        InGameRunner.Instance.OnRoundEnd += AddRoundScores;
    }

    public void AddRoundScores(RoundScoreState roundScore)
    {
        var roundScoreRow = Instantiate(roundScoreRowPrefab, roundScoreRowsCanvas.transform);
        
        var roundNumberText = Instantiate(scoreCellPrefab, roundScoreRow.transform);
        roundNumberText.text = roundScore.RoundNumber.ToString();
        
        foreach (var score in roundScore.Scores)
        {
            var scoreText = Instantiate(scoreCellPrefab, roundScoreRow.transform);
            scoreText.text = score.ToString();
        }
    }

    public void ScorePanelToggle()
    {
        if (InGameRunner.Instance.GameHasEnded)
        {
            return;
        }
        
        Toggle();
    }
}