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
	
	public void SetHand(Hand hand, bool isDealer = false) {
		// Unsubscribe from previous hand
		if (Hand != null) {
			Hand.CardAdded -= OnCardAdded;
		}
		
		if (hand == null) {
			return;
		}

		Hand = hand;
		Hand.CardAdded += OnCardAdded;

		var handCards = Hand.GetCards();
		for (var i = 0; i < _cardUIs.Count; i++) {
			if (i >= handCards.Count) {
				_cardUIs[i].SetCard(null);
				continue;
			}
			
			var faceDown = i == handCards.Count - 1 && isDealer;
			_cardUIs[i].SetCard(handCards[i], faceDown);
		}
	}

	private void OnCardAdded(Card card) {
		var handCards = Hand.GetCards();
		for (var i = 0; i < _cardUIs.Count; i++) {
			if (i >= handCards.Count) {
				_cardUIs[i].SetCard(null);
				continue;
			}
			
			_cardUIs[i].SetCard(handCards[i]);
		}
	}
}
