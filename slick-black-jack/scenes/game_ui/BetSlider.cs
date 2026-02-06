using Godot;
using System;

public partial class BetSlider : Control {

	[Signal]
	public delegate void ValueChangedEventHandler(int value);

	[Export] public int MinValue { get; set; } = 10;
	[Export] public int MaxValue { get; set; } = 1000;
	[Export] public int Step { get; set; } = 10;

	private Sprite2D _sprite;
	private int _totalFrames = 67;
	private int _currentValue;
	private bool _isDragging = false;

	public int Value {
		get => _currentValue;
		set {
			_currentValue = Mathf.Clamp(value, MinValue, MaxValue);
			UpdateFrame();
			EmitSignal(SignalName.ValueChanged, _currentValue);
		}
	}

	public override void _Ready() {
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_totalFrames = _sprite.Hframes;
		Step = (MaxValue - MinValue) / _totalFrames;
		_currentValue = MinValue;
		UpdateFrame();
	}

	public override void _GuiInput(InputEvent @event) {
		if (@event is InputEventMouseButton mouseButton) {
			if (mouseButton.ButtonIndex == MouseButton.Left) {
				_isDragging = mouseButton.Pressed;
				if (_isDragging) {
					UpdateValueFromMouse(mouseButton.Position);
				}
			}
		}

		if (@event is InputEventMouseMotion mouseMotion && _isDragging) {
			UpdateValueFromMouse(mouseMotion.Position);
		}
	}

	private void UpdateValueFromMouse(Vector2 mousePos) {
		float percentage = Mathf.Clamp(mousePos.X / Size.X, 0f, 1f);
		percentage = Mathf.Round(percentage * 100f) / 100f;
		int rawValue = (int)Mathf.Round(Mathf.Lerp(MinValue, MaxValue, percentage));

		Value = rawValue;
	}

	private void UpdateFrame() {
		float percentage = (float)(_currentValue - MinValue) / (MaxValue - MinValue);
		int frameIndex = _totalFrames - (Mathf.RoundToInt(percentage * (_totalFrames - 1))) - 1;
		_sprite.Frame = Mathf.Clamp(frameIndex, 0, _totalFrames - 1);
	}
}
