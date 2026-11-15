using Godot;
using Attacks;

[GlobalClass]
public partial class PlayerDefinitionData : Resource
{
    [Export] public string DisplayName = "Hero";
    [Export] public Texture2D SpriteRef;
    [Export] public int MaxHP = 100;
    [Export] public int MaxMP = 20;
    [Export] public AttacksDefinitionResource Attacks;
    [Export] public int Defense = 10;
    [Export] public int Speed = 8;
}

// Runtime state that persists across battles
public class PlayerState
{
    public PlayerDefinitionData Definition;
    public int CurrentHP;
    public int CurrentMP;

    public PlayerState(PlayerDefinitionData def)
    {
        Definition = def;
        CurrentHP = def.MaxHP;
        CurrentMP = def.MaxMP;
    }
}