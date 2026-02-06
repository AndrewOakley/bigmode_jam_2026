using Godot;

public partial class MenuCards : Node2D
{
    [Export] public float SwayX { get; set; } = 2f;
    [Export] public float SwayY { get; set; } = 3f;
    [Export] public float RotationAmount { get; set; } = 0.02f;
    [Export] public float Speed { get; set; } = 1f;
    [Export] public float Offset { get; set; } = 0f;
    [Export] public float StutterInterval { get; set; } = 0.1f; // How often to update position

    private Vector2 _originalPos;
    private float _originalRotation;
    private float _time = 0;
    private float _stutterTimer = 0;

    public override void _Ready()
    {
        _originalPos = Position;
        _originalRotation = Rotation;
        _time = Offset;
    }

    public override void _Process(double delta)
    {
        _time += (float)delta * Speed;
        _stutterTimer += (float)delta;

        // Only update position at stutter intervals
        if (_stutterTimer >= StutterInterval)
        {
            _stutterTimer = 0;

            // Calculate and snap to whole pixels
            var targetPos = _originalPos + new Vector2(
                Mathf.Sin(_time * 1.1f) * SwayX,
                Mathf.Cos(_time * 0.9f) * SwayY
            );
            Position = new Vector2(Mathf.Round(targetPos.X), Mathf.Round(targetPos.Y));

            // Snap rotation to discrete steps
            float targetRot = _originalRotation + Mathf.Sin(_time * 0.8f) * RotationAmount;
            Rotation = Mathf.Round(targetRot * 50) / 50f;
        }
    }
}
