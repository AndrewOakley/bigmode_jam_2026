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
    public Marker2D PlayerPositionMarker { get; set; }
    private Sprite2D _eyeSprite;

    public List<Hand> Hands { get; set; } = []; // holds the list of all hands (needed for splitting)
    public int PlayerBet { get; set; } = 10;

    private const int SwapHitSpeed = 300;
    private const int _swapHitRequired = 2;

    public bool IsDealerWatching { get; set; } = false;

    private int _heat = 0;
    public int Heat {
        get => _heat;
        set {
            _heat = Math.Clamp(value, 0, 100);
            EmitSignal(SignalName.HeatChanged, _heat);
        }
    }

    private const int OkHitHeat = 5;
    private const int MissHeat = 10;
    private const int CaughtCheatingHeat = 20;
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
    [Export] private AnimationPlayer _handAnimations;
    [Export] private int _chips = 1000;
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
    }
    private CheatingStates _cheatingState = CheatingStates.None;

    public override void _Ready() {
        ClearHand();
        _winSfx = GetNode<AudioStreamPlayer>("WinSFX");
        PlayerPositionMarker = GetNode<Marker2D>("%PlayerPosition");
        _eyeSprite = GetNode<Sprite2D>("eye");
        _eyeSprite.Hide();
        _cheatMeter?.Hide();

        Utils.CheatStarted += OnCheatStarted;
        Utils.CardSelected += OnCardSelected;
        Utils.NpcCardSelectStart += OnNpcCardSelectStart;
        Utils.UserSwappedCard += OnUserSwappedCard;

        if (_cheatMeter != null) {
            _cheatMeter.PerfectHit += OnPerfectHit;
            _cheatMeter.OkHit += OnOkHit;
            _cheatMeter.Miss += OnMiss;
        }
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
        }
        else if (CheatingStates.CardSwap == _cheatingState) {
            Utils.EmitStopAllCardsSelectable();
            _npcSwapCard = card;
            _cheatMeter.StartMeter(SwapHitSpeed);
            _npcSwapCardUI = cardUI;
        }
    }

    private Sprite2D _handSprite;
    public async Task PlayStealAnimation(Vector2 targetGlobalPos, Vector2 finalGlobalPos) {
        _handSprite ??= GetNode<Sprite2D>("hand");
        var animation = _handAnimations.GetAnimation("steal");

        var targetLocalPos = ToLocal(targetGlobalPos);
        var finalLocalPos = ToLocal(finalGlobalPos);
        bool isTargetOnRight = targetLocalPos.Y > 0;

        int posTrackIdx = animation.FindTrack(".:position", Animation.TrackType.Value);

        // Get start position from keyframe 0
        var startPos = (Vector2)animation.TrackGetKeyValue(posTrackIdx, 0);

        // Calculate 30% of the way to target for keyframes 1 and 2
        var partialPos = startPos.Lerp(targetLocalPos, 0.3f);
        animation.TrackSetKeyValue(posTrackIdx, 1, partialPos);
        animation.TrackSetKeyValue(posTrackIdx, 2, partialPos);

        // Set keyframe 3 to target position (NPC card)
        animation.TrackSetKeyValue(posTrackIdx, 3, targetLocalPos);

        // Set final keyframe to user's card position
        animation.TrackSetKeyValue(posTrackIdx, 4, finalLocalPos);


        _handSprite.FlipH = !isTargetOnRight;

        _handAnimations.Play("steal");

        // Wait for 90% of animation, then call the swap callback
        float animLength = animation.Length;
        await ToSignal(GetTree().CreateTimer(animLength * 0.67f), "timeout");
        _onSwapCallback?.Invoke();

        // Wait for remaining 10%
        await ToSignal(_handAnimations, "animation_finished");

        _handSprite.FlipH = false;
    }

    private Action _onSwapCallback;

    public async void SuccessfulSwap() {
        _cheatMeter.StopMeter();

        // Set the swap to happen at 90% of the animation
        _onSwapCallback = () => Card.Swap(_userSwapCard, _npcSwapCard);

        var stealTarget = _npcSwapCardUI.GetNode<Marker2D>("Marker2D");
        var userCardTarget = _userSwapCardUI.GetNode<Marker2D>("Marker2D");
        await PlayStealAnimation(stealTarget.GlobalPosition, userCardTarget.GlobalPosition);

        _onSwapCallback = null;

        _userSwapCard = null;
        _npcSwapCard = null;
        _userSwapCardUI = null;
        _npcSwapCardUI = null;
        _cheatingState = CheatingStates.None;
        Utils.EmitUserSwappedCard();
    }

    private void OnPerfectHit(int hitCount) {
        OnHit(hitCount);
    }

    private void OnOkHit(int hitCount) {
        OnHit(hitCount);
        Heat += OkHitHeat;
    }

    private void OnHit(int hitCount) {
        if (_cheatingState == CheatingStates.CardSwap && hitCount == _swapHitRequired) {
            SuccessfulSwap();
        }
    }

    private void OnMiss() {
        GD.Print($"Player {_cheatingState} missed!");
        StopCheat();
        Heat += MissHeat;
    }

    private void StopCheat() {
        _cheatingState = CheatingStates.None;
        Utils.EmitStopAllCardsSelectable();
        _cheatMeter?.StopMeter();
    }

    private void OnNpcCardSelectStart() {
        if (!IsNpc) return;

        SetHandsSelectable(true);
    }

    private void OnUserSwappedCard() {
        if (!IsNpc) return;

        SetHandsSelectable(false);
    }

    private void SetHandsSelectable(bool selectable) {
        foreach (var node in HandsUIContainer.GetChildren()) {
            if (node is HandUI handUi) {
                handUi.SetHandSelectable(selectable);
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
        Heat -= RoundEndHeatMinus;
    }

    private void ClearHand() {
        Hands.Clear();
        CurrentHandIndex = 0;
        foreach (var handUi in HandsUIContainer.GetChildren()) {
            handUi.QueueFree();
        }
    }

    public void StartRound(int mainPlayerBet = 0) {
        if (IsNpc) {
             RandomNumberGenerator _rng = new RandomNumberGenerator();
            // Set to a random number between 2 percent and 20 percent of their chips
            // Use exponential distribution for bet percentage (2% to 40%)
            // Lower percentages are much more likely than higher ones
            double randomValue = _rng.Randf(); // 0.0 to 1.0
            double skewedValue = Math.Pow(randomValue, 3); // Cube it to heavily favor lower values
            int betPercent = 2 + (int)(skewedValue * 38); // 2 + (0 to 38) = 2 to 40
            double betPercentDouble = betPercent / 100.0;
            int baseBet = (int)(Chips * betPercentDouble);
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
            StopCheat();
        }
    }

    // BLACK JACk MOVES ------------------------------------------------------------
    // Returns true if hand was busted
    public async Task<bool> HitCurrentHand(Card card, bool noAnimation = false) {
        StopCheat();

        if (_handAnimations != null && !noAnimation) {
            _handAnimations.Play("hit");
            await ToSignal(_handAnimations, "animation_finished");
        }

        Hand activeHand = GetCurrentHand();
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

        if (!currentHand.CanDoubleDown() && Chips > 0) {
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
