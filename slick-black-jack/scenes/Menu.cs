using Godot;

public partial class Menu : Node2D {
    public override void _Ready() {
        var playBtn = GetNode<Button>("buttons/Button");
        var cashOutBtn = GetNode<Button>("buttons/Button3");

        playBtn.Pressed += OnPlayPressed;
        cashOutBtn.Pressed += OnCashOutPressed;
    }

    private void OnPlayPressed() {
        GetTree().ChangeSceneToFile("res://scenes/test/test.tscn");
    }

    private void OnCashOutPressed() {
        GetTree().Quit();
    }
}
