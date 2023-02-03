
using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class InGameRunner : NetworkBehaviour
{
    [SerializeField] private PlayerManager playerManagerPrefab;
    [SerializeField] private GameDisplayContainer gameDisplayContainerPrefab;
    
    [SerializeField] private NetworkedDataStore dataStore;

    [SerializeField] private GameDisplayContainer _gameDisplayContainerInstance;

    public GameDisplayContainer GameDisplay => _gameDisplayContainerInstance;

    public PlayerData LocalUser => _localUserData;
    public int LocalPlayerIndex { get; set; }

    public bool GameHasEnded { get; private set; }

    public Action<RoundScoreState> OnRoundEnd;

    public PlayerManager LocalPlayerManager;
    public Action OnGameBeginning;
    private Action _onConnectionVerified, _onGameEnd;
    private int _expectedPlayerCount;
    private PlayerData _localUserData;
    private bool _hasConnected;

    private int _dealerIndex;
    private int _leadingPlayerIndex;
    private RoundState _currentRound;
    
    public static InGameRunner Instance
    {
        get
        {
            if (_instance!) return _instance;
            return _instance = FindObjectOfType<InGameRunner>();
        }
    }

    private static InGameRunner _instance;
    
    #region Setup

    public void Initialize(
        Action onConnectionVerified, 
        int expectedPlayerCount, 
        Action onGameBegin,
        Action onGameEnd, 
        LocalPlayer localUser)
    {
        _onConnectionVerified = onConnectionVerified;
        _expectedPlayerCount = expectedPlayerCount;
        OnGameBeginning = onGameBegin;
        _onGameEnd = onGameEnd;
        _localUserData = new PlayerData(localUser.ID.Value, 0); // todo name
    }

    public override void OnNetworkSpawn()
    {
        _localUserData = new PlayerData(_localUserData.name, NetworkManager.Singleton.LocalClientId);
        VerifyConnection_ServerRpc();
    }

    public override void OnNetworkDespawn()
    {
        _onGameEnd();
    }

    [ServerRpc(RequireOwnership = false)]
    private void VerifyConnection_ServerRpc(ServerRpcParams serverRpcParams = default)
    {
        VerifyConnection_ClientRpc(serverRpcParams.Receive.SenderClientId);
    }

    [ClientRpc]
    private void VerifyConnection_ClientRpc(ulong clientId)
    {
        if (clientId == _localUserData.id)
        {
            VerifyConnectionConfirm_ServerRpc(_localUserData);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void VerifyConnectionConfirm_ServerRpc(PlayerData clientData)
    {
        dataStore.AddPlayer(clientData.id, clientData.name);
        var areAllPlayersConnected = NetworkManager.Singleton.ConnectedClients.Count >= _expectedPlayerCount;
        VerifyConnectionConfirm_ClientRpc(clientData.id, areAllPlayersConnected);
    }

    [ClientRpc]
    private void VerifyConnectionConfirm_ClientRpc(ulong clientId, bool canBeginGame)
    {
        if (clientId == _localUserData.id)
        {
            _onConnectionVerified?.Invoke();
            _hasConnected = true;
        }

        if (canBeginGame && _hasConnected)
        {
            BeginGame();
        }
    }

    private void BeginGame()
    {
        OnGameBeginning?.Invoke();

        LocalPlayerManager = Instantiate(playerManagerPrefab);
        _gameDisplayContainerInstance = Instantiate(gameDisplayContainerPrefab);
        dataStore.InitPlayerNames();

        if (IsServer)
        {
            dataStore.InitGameData();
            StartCoroutine(FinishInitialize());
        }
    }

    private IEnumerator FinishInitialize()
    {
        yield return new WaitForSeconds(0.3f);

        // todo init game
        _dealerIndex = GetDealerIndex();
        RoundState.NumPlayers = _expectedPlayerCount;

        // todo settings for num tricks
        StartRound(roundNumber: 1, numTricks: 1, isNumTricksAscending: true, isFirstRound: true);

        PlayerManagerRefreshState_ClientRpc();

        int GetDealerIndex()
        {
            return Random.Range(minInclusive: 0, maxExclusive: _expectedPlayerCount);
        }
    }
    
    #endregion

    [ClientRpc]
    public void PlayerManagerRefreshState_ClientRpc()
    {
        if (LocalPlayerManager != null)
        {
            LocalPlayerManager.RefreshState();
        }
    }

    public void LoadData()
    {
        dataStore.GetFullState(SetLocalPlayerData, SetLocalRoundData);
    }

    private void SetLocalPlayerData(PlayerData playerData)
    {
        _localUserData = playerData;
        LocalPlayerManager.OnLocalUserDataUpdate(_localUserData);
    }

    private void SetLocalRoundData(RoundState roundState)
    {
        _currentRound = roundState;
        _gameDisplayContainerInstance.OnCurrentRoundDataUpdate(_currentRound);
        LocalPlayerManager.OnCurrentRoundDataUpdate(_currentRound);
    }

    private int GetPlayerIndexAfter(int index)
    {
        index++;

        if (index >= _expectedPlayerCount)
        {
            index = 0;
        }

        return index;
    }

    private int AdvancePlayerIndexUntilLeadingPlayer(int index)
    {
        if (index >= 0)
        {
            index++;
        }

        if (index >= _expectedPlayerCount)
        {
            index = 0;
        }

        if (index == _leadingPlayerIndex)
        {
            index = -1;
        }

        return index;
    }

    private void SaveBet(int playerIndex, int betAmount)
    {
        if (!IsServer)
        {
            return;
        }
        
        _currentRound.PlayerIndexToBet = AdvancePlayerIndexUntilLeadingPlayer(_currentRound.PlayerIndexToBet);
        _currentRound.Bets[playerIndex] = betAmount;
        dataStore.SetCurrentRound(_currentRound);

        PlayerManagerRefreshState_ClientRpc();
    }

    public void MakeBet(int betAmount)
    {
        if (IsBetValid(betAmount))
        {
            SubmitBet_ServerRpc(betAmount);
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void SubmitBet_ServerRpc(int betAmount, ServerRpcParams serverRpcParams = default)
    {
        if (IsBetValid(betAmount) && _currentRound.PlayerIndexToBet == dataStore.GetIndexForClientId(serverRpcParams.Receive.SenderClientId))
        {
            SaveBet(_currentRound.PlayerIndexToBet, betAmount);
        }
    }

    private bool IsBetValid(int betAmount)
    {
        return betAmount >= 0 && betAmount <= _currentRound.NumTricks;
    }

    public void PlayCard(CardState card)
    {
        if (IsCardInLocalPlayerHand(card))
        {
            SubmitPlay_ServerRpc(StateConverter.SerializeCardState(card));
        }
    }

    public bool IsCardInLocalPlayerHand(CardState card)
    {
        // todo
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitPlay_ServerRpc(string serializedCard, ServerRpcParams serverRpcParams = default)
    {
        var card = StateConverter.DeserializeCardState(serializedCard);
        Debug.Log($"Valid play: {IsValidPlay(_currentRound.PlayerIndexToPlay, card)}");
        if (IsCardInPlayerHand(_currentRound.PlayerIndexToPlay, card) && _currentRound.PlayerIndexToPlay == dataStore.GetIndexForClientId(serverRpcParams.Receive.SenderClientId)
            && IsValidPlay(_currentRound.PlayerIndexToPlay, card))
        {
            SavePlay(_currentRound.PlayerIndexToPlay, card);
        }
    }

    private void SavePlay(int playerIndex, CardState card)
    {
        if (!IsServer)
        {
            return;
        }
        
        _currentRound.PlayerIndexToPlay = AdvancePlayerIndexUntilLeadingPlayer(_currentRound.PlayerIndexToPlay);

        if (_currentRound.CurrentTrick.LeadingCard.IsEmpty())
        {
            _currentRound.CurrentTrick.LeadingCard = card;
        }
        
        _currentRound.CurrentTrick.PlayedCards[playerIndex] = card;
        dataStore.SetCurrentRound(_currentRound);

        dataStore.RemoveCardFromPlayerHand(playerIndex, card);

        CheckRoundState();
        
        PlayerManagerRefreshState_ClientRpc();
    }

    private bool IsCardInPlayerHand(int playerIndex, CardState card)
    {
        // todo
        return true;
    }

    private bool IsValidPlay(int playerIndex, CardState card)
    {
        if (_currentRound.CurrentTrick.LeadingCard.IsEmpty())
        {
            return true;
        }

        if (_currentRound.CurrentTrick.LeadingCard.SuitKey == card.SuitKey)
        {
            return true;
        }
        
        return dataStore.GetPlayerHand(playerIndex).All(c => c.SuitKey != _currentRound.CurrentTrick.LeadingCard.SuitKey);
    }
    
    private void CheckRoundState()
    {
        if (!IsServer)
        {
            return;
        }
        
        // check if we are at the end of a trick
        if (_currentRound.PlayerIndexToPlay < 0)
        {
            ScoreTrick();
            // todo indicate this has happened?
            
            // check if we are at the end of a round
            if (IsRoundDone())
            {
                ScoreRound();
                NextRound();
            }
        }

        bool IsRoundDone()
        {
            return _currentRound.CompletedTricks.Length == _currentRound.NumTricks;
        }
    }

    private void ScoreTrick()
    {
        if (!IsServer)
        {
            return;
        }
        
        var trickWinnerIndex = DetermineTrickWinnerIndex();
        _currentRound.CurrentTrick.TrickWinnerIndex = trickWinnerIndex;
        var tricks = _currentRound.CompletedTricks.ToList();
        tricks.Add(_currentRound.CurrentTrick);
        _currentRound.CompletedTricks = tricks.ToArray();
        _currentRound.CurrentTrick = new TrickState
            { TrickWinnerIndex = -1, PlayedCards = new CardState[_expectedPlayerCount] };
        _currentRound.PlayerIndexToPlay = trickWinnerIndex;
        _leadingPlayerIndex = trickWinnerIndex;
        
        dataStore.SetCurrentRound(_currentRound);
        
        EndOfTrick_ClientRpc(_currentRound.CompletedTricks[^1]);

        int DetermineTrickWinnerIndex()
        {
            var leadingCard = _currentRound.CurrentTrick.LeadingCard;
            var trumpCard = _currentRound.TrumpCard;
            var highestCard = leadingCard;

            foreach (var card in _currentRound.CurrentTrick.PlayedCards)
            {
                if (card.Equals(leadingCard))
                {
                    continue;
                }

                if (card.SuitKey == leadingCard.SuitKey && card.SuitKey == highestCard.SuitKey &&
                    card.ValueKey > highestCard.ValueKey)
                {
                    highestCard = card;
                }

                if (!trumpCard.IsEmpty() && card.SuitKey == trumpCard.SuitKey &&
                    (card.SuitKey != highestCard.SuitKey || card.ValueKey > highestCard.ValueKey))
                {
                    highestCard = card;
                }
            }

            return Array.IndexOf(_currentRound.CurrentTrick.PlayedCards, highestCard);
        }
    }

    [ClientRpc]
    private void EndOfTrick_ClientRpc(TrickState trickState)
    {
        Debug.Log("EndOfTrick_ClientRpc");
        _gameDisplayContainerInstance.ClearPlayedCards();
        _gameDisplayContainerInstance.DisplayEndOfTrickUI(trickState);
    }

    private void ScoreRound()
    {
        if (!IsServer)
        {
            return;
        }

        var scores = new int[RoundState.NumPlayers];
        var tricksWonCounts = new int[RoundState.NumPlayers];

        for (var i = 0; i < _currentRound.CompletedTricks.Length; i++)
        {
            tricksWonCounts[_currentRound.CompletedTricks[i].TrickWinnerIndex]++;
        }

        for (var i = 0; i < _currentRound.Bets.Length; i++)
        {
            // todo config for points
            scores[i] = _currentRound.Bets[i] == tricksWonCounts[i] ? _currentRound.Bets[i] + 10 : 0;
        }

        var roundScore = new RoundScoreState
        {
            RoundNumber = _currentRound.RoundNumber, 
            Scores = scores
        };
        
        EndOfRound_ClientRpc(roundScore);
    }

    [ClientRpc]
    private void EndOfRound_ClientRpc(RoundScoreState roundScoreState)
    {
        _gameDisplayContainerInstance.InitEndOfRoundUI(roundScoreState);
        OnRoundEnd?.Invoke(roundScoreState);
    }
    
    private void NextRound()
    {
        if (!IsServer)
        {
            return;
        }
        
        int numTricks;
        
        if (_currentRound.IsNumTricksAscending)
        {
            numTricks = _currentRound.NumTricks + 1;

            if (numTricks == GetMaxNumTricks())
            {
                _currentRound.IsNumTricksAscending = false;
            }
        }
        else
        {
            numTricks = _currentRound.NumTricks - 1;

            if (numTricks < GetEndingNumTricks())
            {
                EndOfGame_ClientRpc();
                return;
            }
        }
        
        StartRound(_currentRound.RoundNumber + 1, numTricks, _currentRound.IsNumTricksAscending);

        // todo handle game settings properly
        int GetMaxNumTricks()
        {
            return 5;
        }

        int GetEndingNumTricks()
        {
            return 1;
        }
    }

    private void StartRound(int roundNumber, int numTricks, bool isNumTricksAscending, bool isFirstRound = false)
    {
        if (!IsServer)
        {
            return;
        }
        
        if (!isFirstRound)
        {
            _dealerIndex = GetPlayerIndexAfter(_dealerIndex);
        }
        
        _leadingPlayerIndex = GetPlayerIndexAfter(_dealerIndex);
        var cardShoe = new CardShoeState();
        _currentRound = new RoundState
        {
            RoundNumber = roundNumber,
            NumTricks = numTricks,
            IsNumTricksAscending = isNumTricksAscending,
            PlayerIndexToBet = _leadingPlayerIndex,
            PlayerIndexToPlay = _leadingPlayerIndex,
            CurrentTrick = new TrickState{TrickWinnerIndex = -1, PlayedCards = new CardState[_expectedPlayerCount]},
            CompletedTricks = Array.Empty<TrickState>()
        };

        DealForRound();

        _currentRound.TrumpCard = ShouldDrawTrumpCard() ? cardShoe.DealCard() : default;
        dataStore.SetCurrentRound(_currentRound);

        void DealForRound()
        {
            var hands = new CardState[_expectedPlayerCount][];
            
            for (var i = 0; i < _currentRound.NumTricks; i++)
            {
                var currentPlayerIndex = _leadingPlayerIndex;
                
                do
                {
                    hands[currentPlayerIndex] ??= new CardState[_currentRound.NumTricks];
                    
                    hands[currentPlayerIndex][i] = cardShoe.DealCard();
                    currentPlayerIndex = GetPlayerIndexAfter(currentPlayerIndex);
                } while (currentPlayerIndex != _leadingPlayerIndex);
            }

            for (var i = 0; i < hands.Length; i++)
            {
                dataStore.SetPlayerHand(i, hands[i]);
            }
        }

        bool ShouldDrawTrumpCard()
        {
            // todo game settings
            return true;
        }
    }

    [ClientRpc]
    public void EndOfGame_ClientRpc()
    {
        // todo update this when returning to lobby
        GameHasEnded = true;
    }
}