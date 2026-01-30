using Godot;
using SlickBlackJack.Components;

public partial class BlackjackTable : Node {
    [Export] public int NumberOfDecks { get; set; } = 1;

    private BlackjackGame _game;

    [Signal]
    public delegate void RoundStartedEventHandler();

    [Signal]
    public delegate void HitEventHandler(Card card);

    [Signal]
    public delegate void StoodEventHandler();

    [Signal]
    public delegate void DealerRevealedEventHandler();

    [Signal]
    public delegate void RoundEndedEventHandler(GameResult result);

    public override void _Ready() {
        _game = new BlackjackGame(NumberOfDecks);
        GD.Print("Blackjack Table initialized");
    }

    /// <summary>
    /// Starts a new round of blackjack
    /// </summary>
    public void StartNewRound() {
        _game.StartNewRound();
        EmitSignal(SignalName.RoundStarted);

        PrintGameState();

        // If round ended immediately (blackjacks), emit the signal
        if (_game.State == GameState.RoundOver) {
            EmitSignal(SignalName.RoundEnded, (int)_game.Result);
        }
    }

    /// <summary>
    /// Player hits (requests another card)
    /// </summary>
    public void PlayerHit() {
        if (_game.State != GameState.PlayerTurn) {
            GD.PrintErr("Cannot hit - not player's turn");
            return;
        }

        _game.Hit();
        var lastCard = _game.PlayerHand.GetCards()[_game.PlayerHand.CardCount - 1];
        EmitSignal(SignalName.Hit, lastCard);

        PrintGameState();

        // Check if round ended (player busted)
        if (_game.State == GameState.RoundOver) {
            EmitSignal(SignalName.RoundEnded, (int)_game.Result);
        }
    }

    /// <summary>
    /// Player stands (ends their turn, dealer plays)
    /// </summary>
    public void PlayerStand() {
        if (_game.State != GameState.PlayerTurn) {
            GD.PrintErr("Cannot stand - not player's turn");
            return;
        }

        _game.Stand();
        EmitSignal(SignalName.Stood);
        EmitSignal(SignalName.DealerRevealed);

        PrintGameState();

        // Dealer has finished playing, round is over
        EmitSignal(SignalName.RoundEnded, (int)_game.Result);
    }

    /// <summary>
    /// Gets the current player hand value
    /// </summary>
    public int GetPlayerValue() {
        return _game.PlayerHand.GetValue();
    }

    /// <summary>
    /// Gets the current dealer hand value
    /// </summary>
    public int GetDealerValue() {
        return _game.DealerHand.GetValue();
    }

    /// <summary>
    /// Gets the dealer's visible card
    /// </summary>
    public Card GetDealerUpCard() {
        return _game.GetDealerUpCard();
    }

    /// <summary>
    /// Gets all player cards
    /// </summary>
    public Card[] GetPlayerCards() {
        return _game.PlayerHand.GetCards().ToArray();
    }
    
    /// <summary>
    /// Gets dealer hand
    /// </summary>
    public Hand GetPlayerHand() {
        return _game.PlayerHand;
    }

    /// <summary>
    /// Gets all dealer cards (use ShouldRevealDealerHand to check if they should be visible)
    /// </summary>
    public Card[] GetDealerCards() {
        return _game.DealerHand.GetCards().ToArray();
    }
    
    /// <summary>
    /// Gets dealer hand
    /// </summary>
    public Hand GetDealerHand() {
        return _game.DealerHand;
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
    /// Gets current game result
    /// </summary>
    public GameResult GetGameResult() {
        return _game.Result;
    }

    /// <summary>
    /// Helper method to print current game state to console (useful for debugging)
    /// </summary>
    private void PrintGameState() {
        GD.Print("--- Game State ---");
        GD.Print($"State: {_game.State}");
        GD.Print($"Player Hand: {_game.PlayerHand}");

        if (ShouldRevealDealerHand()) {
            GD.Print($"Dealer Hand: {_game.DealerHand}");
        } else {
            GD.Print($"Dealer Up Card: {GetDealerUpCard()}");
        }

        if (_game.State == GameState.RoundOver) {
            GD.Print($"Result: {_game.Result}");
        }
        GD.Print("------------------");
    }
}
