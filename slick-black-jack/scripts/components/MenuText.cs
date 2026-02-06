using Godot;

public partial class MenuText : Label {
    [Export] public float SwayX { get; set; } = 3f;
    [Export] public float SwayY { get; set; } = 5f;
    [Export] public float TypeSpeed { get; set; } = 0.05f; // Seconds per character

    private Vector2 _originalPos;
    private float _time = 0;
    private string _fullText;
    private bool _isTyping = false;

    public override void _Ready() {
        _originalPos = Position;
        _fullText = Text;
        Text = "";
        StartTyping();
    }

    public override void _Process(double delta) {
        // _time += (float)delta;
        // Position = _originalPos + new Vector2(
        //     Mathf.Sin(_time) * SwayX,
        //     Mathf.Cos(_time * 1.5f) * SwayY
        // );
    }

    public async void StartTyping() {
        if (_isTyping) return;
        _isTyping = true;
        Text = "";

        foreach (char c in _fullText) {
            Text += c;
            await ToSignal(GetTree().CreateTimer(TypeSpeed), "timeout");
        }

        _isTyping = false;
    }

    // Call this to replay the typing effect
    public void ResetAndType() {
        _fullText = Text;
        StartTyping();
    }
}
