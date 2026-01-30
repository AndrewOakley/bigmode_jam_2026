using Godot;
using System.Collections.Generic;
using System.Linq;

namespace SlickBlackJack.Components {
    public partial class Hand : RefCounted {
        private List<Card> _cards;

        public Hand() {
            _cards = new List<Card>();
        }

        public void AddCard(Card card) {
            if (card != null) {
                _cards.Add(card);
            }
        }

        public void Clear() {
            _cards.Clear();
        }

        public int CardCount => _cards.Count;

        public List<Card> GetCards() {
            return new List<Card>(_cards);
        }

        /// <summary>
        /// Calculates the best possible value for this hand.
        /// Aces count as 11 unless that would cause a bust, then they count as 1.
        /// </summary>
        public int GetValue() {
            int value = 0;
            int aceCount = 0;

            // First pass: count base value and aces
            foreach (Card card in _cards) {
                if (card.IsAce()) {
                    aceCount++;
                    value += 11; // Initially count aces as 11
                } else {
                    value += card.GetValue();
                }
            }

            // Second pass: convert aces from 11 to 1 if needed to avoid bust
            while (value > 21 && aceCount > 0) {
                value -= 10; // Convert one ace from 11 to 1
                aceCount--;
            }

            return value;
        }

        public bool IsBusted() {
            return GetValue() > 21;
        }

        public bool IsBlackjack() {
            return _cards.Count == 2 && GetValue() == 21;
        }

        /// <summary>
        /// Returns true if the hand has a soft total (contains an ace counted as 11)
        /// </summary>
        public bool IsSoft() {
            if (!_cards.Any(c => c.IsAce())) {
                return false;
            }

            int value = 0;
            foreach (Card card in _cards) {
                value += card.IsAce() ? 11 : card.GetValue();
            }

            return value <= 21;
        }

        public override string ToString() {
            var cardStrings = _cards.Select(c => c.ToString());
            return $"[{string.Join(", ", cardStrings)}] = {GetValue()}";
        }
    }
}
