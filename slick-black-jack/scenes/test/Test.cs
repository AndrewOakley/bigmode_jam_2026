using Godot;
using System;

public partial class Test : Node {
    [Export] private BlackjackTable _table;
    [Export] private HandUI _dealerHandUi;
    
    public override void _Ready() {
        _table.StartNewRound();
        GD.Print(_table.GetDealerHand().ToString());
        _dealerHandUi.SetHand(_table.GetDealerHand(), true);
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
    
    private void OnSplitPressed() {
        GD.Print("Split...");
    }
}
