using Godot;
using System;

public partial class Test : Node {
    [Export] private BlackjackTable _table;
    [Export] private HandUI _dealerHandUi;
    [Export] private GameUI _gameUi;
    
    public override void _Ready() {
        _gameUi ??= GetNode<GameUI>("GameUI");
        _gameUi.Hit += OnHitPressed;
        _gameUi.Stand += OnStandPressed;
        _gameUi.Split += OnSplitPressed;
        
        _table.StartNewRound();
     }
    
    private void OnHitPressed() {
        _table.PlayerHit();
    }
    
    private void OnStandPressed() {
        _table.PlayerStand();
    }
    
    private void OnSplitPressed() {
        GD.Print("Split...");
        _table.PlayerSplit();
    }
}
