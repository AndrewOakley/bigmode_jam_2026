using Godot;
using System;

public partial class HeatMeter : TextureProgressBar {
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {

		Value = 0;
	}
	
	public void SetHeat(float heat) {
		Value = heat;
	}

	public void AddHeat(float heat) {
		Value += heat;
	}
}
