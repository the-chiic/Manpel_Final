using System;
using System.Collections.Generic;
using Godot;

public static class Pathfinder
{
    /// Devuelve el primer paso desde el principio hacia el target
    /// O Vector2I.Zero si no existe el path o ya está ahí
    public static Vector2I FindNextStep(
        Vector2I start,
        Vector2I goal,
        Func<Vector2I, bool> isBlocked)
    {
        if (start == goal) return Vector2I.Zero;

        var cameFrom = new Dictionary<Vector2I, Vector2I> { [start] = start };
        var queue = new Queue<Vector2I>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();

            foreach (var d in GridMath.CardinalDirs)
            {
                var nxt = cur + d;
                if (cameFrom.ContainsKey(nxt)) continue;

                //Se choca con el jugador para entrar al combate
                if (nxt != goal && isBlocked(nxt)) continue;

                cameFrom[nxt] = cur;
                if (nxt == goal) goto found;
                queue.Enqueue(nxt);
            }
        }
        return Vector2I.Zero; // No se llega

    found:
        var step = goal;
        while (cameFrom[step] != start) step = cameFrom[step];
        return step - start;
    }
}