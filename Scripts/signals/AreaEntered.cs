using Godot;

public partial class AreaEntered : Area2D
{
    [Export] public int RegionIndex;
    
    [Export] public bool TeleportPlayer = false;
    [Export] public Vector2 SpawnPosition;

    private bool _triggered;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    private async void OnBodyEntered(Node2D body)
    {
        if (_triggered || !body.IsInGroup("player")) return;
        _triggered = true;

        GD.Print("1. Passed checks. Starting transition...");

        var transition = GetNode<Transition>("/root/Transition");
        var player = body as Player;

        // Access the singleton directly!
        if (WorldGenerator.Instance == null)
        {
            GD.PrintErr("ERROR: WorldGenerator Instance is null! Is the script attached and in the scene?");
            return;
        }

        await transition.TransitionToAsync(() =>
        {
            GD.Print("2. Inside the transition lambda!");
            
            Vector2? spawn = null;
            
            if (TeleportPlayer)
            {
                Vector2 offset = player.LastMoveDirection * GridMath.TileSize;
                spawn = SpawnPosition + offset;
                GD.Print($"3. Teleporting to: {spawn}");
            }

            WorldGenerator.Instance.ApplyRegionSettings(RegionIndex, player, spawn);
        });
        
        GD.Print("4. Transition complete. Resuming gameplay.");
    }

    private void OnBodyExited(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            _triggered = false;
        }
    }
}