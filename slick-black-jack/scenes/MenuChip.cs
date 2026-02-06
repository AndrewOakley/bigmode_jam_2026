using Godot;

public partial class MenuChip : TextureRect
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GetNode<TextureRect>("TextureRect").Hide();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    private void OnButtonMouseEntered()
    {
        GetNode<TextureRect>("TextureRect").Show();
    }

    private void OnButtonMouseExited()
    {
        GetNode<TextureRect>("TextureRect").Hide();
    }

}
