using Godot;
using System;
using SlickBlackJack.Components;

public partial class GameUI : Control {
	[Signal] public delegate void HitEventHandler();
	[Signal] public delegate void SplitEventHandler();
	[Signal] public delegate void StandEventHandler();
	[Signal] public delegate void DoubleDownEventHandler();
	
	[Export] private bool _enableTimer = true;

	private Label _playerChipCountLabel;
	private Label _npcOneChipsLabel;
	private Label _npcTwoChipsLabel;
	private Label _handTimerLabel;

	private Player _mainPlayer;
	private Player _npcOne;
	private Player _npcTwo;

	private Timer _handTimer;
	private int _timeRemaining;
	
	public override void _Ready() {
		_playerChipCountLabel = GetNode<Label>("%PlayerChipCount");
		_npcOneChipsLabel = GetNode<Label>("%NpcOneChipCount");
		_npcTwoChipsLabel = GetNode<Label>("%NpcTwoChipCount");
		_handTimerLabel = GetNode<Label>("%HandTimer");
		_handTimerLabel.Hide();

		_handTimer = new Timer();
		_handTimer.WaitTime = 1.0;
		_handTimer.OneShot = false;
		_handTimer.Timeout += OnTimerTick;
		AddChild(_handTimer);

		var mainPlayers = GetTree().GetNodesInGroup("main_player");
		foreach (var player in mainPlayers) {
			if (player is Player mainPlayer) {
				_mainPlayer = mainPlayer;
				_mainPlayer.ChipsChanged += UpdatePlayerChipCount;
				UpdatePlayerChipCount(_mainPlayer.Chips);
				_mainPlayer.HandStarted += OnMainHandStarted;
				_mainPlayer.PlayerTurnEnded += OnMainTurnEnded;
			}
		}
		
		var npcs = GetTree().GetNodesInGroup("npc_player");
		if (npcs.Count < 2) {
			throw new Exception("Expected at least 2 NPC players in scene");
		};
		
		_npcOne = npcs[0] as Player;
		_npcTwo = npcs[1] as Player;
		
		_npcOne.ChipsChanged += UpdateNpcOneChipCount;
		_npcTwo.ChipsChanged += UpdateNpcTwoChipCount;
		
		Utils.StopTurnTimer += OnStopTurnTimer;
	}
	
	// ALWAYS DO THIS IF CALLING SIGNALS FROM UTILS
	protected override void Dispose(bool disposing) {
		Utils.StopTurnTimer -= OnStopTurnTimer;
		base.Dispose(disposing);
	}

	private void OnStopTurnTimer() {
		StopTimer();
	}

	public void OnHitPressed() {
		EmitSignal(SignalName.Hit);
	}
	
	public void OnSplitPressed() {
		EmitSignal(SignalName.Split);
	}
	
	public void OnStandPressed() {
		EmitSignal(SignalName.Stand);
	}
	
	public void OnDoubleDownPressed() {
		EmitSignal(SignalName.DoubleDown);
	}

	public void OnRestartPressed() {
        GD.Print("Restarting...");
		GetTree().ReloadCurrentScene();
	}
	
	public void OnCheatPressed() {
		Utils.EmitCheatStarted();
	}
	
	public void StopTimer() {
		_handTimer.Stop();
		_handTimerLabel.Hide();
	}
	
	private void UpdatePlayerChipCount(int chipCount) {
		_playerChipCountLabel.Text = "Current: $" + chipCount.ToString();
	}

	private void UpdateNpcOneChipCount(int chipCount) {
		UpdateNpcChipCount(0, chipCount);
	}
	
	private void UpdateNpcTwoChipCount(int chipCount) {
		UpdateNpcChipCount(1, chipCount);
	}
	
	private void OnMainHandStarted(Hand hand) {
		if (!_enableTimer) return;
		
		StopTimer();

		_handTimerLabel.Show();
		_timeRemaining = 10;
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
		if (!_enableTimer) return;
		
		StopTimer();
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
