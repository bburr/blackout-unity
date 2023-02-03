using TMPro;
using Unity.Netcode;
using UnityEngine;

public class BettingViewUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField betInputField;

    // todo display prior players' bets 
    
    public void SubmitBet()
    {
        var betAmount = int.Parse(betInputField.text);
        
        InGameRunner.Instance.MakeBet(betAmount);
    }
}