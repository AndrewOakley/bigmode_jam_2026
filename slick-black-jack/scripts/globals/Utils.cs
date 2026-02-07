using Godot;
using System;
using SlickBlackJack.Components;

public partial class Utils : Node {
    public static int FattestStack { get; set; } = 0;
    public delegate void CheatStartedEventHandler();
    public static event CheatStartedEventHandler CheatStarted;
    
    public static void EmitCheatStarted() => CheatStarted?.Invoke();
    
    public delegate void CardSelectedventHandler(Card card, CardUI cardUI);
    public static event CardSelectedventHandler CardSelected;
    
    public static void EmitCardSelected(Card card, CardUI cardUI) => CardSelected?.Invoke(card, cardUI);
    
    public delegate void NpcCardSelectStartEventHandler();
    public static event NpcCardSelectStartEventHandler NpcCardSelectStart;
    
    public static void EmitNpcCardSelectStart() => NpcCardSelectStart?.Invoke();
    
    public delegate void UserSwappedCardEventHandler();
    public static event UserSwappedCardEventHandler UserSwappedCard;
    
    public static void EmitUserSwappedCard() => UserSwappedCard?.Invoke();
    
    public delegate void StopAllCardsSelectableEventHandler();
    public static event StopAllCardsSelectableEventHandler StopAllCardsSelectable;
    
    public static void EmitStopAllCardsSelectable() => StopAllCardsSelectable?.Invoke();
    
    public delegate void TurnTimerExpiredEventHandler();
    public static event TurnTimerExpiredEventHandler TurnTimerExpired;
    
    public static void EmitTurnTimerExpired() => TurnTimerExpired?.Invoke();
    
    public delegate void StopTurnTimerEventHandler();
    public static event StopTurnTimerEventHandler StopTurnTimer;
    
    public static void EmitStopTurnTimer() => StopTurnTimer?.Invoke();
    

    public delegate void PlayerbetSubmittedEventHandler(int bet);
    public static event PlayerbetSubmittedEventHandler PlayerbetSubmitted;
    
    public static void EmitPlayerbetSubmitted(int bet) => PlayerbetSubmitted?.Invoke(bet);
    
    public delegate void BettingStartedEventHandler();
    public static event BettingStartedEventHandler BettingStarted;
    
    public static void EmitBettingStarted() => BettingStarted?.Invoke();
    
    public delegate void CheatingStoppedEventHandler();
    public static event CheatingStoppedEventHandler CheatingStopped;
    
    public static void EmitCheatingStopped() => CheatingStopped?.Invoke();
    
    public delegate void ShowInsuranceEventHandler();
    public static event ShowInsuranceEventHandler ShowInsurance;
    
    public static void EmitShowInsurance() => ShowInsurance?.Invoke();
    
    public delegate void InsuranceSelectedEventHandler();
    public static event InsuranceSelectedEventHandler InsuranceSelected;
    
    public static void EmitInsuranceSelected() => InsuranceSelected?.Invoke();
    
    public delegate void InsuranceYesEventHandler();
    public static event InsuranceYesEventHandler InsuranceYes;
    
    public static void EmitInsuranceYes() => InsuranceYes?.Invoke();
}
