using Godot;

public partial class Menu : Node2D {
    private AudioStreamPlayer _winSfx;

    public override void _Ready() {
        var playBtn = GetNode<Button>("buttons/Button");
        var cashOutBtn = GetNode<Button>("buttons/Button3");
        _winSfx = GetNode<AudioStreamPlayer>("winsfx");

        playBtn.Pressed += OnPlayPressed;
        cashOutBtn.Pressed += OnCashOutPressed;
    }

    private async void OnPlayPressed() {
        _winSfx.Play();
        await ToSignal(_winSfx, AudioStreamPlayer.SignalName.Finished);
        GetTree().ChangeSceneToFile("res://scenes/test/test.tscn");
    }

    private void OnCashOutPressed() {
        GetTree().Quit();
    }
}
