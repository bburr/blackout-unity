using UnityEngine;
using UnityEngine.UI;

public class CardDisplayController : MonoBehaviour
{
    private CardState _cardState;
    
    public CardState CardState
    {
        get => _cardState;
        set
        {
            _cardState = value;
            Refresh();
        }
    }

    public void Refresh()
    {
        GetComponentInChildren<Image>().sprite = CardImageManager.Instance.GetImageForCard(CardState);
    }
}
