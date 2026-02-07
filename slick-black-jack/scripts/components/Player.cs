using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    [Signal] public delegate void PlayerTurnStartedEventHandler();
    [Signal] public delegate void PlayerTurnEndedEventHandler();
    [Signal] public delegate void HandStartedEventHandler(Hand hand);
    [Signal] public delegate void HeatChangedEventHandler(int newHeat);
    [Signal] public delegate void PlayerMoveFinishedEventHandler();

    [Export] public Container HandsUIContainer { get; set; }
    [Export] public string Name { get; set; }
    [Export] public bool IsNpc { get; set; } = true;
    [Export] public bool PlayWinSfx { get; set; } = false;
    [Export] private CheatMeter _cheatMeter;
    [Export] private float _stealHoldTime = 1.5f;
    [Export] private AnimationPlayer _animationPlayer;
    
    private Label _stealTooltip;
    public Marker2D PlayerPositionMarker { get; set; }
    private Sprite2D _eyeSprite;
    public bool PlayerIsOut { get; set; } = false;

    public List<Hand> Hands { get; set; } = []; // holds the list of all hands (needed for splitting)
    public int PlayerBet { get; set; } = 10;

    public bool IsDealerWatching { get; set; } = false;

    private int _heat = 0;
    public int Heat {
        get => _heat;
        set {
            _heat = Math.Clamp(value, 0, 100);
            EmitSignal(SignalName.HeatChanged, _heat);
        }
    }

    private const int CaughtCheatingHeat = 25;
    private const int RoundEndHeatMinus = 10;

    private bool _isTurn = false;
    public bool IsTurn {
        get => _isTurn;
        set {
            if (_isTurn == value) return;

            // always stop cheat if player is changing turns
            StopCheat();

            _isTurn = value;

            if (_isTurn) {
                EmitSignal(SignalName.PlayerTurnStarted);
            }
            else {
                EmitSignal(SignalName.PlayerTurnEnded);
            }
        }
    }

    private AudioStreamPlayer _winSfx;
    private AudioStreamPlayer _handSfx;
    private AudioStream _slideSfx;
    private AudioStream _wrongSfx;
    private AudioStream _successfulStealSfx;
    [Export] private AnimationPlayer _handAnimations;
    [Export] private int _initialChips = 6969;
    private int _chips = 10;
    public int Chips {
        get => _chips;
        set {
            _chips = value;
            EmitSignal(SignalName.ChipsChanged, _chips);
        }
    }

    public int CurrentHandIndex { get; set; } = 0;

    private PackedScene _handScene = GD.Load<PackedScene>("res://scenes/hand/HandUI.tscn");

    enum CheatingStates {
        None,
        Cheating,
        CardSwap,
        HoldingSteal,
    }
    private CheatingStates _cheatingState = CheatingStates.None;
    private float _stealHoldElapsed = 0f;
    private Animation _stealAnimation;
    private float _stealReachTime; // the point in the animation where the hand reaches the card

    private Vector2 _targetCardPos;

    public override void _Ready() {
        ClearHand();
        _winSfx = GetNode<AudioStreamPlayer>("WinSFX");
        _handSfx = GetNode<AudioStreamPlayer>("handsfx");
        _slideSfx = GD.Load<AudioStream>("res://assets/sfx/slideasc.wav");
        _wrongSfx = GD.Load<AudioStream>("res://assets/sfx/wrong.wav");
        _successfulStealSfx = GD.Load<AudioStream>("res://assets/sfx/success.wav");
        PlayerPositionMarker = GetNode<Marker2D>("%PlayerPosition");
        _eyeSprite = GetNode<Sprite2D>("eye");
        _eyeSprite.Hide();
        _cheatMeter?.Hide();
        _stealTooltip = GetNode<Label>("%stealToolTip");
        _stealTooltip.Hide();

        Utils.CheatStarted += OnCheatStarted;
        Utils.CardSelected += OnCardSelected;
        Utils.NpcCardSelectStart += OnNpcCardSelectStart;
        Utils.UserSwappedCard += OnUserSwappedCard;

        Utils.StopAllCardsSelectable += StopAllCardsSelectable;
        Utils.CheatingStopped += CheatingStopped;

        Chips = _initialChips;

    }

    private async void HideStealTooltip() {
        // wait 2 seconds before hiding tooltip
        await ToSignal(GetTree().CreateTimer(2f), "timeout");
        _targetCardPos = new Vector2(0, 0);
        _stealTooltip.Hide();
        _stealTooltip.Position = new Vector2(0, 0);
    }

    private void StopAllCardsSelectable() {
        HideStealTooltip();
    }

    private void CheatingStopped() {
        HideStealTooltip();
    }
    public void SetIsDealerWatching(bool isWatching) {
        IsDealerWatching = isWatching;
        GD.Print($"Dealer is watching: {Name}");

        if (IsDealerWatching) {
            _eyeSprite.Show();
        }
        else {
            _eyeSprite.Hide();
        }

        if (IsDealerWatching && _cheatingState != CheatingStates.None) {
            PlayerCaughtCheating();
        }
    }

    public void PlayerCaughtCheating() {
        GD.Print("Player caught cheating");
        StopCheat();
        HideStealTooltip();
        Heat += CaughtCheatingHeat;
    }

    private void OnBetAmountTextChanged(string newText) {
        if (int.TryParse(newText, out int betAmount)) {
            PlayerBet = betAmount;
        }
    }

    // ALWAYS DO THIS IF CALLING SIGNALS FROM UTILS
    protected override void Dispose(bool disposing) {
        Utils.CheatStarted -= OnCheatStarted;
        Utils.CardSelected -= OnCardSelected;
        Utils.NpcCardSelectStart -= OnNpcCardSelectStart;
        Utils.UserSwappedCard -= OnUserSwappedCard;
        base.Dispose(disposing);
    }

    private void OnCheatStarted() {
        if (_cheatingState != CheatingStates.None) {
            GD.PrintErr($"Player {_cheatingState} already cheating!");
            return;
        }
        ;

        if (IsNpc) return;

        if (IsDealerWatching) {
            PlayerCaughtCheating();
            return;
        }

        SetHandsSelectable(true);
        _cheatingState = CheatingStates.Cheating;
    }

    public async Task TurnTimeOut() {
        StopCheat();
        await StandCurrentHand();
    }

    private Card _userSwapCard;
    private Card _npcSwapCard;
    private CardUI _userSwapCardUI;
    private CardUI _npcSwapCardUI;

    private void OnCardSelected(Card card, CardUI cardUI) {
        if (CheatingStates.Cheating == _cheatingState) {
            _userSwapCard = card;
            _userSwapCardUI = cardUI;

            GD.Print($"Player {_cheatingState} selected card {card}");
            SetHandsSelectable(false);
            Utils.EmitNpcCardSelectStart();
            _cheatingState = CheatingStates.CardSwap;
            HideStealTooltip();
        }
        else if (CheatingStates.CardSwap == _cheatingState) {
            Utils.EmitStopAllCardsSelectable();
            _npcSwapCard = card;
            _npcSwapCardUI = cardUI;
            PrepareStealAnimation();
            _stealHoldElapsed = 0f;
            _cheatingState = CheatingStates.HoldingSteal;
            if (!IsNpc) {
                _handSfx.Stream = _slideSfx;
                _handSfx.Play();
            }
        }
    }

    private Sprite2D _handSprite;
    private Action _onSwapCallback;

    private void PrepareStealAnimation() {
        _handSprite ??= GetNode<Sprite2D>("hand");
        _stealAnimation = _handAnimations.GetAnimation("steal");

        var stealTarget = _npcSwapCardUI.GetNode<Marker2D>("Marker2D");
        var userCardTarget = _userSwapCardUI.GetNode<Marker2D>("Marker2D");

        var targetLocalPos = ToLocal(stealTarget.GlobalPosition);
        _targetCardPos = targetLocalPos;
        OnNpcCardSelectStart();
        var finalLocalPos = ToLocal(userCardTarget.GlobalPosition);
        bool isTargetOnRight = targetLocalPos.Y > 0;

        int posTrackIdx = _stealAnimation.FindTrack(".:position", Animation.TrackType.Value);

        // Set keyframe 1 to NPC card position (the "reach" target)
        _stealAnimation.TrackSetKeyValue(posTrackIdx, 1, targetLocalPos);
        // Set keyframe 2 to user's card position (the "return" target)
        _stealAnimation.TrackSetKeyValue(posTrackIdx, 2, finalLocalPos);

        _handSprite.FlipH = !isTargetOnRight;

        // The reach portion ends at keyframe 1's time
        _stealReachTime = (float)_stealAnimation.TrackGetKeyTime(posTrackIdx, 1);

        // Seek to start so the hand is visible and at start position
        _handAnimations.Play("steal");
        _handAnimations.Seek(0, true);
        _handAnimations.Pause();
    }

    private async void CompleteSteal() {
        _cheatingState = CheatingStates.None;
        if (!IsNpc) {
            _handSfx.Stop();
            _handSfx.Stream = _successfulStealSfx;
            _handSfx.Play();
        }

        HideStealTooltip();
        _stealTooltip.Position = new Vector2(0, 0);

        // Set the swap callback
        _onSwapCallback = () => Card.Swap(_userSwapCard, _npcSwapCard);

        // Resume playing from the reach point
        _handAnimations.Play("steal");
        _handAnimations.Seek(_stealReachTime, true);

        // Wait for swap timing (67% of full animation)
        float animLength = _stealAnimation.Length;
        float remainingToSwap = (animLength * 0.67f) - _stealReachTime;
        if (remainingToSwap > 0) {
            await ToSignal(GetTree().CreateTimer(remainingToSwap), "timeout");
        }
        _onSwapCallback?.Invoke();

        // Wait for animation to finish
        await ToSignal(_handAnimations, "animation_finished");

        _handSprite.FlipH = false;
        _onSwapCallback = null;
        _userSwapCard = null;
        _npcSwapCard = null;
        _userSwapCardUI = null;
        _npcSwapCardUI = null;
        Utils.EmitUserSwappedCard();
    }

    private void CancelStealHold() {
        _handAnimations.Stop();
        _handSprite ??= GetNode<Sprite2D>("hand");
        _handSprite.FlipH = false;
        _handSprite.Modulate = new Color(1, 1, 1, 0);
        _stealHoldElapsed = 0f;
        StopCheat();
        if (!IsNpc) {
            _handSfx.Stream = _wrongSfx;
            _handSfx.Play();
        }
    }

    public void StopCheat() {
        if (_cheatingState == CheatingStates.HoldingSteal) {
            _handAnimations?.Stop();
            _handSprite ??= GetNode<Sprite2D>("hand");
            _handSprite.FlipH = false;
            _handSprite.Modulate = new Color(1, 1, 1, 0);
            _stealHoldElapsed = 0f;
            if (!IsNpc) {
                _handSfx.Stop();
            }
        }
        _cheatingState = CheatingStates.None;
        Utils.EmitStopAllCardsSelectable();
        Utils.EmitCheatingStopped();
        _cheatMeter?.StopMeter();
    }

    private void OnNpcCardSelectStart() {
        if (!IsNpc) {
            if (_targetCardPos != new Vector2(0, 0)) {
                _stealTooltip.Position = _targetCardPos + new Vector2(30, -5);
                _stealTooltip.Show();
            }
            return;
        }

        SetHandsSelectable(true, Control.CursorShape.PointingHand);
    }

    private void OnUserSwappedCard() {
        if (!IsNpc) return;

        SetHandsSelectable(false);
    }

    private void SetHandsSelectable(bool selectable, Control.CursorShape cursor = Control.CursorShape.PointingHand) {
        foreach (var node in HandsUIContainer.GetChildren()) {
            if (node is HandUI handUi) {
                handUi.SetHandSelectable(selectable, cursor);
            }
        }
    }

    public Hand GetCurrentHand() {
        if (CurrentHandIndex >= Hands.Count) {
            GD.PrintErr($"Player {Name} has no active hands left to play.");
            return null;
        }

        return Hands[CurrentHandIndex];
    }

    public void EmitHandStarted() {
        EmitSignal(SignalName.HandStarted, GetCurrentHand());
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

    public void EndRound() {
        ClearHand();
    }

    private void ClearHand() {
        Hands.Clear();
        CurrentHandIndex = 0;
        foreach (var handUi in HandsUIContainer.GetChildren()) {
            handUi.QueueFree();
        }
    }

    public void StartRound(int mainPlayerBet = 0) {
        if (Chips == 0) {
            PlayerIsOut = true;
        }
        
        if (PlayerIsOut) {
            return;
        }
        
        if (IsNpc) {
            RandomNumberGenerator _rng = new RandomNumberGenerator();
            // Set to a random number between 2 percent and 20 percent of their chips
            // Use exponential distribution for bet percentage (2% to 40%)
            // Lower percentages are much more likely than higher ones
            int baseBet;
            if (true) {
                // double randomValue = _rng.Randf(); // 0.0 to 1.0
                // double skewedValue = Math.Pow(randomValue, 2); // square it to heavily favor lower values
                // int betPercent = (int)(skewedValue * 100);
                // double betPercentDouble = betPercent / 100.0;
                // baseBet = (int)(Chips * betPercentDouble);
                // base bet is random number between 10 and Chips
                baseBet = _rng.RandiRange(0, Chips);
            }
            else {

                double randomValue = _rng.Randf(); // 0.0 to 1.0
                double skewedValue = Math.Pow(randomValue, 2); // square it to heavily favor lower values
                int betPercent = 2 + (int)(skewedValue * 38); // 2 + (0 to 38) = 2 to 40
                double betPercentDouble = betPercent / 100.0;
                baseBet = (int)(Chips * betPercentDouble);
            }
            
            int roundedBet = (int)(Math.Ceiling(baseBet / 10.0) * 10);
            PlayerBet = Math.Clamp(roundedBet, 10, Chips);
        }
        else {
            PlayerBet = mainPlayerBet;
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

    public override void _PhysicsProcess(double delta) {
        if (Input.IsActionJustPressed("escape") && _cheatingState != CheatingStates.None) {
            if (_cheatingState == CheatingStates.HoldingSteal) {
                CancelStealHold();
            }
            else {
                StopCheat();
            }
        }

        if (_cheatingState == CheatingStates.HoldingSteal) {
            if (Input.IsActionPressed("click")) {
                _stealHoldElapsed += (float)delta;
                float progress = Mathf.Clamp(_stealHoldElapsed / _stealHoldTime, 0f, 1f);

                // Seek the animation to match hold progress (0 to reach point)
                _handAnimations.Seek(progress * _stealReachTime, true);

                if (_stealHoldElapsed >= _stealHoldTime) {
                    CompleteSteal();
                }
            }
            else if (Input.IsActionJustReleased("click")) {
                CancelStealHold();
            }
        }
    }

    // BLACK JACk MOVES ------------------------------------------------------------
    // Returns true if hand was busted
    public async Task<bool> HitCurrentHand(Card card, bool noAnimation = false) {
        Hand activeHand = GetCurrentHand();
        // forces player to get 21
        // if (activeHand.CardCount >= 2) {
        //     var value = activeHand.GetValue();
        //     var neededValue = 21 - value;
        //     // force user to get 21 on this hand
        //     card = new Card(card.Suit, (Rank)neededValue);
        //     
        // }
        
        StopCheat();

        if (_handAnimations != null && !noAnimation) {
            _handAnimations.Play("hit");
            await ToSignal(_handAnimations, "animation_finished");
        }

        activeHand.AddCard(card);

        // hand is done
        if (activeHand.GetValue() >= 21) {
            activeHand.Status = HandStatus.Done;
            CurrentHandIndex++;
            EmitSignal(SignalName.CurrentHandChanged, CurrentHandIndex);
            return true;
        }

        EmitSignal(SignalName.PlayerMoveFinished);

        return false;
    }

    public async Task<bool> SplitHand() {
        StopCheat();

        // TODO: need to check if player has enough chips to split
        var currentHand = GetCurrentHand();

        if (Chips <= 0) {
            GD.Print("Not enough chips to split hand");
            EmitSignal(SignalName.PlayerMoveFinished);
            return false;
        }

        if (!currentHand.CanSplit()) {
            GD.PrintErr($"invalid attempt to split {Name} hand {currentHand}");
            EmitSignal(SignalName.PlayerMoveFinished);
            return false;
        }

        if (_handAnimations != null) {
            _handAnimations.Play("split");
            await ToSignal(_handAnimations, "animation_finished");
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

    public void EmitPlayerMoveFinished() {
        EmitSignal(SignalName.PlayerMoveFinished);
    }

    public async Task<bool> DoubleDownCurrentHand(Card card) {
        StopCheat();

        var currentHand = GetCurrentHand();

        if (!currentHand.CanDoubleDown() || Chips <= 0) {
            EmitSignal(SignalName.PlayerMoveFinished);
            return false;
        }

        var chipsForHand = Math.Min(currentHand.Chips, Chips);
        currentHand.Chips += chipsForHand;

        Chips -= chipsForHand;

        await HitCurrentHand(card);
        return true;
    }

    public async Task StandCurrentHand(bool noAnimation = false) {
        StopCheat();
        if (_handAnimations != null && !noAnimation) {
            _handAnimations.Play("stand");
            await ToSignal(_handAnimations, "animation_finished");
        }

        Hand activeHand = GetCurrentHand();
        activeHand.Stand();
        CurrentHandIndex++;
        EmitSignal(SignalName.CurrentHandChanged, CurrentHandIndex);
        EmitSignal(SignalName.PlayerMoveFinished);
    }
    // End Black jack moves ----------------------------------------------------------------------------------------
    public bool CheckIfPlayerIsOut() {
        if (Chips <= 0) {
            // EmitSignal(SignalName.PlayerIsOut);
            PlayerIsOut = true;
            _animationPlayer?.Play("fade_away");
        }
        
        return PlayerIsOut;
    }

    public void DetermineHandResults(int dealerValue) {
        for (var i = 0; i < Hands.Count; i++) {
            var hand = Hands[i];
            var handUi = GetHandUIByIndex(i);

            var playerValue = hand.GetValue();

            if (hand.IsBlackjack() && dealerValue != 21) {
                hand.Result = HandResult.PlayerBlackjack;
            }
            else if (playerValue > 21) {
                hand.Result = HandResult.DealerWin;
            }
            else if (dealerValue > 21) {
                hand.Result = HandResult.PlayerWin;
            }
            else if (playerValue < dealerValue) {
                hand.Result = HandResult.DealerWin;
            }
            else if (playerValue > dealerValue) {
                hand.Result = HandResult.PlayerWin;
            }
            else if (playerValue == dealerValue) {
                hand.Result = HandResult.Push;
            }
            else {
                GD.PrintErr($"Invalid hand result for hand {hand}");
                hand.Result = HandResult.PlayerWin;
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
            Chips += hand.Chips + (int)(hand.Chips * (3.0 / 2.0));
        }
        else if (hand.Result == HandResult.Push) {
            Chips += hand.Chips;
        }
    }

    public bool CanDoubleDown() {
        return GetCurrentHand().CanDoubleDown();
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
        bool canSplit = currentHand.CanSplit() && Chips > 0;
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
