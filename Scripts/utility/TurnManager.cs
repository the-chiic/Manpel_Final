using System.Collections.Generic;
using Godot;

public partial class TurnManager : Node
{
    public enum Phase { PlayerTurn, EnemyTurn }

    public static TurnManager Instance { get; private set; }

    public Phase CurrentPhase { get; private set; } = Phase.PlayerTurn;

    public readonly HashSet<Vector2I> ReservedCells = new();

    private int _pendingEnemies;

    [Signal] public delegate void EnemyTurnStartedEventHandler();

    public override void _EnterTree()
    {
        Instance = this;
    }

    public void StartEnemyTurn()
    {
        //NOTE(ullr): Crear un grupo en la GUI de godot y añadirlos al grupo enemies
        var enemies = GetTree().GetNodesInGroup("enemies");
        _pendingEnemies = enemies.Count;

        if (_pendingEnemies == 0)
        {
            // No hay enemigos: devolvemos el turno al jugador de inmediato.
            CurrentPhase = Phase.PlayerTurn;
            return;
        }

        CurrentPhase = Phase.EnemyTurn;
        ReservedCells.Clear();

        foreach (var n in enemies)
            ReservedCells.Add(GridMath.WorldToCell(((Node2D)n).GlobalPosition));
        EmitSignal(SignalName.EnemyTurnStarted);
    }

    public void EnemyFinishedMoving()
    {
        _pendingEnemies--;
        if (_pendingEnemies <= 0)
            CurrentPhase = Phase.PlayerTurn;
    }

    public void  ResetToPlayerTurn()
    {
        _pendingEnemies = 0;
        CurrentPhase = Phase.PlayerTurn;
    }
}