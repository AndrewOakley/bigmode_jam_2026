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
		if (isDealer) {
			Hand.FlipDealerCard += OnFlipDealerCard;
		}
	}

	private void OnCardAdded(Card card, bool faceDown = false) {
		var handCards = Hand.GetCards();
		for (var i = 0; i < _cardUIs.Count; i++) {
			// place a card in next available slot
			if (_cardUIs[i].Card == null) {
				_cardUIs[i].SetCard(handCards[i], faceDown);
				return;
			}
		}
	}
	
	private void OnFlipDealerCard() {
		var handCards = Hand.GetCards();
		for (var i = 0; i < _cardUIs.Count; i++) {
			if (_cardUIs[i].Card != null) {
				_cardUIs[i].SetCard(handCards[i]);
				return;
			}
		}
	}
}
