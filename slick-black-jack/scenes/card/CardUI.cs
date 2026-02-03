using Godot;
using System;
using System.Threading.Tasks;
using SlickBlackJack.Components;

public partial class CardUI : TextureRect {
	[Signal] public delegate void CardChangedEventHandler(Card card, bool faceDown);
	
	private const int AtlasRegionOffsetX = 17;
	private const int AtlasRegionOffsetY = 7;
	private const int AtlasRegionSeparationX = 32;
	private const int AtlasRegionSeparationY = 32;
	private const int CardWidth = 15;
	private const int CardHeight = 22;
	private Vector2 FaceDownCoordinate = new Vector2(2, 4);
	private bool _isFaceDown = false;

	private bool _selectable = false;
	
	[Export] private AnimationPlayer _animationPlayer;
	[Export] private bool _test = false;

	public Card Card;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		_animationPlayer ??= GetNode<AnimationPlayer>("AnimationPlayer");
		Card = null;
		Hide();
		var atlasTexture = (AtlasTexture)Texture.Duplicate();
		Texture = atlasTexture;
		Utils.StopAllCardsSelectable += OnStopAllCardsSelectable;
		
		if (_test) {
			Card = new Card(Suit.Hearts, Rank.Ace);
			Show();
		} 
	}
	    
	// ALWAYS DO THIS IF CALLING SIGNALS FROM UTILS
	protected override void Dispose(bool disposing) {
		Utils.StopAllCardsSelectable -= OnStopAllCardsSelectable;
		base.Dispose(disposing);
	}
	
	private void OnStopAllCardsSelectable() {
		SetCardSelectable(false);
	}
	
	public void SetCard(Card card, bool faceDown = false) {
		if (Card != null) {
			Card.CardSwapped -= OnCardSwapped;
		}
		
		Card = card;

		if (card == null) {
			Hide();
			return;
		}

		Card.CardSwapped += OnCardSwapped;
		SetCardSide(faceDown);
		EmitSignal(SignalName.CardChanged, card, faceDown);
	}
	
	private void OnCardSwapped(Card card) {
		SetCard(card, _isFaceDown);
	}

	public void SetCardSelectable(bool selectable) {
		_selectable = selectable;

		if (selectable) {
			_animationPlayer.Play("selectable");
			MouseDefaultCursorShape = CursorShape.PointingHand;
		}
		else {
			_animationPlayer.Stop();
			MouseDefaultCursorShape = CursorShape.Arrow;
		}
	}
	
	private void OnGuiInput(InputEvent @event) {
		if (!_selectable) return;
		
		// on click emit click from global Utils
		if (@event.IsActionPressed("click")) {
			GD.Print("Card selected", Card);
			AcceptEvent();
			// wait one frame then emit
			EmitCardSelected();
		}
	}

	// trick to ensure mouse clicks dont bleed over to next gui input
	private async Task EmitCardSelected() {
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		Utils.EmitCardSelected(Card);
	}
	
	public void SetCardFaceUp() {
		SetCardSide(false);
	}
	
	private void SetCardSide(bool faceDown) {
		_isFaceDown = faceDown;
		
		
		if (Card == null) {
			GD.PushError("Card is null, cannot set card side");
			return;
		}
		
		if (faceDown) {
			SetAtlasTexture((int)FaceDownCoordinate.X, (int)FaceDownCoordinate.Y);
			Show();
			return;
		}

		// Calculate atlas region based on rank and suit
		var rankIndex = (int)Card.Rank - 1;
		var suitIndex = (int)Card.Suit - 1;

		SetAtlasTexture(rankIndex, suitIndex);
		Show();
	}

	private void SetAtlasTexture(int x, int y) {
		var adjustedX = (x * AtlasRegionSeparationX) + AtlasRegionOffsetX;
		var adjustedY = (y * AtlasRegionSeparationY) + AtlasRegionOffsetY;
		
		var atlasTexture = (AtlasTexture)Texture;
		atlasTexture.Region = new Rect2(adjustedX, adjustedY, CardWidth, CardHeight);
	}
}
