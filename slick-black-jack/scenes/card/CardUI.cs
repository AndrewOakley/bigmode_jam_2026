using Godot;
using System;
using SlickBlackJack.Components;

public partial class CardUI : Panel {
	private Label _label;
	
	public Card _card;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		_label = GetNode<Label>("Label");
	}
	
	public void SetCard(Card card) {
		_card = card;
		_label.Text = card.ToString();
	}
}
