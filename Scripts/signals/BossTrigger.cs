using Godot;

public partial class BossTrigger : Area2D
{
    [Export] public Enemy1 Boss;

    private bool _triggered;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    private async void OnBodyEntered(Node2D body)
    {
        if (_triggered || !body.IsInGroup("player")) return;
        if (Boss == null || !IsInstanceValid(Boss)) return;
        _triggered = true;

        var transition = GetNode<Transition>("/root/Transition");
        var player = body as Player;

        await transition.TransitionToAsync(() =>
        {
            CombatTransition.Instance.StartBattle(player, Boss);
        });
    }

    private void OnBodyExited(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            _triggered = false;
        }
    }
}
