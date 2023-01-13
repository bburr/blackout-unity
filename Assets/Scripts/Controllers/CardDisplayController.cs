using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardDisplayController : MonoBehaviour
{
    public CardState CardState { get; set; }
    
    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        GetComponentInChildren<Image>().sprite = CardImageManager.Instance.GetImageForCard(CardState);
    }
}
