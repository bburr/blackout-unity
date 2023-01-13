using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndOfTrickModalUI : MonoBehaviour
{
    [SerializeField] private CardDisplayController cardImage;
    [SerializeField] private TMP_Text playerName;
    
    public CardState WinningCard
    {
        set
        {
            cardImage.CardState = value;
            cardImage.Refresh();
        }
    }

    public LocalPlayer WinningPlayer
    {
        set => playerName.text = value.ID.Value;
    }
}