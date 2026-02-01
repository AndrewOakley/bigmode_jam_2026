using Godot;
using System;

public partial class GameUI : Control {
	[Signal] public delegate void HitEventHandler();
	[Signal] public delegate void SplitEventHandler();
	[Signal] public delegate void StandEventHandler();
	[Signal] public delegate void DoubleDownEventHandler();

	private Label _playerChipCountLabel;
	private Label _npcOneChipsLabel;
	private Label _npcTwoChipsLabel;

	private Player _npcOne;
	private Player _npcTwo;
	
	public override void _Ready() {
		_playerChipCountLabel = GetNode<Label>("%PlayerChipCount");
		_npcOneChipsLabel = GetNode<Label>("%NpcOneChipCount");
		_npcTwoChipsLabel = GetNode<Label>("%NpcTwoChipCount");

		var players = GetTree().GetNodesInGroup("main_player");
		foreach (var player in players) {
			if (player is Player mainPlayer) {
				mainPlayer.ChipsChanged += UpdatePlayerChipCount;
				UpdatePlayerChipCount(mainPlayer.Chips);
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
		_playerChipCountLabel.Text = "Current: $" + chipCount.ToString();
	}

	private void UpdateNpcOneChipCount(int chipCount) {
		UpdateNpcChipCount(0, chipCount);
	}
	
	private void UpdateNpcTwoChipCount(int chipCount) {
		UpdateNpcChipCount(1, chipCount);
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
