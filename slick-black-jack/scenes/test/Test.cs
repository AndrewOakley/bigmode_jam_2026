using Godot;
using System;

public partial class Test : Node
{
    [Export] private BlackjackTable _table;
    [Export] private HandUI _dealerHandUi;
    [Export] private GameUI _gameUi;

    private TextureRect _gameOverOverlay;
    private Node2D _gameOverUi;
    private Node2D _gameWinUi;
    private Player _mainPlayer;

    public override void _Ready()
    {
        _gameUi ??= GetNode<GameUI>("GameUI");
        _gameUi.Hit += OnHitPressed;
        _gameUi.Stand += OnStandPressed;
        _gameUi.Split += OnSplitPressed;
        _gameUi.DoubleDown += OnDoubleDownPressed;
        _table.RoundEnded += RoundEnded;

        // Get game over overlay and UI
        _gameOverOverlay = GetNode<TextureRect>("TextureRect");
        _gameOverUi = _gameOverOverlay.GetNode<Node2D>("GameOverUI");

        // Wire up game over buttons
        var losePlayAgainBtn = _gameOverUi.GetNode<Button>("VBoxContainer/HBoxContainer/Button");
        var loseQuit = _gameOverUi.GetNode<Button>("VBoxContainer/HBoxContainer/Button2");
        losePlayAgainBtn.Pressed += OnLetItRidePressed;
        loseQuit.Pressed += OnGiveUpPressed;

        // Get game win UI and wire up buttons
        _gameWinUi = _gameOverOverlay.GetNode<Node2D>("GameWinUI");
        // _gameWinUi.Hide();
        var winPlayAgainBtn = _gameWinUi.GetNode<Button>("VBoxContainer/HBoxContainer/Button");
        var winQuitBtn = _gameWinUi.GetNode<Button>("VBoxContainer/HBoxContainer/Button2");
        winPlayAgainBtn.Pressed += OnLetItRidePressed;
        winQuitBtn.Pressed += OnGiveUpPressed;

        // Find the main player and track peak chips
        var mainPlayers = GetTree().GetNodesInGroup("main_player");
        foreach (var player in mainPlayers)
        {
            if (player is Player p)
            {
                _mainPlayer = p;
                _mainPlayer.ChipsChanged += OnMainPlayerChipsChanged;
                UpdateFattestStack(_mainPlayer.Chips);
                break;
            }
        }

        _table.OpenForBets();
    }

    private void OnHitPressed()
    {
        _table.PlayerHit();
    }

    private void OnStandPressed()
    {
        _table.PlayerStand();
    }

    private void OnDoubleDownPressed()
    {
        _table.PlayerDoubleDown();
    }

    private void OnSplitPressed()
    {
        GD.Print("Split...");
        _table.PlayerSplit();
    }

    private void RoundEnded()
    {
        GD.Print("Round ended!");
        RoundEndedHelper();
    }

    public async void RoundEndedHelper()
    {
        GD.Print("Round ended!");
        await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);

        if (_mainPlayer != null && _mainPlayer.Chips <= 0)
        {
            var fattestStackLabel = _gameOverUi.GetNode<Label>("VBoxContainer/Label3");
            fattestStackLabel.Text = $"CLOSEST TO BEING A GOOD DAD: ${Utils.FattestStack:N0}";
            _gameWinUi.Hide();
            _gameOverUi.Show();
            _gameOverOverlay.Show();
            return;
        }

        if (_mainPlayer.Chips >= 100)
        {
            _gameOverUi.Hide();
            _gameWinUi.Show();
            _gameOverOverlay.Show();
            return;
        }

        _table.OpenForBets();
    }

    private void OnMainPlayerChipsChanged(int newChips)
    {
        UpdateFattestStack(newChips);
    }

    private void UpdateFattestStack(int chips)
    {
        if (chips > Utils.FattestStack)
        {
            Utils.FattestStack = chips;
        }
    }

    private void OnLetItRidePressed()
    {
        GetTree().ReloadCurrentScene();
    }

    private void OnGiveUpPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/menu.tscn");
    }
}
