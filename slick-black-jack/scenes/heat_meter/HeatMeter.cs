using Godot;
using System;

public partial class HeatMeter : Control {
	private TextureProgressBar _heatBar;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		_heatBar = GetNode<TextureProgressBar>("HeatBar");

		_heatBar.Value = 0;
	}
	
	public void SetHeat(float heat) {
		_heatBar.Value = heat;
	}

	public void AddHeat(float heat) {
		_heatBar.Value += heat;
	}
}
