using Godot;
using System;
using System.Collections.Generic;

namespace SlickBlackJack.Components;

public partial class Player : RefCounted {
    public string Name { get; set; }
    public List<Hand> Hands { get; set; } = []; // holds the list of all hands (needed for splitting)
    public int Chips { get; set; }
    public bool IsNpc { get; set; } = true;
    public int CurrentHandIndex { get; set; } = 0;
    
    // public Hand GetActiveHand() {
    //     return Hands.Find(hands => hands.Status == HandStatus.Active);
    // }

    public Hand GetCurrentHand() {
        return Hands[CurrentHandIndex];
    }

    public void StartRound() {
        Hands.Clear();
        Hands.Add(new Hand());
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
