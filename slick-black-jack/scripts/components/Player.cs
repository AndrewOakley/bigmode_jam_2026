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
    
    [Export] public Container HandsUIContainer { get; set; }
    [Export] public string Name { get; set; }
    [Export] public bool IsNpc { get; set; } = true;
    
    public List<Hand> Hands { get; set; } = []; // holds the list of all hands (needed for splitting)
    public int Chips { get; set; } = 1000;
    public int CurrentHandIndex { get; set; } = 0;
    
    private PackedScene _handScene = GD.Load<PackedScene>("res://scenes/hand/HandUI.tscn");

    public override void _Ready() {
        GD.Print("Player ready!");
    }

    public Hand GetCurrentHand() {
        if (CurrentHandIndex >= Hands.Count) {
            GD.PushWarning($"Player {Name} has no active hands left to play.");
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

    public void StartRound() {
        Hands.Clear();
        CurrentHandIndex = 0;
        foreach (var handUi in HandsUIContainer.GetChildren()) {
            handUi.QueueFree();
        }
        
        var hand = new Hand();
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
        var currentHand = GetCurrentHand();

        if (!currentHand.CanSplit()) {
            GD.PrintErr($"invalid attempt to split {Name} hand {currentHand}");
            return false;
        }
        
        var newHand = new Hand();
        Hands.Add(newHand);
        AddHandToUi(newHand);
        
        var card = currentHand.RemoveCard(0);
        newHand.AddCard(card);
        
        return true;
    }

    public void DetermineHandResults(int dealerValue) {
        foreach (var hand in Hands) {
            if (hand.Result != HandResult.None) continue;

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
            else {
                hand.Result = HandResult.Push;
            }
        }
    }

    // Returns true if hand was busted
    public bool HitCurrentHand(Card card) {
        Hand activeHand = GetCurrentHand();
        activeHand.AddCard(card);
        
        if (activeHand.IsBusted()) {
            activeHand.Result = HandResult.DealerWin;
        }
        
        if (activeHand.GetValue() >= 21) {
            activeHand.Status = HandStatus.Done;
            CurrentHandIndex++;
            EmitSignal(SignalName.CurrentHandChanged, CurrentHandIndex);
            return true;
        }

        return false;
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
                if (dealerUpcard >= 9) {
                    return PlayerMove.Hit;
                }
                return PlayerMove.Stand;
            }
            // Soft 17 or less - always hit
            else {
                return PlayerMove.Hit;
            }
        }

        // Hard hands
        // Always stand on 17+
        if (handValue >= 17) {
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
