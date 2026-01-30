using Godot;
using System;

public partial class Test : Node  {
    [Export] private BlackjackTable _table;
    [Export] private HandUI _dealerHandUI;
    [Export] private HandUI _playerHandUI;
    
    public override void _Ready() {
        _table.StartNewRound();
        GD.Print(_table.GetDealerHand().ToString());
        _dealerHandUI.SetHand(_table.GetDealerHand());
        _playerHandUI.SetHand(_table.GetPlayerHand());
     }
}
