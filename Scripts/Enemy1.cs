using Godot;

public partial class Enemy1 : GridMover
{
    [Export] public EnemyDefinitionData enemyData;
    public override void _Ready()
    {
        AddToGroup("enemies");
        MoveCompleted += OnMoveCompleted;
        TurnManager.Instance.EnemyTurnStarted += OnEnemyTurn;
    }

    public override bool OnMoveIntercepted(Vector2 targetWorldPos)
    {
        var player = RuntimeDataSpace.RuntimeData.player;
        if (player == null) return false;

        if (player.GlobalPosition.DistanceTo(targetWorldPos) < TileSize * 0.5f)
        {
            CombatTransition.Instance.StartBattle(player, this);
            return true;
        }
        return false;
    }

 private void OnEnemyTurn()
    {
        var player = RuntimeDataSpace.RuntimeData.player;
        if (player == null) { Finish(); return; }

        var myCell     = GridMath.WorldToCell(GlobalPosition);
        var playerCell = GridMath.WorldToCell(player.GlobalPosition);

        var step = Pathfinder.FindNextStep(myCell, playerCell, IsBlockedForPathing);
        if (step == Vector2I.Zero) { Finish(); return; }

        var dest = myCell + step;

        // Reserve (see previous answer) then try to move.
        if (!TurnManager.Instance.ReservedCells.Add(dest)) { Finish(); return; }

        // TryMove → OnMoveIntercepted fires → battle may start here.
        if (!TryMove(new Vector2(step.X, step.Y)))
            Finish(); 
    }

    private bool IsBlockedForPathing(Vector2I cell)
    {
        if (TurnManager.Instance.ReservedCells.Contains(cell))
            return true;

        return IsTileBlocked(GridMath.CellToWorld(cell));
    }

    private void Finish() => TurnManager.Instance.EnemyFinishedMoving();
    private void OnMoveCompleted() => Finish();

    public override void _ExitTree()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.EnemyTurnStarted -= OnEnemyTurn;
        }
    }
}