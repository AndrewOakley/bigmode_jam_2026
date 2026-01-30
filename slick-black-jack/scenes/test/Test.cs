using Godot;
using System;

public partial class Test : Node {
    [Export] private BlackjackTable _table;
    [Export] private HandUI _dealerHandUI;
    [Export] private HandUI _playerHandUI;
    [Export] private HandUI _npcHandUI1;
    [Export] private HandUI _npcHandUI2;
    
    public override void _Ready() {
        _table.StartNewRound();
        GD.Print(_table.GetDealerHand().ToString());
        _dealerHandUI.SetHand(_table.GetDealerHand(), true);
        _npcHandUI1.SetHand(_table.GetPlayerHand(0));
        _playerHandUI.SetHand(_table.GetPlayerHand(1));
        _npcHandUI2.SetHand(_table.GetPlayerHand(2));
     }
    
    private void OnHitPressed() {
        _table.PlayerHit();
    }
    
    private void OnStandPressed() {
        _table.PlayerStand();
    }
    
    private void OnRestartPressed() {
        GD.Print("Restarting...");
        GetTree().ReloadCurrentScene();
    }
}
