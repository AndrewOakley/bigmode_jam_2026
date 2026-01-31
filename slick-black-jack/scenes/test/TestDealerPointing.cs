using Godot;
using System;
using SlickBlackJack.Components;

public partial class TestDealerPointing : Node2D {
    [Export] public Dealer Dealer;
    [Export] public HandUI HandUi;
    
    public override void _Ready() {
        Dealer ??= GetNode<Dealer>("Dealer");
        HandUi ??= GetNode<HandUI>("HandUi");
        
        var testHand = new Hand();
        HandUi.SetHand(testHand);
        testHand.AddCard(new Card(Suit.Clubs, Rank.Ace));
        testHand.AddCard(new Card(Suit.Clubs, Rank.Ace));
        
        Dealer.DealerPointToHand(HandUi);
    }
}
