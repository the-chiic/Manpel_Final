using Godot;
using Attacks;

[GlobalClass]
public partial class EnemyAttackResource : Resource
{
    [Export] public int Damage = 10;
    [Export] public EnemyAttacksType attackType;
}