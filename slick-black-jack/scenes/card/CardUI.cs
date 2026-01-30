using Godot;
using System;
using SlickBlackJack.Components;

public partial class CardUI : TextureRect {
	private const int CardWidth = 48;
	private const int CardHeight = 64;

	private Card _card;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
	}
	
	public void SetCard(Card card) {
		_card = card;

		// Calculate atlas region based on rank and suit
		var rankIndex = (int)card.Rank - 1;
		var suitIndex = (int)card.Suit - 1;

		var x = rankIndex * CardWidth;
		var y = suitIndex * CardHeight;

		var atlasTexture = (AtlasTexture)Texture;
		atlasTexture.Region = new Rect2(x, y, CardWidth, CardHeight);
	}
}
