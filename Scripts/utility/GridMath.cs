using Godot;

public static class GridMath
{
    public const float TileSize = 128f;
    public const float MoveDuration = 0.3f;

    public static Vector2I WorldToCell(Vector2 world) =>
        new Vector2I(Mathf.FloorToInt(world.X / TileSize),
                     Mathf.FloorToInt(world.Y / TileSize));

    public static Vector2 CellToWorld(Vector2I cell) =>
        new Vector2(cell.X * TileSize, cell.Y * TileSize);

    public static readonly Vector2I[] CardinalDirs =
    {
        new( 1, 0), new(-1, 0), new(0, 1), new(0,-1)
    };
}