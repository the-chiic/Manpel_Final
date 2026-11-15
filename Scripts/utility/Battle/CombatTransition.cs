using Godot;

public partial class CombatTransition : Node
{
    public static CombatTransition Instance { get; private set; }

    private const string CombatScenePath = "res://Scenes/combat_scene.tscn";
    private Node2D _worldLayer;
    private Node2D _combatLayer;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public void StartBattle(Player player, Enemy1 enemy)
    {
        _worldLayer = GetTree().CurrentScene as Node2D;
        
        // 1. Get the current camera position BEFORE we hide the world
        var worldCamera = _worldLayer.GetViewport().GetCamera2D();
        Vector2 cameraPos = worldCamera != null ? worldCamera.GlobalPosition : Vector2.Zero;

        // 2. Hide and pause the world
        _worldLayer.Visible = false; 
        _worldLayer.ProcessMode = Node.ProcessModeEnum.Disabled;
        player.CanMove = false;

        // 3. Instantiate the combat scene
        var packed = GD.Load<PackedScene>(CombatScenePath);
        _combatLayer = packed.Instantiate<Node2D>();
        
        // 4. Spawn the combat scene exactly where the camera was looking
        _combatLayer.GlobalPosition = cameraPos;
        
        GetTree().Root.AddChild(_combatLayer);

        // 5. FORCE the combat camera to become the active camera
        var combatCamera = _combatLayer.GetNodeOrNull<Camera2D>("Camera2D");
        if (combatCamera != null)
        {
            combatCamera.MakeCurrent();
        }

        // 6. Setup the manager
        var manager = _combatLayer.GetNode<CombatManager>("CombatManager");
        manager.Setup(player.State, enemy);
    }

    public void EndCombat(bool playerWon, Enemy1 mapEnemy)
    {
        // 1. Unpause and show the world
        _worldLayer.ProcessMode = Node.ProcessModeEnum.Inherit;
        _worldLayer.Visible = true; 
        
        var player = _worldLayer.GetNode<Player>("Player");
        if (player != null) player.CanMove = true;
        
        // 2. Handle enemy death
        if (playerWon && IsInstanceValid(mapEnemy))
            mapEnemy.QueueFree();

        // 3. Delete the combat map
        _combatLayer.QueueFree();
        
        // 4. Force the OVERWORLD camera to become active again
        var worldCamera = _worldLayer.GetViewport().GetCamera2D();
        if (worldCamera != null)
        {
            worldCamera.MakeCurrent();
        }

        // 5. Reset turns
        TurnManager.Instance?.ResetToPlayerTurn();
    }
}