namespace LinkWindow;

internal sealed class WindowGroup
{
    private readonly WindowGeometry geometry;
    private readonly Dictionary<Cell, byte[]> pixels = new();
    private readonly Dictionary<Cell, PixelRegion[]> regions = new();
    public void ClearPixels() { pixels.Clear(); regions.Clear(); }
    public PixelRegion[] GetRegions(Cell cell)
    {
        if (!regions.TryGetValue(cell, out PixelRegion[]? value)) regions[cell] = value = WindowRaster.BuildRegions(GetPixels(cell));
        return value;
    }
    public byte[] GetPixels(Cell cell)
    {
        if (!pixels.TryGetValue(cell, out byte[]? value)) pixels[cell] = value = geometry.Rasterize(cell);
        return value;
    }
    public HashSet<Cell> Cells { get; }
    public WindowSettings Settings { get; }
    public int Left { get; }
    public int Top { get; }
    public int Right { get; }
    public int Bottom { get; }

    public WindowGroup(IEnumerable<Cell> cells, WindowSettings settings)
    {
        Cells = new(cells);
        Settings = settings;
        geometry = new WindowGeometry(Cells, settings);
        Left = Cells.Min(p => p.X);
        Top = Cells.Min(p => p.Y);
        Right = Cells.Max(p => p.X) + 1;
        Bottom = Cells.Max(p => p.Y) + 1;
    }
}


