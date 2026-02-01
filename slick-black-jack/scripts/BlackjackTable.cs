using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using SlickBlackJack.Components;

public partial class BlackjackTable : Node {
    [Export] public int NumberOfDecks { get; set; } = 1;
    [Export] public Node PlayersUIContainer { get; set; }

    public int NumberOfPlayers => _players.Count;
    
    private List<Player> _players = [];
    private BlackjackGame _game;
    private Dealer _dealer;

    [Signal]
    public delegate void RoundStartedEventHandler();

    [Signal]
    public delegate void HitEventHandler(int playerIndex, Card card);
    
    [Signal]
    public delegate void DoubleDownEventHandler(int playerIndex, Card card);

    [Signal]
    public delegate void StoodEventHandler(int playerIndex);

    [Signal]
    public delegate void PlayerTurnStartedEventHandler(int playerIndex);

    [Signal]
    public delegate void DealerRevealedEventHandler();

    [Signal]
    public delegate void RoundEndedEventHandler();

    public override void _Ready() {
        PlayersUIContainer ??= GetNode<Node>("Players");
        foreach (var node in PlayersUIContainer.GetChildren()) {
            if (node is Player playerNode) {
                _players.Add(playerNode);
            }
        }
        
        _dealer ??= GetNode<Dealer>("Dealer");
        
        _game = new BlackjackGame(_dealer, _players, NumberOfDecks);
        _game.RoundEnded += OnRoundEnded;
        GD.Print($"Blackjack Table initialized with {NumberOfPlayers} players");
    }

    public void OnRoundEnded() {
        EmitSignal(SignalName.RoundEnded);
    }

    /// <summary>
    /// Starts a new round of blackjack
    /// </summary>
    public async void StartNewRound() {
        await _game.StartNewRound();
        EmitSignal(SignalName.RoundStarted);

        PrintGameState();
    }

    /// <summary>
    /// Current player hits (requests another card)
    /// </summary>
    public void PlayerHit() {
        if (_game.State != GameState.PlayerTurn) {
            GD.PrintErr("Cannot hit - not player's turn");
            return;
        }

        int playerIndex = _game.CurrentPlayerIndex;
        _game.Hit();
        EmitSignal(SignalName.Hit, playerIndex);

        PrintGameState();
    }
    
    public void PlayerDoubleDown() {
        if (_game.State != GameState.PlayerTurn) {
            GD.PrintErr("Cannot double down - not player's turn");
            return;
        }

        if (!_game.CanDoubleDown()) {
            return;
        }

        int playerIndex = _game.CurrentPlayerIndex;
        _game.DoubleDown();
        EmitSignal(SignalName.DoubleDown, playerIndex);

        PrintGameState();
    }
    
    /// <summary>
    /// Current player splits
    /// </summary>
    public async void PlayerSplit() {
        if (_game.State != GameState.PlayerTurn) {
            GD.PrintErr("Cannot split - not player's turn");
            return;
        }

        int playerIndex = _game.CurrentPlayerIndex;
        var success = await _game.Split();

        if (!success) {
            return;
        }
        
        PrintGameState();
        
        // var lastCard = _game.Players[playerIndex].GetCurrentHand().GetCards()[_game.Players[playerIndex].GetCurrentHand().CardCount - 1];
        // EmitSignal(SignalName.Hit, playerIndex, lastCard);
        //
        //
        // // Check if this player busted and moved to next player
        // if (_game.State == GameState.PlayerTurn && _game.CurrentPlayerIndex != playerIndex) {
        //     EmitSignal(SignalName.PlayerTurnStarted, _game.CurrentPlayerIndex);
        // }
        //
        // // Check if all players are done and dealer plays
        // if (_game.State == GameState.DealerTurn || _game.State == GameState.RoundOver) {
        //     EmitSignal(SignalName.DealerRevealed);
        //     if (_game.State == GameState.RoundOver) {
        //         EmitSignal(SignalName.RoundEnded);
        //     }
        // }
    }

    /// <summary>
    /// Current player stands (ends their turn)
    /// </summary>
    public async void PlayerStand() {
        if (_game.State != GameState.PlayerTurn) {
            GD.PrintErr("Cannot stand - not player's turn");
            return;
        }

        int playerIndex = _game.CurrentPlayerIndex;
        await _game.Stand();
        EmitSignal(SignalName.Stood, playerIndex);

        PrintGameState();
    }

    /// <summary>
    /// Gets a specific player's hand value
    /// </summary>p
    public int GetPlayerValue(int playerIndex) {
        if (playerIndex < 0 || playerIndex >= NumberOfPlayers) {
            GD.PrintErr($"Invalid player index: {playerIndex}");
            return 0;
        }
        return _game.Players[playerIndex].GetCurrentHand().GetValue();
    }

    /// <summary>
    /// Gets the current dealer hand value
    /// </summary>
    public int GetDealerValue() {
        return _game.Dealer.Hand.GetValue();
    }

    /// <summary>
    /// Gets the dealer's visible card
    /// </summary>
    public Card GetDealerUpCard() {
        return _game.GetDealerUpCard();
    }

    /// <summary>
    /// Gets all cards for a specific player
    /// </summary>
    public Card[] GetPlayerCards(int playerIndex) {
        if (playerIndex < 0 || playerIndex >= NumberOfPlayers) {
            GD.PrintErr($"Invalid player index: {playerIndex}");
            return new Card[0];
        }
        return _game.Players[playerIndex].GetCurrentHand().GetCards().ToArray();
    }

    /// <summary>
    /// Gets a specific player's hand
    /// </summary>
    public Hand GetPlayerHand(int playerIndex) {
        if (playerIndex < 0 || playerIndex >= NumberOfPlayers) {
            GD.PrintErr($"Invalid player index: {playerIndex}");
            return null;
        }
        return _game.Players[playerIndex].GetCurrentHand();
    }
    
    /// <summary>
    /// Gets a specific player
    /// </summary>
    public Player GetPlayer(int playerIndex) {
        if (playerIndex < 0 || playerIndex >= NumberOfPlayers) {
            GD.PrintErr($"Invalid player index: {playerIndex}");
            return null;
        }
        return _game.Players[playerIndex];
    }

    /// <summary>
    /// Gets all dealer cards (use ShouldRevealDealerHand to check if they should be visible)
    /// </summary>
    public Card[] GetDealerCards() {
        return _game.Dealer.Hand.GetCards().ToArray();
    }

    /// <summary>
    /// Gets dealer hand
    /// </summary>
    public Hand GetDealerHand() {
        return _game.Dealer.Hand;
    }

    /// <summary>
    /// Gets the current player index
    /// </summary>
    public int GetCurrentPlayerIndex() {
        return _game.CurrentPlayerIndex;
    }

    /// <summary>
    /// Returns true if dealer's full hand should be revealed
    /// </summary>
    public bool ShouldRevealDealerHand() {
        return _game.ShouldRevealDealerHand();
    }

    /// <summary>
    /// Gets current game state
    /// </summary>
    public GameState GetGameState() {
        return _game.State;
    }

    /// <summary>
    /// Helper method to print current game state to console (useful for debugging)
    /// </summary>
    private void PrintGameState() {
        if (true) {
            return;
        }
        
        GD.Print("--- Game State ---");
        GD.Print($"State: {_game.State}");
        if (_game.State == GameState.PlayerTurn) {
            GD.Print($"Current Player: {_game.CurrentPlayerIndex}");
        }

        for (int i = 0; i < NumberOfPlayers; i++) {
            GD.Print($"Player {i} Hands:");
            _game.Players[i].PrintHands();
        }

        if (ShouldRevealDealerHand()) {
            GD.Print($"Dealer Hand: {_game.Dealer.Hand}");
        } else {
            GD.Print($"Dealer Up Card: {GetDealerUpCard()}");
        }

        if (_game.State == GameState.RoundOver) {
            for (int i = 0; i < NumberOfPlayers; i++) {
                GD.Print($"Player {i} Results:");
                _game.Players[i].PrintResults();
            }
        }
        GD.Print("------------------");
    }
}
