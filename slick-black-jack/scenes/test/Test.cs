using Godot;
using System;

public partial class Test : Node {
    [Export] private BlackjackTable _table;
    [Export] private HandUI _dealerHandUI;
    [Export] private HandUI _playerHandUI;
    
    private Button playerHitButton;
    
    public override void _Ready() {
        _table.StartNewRound();
        GD.Print(_table.GetDealerHand().ToString());
        _dealerHandUI.SetHand(_table.GetDealerHand(), true);
        _playerHandUI.SetHand(_table.GetPlayerHand(0));
     }
    
    private void OnHitPressed() {
        _table.PlayerHit();
    }
    
    private void OnStandPressed() {
        _table.PlayerStand();
    }
}
