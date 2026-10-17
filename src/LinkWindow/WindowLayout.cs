namespace LinkWindow;

internal readonly record struct Cell(int X, int Y);

internal static class WindowLayout
{
    public static readonly Cell[] Directions = { new(0, -1), new(1, 0), new(0, 1), new(-1, 0) };

    public static List<List<Cell>> FindGroups(IEnumerable<Cell> cells)
    {
        HashSet<Cell> remaining = new(cells);
        List<List<Cell>> result = new();
        foreach (Cell seed in remaining.OrderBy(p => p.Y).ThenBy(p => p.X).ToArray())
        {
            if (!remaining.Remove(seed)) continue;
            List<Cell> group = new();
            Queue<Cell> queue = new();
            queue.Enqueue(seed);
            while (queue.TryDequeue(out Cell cell))
            {
                group.Add(cell);
                foreach (Cell direction in Directions)
                {
                    Cell next = new(cell.X + direction.X, cell.Y + direction.Y);
                    if (remaining.Remove(next)) queue.Enqueue(next);
                }
            }
            result.Add(group.OrderBy(p => p.Y).ThenBy(p => p.X).ToList());
        }
        return result;
    }
}
