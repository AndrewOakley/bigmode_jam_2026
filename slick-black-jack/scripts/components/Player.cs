using Godot;
using System;

namespace SlickBlackJack.Components;

public partial class Player : RefCounted {
    public string Name { get; set; }
    public Hand Hand { get; set; }
    public int Chips { get; set; }
    public bool IsNpc { get; set; } = true;
}
