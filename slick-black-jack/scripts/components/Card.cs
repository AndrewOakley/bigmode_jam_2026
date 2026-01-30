using Godot;

namespace SlickBlackJack.Components {
	public enum Suit {
		Hearts,
		Diamonds,
		Clubs,
		Spades
	}

	public enum Rank {
		Ace = 1,
		Two = 2,
		Three = 3,
		Four = 4,
		Five = 5,
		Six = 6,
		Seven = 7,
		Eight = 8,
		Nine = 9,
		Ten = 10,
		Jack = 11,
		Queen = 12,
		King = 13
	}

	public partial class Card : RefCounted {
		public Suit Suit { get; private set; }
		public Rank Rank { get; private set; }

		public Card(Suit suit, Rank rank) {
			Suit = suit;
			Rank = rank;
		}

		/// <summary>
		/// Gets the blackjack value of the card. Ace can be 1 or 11 (handled by Hand class).
		/// Face cards (Jack, Queen, King) are worth 10.
		/// </summary>
		public int GetValue() {
			if (Rank >= Rank.Jack) {
				return 10;
			}
			return (int)Rank;
		}

		public bool IsAce() {
			return Rank == Rank.Ace;
		}

		public override string ToString() {
			return $"{Rank} of {Suit}";
		}
	}
}
