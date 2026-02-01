using Godot;
using System.Collections.Generic;
using System.Linq;

namespace SlickBlackJack.Components {
    public enum HandStatus {
        Empty,
        Active,
        Done,
    }
    
    public enum HandResult { 
        None,
        PlayerWin,
        DealerWin,
        Push,
        PlayerBlackjack
    }
    
    public partial class Hand : RefCounted {
        [Signal] public delegate void CardAddedEventHandler(Card card, bool faceDown = false);
        [Signal] public delegate void FlipDealerCardEventHandler();
        [Signal] public delegate void HandChangedEventHandler();
        [Signal] public delegate void HandStoodEventHandler();
        [Signal] public delegate void CardRemovedEventHandler();
        
        public HandStatus Status { get; set; }
        public HandResult Result { get; set; }
        private List<Card> _cards;

        public Hand() {
            _cards = new List<Card>();
            Status = HandStatus.Empty;
        }
        
        public void StandHand() {
            Status = HandStatus.Done;
            EmitSignal(SignalName.HandStood);
        }
        
        public void AddCard(Card card, bool faceDown = false, bool forceBlackjack = false, bool forceSplitCards = false) {
            if (forceBlackjack && card.GetValue() != 10) {
                card = new Card(card.Suit, Rank.Ace);
            }
            
            if (forceSplitCards) {
                card = new Card(card.Suit, Rank.Ace);
            }
            
            if (card == null) {
                GD.PrintErr("Cannot add null card to hand");
                return;
            };
            
            _cards.Add(card);
            EmitSignal(SignalName.CardAdded, card, faceDown);
            EmitSignal(SignalName.HandChanged);
            
            if (Status == HandStatus.Empty) {
                Status = HandStatus.Active;
            }
            
            if (GetValue() >= 21) {
                Status = HandStatus.Done;
            }
        }

        public Card RemoveCard(int index) {
            if (index < 0 || index >= _cards.Count) {
                GD.PrintErr("Invalid card index for removal");
                return null;
            }
            
            var cardRemoved = _cards[index];
            _cards.RemoveAt(index);
            
            EmitSignal(SignalName.CardRemoved);
            EmitSignal(SignalName.HandChanged);
            
            return cardRemoved;
        }

        public void Stand() {
            Status = HandStatus.Done;
            EmitSignal(SignalName.HandStood);
        }

        public void Clear() {
            _cards.Clear();
            Status = HandStatus.Empty;
            EmitSignal(SignalName.HandChanged);
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
        
        public bool CanSplit() {
            if (CardCount != 2) {
                return false;
            }
            
            if (_cards[0].GetValue() != _cards[1].GetValue()) {
                return false;
            }

            return true;
        }

        public bool CanDoubleDown() {
            return CardCount == 2;
        }
        
        public override string ToString() {
            var cardStrings = _cards.Select(c => c.ToString());
            return $"[{string.Join(", ", cardStrings)}] = {GetValue()}";
        }

        public void FlipOverDealerCard() {
            // TODO: This is a hacky way to reveal the dealer's first card'
            EmitSignal(SignalName.FlipDealerCard);
        }
    }
}
