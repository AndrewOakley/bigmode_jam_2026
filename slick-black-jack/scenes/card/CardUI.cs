using Godot;
using System;
using SlickBlackJack.Components;

public partial class CardUI : TextureRect {
	private const int CardWidth = 48;
	private const int CardHeight = 64;
	private Vector2 FaceDownOffset = new Vector2(1 * CardWidth, 4 * CardHeight);

	private Card _card;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
	}
	
	public void SetCard(Card card, bool faceDown = false) {
		_card = card;

		if (card == null) {
			Hide();
			return;
		}

		var atlasTexture = (AtlasTexture)Texture.Duplicate();
		Texture = atlasTexture;

		if (faceDown) {
			atlasTexture.Region = new Rect2(FaceDownOffset.X, FaceDownOffset.Y, CardWidth, CardHeight);
			Show();
			return;
		}

		// Calculate atlas region based on rank and suit
		var rankIndex = (int)card.Rank - 1;
		var suitIndex = (int)card.Suit - 1;

		var x = rankIndex * CardWidth;
		var y = suitIndex * CardHeight;

		atlasTexture.Region = new Rect2(x, y, CardWidth, CardHeight);
		Show();
	}
}
