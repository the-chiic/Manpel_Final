using Godot;
using System;
using System.Runtime;
public partial class GridMover : CharacterBody2D
{
    [Signal] public delegate void MoveCompletedEventHandler();

    public const float TileSize = 128f;        // tamaño del tile en píxeles
    public const float MoveDuration = 0.2f;   // segundos por casilla

    private bool _isMoving;
    private Vector2 _targetPosition;

    public bool IsMoving => _isMoving;

    public virtual bool OnMoveIntercepted(Vector2 targetWorldPos) => false;

    public bool TryMove(Vector2 direction)
    {
        if (_isMoving) return false;
        if (direction == Vector2.Zero) return false;

        _targetPosition = GlobalPosition + direction * TileSize;

        if (OnMoveIntercepted(_targetPosition)) return false;

        if (IsTileBlocked(_targetPosition)) return false;
        _isMoving = true;
        return true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_isMoving) return;

        float speed = TileSize / MoveDuration; // px/s
        GlobalPosition = GlobalPosition.MoveToward(_targetPosition, speed * (float)delta);

        if (GlobalPosition.DistanceTo(_targetPosition) < 0.1f)
        {
            GlobalPosition = _targetPosition;
            _isMoving = false;
            EmitSignal(SignalName.MoveCompleted);
        }
    }

    public bool IsTileBlocked(Vector2 worldPosition)
    {
        var space = GetWorld2D().DirectSpaceState;
        var query = new PhysicsPointQueryParameters2D
        {
            Position = worldPosition,
            CollisionMask = 1
        };
        var hits = space.IntersectPoint(query);

        foreach (var hit in hits)
        {
            var body = hit["collider"].As<GodotObject>();
            if (body != this && body != GetParent())
                return true;
        }
        return false;
    }
}