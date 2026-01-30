using Godot;
using System;

namespace SlickBlackJack.Components {
    public enum GameState {
        Betting,
        PlayerTurn,
        DealerTurn,
        RoundOver
    }

    public enum GameResult { 
        None,
        PlayerWin,
        DealerWin,
        Push,
        PlayerBlackjack
    }

    public partial class BlackjackGame : RefCounted {
        public Deck Deck { get; private set; }
        public Hand PlayerHand { get; private set; }
        public Hand DealerHand { get; private set; }
        public GameState State { get; private set; }
        public GameResult Result { get; private set; }

        private int _numberOfDecks;

        public BlackjackGame(int numberOfDecks = 1) {
            _numberOfDecks = numberOfDecks;
            Deck = new Deck(numberOfDecks);
            PlayerHand = new Hand();
            DealerHand = new Hand();
            State = GameState.Betting;
            Result = GameResult.None;
        }

        /// <summary>
        /// Starts a new round of blackjack
        /// </summary>
        public void StartNewRound() {
            // Check if deck needs reshuffling (less than 25% remaining)
            if (Deck.CardsRemaining < (_numberOfDecks * 52) / 4) {
                Deck.Reset(_numberOfDecks);
                GD.Print("Deck reshuffled");
            }

            PlayerHand.Clear();
            DealerHand.Clear();
            Result = GameResult.None;

            // Deal initial cards: Player, Dealer, Player, Dealer
            PlayerHand.AddCard(Deck.DrawCard());
            DealerHand.AddCard(Deck.DrawCard());
            PlayerHand.AddCard(Deck.DrawCard());
            DealerHand.AddCard(Deck.DrawCard());

            // Check for immediate blackjacks
            if (PlayerHand.IsBlackjack()) {
                if (DealerHand.IsBlackjack()) {
                    State = GameState.RoundOver;
                    Result = GameResult.Push;
                } else {
                    State = GameState.RoundOver;
                    Result = GameResult.PlayerBlackjack;
                }
            } else if (DealerHand.IsBlackjack()) {
                State = GameState.RoundOver;
                Result = GameResult.DealerWin;
            } else {
                State = GameState.PlayerTurn;
            }
        }

        /// <summary>
        /// Player hits (takes another card)
        /// </summary>
        public void Hit() {
            if (State != GameState.PlayerTurn) {
                GD.PrintErr("Cannot hit - not player's turn");
                return;
            }

            PlayerHand.AddCard(Deck.DrawCard());

            if (PlayerHand.IsBusted()) {
                State = GameState.RoundOver;
                Result = GameResult.DealerWin;
            }
        }

        /// <summary>
        /// Player stands (ends their turn)
        /// </summary>
        public void Stand() {
            if (State != GameState.PlayerTurn) {
                GD.PrintErr("Cannot stand - not player's turn");
                return;
            }

            State = GameState.DealerTurn;
            PlayDealerTurn();
        }

        /// <summary>
        /// Dealer plays according to standard rules: hit on 16 or less, stand on 17 or more
        /// </summary>
        private void PlayDealerTurn() {
            while (DealerHand.GetValue() < 17) {
                DealerHand.AddCard(Deck.DrawCard());
            }

            DetermineWinner();
        }

        /// <summary>
        /// Determines the winner after both player and dealer have finished
        /// </summary>
        private void DetermineWinner() {
            State = GameState.RoundOver;

            int playerValue = PlayerHand.GetValue();
            int dealerValue = DealerHand.GetValue();

            if (DealerHand.IsBusted()) {
                Result = GameResult.PlayerWin;
            } else if (playerValue > dealerValue) {
                Result = GameResult.PlayerWin;
            } else if (dealerValue > playerValue) {
                Result = GameResult.DealerWin;
            } else {
                Result = GameResult.Push;
            }
        }

        /// <summary>
        /// Gets the dealer's visible card (first card)
        /// </summary>
        public Card GetDealerUpCard() {
            var cards = DealerHand.GetCards();
            return cards.Count > 0 ? cards[0] : null;
        }

        /// <summary>
        /// Returns true if the dealer's hand should be fully revealed
        /// </summary>
        public bool ShouldRevealDealerHand() {
            return State == GameState.DealerTurn || State == GameState.RoundOver;
        }
    }
}
