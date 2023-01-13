using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardImageManager : MonoBehaviour
{
    public Sprite[] sprites;

    private readonly Dictionary<char, string> _suitNames = new()
    {
        {'c', "Club"},
        {'d', "Diamond"},
        {'h', "Heart"},
        {'s', "Spade"},
    };

    private static CardImageManager _instance;

    public static CardImageManager Instance
    {
        get
        {
            if (_instance != null)
            {
                return _instance;
            }

            return _instance = FindObjectOfType<CardImageManager>();
        }
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Sprite GetSpriteByName(string spriteName)
    {
        return (from sprite in sprites where sprite.name == spriteName select sprite).FirstOrDefault();
    }

    public Sprite GetImageForCard(CardState cardState)
    {
        var cardNum = cardState.ValueKey == 12 ? 1 : cardState.ValueKey + 2;
        
        return GetSpriteByName(_suitNames[char.ToLower(cardState.SuitKey)] + cardNum.ToString().PadLeft(2, '0'));
    }
}
