using Godot;
using System.Collections.Generic;

public partial class Player : GridMover
{
    [Export] public PlayerDefinitionData Definition;
    [Export] public AnimatedSprite2D AnimSprite;

    public bool CanMove = true;
    public PlayerState State;
    private static readonly StringName[] InputActions =
        { "ui_left", "ui_right", "ui_up", "ui_down" };

    private readonly List<StringName> _heldOrder = new();

    public override void _Ready()
    {
        RuntimeDataSpace.RuntimeData.player = this;

        // Initialize state once (or load from save later)
        if (State == null)
            State = new PlayerState(Definition);

        MoveCompleted += OnMoveCompleted;
    }

    public override void _PhysicsProcess(double delta)
    {
        AnimSprite.Play("Idle");
        base._PhysicsProcess(delta);

        if (!CanMove) return;
        if (IsMoving) return;
        if (TurnManager.Instance.CurrentPhase != TurnManager.Phase.PlayerTurn) return;

        foreach (var a in InputActions)
        {
            if (Input.IsActionJustPressed(a))
            {
                _heldOrder.Remove(a);
                _heldOrder.Add(a);
            }
        }
        _heldOrder.RemoveAll(a => !Input.IsActionPressed(a));

        if (_heldOrder.Count == 0) return;

        Vector2 dir = ActionToDirection(_heldOrder[^1]);
        TryMove(dir);
    }

    private void OnMoveCompleted() => TurnManager.Instance.StartEnemyTurn();

   public override bool OnMoveIntercepted(Vector2 targetWorldPos)
    {
        Enemy1 enemy = FindEnemyAt(targetWorldPos);
        if (enemy != null)
        {
            CombatTransition.Instance.StartBattle(this, enemy);
            return true; 
        }
        return false;
    } 

    private Enemy1 FindEnemyAt(Vector2 worldPos)
    {
        foreach (var n in GetTree().GetNodesInGroup("enemies"))
            if (n is Enemy1 e && e.GlobalPosition.DistanceTo(worldPos) < TileSize * 0.5f)
                return e;
        return null;
    }

    private static Vector2 ActionToDirection(StringName action)
    {
        if (action == "ui_left")  return Vector2.Left;
        if (action == "ui_right") return Vector2.Right;
        if (action == "ui_up")    return Vector2.Up;
        if (action == "ui_down")  return Vector2.Down;
        return Vector2.Zero;
    }
}