using Godot;
using System;
using System.Collections.Generic;
using SlickBlackJack.Components;

public partial class GameUI : Control {
    [Signal] public delegate void HitEventHandler();
    [Signal] public delegate void SplitEventHandler();
    [Signal] public delegate void StandEventHandler();
    [Signal] public delegate void DoubleDownEventHandler();

    [Export] private bool _enableTimer = true;
    [Export] private int _timeLimit = 15;
    [Export] public int BetStep = 10;

    private Label _playerChipCountLabel;
    private Label _npcOneChipsLabel;
    private Label _npcTwoChipsLabel;
    private Label _handTimerLabel;
    private Label _betAmountLabel;
    private Label _minBetLabel;
    private Label _maxBetLabel;
    private BetSlider _betSlider;
    private Button _placeBetButton;
    private HeatMeter _heatMeter;
    private AudioStreamPlayer _chipSfx;

    private Player _mainPlayer;
    private Player _npcOne;
    private Player _npcTwo;

    private Timer _handTimer;
    private int _timeRemaining;
    
    private List<Button> _actionButtons = [];

    public override void _Ready() {
        _playerChipCountLabel = GetNode<Label>("%PlayerChipCount");
        _npcOneChipsLabel = GetNode<Label>("%NpcOneChipCount");
        _npcTwoChipsLabel = GetNode<Label>("%NpcTwoChipCount");
        _handTimerLabel = GetNode<Label>("%HandTimer");
        _handTimerLabel.Hide();

        _betAmountLabel = GetNode<Label>("%BetAmount");
        _minBetLabel = GetNode<Label>("%MinBet");
        _maxBetLabel = GetNode<Label>("%MaxBet");
        _betSlider = GetNode<BetSlider>("%BetSlider");
        _betSlider.ValueChanged += OnBetValueChanged;

        _placeBetButton = GetNode<Button>("%PlaceBet");
        
        _heatMeter = GetNode<HeatMeter>("%HeatMeter");
        _chipSfx = GetNode<AudioStreamPlayer>("chipsfx");

        _handTimer = new Timer();
        _handTimer.WaitTime = 1.0;
        _handTimer.OneShot = false;
        _handTimer.Timeout += OnTimerTick;
        AddChild(_handTimer);

        var actionNodes = GetTree().GetNodesInGroup("action");
        foreach (var actionNode in actionNodes) {
            if (actionNode is Button actionButton) {
                _actionButtons.Add(actionButton);
            }
        }
;
        var mainPlayers = GetTree().GetNodesInGroup("main_player");
        foreach (var player in mainPlayers) {
            if (player is Player mainPlayer) {
                _mainPlayer = mainPlayer;
                _mainPlayer.ChipsChanged += UpdatePlayerChipCount;
                UpdatePlayerChipCount(_mainPlayer.Chips);
                _mainPlayer.HandStarted += OnMainHandStarted;
                _mainPlayer.PlayerTurnEnded += OnMainTurnEnded;
                _mainPlayer.PlayerTurnStarted += OnMainTurnStarted;
                _mainPlayer.PlayerMoveFinished += OnMoveFinished;
            }
        }

        var npcs = GetTree().GetNodesInGroup("npc_player");
        if (npcs.Count < 2) {
            throw new Exception("Expected at least 2 NPC players in scene");
        }
        ;

        _npcOne = npcs[0] as Player;
        _npcTwo = npcs[1] as Player;

        _npcOne.ChipsChanged += UpdateNpcOneChipCount;
        UpdateNpcOneChipCount(_npcOne.Chips);
        _npcTwo.ChipsChanged += UpdateNpcTwoChipCount;
        UpdateNpcTwoChipCount(_npcTwo.Chips);
        _mainPlayer.ChipsChanged += OnChipsChanged;

        OnChipsChanged(_mainPlayer.Chips);

        Utils.StopTurnTimer += OnStopTurnTimer;
        Utils.BettingStarted += OnBettingStarted;

        _mainPlayer.HeatChanged += OnHeatChanged;
        
        DisableActionButtons();
    }

    private void OnChipsChanged(int chips) {
        var maxBet = Mathf.Clamp(chips, 0, _mainPlayer.Chips);
        var minBet = Mathf.Clamp(chips, 0, BetStep);

        _betSlider.MaxValue = maxBet;
        _betSlider.MinValue = minBet;
        _maxBetLabel.Text = "$" + maxBet.ToString("N0");
        _minBetLabel.Text = "$" + minBet.ToString("N0");

        _betSlider.Value = Math.Min(RoundBet(_mainPlayer.PlayerBet, minBet, maxBet), maxBet);
        _mainPlayer.PlayerBet = Mathf.Clamp(_mainPlayer.PlayerBet, minBet, maxBet);

        OnBetValueChanged(_mainPlayer.PlayerBet);
    }

    private int RoundBet(int bet, int minBet, int maxBet) {
        int rounded = (int)(Math.Floor(bet / (float)BetStep) * BetStep);
        return Math.Clamp(rounded, minBet, maxBet);
    }

    private void OnHeatChanged(int heat) {
        _heatMeter.SetHeat(heat);
    }

    private void OnBetValueChanged(int value) {
        var maxBet = Mathf.Clamp(value, 0, _mainPlayer.Chips);
        var minBet = Mathf.Clamp(value, 0, BetStep);
        _mainPlayer.PlayerBet = Math.Min(RoundBet(value, minBet, maxBet),
            _betSlider.MaxValue);
        _betAmountLabel.Text = $"BET: ${_mainPlayer.PlayerBet:N0}";
    }

    // ALWAYS DO THIS IF CALLING SIGNALS FROM UTILS
    protected override void Dispose(bool disposing) {
        Utils.StopTurnTimer -= OnStopTurnTimer;
        Utils.BettingStarted -= OnBettingStarted;
        base.Dispose(disposing);
    }

    private void OnStopTurnTimer() {
        StopTimer();
    }

    private void OnBettingStarted() {
        _placeBetButton.Disabled = false;
    }

    private void OnPlaceBetPressed() {
        _chipSfx.Play();
        Utils.EmitPlayerbetSubmitted(_mainPlayer.PlayerBet);
        _placeBetButton.Disabled = true;
    }

    public void OnHitPressed() {
        DisableActionButtons();
        EmitSignal(SignalName.Hit);
    }

    public void OnSplitPressed() {
        DisableActionButtons();
        EmitSignal(SignalName.Split);
    }

    public void OnStandPressed() {
        DisableActionButtons();
        EmitSignal(SignalName.Stand);
    }

    public void OnDoubleDownPressed() {
        DisableActionButtons();
        EmitSignal(SignalName.DoubleDown);
    }

    public void OnRestartPressed() {
        GD.Print("Restarting...");
        GetTree().ReloadCurrentScene();
    }
    
    private void OnMoveFinished() {
        EnableActionButtons();
    }

    public void OnCheatPressed() {
        Utils.EmitCheatStarted();
    }

    public void StopTimer() {
        _handTimer.Stop();
        _handTimerLabel.Hide();
    }

    private void UpdatePlayerChipCount(int chipCount) {
        _playerChipCountLabel.Text = "Tuition Fund:\n $" + chipCount.ToString("N0");
    }

    private void UpdateNpcOneChipCount(int chipCount) {
        UpdateNpcChipCount(0, chipCount);
    }

    private void UpdateNpcTwoChipCount(int chipCount) {
        UpdateNpcChipCount(1, chipCount);
    }

    private void OnMainHandStarted(Hand hand) {
        EnableActionButtons();
        if (!_enableTimer) return;

        StopTimer();

        _handTimerLabel.Show();
        _timeRemaining = _timeLimit;
        _handTimerLabel.Text = _timeRemaining.ToString();
        _handTimer.Start();
    }

    private void OnTimerTick() {
        _timeRemaining--;
        _handTimerLabel.Text = _timeRemaining.ToString();

        if (_timeRemaining <= 0 && !_handTimer.IsStopped()) {
            StopTimer();
            _handTimerLabel.Hide();
            Utils.EmitTurnTimerExpired();
            GD.Print("Turn timer expired");
        }
    }

    private void OnMainTurnEnded() {
        DisableActionButtons();
        
        if (!_enableTimer) return;

        StopTimer();
    }
    
    private void DisableActionButtons() {
        foreach (var button in _actionButtons) {
            button.Disabled = true;
        }
    }
    
    private void EnableActionButtons() {
        foreach (var button in _actionButtons) {
            button.Disabled = false;
        }
    }
    
    private void OnMainTurnStarted() {
        EnableActionButtons();
    }

    private void UpdateNpcChipCount(int npcIndex, int chipCount) {
        if (npcIndex == 0) {
            _npcOneChipsLabel.Text = $"{_npcOne.Name}: " + chipCount.ToString();
        }
        else if (npcIndex == 1) {
            _npcTwoChipsLabel.Text = $"{_npcTwo.Name}: " + chipCount.ToString();
        }
    }
}
