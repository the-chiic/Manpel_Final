using Godot;

[GlobalClass]
public partial class EnemyDefinitionData : Resource
{
    [Export] public string DisplayName = "Banana";
    [Export] public Texture2D SpriteRef;
    [Export] public int MaxHP = 30;
    [Export] public AttacksDefinitionResource Attacks;
    [Export] public int Defense = 5;
    [Export] public int Speed = 5;
    [Export] public int GoldReward = 5;
}