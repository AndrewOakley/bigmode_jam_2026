using Godot;
using System;
using System.Collections.Generic;
using SlickBlackJack.Components;


public partial class Player : Node2D {
    [Export] public HBoxContainer HandsUIContainer { get; set; }
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
        return Hands[CurrentHandIndex];
    }

    public void StartRound() {
        Hands.Clear();
        foreach (var handUi in HandsUIContainer.GetChildren()) {
            handUi.QueueFree();
        }
        
        var hand = new Hand();
        Hands.Add(hand);
        var handNode = _handScene.Instantiate<HandUI>();
        handNode.SetHand(hand);
        HandsUIContainer.AddChild(handNode);
    }

    public void InitialDeal(Card card, bool forceBlackjack = false) {
        Hands[0].AddCard(card, false, forceBlackjack);
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
