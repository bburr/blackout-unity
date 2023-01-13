using Unity.Netcode;
using UnityEngine;

public class GameDisplayContainer : MonoBehaviour
{
    [SerializeField] private GameObject playedCards;
    [SerializeField] private GameObject trumpCard;
    [SerializeField] private EndOfTrickModalUI endOfTrickModalUI;
    [SerializeField] private EndOfRoundModalUI endOfRoundModalUI;

    [SerializeField] private CardDisplayController cardPrefab;

    public CallbackValue<bool> IsTrickModalActive = new(false);

    private bool _isRoundModalWaiting;
    
    public void OnCurrentRoundDataUpdate(RoundState roundState)
    {
        if (!roundState.TrumpCard.IsEmpty())
        {
            trumpCard.GetComponentInChildren<CardDisplayController>().CardState = roundState.TrumpCard;
            trumpCard.SetActive(true);
        }
        else
        {
            trumpCard.SetActive(false);
        }
        
        for (var i = playedCards.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(playedCards.transform.GetChild(i).gameObject);
        }
        
        // todo display in order of play?
        foreach (var card in roundState.CurrentTrick.PlayedCards)
        {
            if (card.IsEmpty())
            {
                continue;
            }
            
            var cardInHand = Instantiate(cardPrefab, playedCards.transform);
            cardInHand.GetComponentInChildren<CardDisplayController>().CardState = card;
        }
    }
    
    public void DisplayEndOfTrickUI(TrickState completedTrick)
    {
        endOfTrickModalUI.WinningCard = completedTrick.PlayedCards[completedTrick.TrickWinnerIndex];
        endOfTrickModalUI.WinningPlayer = GameManager.Instance.LocalLobby.GetLocalPlayer(completedTrick.TrickWinnerIndex);
        endOfTrickModalUI.gameObject.SetActive(true);
    }

    public void CloseEndOfTrickUI()
    {
        endOfTrickModalUI.gameObject.SetActive(false);

        if (_isRoundModalWaiting)
        {
            EnableEndOfRoundModal();
        }
    }

    public void EnableEndOfRoundModal(bool value = true)
    {
        endOfRoundModalUI.gameObject.SetActive(value);
        _isRoundModalWaiting = false;
    }

    public void InitEndOfRoundUI(RoundState roundState)
    {
        // todo
        _isRoundModalWaiting = true;
    }

    public void CloseEndOfRoundUI()
    {
        endOfRoundModalUI.gameObject.SetActive(false);
        InGameRunner.Instance.LocalPlayerManager.EnableBettingView();
    }
}