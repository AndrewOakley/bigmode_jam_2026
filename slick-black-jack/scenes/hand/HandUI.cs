using Godot;
using System;
using System.Collections.Generic;
using SlickBlackJack.Components;

public partial class HandUI : HBoxContainer {
	public Hand Hand { get; private set; }
	private readonly List<CardUI> _cardUIs = [];
	
	public override void _Ready() {
		foreach (Node child in GetChildren()) {
			if (child is CardUI cardUi) {
				_cardUIs.Add(cardUi);
			}
		}
		
	}
	
	public void SetHand(Hand hand) {
		Hand = hand;

		var handCards = hand.GetCards();
		for (var i = 0; i < _cardUIs.Count; i++) {
			_cardUIs[i].SetCard(handCards[i]);
		}
	}
}
