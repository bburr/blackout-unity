using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[Flags]
public enum PlayerGameState
{
    Waiting = 1,
    Betting = 2,
    Playing = 4,
}

public class PlayerManager : NetworkBehaviour
{
    [FormerlySerializedAs("cardDisplayPrefab")] [SerializeField] private GameObject cardInHandPrefab;
    [SerializeField] private GameObject handContainer;
    [SerializeField] private GameObject bettingViewPrefab;
    
    private CardState[] _hand;
    private BettingViewUI _bettingViewInstance;

    public CallbackValue<PlayerGameState> LocalPlayerGameState = new(PlayerGameState.Waiting);
    private bool _enableBettingView = true;
    private bool _isFirstTime = true;

    public void RefreshState()
    {
        LoadData();
    }

    private void LoadData()
    {
        InGameRunner.Instance.LoadData();
    }

    public void OnLocalUserDataUpdate(PlayerData playerData)
    {
        // todo compare differences in hand?
        _hand = playerData.Hand;

        for (var i = handContainer.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(handContainer.transform.GetChild(i).gameObject);
        }

        foreach (var card in _hand)
        {
            var cardInHand = Instantiate(cardInHandPrefab, handContainer.transform);
            cardInHand.GetComponentInChildren<CardDisplayController>().CardState = card;
            cardInHand.GetComponent<CardInHandUI>().LocalPlayerManager = this;
        }
    }
    
    public void EnableCardsAfterUI()
    {
        // todo make this work so the cards are not interactable until the trick modal is closed
        if (_isFirstTime)
        {
            _isFirstTime = false;
            EnableCardsInHand(true);
            return;
        }
        
        InGameRunner.Instance.GameDisplay.IsTrickModalActive.OnChanged += OnTrickModalStateChanged;
        
        void OnTrickModalStateChanged(bool isActive)
        {
            if (!isActive)
            {
                EnableCardsInHand(true);
                InGameRunner.Instance.GameDisplay.IsTrickModalActive.OnChanged -= OnTrickModalStateChanged;
            }
        }
    }

    public void EnableCardsInHand(bool value)
    {
        foreach (var button in handContainer.GetComponentsInChildren<Button>())
        {
            button.interactable = value;
        }
    }

    private void InitBettingView(bool value)
    {
        if (value)
        {
            _bettingViewInstance = Instantiate(bettingViewPrefab).GetComponent<BettingViewUI>();
        }
        else if (_bettingViewInstance != null)
        {
            Destroy(_bettingViewInstance.gameObject);
            _enableBettingView = false;
        }
    }

    public void EnableBettingView(bool value = true)
    {
        if (_bettingViewInstance == null)
        {
            _enableBettingView = true;
            return;
        }
        
        _enableBettingView = false;
        _bettingViewInstance.gameObject.SetActive(value);
    }

    private void SetState(PlayerGameState state)
    {
        LocalPlayerGameState.Value = state;
    }

    public void OnCurrentRoundDataUpdate(RoundState currentRound)
    {
        if (currentRound.PlayerIndexToBet >= 0 || LocalPlayerGameState.Value == PlayerGameState.Betting)
        {
            if (InGameRunner.Instance.LocalPlayerIndex == currentRound.PlayerIndexToBet)
            {
                if (LocalPlayerGameState.Value != PlayerGameState.Betting)
                {
                    SetState(PlayerGameState.Betting);
                    InitBettingView(true);

                    if (_enableBettingView)
                    {
                        EnableBettingView();
                    }
                }
            }
            else if (LocalPlayerGameState.Value == PlayerGameState.Betting)
            {
                SetState(PlayerGameState.Waiting);
                InitBettingView(false);
            }
        }
        else if (currentRound.PlayerIndexToPlay >= 0 || LocalPlayerGameState.Value == PlayerGameState.Playing)
        {
            if (InGameRunner.Instance.LocalPlayerIndex == currentRound.PlayerIndexToPlay)
            {
                SetState(PlayerGameState.Playing);
                EnableCardsInHand(true);
            }
            else if (LocalPlayerGameState.Value == PlayerGameState.Playing)
            {
                SetState(PlayerGameState.Waiting);
                EnableCardsInHand(false);
            }
        }
    }
}