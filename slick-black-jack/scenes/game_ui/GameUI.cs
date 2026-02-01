using Godot;
using System;

public partial class GameUI : Control {
	[Signal] public delegate void HitEventHandler();
	[Signal] public delegate void SplitEventHandler();
	[Signal] public delegate void StandEventHandler();
	[Signal] public delegate void DoubleDownEventHandler();

	private Label _playerChipCountLabel;
	
	public override void _Ready() {
		_playerChipCountLabel = GetNode<Label>("%PlayerChipCount");

		var players = GetTree().GetNodesInGroup("main_player");
		foreach (var player in players) {
			if (player is Player mainPlayer) {
				mainPlayer.ChipsChanged += UpdatePlayerChipCount;
				UpdatePlayerChipCount(mainPlayer.Chips);
			}
		}
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
	
	private void UpdatePlayerChipCount(int chipCount) {
		_playerChipCountLabel.Text = "Current: " + chipCount.ToString();
	}
}
