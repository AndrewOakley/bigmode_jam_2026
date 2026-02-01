using Godot;
using System;
using System.Threading.Tasks;
using SlickBlackJack.Components;

public partial class Dealer : Node2D {
    public Marker2D DealerFinger;
    private Marker2D _fingerOrigin;
    private Control _handContainer;
    
    public Hand Hand { get; private set; } = new Hand();
    
    private PackedScene _handScene = GD.Load<PackedScene>("res://scenes/hand/HandUI.tscn");
    
    public override void _Ready() {
        DealerFinger ??= GetNode<Marker2D>("DealerFinger");
        _handContainer ??= GetNode<Control>("HandContainer");
        _fingerOrigin ??= GetNode<Marker2D>("FingerOrigin");
    }

    public void StartRound() {
        foreach (var child in _handContainer.GetChildren()) {
            child.QueueFree();
        }
        Hand.Clear();
        DealerFinger.Hide();
        DealerFinger.GlobalPosition = _fingerOrigin.GlobalPosition;

        Hand = new Hand();
        var handUi = _handScene.Instantiate<HandUI>();
        handUi.SetHand(Hand, true);
        _handContainer.AddChild(handUi);
        _handContainer.MoveChild(handUi, 0);
    }

    public void StartDealerTurn() {
        DealerFinger.Hide();
    }
    
    public void AddCard(Card card) {
        var faceDown = Hand.CardCount == 1;
        Hand.AddCard(card, faceDown);
    }

    public async Task DealerPointToHand(HandUI handUi) {
        DealerFinger.Show();
        
        var tween = CreateTween();
        tween.TweenProperty(DealerFinger, "global_position", handUi.DealerPointTo.GlobalPosition, 0.3);
        await ToSignal(tween, "finished");    }
    
    public Card GetUpCard() {
        return Hand.GetCards()[0];
    }
}
