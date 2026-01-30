using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        public List<Player> Players { get; private set; }
        public Hand DealerHand { get; private set; }
        public GameState State { get; private set; }
        public List<GameResult> Results { get; private set; }
        public int CurrentPlayerIndex { get; private set; }
        public int NumberOfPlayers { get; private set; }

        private int _numberOfDecks;
        private const int DealOutTimer = 500;

        public BlackjackGame(int numberOfDecks = 1, int numberOfPlayers = 3) {
            _numberOfDecks = numberOfDecks;
            NumberOfPlayers = numberOfPlayers;
            Deck = new Deck(numberOfDecks);
            Players = [];
            for (var i = 0; i < numberOfPlayers; i++) {
                Players.Add(new Player {
                    Name = $"Player {i + 1}",
                    Hand = new Hand(),
                    Chips = 1000
                });
                
                // TODO: change this but for now hard code player to always be 2nd
                if (i == 1) {
                    Players[i].IsNpc = false;
                }
            }
            DealerHand = new Hand();
            State = GameState.Betting;
            Results = [];
            for (var i = 0; i < numberOfPlayers; i++) {
                Results.Add(GameResult.None);
            }
            CurrentPlayerIndex = 0;
        }

        /// <summary>
        /// Starts a new round of blackjack
        /// </summary>
        public async void StartNewRound() {
            // Check if deck needs reshuffling (less than 25% remaining)
            if (Deck.CardsRemaining < (_numberOfDecks * 52) / 4) {
                Deck.Reset(_numberOfDecks);
                GD.Print("Deck reshuffled");
            }

            // Clear all player hands
            foreach (var player in Players) {
                player.Hand.Clear();
            }
            DealerHand.Clear();

            // Reset all results
            for (var i = 0; i < NumberOfPlayers; i++) {
                Results[i] = GameResult.None;
            }

            CurrentPlayerIndex = 0;

            // Deal initial cards: round-robin style
            // First card to each player, then dealer, then second card to each player, then dealer
            for (var i = 0; i < NumberOfPlayers; i++) {
                await Task.Delay(TimeSpan.FromMilliseconds(DealOutTimer));
                Players[i].Hand.AddCard(Deck.DrawCard());
            }
            
            await Task.Delay(TimeSpan.FromMilliseconds(DealOutTimer));
            DealerHand.AddCard(Deck.DrawCard());
            for (var i = 0; i < NumberOfPlayers; i++) {
                await Task.Delay(TimeSpan.FromMilliseconds(DealOutTimer));
                Players[i].Hand.AddCard(Deck.DrawCard());
            }
            
            await Task.Delay(TimeSpan.FromMilliseconds(DealOutTimer));
            DealerHand.AddCard(Deck.DrawCard(), true);

            // Check for immediate blackjacks
            var dealerHasBlackjack = DealerHand.IsBlackjack();
            var allPlayersFinished = true;

            for (var i = 0; i < NumberOfPlayers; i++) {
                if (Players[i].Hand.IsBlackjack()) {
                    if (dealerHasBlackjack) {
                        Results[i] = GameResult.Push;
                    } else {
                        Results[i] = GameResult.PlayerBlackjack;
                    }
                } else if (dealerHasBlackjack) {
                    Results[i] = GameResult.DealerWin;
                } else {
                    allPlayersFinished = false;
                }
            }

            if (allPlayersFinished) {
                State = GameState.RoundOver;
                PlayDealerTurn(); // TODO: fix this hacky way of showing dealers hand on blackjack
            } else {
                State = GameState.PlayerTurn;
                // Skip to first player who hasn't finished
                while (CurrentPlayerIndex < NumberOfPlayers && Results[CurrentPlayerIndex] != GameResult.None) {
                    CurrentPlayerIndex++;
                }
                
                // TODO: consolidate this logic with the move to next player method
                if (Players[CurrentPlayerIndex].IsNpc) {
                    // TODO: implement NPC logic
                    Stand();
                }
            }
        }

        /// <summary>
        /// Current player hits (takes another card)
        /// </summary>
        public void Hit() {
            
            if (State != GameState.PlayerTurn) {
                GD.PrintErr("Cannot hit - not player's turn");
                return;
            }

            Players[CurrentPlayerIndex].Hand.AddCard(Deck.DrawCard());
            
            if (Players[CurrentPlayerIndex].Hand.IsBusted()) {
                Results[CurrentPlayerIndex] = GameResult.DealerWin;
                MoveToNextPlayer();
            }
        }

        /// <summary>
        /// Current player stands (ends their turn)
        /// </summary>
        public void Stand() {
            if (State != GameState.PlayerTurn) {
                GD.PrintErr("Cannot stand - not player's turn");
                return;
            }

            MoveToNextPlayer();
        }

        /// <summary>
        /// Moves to the next player or starts dealer turn if all players are done
        /// </summary>
        private void MoveToNextPlayer() {
            CurrentPlayerIndex++;

            // Skip players who already finished (blackjack/bust)
            while (CurrentPlayerIndex < NumberOfPlayers && Results[CurrentPlayerIndex] != GameResult.None) {
                CurrentPlayerIndex++;
            }

            if (CurrentPlayerIndex >= NumberOfPlayers) {
                // All players finished, dealer's turn
                State = GameState.DealerTurn;
                PlayDealerTurn();
                return;
            }
            
            if (Players[CurrentPlayerIndex].IsNpc) {
                // TODO: implement NPC logic
                Stand();
                return;
            }
        }

        /// <summary>
        /// Dealer plays according to standard rules: hit on 16 or less, stand on 17 or more
        /// </summary>
        private async void PlayDealerTurn() {
            await Task.Delay(TimeSpan.FromMilliseconds(1000));
            DealerHand.FlipOverDealerCard();
            
            while (DealerHand.GetValue() < 17) {
                await Task.Delay(TimeSpan.FromMilliseconds(1000));
                DealerHand.AddCard(Deck.DrawCard());
            }

            DetermineWinner();
        }

        /// <summary>
        /// Determines the winner after both players and dealer have finished
        /// </summary>
        private void DetermineWinner() {
            State = GameState.RoundOver;

            var dealerValue = DealerHand.GetValue();

            for (var i = 0; i < NumberOfPlayers; i++) {
                // Skip players who already have a result (blackjack/bust)
                if (Results[i] != GameResult.None) {
                    continue;
                }

                var playerValue = Players[i].Hand.GetValue();

                if (DealerHand.IsBusted()) {
                    Results[i] = GameResult.PlayerWin;
                } else if (playerValue > dealerValue) {
                    Results[i] = GameResult.PlayerWin;
                } else if (dealerValue > playerValue) {
                    Results[i] = GameResult.DealerWin;
                } else {
                    Results[i] = GameResult.Push;
                }
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
