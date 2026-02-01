using Godot;
using System;

public partial class CheatMeter : Node2D {
    private Area2D _orangeArea;
    private Area2D _greenArea;
    private Area2D _line;
    private Node2D _target;
    private Label _feedbackLabel;
    private Marker2D _feedbackMarker;

    private float _startX;
    private float _direction = 1f;
    [Export] private float _speed = 100f;
    private const float RANGE = 150f;

    public override void _Ready() {
        _orangeArea ??= GetNode<Area2D>("%Orange");
        _greenArea ??= GetNode<Area2D>("%Green");
        _line ??= GetNode<Area2D>("%Line");
        _target ??= GetNode<Node2D>("Target");
        _feedbackMarker ??= GetNode<Marker2D>("FeedbackMarker");

        // Create feedback label
        _feedbackLabel = new Label();
        _feedbackLabel.GlobalPosition = _feedbackMarker.GlobalPosition;
        _feedbackLabel.Modulate = new Color(1, 1, 1, 0);
        AddChild(_feedbackLabel);

        // Randomize target position within 30 pixels left or right
        float randomOffset = (float)GD.RandRange(-20, 40);
        _target.Position += new Vector2(randomOffset, 0);

        _startX = _line.Position.X;
    }

    public override void _PhysicsProcess(double delta) {
        base._PhysicsProcess(delta);

        // Move the line
        _line.Position += new Vector2(_direction * _speed * (float)delta, 0);

        // Check if we've exceeded the range and reverse direction
        float distanceFromStart = _line.Position.X - _startX;
        if (distanceFromStart >= RANGE || distanceFromStart <= 0) {
            _direction *= -1;
            // Clamp position to stay within bounds
            _line.Position = new Vector2(Mathf.Clamp(_line.Position.X, _startX, _startX + RANGE), _line.Position.Y);
        }

        // Check for space input
        if (Input.IsActionJustPressed("ui_accept")) {
            CheckLinePosition();
        }
    }

    private void CheckLinePosition() {
        bool onGreen = _line.HasOverlappingAreas() && _line.GetOverlappingAreas().Contains(_greenArea);
        bool onOrange = _line.HasOverlappingAreas() && _line.GetOverlappingAreas().Contains(_orangeArea);

        if (onGreen) {
            GD.Print("green");
            ShowFeedback("Nice!");
        } else if (onOrange) {
            GD.Print("orange");
            ShowFeedback("Ok");
        } else {
            GD.Print("neither");
            ShowFeedback("Bad");
        }
    }

    private void ShowFeedback(string text) {
        _feedbackLabel.Text = text;

        // Create fade-in tween
        Tween tween = CreateTween();
        tween.TweenProperty(_feedbackLabel, "modulate:a", 1.0f, 0.2f);
        tween.TweenProperty(_feedbackLabel, "modulate:a", 0.0f, 0.5f).SetDelay(0.3f);
    }
}
