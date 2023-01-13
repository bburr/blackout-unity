using System;
using UnityEngine;

public class CardInHandUI : MonoBehaviour
{
    public PlayerManager LocalPlayerManager { get; set; }
    
    public void SelectCard()
    {
        LocalPlayerManager.EnableCardsInHand(false);
        
        InGameRunner.Instance.PlayCard(GetComponentInChildren<CardDisplayController>().CardState);
    }
}