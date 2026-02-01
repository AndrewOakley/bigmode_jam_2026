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
        [Signal] public delegate void RoundEndedEventHandler();
        
        public Deck Deck { get; private set; }
        public List<Player> Players { get; private set; }
        public GameState State { get; private set; }
        public int CurrentPlayerIndex { get; private set; }
        public int NumberOfPlayers { get; private set; }
        public Dealer Dealer { get; private set; }

        private int _numberOfDecks;
        private const int DealOutTimer = 500;
        private const int InitalChipCount = 1000;
        
        // FOR DEBUGGING
        private bool ForcePlayerBlackjack = false;
        private bool ForcePlayerSplitCards = true;
        
        public BlackjackGame(Dealer dealer, List<Player> players, int numberOfDecks = 1) {
            Dealer = dealer;
            Players = players;
            //
            // foreach (var player in Players) {
            //     player.CurrentHandChanged += OnPlayerHandChanged;
            // }
            
            NumberOfPlayers = players.Count;
            
            _numberOfDecks = numberOfDecks;
            Deck = new Deck(numberOfDecks);
            State = GameState.Betting;
            CurrentPlayerIndex = 0;
        }

        /// <summary>
        /// Starts a new round of blackjack
        /// </summary>
        public async Task StartNewRound() {
            // Check if deck needs reshuffling (less than 25% remaining)
            if (Deck.CardsRemaining < (_numberOfDecks * 52) / 4) {
                Deck.Reset(_numberOfDecks);
                GD.Print("Deck reshuffled");
            }

            // Clear all player hands
            foreach (var player in Players) {
                player.StartRound();
            }

            Dealer.StartRound();
            CurrentPlayerIndex = 0;

            // Deal initial cards: round-robin style
            // First card to each player, then dealer, then second card to each player, then dealer
            for (var i = 0; i < NumberOfPlayers; i++) {
                await Task.Delay(TimeSpan.FromMilliseconds(DealOutTimer));
                var forceBlackjack = ForcePlayerBlackjack && !Players[i].IsNpc;
                Players[i].InitialDeal(Deck.DrawCard(), forceBlackjack, ForcePlayerSplitCards);
            }
            
            await Task.Delay(TimeSpan.FromMilliseconds(DealOutTimer));
            Dealer.AddCard(Deck.DrawCard());
            for (var i = 0; i < NumberOfPlayers; i++) {
                await Task.Delay(TimeSpan.FromMilliseconds(DealOutTimer));
                var forceBlackjack = ForcePlayerBlackjack && !Players[i].IsNpc;
                Players[i].InitialDeal(Deck.DrawCard(), forceBlackjack, ForcePlayerSplitCards);
            }
            
            await Task.Delay(TimeSpan.FromMilliseconds(DealOutTimer));
            Dealer.AddCard(Deck.DrawCard());

            // Check for immediate blackjacks
            var dealerHasBlackjack = Dealer.Hand.IsBlackjack();
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
                await PlayDealerTurn(); // TODO: fix this hacky way of showing dealers hand on blackjack
            } else {
                State = GameState.PlayerTurn;
                // Skip to first player who hasn't finished
                while (CurrentPlayerIndex < NumberOfPlayers && Players[CurrentPlayerIndex].GetCurrentHand().Result != HandResult.None) {
                    CurrentPlayerIndex++;
                }
                
                await PlayNpcTurn();
            }
        }

        private async Task PlayNpcTurn() {
            var currentPlayer = Players[CurrentPlayerIndex];
            if (CurrentPlayerIndex >= NumberOfPlayers || !currentPlayer.IsNpc) return;
            
            while (currentPlayer.HasActiveHand()) {
                await Task.Delay(TimeSpan.FromMilliseconds(1000));
                if (currentPlayer.GetCurrentHand() == null) {
                    GD.PrintErr("This is a bug, current hand should never be null, this is a bandaid for that lol");
                    return;
                }
                
                var npcMove = currentPlayer.NpcTurn(Dealer.GetUpCard().GetValue());

                switch (npcMove) {
                    case PlayerMove.Hit:
                        Hit();
                        break;
                    case PlayerMove.Stand:
                        await Stand();
                        return;
                    case PlayerMove.Split:
                        await Split();
                        break;
                    case PlayerMove.DoubleDown:
                        if (CanDoubleDown()) {
                            DoubleDown();
                        } else {
                            GD.PrintErr("NPC cannot double down");
                            Hit();
                        }
                        break;
                    default:
                        GD.PrintErr($"Invalid npc move: {npcMove}");
                        break;
                }
            }
        }

        /// <summary>
        /// Current player hits (takes another card)
        /// </summary>
        public bool Hit() {
            if (State != GameState.PlayerTurn) {
                GD.PrintErr("Cannot hit - not player's turn");
                return false;
            }

            var currentPlayer = Players[CurrentPlayerIndex];
            var done = currentPlayer.HitCurrentHand(Deck.DrawCard());
            if (done) {
                MoveToNextPlayer();
            }

            return done;
        }

        public bool CanDoubleDown() {
            if (State != GameState.PlayerTurn) {
                GD.PrintErr("Cannot double down - not player's turn");
                return false;
            }

            var currentPlayer = Players[CurrentPlayerIndex];
            var currentHand = currentPlayer.GetCurrentHand();

            return currentHand.CanDoubleDown();
        }
        
        /// <summary>
        /// Current player hits (takes another card)
        /// </summary>
        public async void DoubleDown() {
            if (State != GameState.PlayerTurn) {
                GD.PrintErr("Cannot double down - not player's turn");
            }
            
            GD.Print($"{Players[CurrentPlayerIndex].Name} doubled down");

            var currentPlayer = Players[CurrentPlayerIndex];
            var done = currentPlayer.HitCurrentHand(Deck.DrawCard());
            if (done) {
                MoveToNextPlayer();
            }
            else {
                await Stand();
            }

        }
        
        /// <summary>
        /// Current player splits
        /// </summary>
        public async Task<bool> Split() {
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
            
            
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            Dealer.DealerPointToHand(Players[CurrentPlayerIndex].GetCurrentHandUI());
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            Hit();

            return true;
        }

        /// <summary>
        /// Current player stands (ends their turn)
        /// </summary>
        public async Task Stand() {
            if (State != GameState.PlayerTurn) {
                GD.PrintErr("Cannot stand - not player's turn");
                return;
            }

            Players[CurrentPlayerIndex].StandCurrentHand();
            await MoveToNextPlayer();
        }

        /// <summary>
        /// Moves to the next player or starts dealer turn if all players are done
        /// </summary>
        private async Task MoveToNextPlayer() {
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
                await PlayDealerTurn();
                return;
            }
            
            Dealer.DealerPointToHand(Players[CurrentPlayerIndex].GetCurrentHandUI());
            while (Players[CurrentPlayerIndex].GetCurrentHand().CardCount < 2) {
                await Task.Delay(TimeSpan.FromMilliseconds(DealOutTimer));
                Hit();
            }

            await PlayNpcTurn();
        }

        /// <summary>
        /// Dealer plays according to standard rules: hit on 16 or less, stand on 17 or more
        /// </summary>
        private async Task PlayDealerTurn() {
            await Task.Delay(TimeSpan.FromMilliseconds(1000));
            Dealer.Hand.FlipOverDealerCard();
            
            while (Dealer.Hand.GetValue() < 17) {
                await Task.Delay(TimeSpan.FromMilliseconds(1000));
                Dealer.AddCard(Deck.DrawCard());
            }

            DetermineWinner();

            EmitSignal(SignalName.RoundEnded);
        }

        /// <summary>
        /// Determines the winner after both players and dealer have finished
        /// </summary>
        private void DetermineWinner() {
            State = GameState.RoundOver;

            var dealerValue = Dealer.Hand.GetValue();

            for (var i = 0; i < NumberOfPlayers; i++) {
                Players[i].DetermineHandResults(dealerValue);
            }
        }

        /// <summary>
        /// Gets the dealer's visible card (first card)
        /// </summary>      
        public Card GetDealerUpCard() {
            var cards = Dealer.Hand.GetCards();
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
