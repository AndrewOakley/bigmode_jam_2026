using Godot;
using System;
using SlickBlackJack.Components;

public partial class Dealer : Node2D {
    public Marker2D DealerFinger;
    private Control _handContainer;
    
    public Hand Hand { get; private set; } = new Hand();
    
    private PackedScene _handScene = GD.Load<PackedScene>("res://scenes/hand/HandUI.tscn");

    public override void _Ready() {
        DealerFinger ??= GetNode<Marker2D>("DealerFinger");
        _handContainer ??= GetNode<Control>("HandContainer");
    }

    public void StartRound() {
        foreach (var child in _handContainer.GetChildren()) {
            child.QueueFree();
        }
        Hand.Clear();
        
        var handUi = _handScene.Instantiate<HandUI>();
        handUi.SetHand(Hand, true);
        _handContainer.AddChild(handUi);
        _handContainer.MoveChild(handUi, 0);
    }
    
    public void AddCard(Card card) {
        var faceDown = Hand.CardCount == 1;
        Hand.AddCard(card, faceDown);
    }

    public void DealerPointToHand(HandUI handUi) {
        var tween = CreateTween();
        tween.TweenProperty(DealerFinger, "global_position", handUi.DealerPointTo.GlobalPosition, 0.3);
    }
}
