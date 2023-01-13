using TMPro;
using Unity.Netcode;
using UnityEngine;

public class BettingViewUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField betInputField;

    public void SubmitBet()
    {
        var betAmount = int.Parse(betInputField.text);
        
        InGameRunner.Instance.MakeBet(betAmount);
    }
}