using Godot;
using System;
using System.Collections.Generic;
using SlickBlackJack.Components;


public enum PlayerMove {
    Hit,
    Stand,
    Split,
    DoubleDown,
}

public partial class Player : Node2D {
    [Signal] public delegate void CurrentHandChangedEventHandler(int index);
    [Signal] public delegate void ChipsChangedEventHandler(int newChips);

    [Export] public Container HandsUIContainer { get; set; }
    [Export] public string Name { get; set; }
    [Export] public bool IsNpc { get; set; } = true;
    [Export] public int Heat { get; set; } = 0;
    [Export] public bool PlayWinSfx { get; set; }= false;
    public List<Hand> Hands { get; set; } = []; // holds the list of all hands (needed for splitting)
    public int PlayerBet { get; set; } = 10;

    private AudioStreamPlayer _winSfx;
    private int _chips = 1000;
    public int Chips {
        get => _chips;
        set {
            _chips = value;
            EmitSignal(SignalName.ChipsChanged, _chips);
        }
    }

    public int CurrentHandIndex { get; set; } = 0;
    
    private PackedScene _handScene = GD.Load<PackedScene>("res://scenes/hand/HandUI.tscn");

    public override void _Ready() {
        GD.Print("Player ready!");
        _winSfx = GetNode<AudioStreamPlayer>("WinSFX");

    }

    public Hand GetCurrentHand() {
        if (CurrentHandIndex >= Hands.Count) {
            GD.PrintErr($"Player {Name} has no active hands left to play.");
            return null;
        }
        
        return Hands[CurrentHandIndex];
    }
    
    // HandsUI is reverse of Hands, need to get the reversed value index
    public HandUI GetCurrentHandUI() {
        int uiIndex = Hands.Count - 1 - CurrentHandIndex;
        if (uiIndex < 0 || uiIndex >= Hands.Count) {
            return null;
        }

        return HandsUIContainer.GetChild(uiIndex) as HandUI;
    }
    
    public HandUI GetHandUIByIndex(int index) {
        int uiIndex = Hands.Count - 1 - index;
        if (uiIndex < 0 || uiIndex >= Hands.Count) {
            throw new IndexOutOfRangeException($"Invalid hand UI index: {index}");
        }

        return HandsUIContainer.GetChild(uiIndex) as HandUI;
    }

    public void StartRound() {
        Hands.Clear();
        CurrentHandIndex = 0;
        foreach (var handUi in HandsUIContainer.GetChildren()) {
            handUi.QueueFree();
        }
        
        var hand = new Hand(PlayerBet);
        Chips -= PlayerBet;
        
        Hands.Add(hand);
        AddHandToUi(hand);
    }
    
    private void AddHandToUi(Hand hand) {
        var handNode = _handScene.Instantiate<HandUI>();
        handNode.SetHand(hand);
        handNode.ZIndex = Hands.Count;
        HandsUIContainer.AddChild(handNode);
        HandsUIContainer.MoveChild(handNode, 0); // Adds new hand to the right
    }

    public void InitialDeal(Card card, bool forceBlackjack = false, bool forceSplitCards = false) {
        Hands[0].AddCard(card, false, forceBlackjack, forceSplitCards);

        if (Hands[0].IsBlackjack()) {
            Hands[0].Status = HandStatus.Done;
        }
    }

    public bool SplitHand() {
        // TODO: need to check if player has enough chips to split
        var currentHand = GetCurrentHand();

        if (Chips <= 0) {
            GD.Print("Not enough chips to split hand");
            return false;
        }

        if (!currentHand.CanSplit()) {
            GD.PrintErr($"invalid attempt to split {Name} hand {currentHand}");
            return false;
        }

        var chipsForHand = Math.Min(currentHand.Chips, Chips);
        var newHand = new Hand(chipsForHand);
        
        Chips -= chipsForHand;
        
        Hands.Add(newHand);
        AddHandToUi(newHand);
        
        var card = currentHand.RemoveCard(0);
        newHand.AddCard(card);
        
        return true;
    }

    public bool DoubleDownCurrentHand(Card card) {
        var currentHand = GetCurrentHand();

        if (!currentHand.CanDoubleDown() && Chips > 0) return false;
        
        var chipsForHand = Math.Min(currentHand.Chips, Chips);
        currentHand.Chips += chipsForHand;
        
        Chips -= chipsForHand;
        
        return HitCurrentHand(card);
    }

    public void DetermineHandResults(int dealerValue) {
        for (var i = 0; i < Hands.Count; i++) {
            var hand = Hands[i];
            var handUi = GetHandUIByIndex(i);
            
            var playerValue = hand.GetValue();
            if (playerValue > 21) {
                hand.Result = HandResult.DealerWin;
            }
            else if (dealerValue > 21) {
                hand.Result = HandResult.PlayerWin;
            }
            else if (playerValue > dealerValue) {
                hand.Result = HandResult.PlayerWin;
            }
            else if (playerValue < dealerValue) {
                hand.Result = HandResult.DealerWin;
            }
            else if (hand.IsBlackjack() && dealerValue != 21) {
                hand.Result = HandResult.PlayerBlackjack;
            }
            else {
                hand.Result = HandResult.Push;
            }
            
            
            if ((hand.Result == HandResult.PlayerWin || hand.Result == HandResult.PlayerBlackjack) && PlayWinSfx) {
                _winSfx.Play();
            }
            
            handUi.ShowResult(hand.Result);
            AdjustChips(hand);
        }
    }

    public void AdjustChips(Hand hand) {
        if (hand.Result == HandResult.PlayerWin) {
            Chips += hand.Chips * 2;
        }
        else if (hand.Result == HandResult.PlayerBlackjack) {
            Chips += hand.Chips + (int)(PlayerBet * (3.0 / 2.0));
        }
    }

    // Returns true if hand was busted
    public bool HitCurrentHand(Card card) {
        Hand activeHand = GetCurrentHand();
        activeHand.AddCard(card);
        
        if (activeHand.IsBusted()) {
            activeHand.Result = HandResult.DealerWin;
        }

        if (activeHand.IsBlackjack()) {
            activeHand.Result = HandResult.PlayerBlackjack;
        }
        
        if (activeHand.GetValue() >= 21) {
            activeHand.Status = HandStatus.Done;
            CurrentHandIndex++;
            EmitSignal(SignalName.CurrentHandChanged, CurrentHandIndex);
            return true;
        }

        return false;
    }

    public bool CanDoubleDown() {
        return GetCurrentHand().CanDoubleDown();
    }

    public void StandCurrentHand() {
        Hand activeHand = GetCurrentHand();
        activeHand.Stand();
        CurrentHandIndex++;
        EmitSignal(SignalName.CurrentHandChanged, CurrentHandIndex);
    }
    
    public bool HasActiveHand() {
        return Hands.Find(hand => hand.Status == HandStatus.Active) != null;
    }

    public void PrintHands() {
        foreach (var hand in Hands) {
            GD.Print($"Hand: {hand}");
        }
    }
    
    public void PrintResults() {
        foreach (var hand in Hands) {
            GD.Print($"Result: {hand.Result}");
        }
    }
    
    public PlayerMove NpcTurn(int dealerUpcard) {
        Hand currentHand = GetCurrentHand();
        int handValue = currentHand.GetValue();
        bool isSoft = currentHand.IsSoft();
        bool canSplit = currentHand.CanSplit();
        bool canDoubleDown = currentHand.CardCount == 2; // Can only double on initial two cards

        // Check for split first (only on initial two cards)
        if (canSplit) {
            var cards = currentHand.GetCards();
            int cardValue = cards[0].GetValue();

            // Always split Aces and 8s
            if (cards[0].IsAce() || cardValue == 8) {
                return PlayerMove.Split;
            }

            // Never split 5s and 10s
            if (cardValue == 5 || cardValue == 10) {
                // Fall through to regular strategy
            }
            // Split 2s, 3s, 7s if dealer shows 2-7
            else if ((cardValue == 2 || cardValue == 3 || cardValue == 7) && dealerUpcard >= 2 && dealerUpcard <= 7) {
                return PlayerMove.Split;
            }
            // Split 4s if dealer shows 5 or 6
            else if (cardValue == 4 && (dealerUpcard == 5 || dealerUpcard == 6)) {
                return PlayerMove.Split;
            }
            // Split 6s if dealer shows 2-6
            else if (cardValue == 6 && dealerUpcard >= 2 && dealerUpcard <= 6) {
                return PlayerMove.Split;
            }
            // Split 9s if dealer shows 2-6, 8, or 9 (not 7, 10, or Ace)
            else if (cardValue == 9 && (dealerUpcard <= 6 || dealerUpcard == 8 || dealerUpcard == 9)) {
                return PlayerMove.Split;
            }
        }

        // Soft hands (contains Ace counted as 11)
        if (isSoft) {
            // Soft 19+ (A,8 or better) - always stand
            if (handValue >= 19) {
                return PlayerMove.Stand;
            }
            // Soft 18 (A,7)
            else if (handValue == 18) {
                if (canDoubleDown && (dealerUpcard == 3 || dealerUpcard == 4 || dealerUpcard == 5 || dealerUpcard == 6)) {
                    return PlayerMove.DoubleDown;
                }
                if (dealerUpcard >= 9) {
                    return PlayerMove.Hit;
                }
                return PlayerMove.Stand;
            }
            // Soft 17 (A,6) - double against 3-6
            else if (handValue == 17 && canDoubleDown && dealerUpcard >= 3 && dealerUpcard <= 6) {
                return PlayerMove.DoubleDown;
            }
            // Soft 15-16 (A,4 or A,5) - double against 4-6
            else if ((handValue == 15 || handValue == 16) && canDoubleDown && dealerUpcard >= 4 && dealerUpcard <= 6) {
                return PlayerMove.DoubleDown;
            }
            // Soft 13-14 (A,2 or A,3) - double against 5-6
            else if ((handValue == 13 || handValue == 14) && canDoubleDown && dealerUpcard >= 5 && dealerUpcard <= 6) {
                return PlayerMove.DoubleDown;
            }
            // Otherwise hit
            else {
                return PlayerMove.Hit;
            }
        }

        // Hard hands
        // Hard 11 - always double if possible
        if (handValue == 11 && canDoubleDown) {
            return PlayerMove.DoubleDown;
        }
        // Hard 10 - double if dealer shows 2-9
        else if (handValue == 10 && canDoubleDown && dealerUpcard >= 2 && dealerUpcard <= 9) {
            return PlayerMove.DoubleDown;
        }
        // Hard 9 - double if dealer shows 3-6
        else if (handValue == 9 && canDoubleDown && dealerUpcard >= 3 && dealerUpcard <= 6) {
            return PlayerMove.DoubleDown;
        }
        // Always stand on 17+
        else if (handValue >= 17) {
            return PlayerMove.Stand;
        }
        // Stand on 13-16 if dealer shows 2-6
        else if (handValue >= 13 && handValue <= 16 && dealerUpcard >= 2 && dealerUpcard <= 6) {
            return PlayerMove.Stand;
        }
        // Stand on 12 if dealer shows 4-6
        else if (handValue == 12 && dealerUpcard >= 4 && dealerUpcard <= 6) {
            return PlayerMove.Stand;
        }
        // Hit on everything else
        else {
            return PlayerMove.Hit;
        }
    }
}
