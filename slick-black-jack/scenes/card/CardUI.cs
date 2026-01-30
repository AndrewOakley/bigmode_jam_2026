using Godot;
using System;
using SlickBlackJack.Components;

public partial class CardUI : TextureRect {
	private const int CardWidth = 48;
	private const int CardHeight = 64;
	private Vector2 FaceDownOffset = new Vector2(1 * CardWidth, 4 * CardHeight);

	public Card Card;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		Card = null;
		Hide();
		var atlasTexture = (AtlasTexture)Texture.Duplicate();
		Texture = atlasTexture;
	}
	
	public void SetCard(Card card, bool faceDown = false) {
		Card = card;

		if (card == null) {
			Hide();
			return;
		}

		SetCardSide(faceDown);
	}
	
	public void SetCardFaceUp() {
		SetCardSide(false);
	}
	
	public void SetCardFaceDown() {
		SetCardFaceUp();
	}
	
	private void SetCardSide(bool faceDown) {
		if (Card == null) {
			GD.PushError("Card is null, cannot set card side");
			return;
		}
		
		var atlasTexture = (AtlasTexture)Texture;
		if (faceDown) {
			atlasTexture.Region = new Rect2(FaceDownOffset.X, FaceDownOffset.Y, CardWidth, CardHeight);
			Show();
			return;
		}

		// Calculate atlas region based on rank and suit
		var rankIndex = (int)Card.Rank - 1;
		var suitIndex = (int)Card.Suit - 1;

		var x = rankIndex * CardWidth;
		var y = suitIndex * CardHeight;

		atlasTexture.Region = new Rect2(x, y, CardWidth, CardHeight);
		Show();
	}
}
