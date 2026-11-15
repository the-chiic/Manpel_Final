using Godot;

[GlobalClass]
public partial class AttacksDefinitionResource : Resource
{
    [Export] public Godot.Collections.Array<EnemyAttackResource> attacks = new();
}