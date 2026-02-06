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
        public async Task StartNewRound(int mainPlayerBet = 0) {
            Deck.Reset(_numberOfDecks);

            // Clear all player hands
            foreach (var player in Players) {
                player.StartRound(mainPlayerBet);
            }
            
            Dealer.StartRound();
            CurrentPlayerIndex = 0;

            // Deal initial cards: round-robin style
            // First card to each player, then dealer, then second card to each player, then dealer
            for (var i = 0; i < NumberOfPlayers; i++) {
                await Task.Delay(TimeSpan.FromMilliseconds(DealOutTimer));
                var forceBlackjack = ForcePlayerBlackjack;
                Players[i].InitialDeal(Deck.DrawCard(), forceBlackjack, ForcePlayerSplitCards);
            }
            
            await Task.Delay(TimeSpan.FromMilliseconds(DealOutTimer));
            Dealer.AddCard(Deck.DrawCard());
            for (var i = 0; i < NumberOfPlayers; i++) {
                await Task.Delay(TimeSpan.FromMilliseconds(DealOutTimer));
                var forceBlackjack = ForcePlayerBlackjack;
                Players[i].InitialDeal(Deck.DrawCard(), forceBlackjack, ForcePlayerSplitCards);
            }
            
            await Task.Delay(TimeSpan.FromMilliseconds(DealOutTimer));
            Dealer.AddCard(Deck.DrawCard());

            // Check for immediate blackjacks
            var dealerHasBlackjack = Dealer.Hand.IsBlackjack();

            if (dealerHasBlackjack) {
                State = GameState.RoundOver;
                await PlayDealerTurn(); // TODO: fix this hacky way of showing dealers hand on blackjack
                return;
            }

            State = GameState.PlayerTurn;
            // Skip to first player who hasn't finished
            await MoveToNextPlayer();
        }

        private async Task PlayNpcTurn() {
            var currentPlayer = Players[CurrentPlayerIndex];
            if (CurrentPlayerIndex >= NumberOfPlayers || !currentPlayer.IsNpc) return;

            if (!currentPlayer.HasActiveHand()) {
                MoveToNextPlayer();
                return;
            }

            while (currentPlayer.HasActiveHand()) {
                await Task.Delay(TimeSpan.FromMilliseconds(1000));
                if (currentPlayer.GetCurrentHand() == null) {
                    GD.PrintErr("This is a bug, current hand should never be null, this is a bandaid for that lol");
                    return;
                }
                
                var npcMove = currentPlayer.NpcTurn(Dealer.GetUpCard().GetValue());

                switch (npcMove) {
                    case PlayerMove.Hit:
                        await Hit();
                        break;
                    case PlayerMove.Stand:
                        await Stand();
                        return;
                    case PlayerMove.Split:
                        await Split();
                        break;
                    case PlayerMove.DoubleDown:
                        if (CanDoubleDown()) {
                            await DoubleDown();
                        } else {
                            GD.PrintErr("NPC cannot double down");
                            await Hit();
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
        public async Task<bool> Hit(bool noAnimation = false) {
            if (State != GameState.PlayerTurn) {
                GD.PrintErr("Cannot hit - not player's turn");
                return false;
            }

            var currentPlayer = Players[CurrentPlayerIndex];
            var done = await currentPlayer.HitCurrentHand(Deck.DrawCard(), noAnimation);
            
            if (done) {
                await MoveToNextPlayer();
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
        public async Task DoubleDown() {
            if (State != GameState.PlayerTurn) {
                GD.PrintErr("Cannot double down - not player's turn");
                return;
            }
            
            GD.Print($"{Players[CurrentPlayerIndex].Name} doubled down");

            var currentPlayer = Players[CurrentPlayerIndex];
            var canDouble = await currentPlayer.DoubleDownCurrentHand(Deck.DrawCard());
            if (canDouble) {
                await Stand(true);
            }
        }
        
        /// <summary>
        /// Current player splits
        /// </summary>
        public async Task<bool> Split() {
            var currentPlayer = Players[CurrentPlayerIndex];
            
            var success = await currentPlayer.SplitHand();
            if (!success) {
                GD.PrintErr("Failed to split hand");
                return false;
            }
            
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            await Dealer.DealerPointToHand(Players[CurrentPlayerIndex].GetCurrentHandUI());
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            await Hit(true);

            return true;
        }

        /// <summary>
        /// Current player stands (ends their turn)
        /// </summary>
        public async Task Stand(bool noAnimation = false) {
            await Players[CurrentPlayerIndex].StandCurrentHand(noAnimation);
            await MoveToNextPlayer();
        }

        /// <summary>
        /// Moves to the next player or starts dealer turn if all players are done
        /// </summary>
        private async Task MoveToNextPlayer() {
            if (CurrentPlayerIndex < NumberOfPlayers && !Players[CurrentPlayerIndex].HasActiveHand()) {
                Players[CurrentPlayerIndex].IsTurn = false;
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
            
            await Dealer.DealerPointToHand(Players[CurrentPlayerIndex].GetCurrentHandUI());
            Players[CurrentPlayerIndex].IsTurn = true;
            Players[CurrentPlayerIndex].EmitHandStarted();
            while (CurrentPlayerIndex < NumberOfPlayers && Players[CurrentPlayerIndex].GetCurrentHand().CardCount < 2) {
                await Task.Delay(TimeSpan.FromMilliseconds(DealOutTimer));
                await Hit(true);
            }
            
            await PlayNpcTurn();
        }

        /// <summary>
        /// Dealer plays according to standard rules: hit on 16 or less, stand on 17 or more
        /// </summary>
        private async Task PlayDealerTurn() {
            Dealer.StartDealerTurn();
            await Task.Delay(TimeSpan.FromMilliseconds(1000));
            Dealer.Hand.FlipOverDealerCard();
            
            while (Dealer.Hand.GetValue() < 17) {
                await Task.Delay(TimeSpan.FromMilliseconds(1000));
                Dealer.AddCard(Deck.DrawCard());
            }

            DetermineWinner();
            
            await Task.Delay(TimeSpan.FromMilliseconds(1000));

            EmitSignal(SignalName.RoundEnded);
            
            foreach (var player in Players) {
                player.EndRound();
            }
            Dealer.ResetRound();
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
