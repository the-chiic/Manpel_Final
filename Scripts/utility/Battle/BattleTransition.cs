using Godot;

public static class BattleTransition
{
    private const string BattleScenePath = "res://Scenes/combat_scene.tscn";

    public static void StartBattle(Node caller, Enemy1 enemy, Player player)
    {
        player.CanMove = false;
        if (!ResourceLoader.Exists(BattleScenePath))
        {
            GD.PrintErr($"Battle scene not found at: {BattleScenePath}");
            return;
        }
        Callable.From(() => ExecuteTransition(caller, enemy, player)).CallDeferred();
    }

    private static void ExecuteTransition(Node caller, Enemy1 enemy, Player player)
    {
        var battlePackedScene = GD.Load<PackedScene>(BattleScenePath);
        if (battlePackedScene == null) return;

        var tree = caller.GetTree();
        var overworldScene = tree.CurrentScene as Node2D;

        var battleInstance = battlePackedScene.Instantiate();

        // 1) Initialize FIRST — sets _enemyDef, _playerNode, _mapScene,
        //    and captures original parents/positions while player/enemy are
        //    still children of the overworld.
        if (battleInstance is BattleScene battleScript)
            battleScript.Initialize(player, enemy, overworldScene);

        // 2) Add to tree — _Ready fires now, and _enemyDef is already set.
        tree.Root.AddChild(battleInstance);

        // 3) Move the real nodes into the battle scene (parent is in tree now).
        player.Reparent(battleInstance, false);
        enemy.Reparent(battleInstance, false);

        player.Position = new Vector2(220, 320);
        enemy.Position  = new Vector2(600, 320);

        player.SetPhysicsProcess(false);
        enemy.SetPhysicsProcess(false);

        overworldScene.ProcessMode = Node.ProcessModeEnum.Disabled;
        overworldScene.Visible = false;
        tree.CurrentScene = battleInstance;
    }

    public static bool BattleEnd(Player player)
    {
       player.CanMove = true; 
       return true;
    }
}