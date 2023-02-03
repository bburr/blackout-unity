using System;
using UnityEngine;

public class CardInHandUI : MonoBehaviour
{
    public PlayerManager LocalPlayerManager { get; set; }
    
    public void SelectCard()
    {
        // todo disable invalid cards based on leading card?
        LocalPlayerManager.EnableCardsInHand(false);
        
        InGameRunner.Instance.PlayCard(GetComponentInChildren<CardDisplayController>().CardState);
    }
}