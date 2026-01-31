using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using SlickBlackJack.Components;

public partial class HandUI : Control {
	public Hand Hand { get; private set; }
	public Marker2D DealerPointTo;

	private Label _scoreLabel;
	private HBoxContainer _cardsContainer;
	private readonly List<CardUI> _cardUIs = [];
	
	public override void _Ready() {
		_scoreLabel = GetNode<Label>("ScoreLabel");
		_cardsContainer = GetNode<HBoxContainer>("Cards");
		DealerPointTo = GetNode<Marker2D>("DealerPointTo");
		
		foreach (var child in _cardsContainer.GetChildren()) {
			if (child is CardUI cardUi) {
				_cardUIs.Add(cardUi);
			}
			
		}

		_scoreLabel.Hide();
	}
	
	public void SetHand(Hand hand, bool isDealer = false) {
		if (Hand != null) {
			GD.PushWarning("Attempting to set a new hand when one is already set.");
			return;
		}
		
		if (hand == null) {
			GD.PushError("Hand is null, cannot set hand.");
			return;
		}

		Hand = hand;
		Hand.CardAdded += OnCardAdded;
		Hand.CardRemoved += RefreshCards;
		if (isDealer) {
			Hand.FlipDealerCard += OnFlipDealerCard;
		}
	}

	private void OnCardAdded(Card card, bool faceDown = false) {
		var handCards = Hand.GetCards();
		// place a card in next available slot
		for (var i = 0; i < _cardUIs.Count; i++) {
			if (_cardUIs[i].Card != null) continue;
			
			_cardUIs[i].SetCard(handCards[i], faceDown);
			// TODO: fix this hacky way of hiding the dealers hand count
			var scoreDealerAdjusted = !faceDown ? Hand.GetValue() : Hand.GetValue() - card.GetValue();
			_scoreLabel.Text = scoreDealerAdjusted.ToString();
			_scoreLabel.Show();
			return;
		}
	}
	
	private void OnFlipDealerCard() {
		var handCards = Hand.GetCards();
		foreach (var cardUi in _cardUIs.Where(cardUi => cardUi.Card != null)) {
			cardUi.SetCardFaceUp();
			_scoreLabel.Text = Hand.GetValue().ToString();
			_scoreLabel.Show();
		}
	}
	
	private void RefreshCards() {
		var handCards = Hand.GetCards();
		for (var i = 0; i < _cardUIs.Count; i++) {
			if (i >= handCards.Count) {
				_cardUIs[i].SetCard(null);
				continue;
			}
			
			_cardUIs[i].SetCard(handCards[i]);
		}
		
		_scoreLabel.Text = Hand.GetValue().ToString();
	}
}
