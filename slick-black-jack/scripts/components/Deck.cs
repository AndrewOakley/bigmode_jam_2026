using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SlickBlackJack.Components {
    public partial class Deck : RefCounted {
        private List<Card> _cards;
        private Random _random;

        public int CardsRemaining => _cards.Count;

        public Deck(int numberOfDecks = 1) {
            _random = new Random();
            _cards = new List<Card>();
            InitializeDeck(numberOfDecks);
        }

        /// <summary>
        /// Creates a fresh deck with the specified number of standard 52-card decks
        /// </summary>
        private void InitializeDeck(int numberOfDecks) {
            _cards.Clear();

            for (int deckIndex = 0; deckIndex < numberOfDecks; deckIndex++) {
                foreach (Suit suit in Enum.GetValues(typeof(Suit))) {
                    foreach (Rank rank in Enum.GetValues(typeof(Rank))) {
                        _cards.Add(new Card(suit, rank));
                    }
                }
            }
        }

        /// <summary>
        /// Shuffles the deck using Fisher-Yates algorithm
        /// </summary>
        public void Shuffle() {
            int n = _cards.Count;
            for (int i = n - 1; i > 0; i--) {
                int j = _random.Next(i + 1);
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }
        }

        /// <summary>
        /// Draws a single card from the deck
        /// </summary>
        public Card DrawCard() {
            if (_cards.Count == 0) {
                GD.PrintErr("Deck is empty! Cannot draw card.");
                return null;
            }

            Card card = _cards[0];
            _cards.RemoveAt(0);
            return card;
        }

        /// <summary>
        /// Draws multiple cards from the deck
        /// </summary>
        public List<Card> DrawCards(int count) {
            List<Card> drawnCards = new List<Card>();
            for (int i = 0; i < count; i++) {
                Card card = DrawCard();
                if (card != null) {
                    drawnCards.Add(card);
                }
            }
            return drawnCards;
        }

        /// <summary>
        /// Resets the deck to a fresh state and shuffles
        /// </summary>
        public void Reset(int numberOfDecks = 1) {
            InitializeDeck(numberOfDecks);
            Shuffle();
        }
    }
}
