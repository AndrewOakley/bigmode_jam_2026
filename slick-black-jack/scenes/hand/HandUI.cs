using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using SlickBlackJack.Components;

public partial class HandUI : Control {
	public Hand Hand { get; private set; }
	public Marker2D DealerPointTo;
	private Label _winResultLabel;
	private Label _lossResultLabel;
	private Label _pushResultLabel;
	private Label _chipCountLabel;
	private TextureRect _chipTexture;

	private Label _scoreLabel;
	private HBoxContainer _cardsContainer;
	private readonly List<CardUI> _cardUIs = [];
	private AudioStreamPlayer _sfxPlayer;
	
	public override void _Ready() {
		_scoreLabel = GetNode<Label>("ScoreLabel");
		_cardsContainer = GetNode<HBoxContainer>("Cards");
		DealerPointTo = GetNode<Marker2D>("DealerPointTo");
		_winResultLabel = GetNode<Label>("WinResult");
		_lossResultLabel = GetNode<Label>("LossResult");
		_pushResultLabel = GetNode<Label>("PushResult");
		_chipCountLabel = GetNode<Label>("%ChipCount");
		_chipTexture = GetNode<TextureRect>("ChipTexture");
		_sfxPlayer = GetNode<AudioStreamPlayer>("DealSFX");
		
		_winResultLabel.Hide();
		_lossResultLabel.Hide();
		_pushResultLabel.Hide();
		_chipCountLabel.Hide();
		_chipTexture.Hide();
		
		foreach (var child in _cardsContainer.GetChildren()) {
			if (child is CardUI cardUi) {
				_cardUIs.Add(cardUi);
			}
			
		}

		_scoreLabel.Hide();

		if (Hand != null) {
			OnChipsChanged(Hand.Chips);
		}
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
		Hand.ChipsChanged += OnChipsChanged;
		OnChipsChanged(Hand.Chips);
		
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
			_sfxPlayer.Play();
			return;
		}
	}
	
	private void OnChipsChanged(int chipCount) {
		if (chipCount == 0 || _chipCountLabel == null || _chipTexture == null) return;
		
		_chipCountLabel.Text = $"{chipCount}";
		_chipCountLabel.Show();
		_chipTexture.Show();
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
	
	public void ShowResult(HandResult result) {
		switch (result) {
			case HandResult.None:
				GD.Print($"No result to show. {Hand}");
				return;
			case HandResult.Push:
				_pushResultLabel.Show();
				break;
			case HandResult.PlayerWin:
				_winResultLabel.Show();
				break;
			case HandResult.DealerWin:
				_lossResultLabel.Show();
				break;
			case HandResult.PlayerBlackjack:
				_winResultLabel.Text = "Blackjack!";
				_winResultLabel.Show();
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(result), result, null);
		}
	}
}
