using Godot;
using System;
using System.Collections.Generic;
using SlickBlackJack.Components;


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
}
