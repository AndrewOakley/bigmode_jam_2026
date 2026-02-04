using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SlickBlackJack.Components;

public partial class Dealer : Node2D {
    public Marker2D DealerFinger;
    private Marker2D _fingerOrigin;
    private Control _handContainer;
    [Export] private AnimatedSprite2D _headSprite;
    private List<Player> _players = [];
    private Timer _lookTimer;
    private bool _isRoundActive = false;
    private float _originalRotation = 0.0f;
    
    public Hand Hand { get; private set; } = new Hand();
    
    private PackedScene _handScene = GD.Load<PackedScene>("res://scenes/hand/HandUI.tscn");
    
    public override void _Ready() {
        DealerFinger ??= GetNode<Marker2D>("DealerFinger");
        _handContainer ??= GetNode<Control>("HandContainer");
        _fingerOrigin ??= GetNode<Marker2D>("FingerOrigin");
        _headSprite ??= GetNode<AnimatedSprite2D>("head");
        
        _originalRotation = _headSprite.Rotation;
        
        DealerFinger.Hide();

        ResetRound();

        Utils.NpcCardSelectStart += OnNpcCardSelectStart;
        Utils.UserSwappedCard += OnUserSwappedCards;

        foreach (var player in GetTree().GetNodesInGroup("player")) {
            if (player is Player p) {
                _players.Add(p);
            }
        }

        // Setup look timer
        _lookTimer = new Timer();
        AddChild(_lookTimer);
        _lookTimer.Timeout += OnLookTimerTimeout;
    }
    
        
    // ALWAYS DO THIS IF CALLING SIGNALS FROM UTILS
    protected override void Dispose(bool disposing) {
        Utils.NpcCardSelectStart -= OnNpcCardSelectStart;
        Utils.UserSwappedCard -= OnUserSwappedCards;
        base.Dispose(disposing);
    }
    
    private void OnNpcCardSelectStart() {
        var upCardUI = GetUpCardUI();
        upCardUI.SetCardSelectable(true);
    }
    
    private void OnUserSwappedCards() {
        var upCardUI = GetUpCardUI();
        upCardUI.SetCardSelectable(false);
    }

    public void ResetRound() {
        foreach (var child in _handContainer.GetChildren()) {
            child.QueueFree();
        }
        Hand.Clear();
        DealerFinger.Hide();
        DealerFinger.GlobalPosition = _fingerOrigin.GlobalPosition;
        
        var tween = CreateTween();
        tween.TweenProperty(_headSprite, "rotation", _originalRotation, 1.0)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.InOut);

        foreach (var player in _players) {
            player.SetIsDealerWatching(false);
        }
    }

    public void StartRound() {
        Hand = new Hand();
        var handUi = _handScene.Instantiate<HandUI>();
        handUi.SetHand(Hand, true);
        _handContainer.AddChild(handUi);
        _handContainer.MoveChild(handUi, 0);

        // Start looking at players
        _isRoundActive = true;
        StartRandomLook();
    }

    public void StartDealerTurn() {
        DealerFinger.Hide();
        _isRoundActive = false;
        _lookTimer.Stop();
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
    
    public CardUI GetUpCardUI() {
        var handUIs = _handContainer.GetChildren();
        var handUI = handUIs[0] as HandUI;
        
        return handUI.GetUpCardUi();
    }

    public void LookAtPlayer(Player player) {
        _headSprite.LookAt(player.PlayerPositionMarker.GlobalPosition);
    }

    Player _currentLookAtPlayer = null;
    private void StartRandomLook() {
        if (_players.Count == 0) return;

        // Set random interval between 1-3 seconds
        var randomInterval = GD.RandRange(2.0, 6.0);
        _lookTimer.WaitTime = randomInterval;
        _lookTimer.Start();
    }

    private void OnLookTimerTimeout() {
        if (!_isRoundActive || _players.Count == 0) return;

        if (_currentLookAtPlayer != null) {
            _currentLookAtPlayer.SetIsDealerWatching(false);
        }

        // Pick a random player
        var randomIndex = GD.RandRange(0, _players.Count - 1);
        var randomPlayer = _players[(int)randomIndex];
        _currentLookAtPlayer = randomPlayer;

        // Tween to look at the player
        var targetRotation = _headSprite.GlobalPosition.AngleToPoint(_currentLookAtPlayer.PlayerPositionMarker.GlobalPosition);
        var tween = CreateTween();
        tween.TweenProperty(_headSprite, "rotation", targetRotation, 1.5)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.InOut);
    
        tween.Finished += OnTweenFinished;
    }

    private void OnTweenFinished() {
        _currentLookAtPlayer.SetIsDealerWatching(true);
        StartRandomLook();
    }
}
