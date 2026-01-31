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
        public int CurrentPlayerIndex { get; private set; }
        public int NumberOfPlayers { get; private set; }

        private int _numberOfDecks;
        private const int DealOutTimer = 500;
        private const int InitalChipCount = 1000;
        
        // FOR DEBUGGING
        private bool ForcePlayerBlackjack = false;
        private bool ForcePlayerSplitCards = true;

        public BlackjackGame(List<Player> players, int numberOfDecks = 1) {
            Players = players;
            NumberOfPlayers = players.Count;
            
            _numberOfDecks = numberOfDecks;
            Deck = new Deck(numberOfDecks);
            DealerHand = new Hand();
            State = GameState.Betting;
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
                player.StartRound();
            }
            DealerHand.Clear();
            CurrentPlayerIndex = 0;

            // Deal initial cards: round-robin style
            // First card to each player, then dealer, then second card to each player, then dealer
            for (var i = 0; i < NumberOfPlayers; i++) {
                await Task.Delay(TimeSpan.FromMilliseconds(DealOutTimer));
                var forceBlackjack = ForcePlayerBlackjack && !Players[i].IsNpc;
                Players[i].InitialDeal(Deck.DrawCard(), forceBlackjack, ForcePlayerSplitCards);
            }
            
            await Task.Delay(TimeSpan.FromMilliseconds(DealOutTimer));
            DealerHand.AddCard(Deck.DrawCard());
            for (var i = 0; i < NumberOfPlayers; i++) {
                await Task.Delay(TimeSpan.FromMilliseconds(DealOutTimer));
                var forceBlackjack = ForcePlayerBlackjack && !Players[i].IsNpc;
                Players[i].InitialDeal(Deck.DrawCard(), forceBlackjack, ForcePlayerSplitCards);
            }
            
            await Task.Delay(TimeSpan.FromMilliseconds(DealOutTimer));
            DealerHand.AddCard(Deck.DrawCard(), true);

            // Check for immediate blackjacks
            var dealerHasBlackjack = DealerHand.IsBlackjack();
            var allPlayersFinished = true;

            for (var i = 0; i < NumberOfPlayers; i++) {
                if (Players[i].GetCurrentHand().IsBlackjack()) {
                    if (dealerHasBlackjack) {
                        Players[i].GetCurrentHand().Result = HandResult.Push;
                    } else {
                        Players[i].GetCurrentHand().Result = HandResult.PlayerBlackjack;
                    }
                } else if (dealerHasBlackjack) {
                    Players[i].GetCurrentHand().Result = HandResult.DealerWin;
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
                while (CurrentPlayerIndex < NumberOfPlayers && Players[CurrentPlayerIndex].GetCurrentHand().Result != HandResult.None) {
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

            var currentPlayer = Players[CurrentPlayerIndex];
            var busted = currentPlayer.HitCurrentHand(Deck.DrawCard());
            if (busted) {
                MoveToNextPlayer();
            }
        }
        
        /// <summary>
        /// Current player splits
        /// </summary>
        public bool Split() {
            if (State != GameState.PlayerTurn) {
                GD.PrintErr("Cannot split - not player's turn");
                return false;
            }

            var currentPlayer = Players[CurrentPlayerIndex];
            Hand activeHand = currentPlayer.GetCurrentHand();
            var canSplit = activeHand.CanSplit();
            
            if (!canSplit) {
                GD.PrintErr("Cannot split");
                return false;
            }
            
            var success = currentPlayer.SplitHand();
            if (!success) {
                GD.PrintErr("Failed to split hand");
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Current player stands (ends their turn)
        /// </summary>
        public void Stand() {
            if (State != GameState.PlayerTurn) {
                GD.PrintErr("Cannot stand - not player's turn");
                return;
            }

            Players[CurrentPlayerIndex].StandCurrentHand();
            MoveToNextPlayer();
        }

        /// <summary>
        /// Moves to the next player or starts dealer turn if all players are done
        /// </summary>
        private async void MoveToNextPlayer() {
            if (!Players[CurrentPlayerIndex].HasActiveHand()) {
                CurrentPlayerIndex++;

                // Skip players who already finished (blackjack/bust)
                while (CurrentPlayerIndex < NumberOfPlayers && !Players[CurrentPlayerIndex].HasActiveHand()) {
                    CurrentPlayerIndex++;
                }
            }

            if (CurrentPlayerIndex >= NumberOfPlayers) {
                // All players finished, dealer's turn
                State = GameState.DealerTurn;
                PlayDealerTurn();
                return;
            }
            
            while (Players[CurrentPlayerIndex].GetCurrentHand().CardCount < 2) {
                await Task.Delay(TimeSpan.FromMilliseconds(DealOutTimer));
                Hit();
            }
            
            if (Players[CurrentPlayerIndex].IsNpc) {
                // TODO: implement NPC logic
                Stand();
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
                Players[i].DetermineHandResults(dealerValue);
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
