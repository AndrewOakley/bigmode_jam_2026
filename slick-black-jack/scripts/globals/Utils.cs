using Godot;
using System;
using SlickBlackJack.Components;

public partial class Utils : Node {
    public delegate void CheatStartedEventHandler();
    public static event CheatStartedEventHandler CheatStarted;
    
    public static void EmitCheatStarted() => CheatStarted?.Invoke();
    
    public delegate void CardSelectedventHandler(Card card);
    public static event CardSelectedventHandler CardSelected;
    
    public static void EmitCardSelected(Card card) => CardSelected?.Invoke(card);
    
    public delegate void NpcCardSelectStartEventHandler();
    public static event NpcCardSelectStartEventHandler NpcCardSelectStart;
    
    public static void EmitNpcCardSelectStart() => NpcCardSelectStart?.Invoke();
    
    public delegate void UserSwappedCardEventHandler();
    public static event UserSwappedCardEventHandler UserSwappedCard;
    
    public static void EmitUserSwappedCard() => UserSwappedCard?.Invoke();
    
    public delegate void StopAllCardsSelectableEventHandler();
    public static event StopAllCardsSelectableEventHandler StopAllCardsSelectable;
    
    public static void EmitStopAllCardsSelectable() => StopAllCardsSelectable?.Invoke();
}
