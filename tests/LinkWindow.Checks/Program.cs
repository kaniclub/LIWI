using System.Text.Json;
using LinkWindow;

int checks = 0;
void Check(bool condition, string description)
{
    if (!condition) throw new Exception(description);
    checks++;
    Console.WriteLine($"PASS: {description}");
}
Check(WindowLayout.FindGroups(Array.Empty<Cell>()).Count == 0, "Empty room");
Check(WindowLayout.FindGroups(new[] { new Cell(0, 0), new Cell(1, 1) }).Count == 2, "Diagonal windows stay separate");
List<Cell> tiles = new() { new(0, 0), new(1, 0), new(2, 0), new(2, 1), new(2, 2) };
Check(WindowLayout.FindGroups(tiles).Single().Count == 5, "Horizontal and vertical L-shaped connection");
tiles.Remove(new Cell(2, 0));
Check(WindowLayout.FindGroups(tiles).Count == 2, "Removing a bridge splits a group");
tiles.Add(new Cell(2, 0));
Check(WindowLayout.FindGroups(tiles).Count == 1, "Restoring a bridge merges groups");
Check(WindowLayout.FindGroups(tiles.Concat(tiles)).Single().Count == 5, "Duplicate positions do not duplicate groups");
Check(WindowLayout.FindGroups(tiles.AsEnumerable().Reverse()).Single().SequenceEqual(WindowLayout.FindGroups(tiles).Single()), "Deterministic ordering");
WindowSettings settings = new() { Material = 1, VerticalCount = 3, Corners = new[] { 1, 0, 1, 0, 0, 1, 0, 1 } };
WindowSettings restored = JsonSerializer.Deserialize<WindowSettings>(JsonSerializer.Serialize(settings))!;
Check(restored.Material == 1 && restored.VerticalCount == 3 && restored.Corners.SequenceEqual(settings.Corners), "New settings serialize and restore");
settings = new() { VerticalCount = -5, HorizontalCount = 100, Corners = null! };
settings.Normalize();
Check(settings.VerticalCount == 0 && settings.HorizontalCount == 100 && settings.Corners.Length == 8, "Negative settings recover without arbitrary bar limit");
Cell[] square = { new(10, 20), new(11, 20), new(10, 21), new(11, 21) };
settings = new();
WindowGeometry geometry = new(square, settings);
Check(geometry.Contains(160, 320) && geometry.Contains(191, 351), "Square fills group at actual world position");
Check(!geometry.Contains(-16, -16), "Sentinel draw coordinates are outside actual group");
Check(geometry.Pixel(175, 330) == 1 && geometry.Pixel(176, 330) == 1, "No frame at internal tile seam");
settings.Corners = new[] { 1, 1, 1, 1, 1, 1, 1, 1 };
geometry = new(square, settings);
Check(!geometry.Contains(160, 320) && geometry.Contains(176, 320) && geometry.Contains(176, 336), "Round shape cuts corners and keeps center");
Check(geometry.Pixel(160, 320) == 0, "Outside round window stays transparent to wallpaper");
Check(geometry.Rasterize(square[0])[0] == 0, "Rasterizer preserves transparent corners");
settings = new() { Material = 2 };
geometry = new(square, settings);
Check(geometry.Pixel(160, 320) == 1, "No-frame mode has glass right up to outline");
settings = new() { Material = 2, VerticalCount = 3, HorizontalCount = 0 };
geometry = new(square, settings);
Check(Enumerable.Range(160, 32).Count(x => geometry.Pixel(x, 322) == 3) == 3, "Exactly three vertical bars across connected tiles");
Check(geometry.Pixel(162, 328) == 1, "Vertical mode omits horizontal bars");
settings = new() { VerticalCount = 12 };
geometry = new(new[] { new Cell(0, 0) }, settings);
Check(Enumerable.Range(2, 12).Count(x => geometry.Pixel(x, 8) == 3) == 12,
    "Twelve bars fit inside a one-tile frame without being covered by its border");
settings = new() { Material = 2, HorizontalCount = 2 };
geometry = new(square, settings);
Check(Enumerable.Range(320, 32).Count(y => geometry.Pixel(162, y) == 3) == 2, "Exactly two horizontal bars");
settings = new() { Corners = new[] { 1, 0, 1, 0, 0, 0, 0, 0 } };
geometry = new(square, settings);
Check(!geometry.Contains(160, 320) && geometry.Contains(160, 351), "Top can be rounded while bottom stays square");
geometry = new(new[] { new Cell(0, 0), new Cell(1, 0), new Cell(0, 1) }, new());
Check(!geometry.Contains(20, 20), "L-shaped group does not paint missing tile");
settings = new() { Corners = new[] { 1, 0, 1, 0, 0, 1, 0, 1 } };
geometry = new(square, settings);
Check(Enumerable.Range(328, 16).All(y => geometry.Contains(160, y) && geometry.Contains(191, y)),
    "Round top and bottom keep middle half of square left and right edges straight");
WindowGeometry ellipse = new(square, new() { Corners = new[] { 1, 1, 1, 1, 1, 1, 1, 1 } });
Check(geometry.Contains(160, 328) && !ellipse.Contains(160, 328),
    "Mixed round-square sides differ from the full ellipse");
settings = new() { Corners = new[] { 0, 1, 0, 1, 1, 0, 1, 0 } };
geometry = new(square, settings);
Check(Enumerable.Range(168, 16).All(x => geometry.Contains(x, 320) && geometry.Contains(x, 351)),
    "Round left and right keep middle half of square top and bottom edges straight");
Cell[] cornerPoints = { new(160, 320), new(191, 320), new(191, 351), new(160, 351) };
for (int part = 0; part < 8; part++)
{
    settings = new();
    settings.Corners[part] = 1;
    geometry = new(square, settings);
    Check(cornerPoints.Select((point, corner) => geometry.Contains(point.X, point.Y) == (corner != part / 2)).All(p => p),
        $"Corner control {part} changes only its own corner");
}
WindowGeometry topOnly = new(square, new() { Corners = new[] { 1, 0, 0, 0, 0, 0, 0, 0 } });
WindowGeometry leftOnly = new(square, new() { Corners = new[] { 0, 1, 0, 0, 0, 0, 0, 0 } });
Check(topOnly.Contains(160, 328) && !leftOnly.Contains(160, 328), "Top-left top and left controls have independent curvature");
settings = new() { Corners = new[] { 1, 0, 0 } };
settings.Normalize();
Check(settings.Corners.SequenceEqual(new int[8]), "Malformed corner array resets to eight square parts");
Check(!JsonSerializer.Serialize(settings).Contains("Sides") && !JsonSerializer.Serialize(settings).Contains("GridMode"),
    "New settings contain no legacy direction or side properties");
Console.WriteLine($"{checks} checks passed.");

foreach (int size in new[] { 0, 1 })
{
    settings = new() { Size = size, Material = 2 };
    geometry = new(square, settings);
    int inset = size * 4;
    int edge = 32 - inset * 2;
    Check(Enumerable.Range(160, 32).Sum(x => Enumerable.Range(320, 32).Count(y => geometry.Contains(x, y))) == edge * edge,
        $"Size {size} scales the whole group to expected area");
    Check(geometry.Contains(160 + inset, 320 + inset) && geometry.Contains(191 - inset, 351 - inset),
        $"Size {size} stays centered with equal margins");
    if (size > 0) Check(geometry.Pixel(160, 320) == 0, $"Size {size} exposes wallpaper in outer margin");
    Check(geometry.Pixel(175, 336) == 1 && geometry.Pixel(176, 336) == 1, $"Size {size} preserves connected seam");
}
settings = new() { Size = 1 };
restored = JsonSerializer.Deserialize<WindowSettings>(JsonSerializer.Serialize(settings))!;
Check(restored.Size == 1, "Size serializes and restores");
settings.Size = 999; settings.Normalize();
Check(settings.Size == 1, "Invalid size is constrained");
settings = new() { Size = 0 };
geometry = new(square, settings);
settings.Size = 1;
Check(!geometry.Contains(160, 320) && geometry.Contains(176, 336), "Size updates immediately in existing preview geometry");
Console.WriteLine($"{checks} total checks passed.");

settings = new() { VerticalCount = 12, HorizontalCount = 0 };
settings.AdjustCount(true, 1);
Check(settings.VerticalCount == 13, "Plus does not wrap at twelve bars");
settings.AdjustCount(false, -1);
Check(settings.HorizontalCount == 0, "Minus stays at zero");
settings.AdjustCount(true, -1);
Check(settings.VerticalCount == 12 && settings.HorizontalCount == 0, "Minus affects only selected direction");
settings.VerticalCount = int.MaxValue;
settings.HorizontalCount = int.MaxValue;
settings.AdjustCount(true, 1);
settings.Normalize();
Check(settings.VerticalCount == int.MaxValue, "Plus cannot overflow into negative counts");
restored = JsonSerializer.Deserialize<WindowSettings>(JsonSerializer.Serialize(settings))!;
Check(restored.VerticalCount == int.MaxValue, "High bar counts persist without truncation");
geometry = new(square, settings);
Check(geometry.Rasterize(square[0]).All(role => role < 10), "Dense bar settings rasterize without overflow");
Console.WriteLine($"{checks} final checks passed.");



// --preview <path>: 実際の形状・素材コードから確認用画像を出力する。
if (args.Length == 2 && args[0] == "--preview")
{
    const int imageWidth = 640, imageHeight = 510;
    byte[] rgb = new byte[imageWidth * imageHeight * 3];
    void Put(int x, int y, uint color)
    {
        int i = (y * imageWidth + x) * 3;
        rgb[i] = (byte)(color >> 16); rgb[i + 1] = (byte)(color >> 8); rgb[i + 2] = (byte)color;
    }
    for (int y = 0; y < imageHeight; y++)
    for (int x = 0; x < imageWidth; x++) Put(x, y, x % 32 < 2 ? 0xC4975Du : 0xE6C28Au);
    Cell[] sampleCells = Enumerable.Range(0, 4).SelectMany(x => Enumerable.Range(0, 2).Select(y => new Cell(x, y))).ToArray();
    for (int sample = 0; sample < 6; sample++)
    {
        WindowSettings style = new() { Material = sample % 2, VerticalCount = 3, HorizontalCount = 1 };
        if (sample / 2 == 1) style.Corners = Enumerable.Repeat(1, 8).ToArray();
        if (sample / 2 == 2) style.Corners = new[] { 1, 1, 1, 1, 0, 0, 0, 0 };
        WindowGeometry shape = new(sampleCells, style);
        for (int y = 0; y < 32; y++)
        for (int x = 0; x < 64; x++)
        {
            byte role = shape.Pixel(x, y);
            if (role == 0) continue;
            uint color = WindowStyle.Rgb(style.Material, role);
            for (int yy = 0; yy < 4; yy++)
            for (int xx = 0; xx < 4; xx++) Put(32 + sample % 2 * 320 + x * 4 + xx, 32 + sample / 2 * 170 + y * 4 + yy, color);
        }
    }
    using FileStream output = File.Create(args[1]);
    byte[] header = System.Text.Encoding.ASCII.GetBytes($"P6\n{imageWidth} {imageHeight}\n255\n");
    output.Write(header); output.Write(rgb);
}


// 新しい描画領域が元のピクセル形状を完全に再現することを検証する。
foreach (int material in new[] { 0, 1, 2 })
foreach (int size in new[] { 0, 1 })
foreach (bool round in new[] { false, true })
{
    WindowSettings sample = new() { Material = material, Size = size, VerticalCount = 3, HorizontalCount = 1,
        Corners = Enumerable.Repeat(round ? 1 : 0, 8).ToArray() };
    WindowGroup cachedGroup = new(square, sample);
    foreach (Cell cell in square)
    {
        byte[] original = cachedGroup.GetPixels(cell);
        byte[] recovered = new byte[256];
        bool overlaps = false;
        foreach (PixelRegion region in cachedGroup.GetRegions(cell))
        for (int yy = region.Y; yy < region.Y + region.Height; yy++)
        for (int xx = region.X; xx < region.X + region.Width; xx++)
        {
            if (recovered[yy * 16 + xx] != 0) overlaps = true;
            recovered[yy * 16 + xx] = region.Role;
        }
        if (overlaps || !original.SequenceEqual(recovered)) throw new Exception("Merged drawing regions changed appearance");
    }
}
Check(true, "Merged draw regions exactly reproduce materials, sizes and round/square masks without overlap");
WindowGroup retained = new(square, new());
PixelRegion[] firstRegions = retained.GetRegions(square[0]);
Check(ReferenceEquals(firstRegions, retained.GetRegions(square[0])), "Unchanged group reuses the same cached draw regions");
retained.Settings.Size = 1;
retained.ClearPixels();
Check(!ReferenceEquals(firstRegions, retained.GetRegions(square[0])), "Settings change invalidates cached draw regions");

foreach (var dimensions in new[] { (1280, 720, 1280, 720), (1280, 720, 640, 360), (640, 480, 1280, 960) })
foreach (var world in new[] { (128, 64, 640, 192), (-64, -64, 64, 64), (1200, 650, 1408, 832) })
{
    var region = TrueWindow.Services.ReadbackRegion.FromWorld(world.Item1, world.Item2, world.Item3, world.Item4,
        dimensions.Item1, dimensions.Item2, dimensions.Item3, dimensions.Item4);
    for (int wy = world.Item2 + 2; wy < world.Item4; wy += 4)
    for (int wx = world.Item1 + 2; wx < world.Item3; wx += 4)
    {
        int sx = Math.Clamp((int)MathF.Floor(wx / (float)dimensions.Item1 * dimensions.Item3), 0, dimensions.Item3 - 1);
        int sy = Math.Clamp((int)MathF.Floor(wy / (float)dimensions.Item2 * dimensions.Item4), 0, dimensions.Item4 - 1);
        if (sx < region.X || sy < region.Y || sx >= region.X + region.Width || sy >= region.Y + region.Height)
            throw new Exception("Readback crop misses sampled exterior pixel");
        int index = (sy - region.Y) * region.Width + sx - region.X;
        int restoredX = region.X + index % region.Width, restoredY = region.Y + index / region.Width;
        if (restoredX != sx || restoredY != sy) throw new Exception("Cropped sampling changed color coordinate");
    }
}
Check(true, "Cropped exterior readback preserves every sampled pixel across zoom and map edges");
string imageTestRoot = Path.Combine(Path.GetTempPath(), "LinkWindow-images-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(imageTestRoot);
try
{
    Check(!WindowImageFiles.Find(Path.Combine(imageTestRoot, "missing")).Any(), "Missing image folder needs no setup");
    foreach (string filename in new[] { "z.JPG", "A.png", "b.JPEG", "notes.txt", "not-an-image.png.txt" })
        File.WriteAllText(Path.Combine(imageTestRoot, filename), "test");
    Directory.CreateDirectory(Path.Combine(imageTestRoot, "nested"));
    File.WriteAllText(Path.Combine(imageTestRoot, "nested", "hidden.png"), "test");
    string[] foundImages = WindowImageFiles.Find(imageTestRoot).ToArray();
    Check(foundImages.Select(Path.GetFileName).SequenceEqual(new[] { "A.png", "b.JPEG", "z.JPG" }),
        "Image discovery accepts mixed-case supported extensions, ignores other files and subfolders, and sorts names");
    var catalog = new TrueWindow.Services.RegisteredImageCatalog();
    foreach (string path in foundImages)
        catalog.Register("kaniclub.LinkWindow", "images/" + Path.GetFileName(path), Path.GetFileName(path), path);
    Check(catalog.Options.Count == 3 && catalog.Options[0].DisplayName == "A.png", "Discovered images become named TrueWindow options without decoding files");
    string selectedId = catalog.Options[0].SourceId;
    var relocated = new TrueWindow.Services.RegisteredImageCatalog();
    relocated.Register("kaniclub.LinkWindow", "images/A.png", "A.png", Path.Combine(imageTestRoot, "new-install", "A.png"));
    Check(relocated.Options[0].SourceId == selectedId, "Saved image selection survives installation directory changes");
    catalog.Register("kaniclub.LinkWindow", "images\\A.png", "New name", Path.Combine(imageTestRoot, "replacement.png"));
    Check(catalog.Options.Count == 3 && catalog.Options[0].SourceId == selectedId && catalog.Options[0].DisplayName == "New name",
        "Re-registering an image replaces its option and preserves its saved ID");
    catalog.Register("Another.Mod", "images/A.png", "Other image", foundImages[0]);
    Check(catalog.Options.Count == 4 && catalog.Options[3].SourceId != selectedId, "Different mods can register identical filenames independently");
    bool rejectedRelativePath = false;
    try { catalog.Register("kaniclub.LinkWindow", "relative", "Relative", "relative.png"); }
    catch (ArgumentException) { rejectedRelativePath = true; }
    Check(rejectedRelativePath, "API rejects relative paths that would depend on the working directory");
    int invalidations = 0;
    var imageApi = new TrueWindow.Integrations.TrueWindowApi(catalog, () => invalidations++);
    imageApi.RegisterImageSource("kaniclub.LinkWindow", "images/A.png", "A.png", foundImages[0]);
    Check(invalidations == 1 && catalog.Options.Count == 4 && catalog.Options[0].FilePath == foundImages[0],
        "Public API updates the registered image and clears previously loaded or failed image caches");
}
finally
{
    Directory.Delete(imageTestRoot, recursive: true);
}

Console.WriteLine($"{checks} verified checks passed.");

if (args.Contains("--bench"))
{
    Cell[] cells = Enumerable.Range(0, 8).SelectMany(x => Enumerable.Range(0, 8).Select(y => new Cell(x, y))).ToArray();
    string json = JsonSerializer.Serialize(new WindowSettings { VerticalCount = 3, HorizontalCount = 3 });
    WindowGroup steady = new(cells, JsonSerializer.Deserialize<WindowSettings>(json)!);
    foreach (Cell cell in cells) steady.GetRegions(cell);
    void Measure(string label, Action action, int iterations)
    {
        for (int i = 0; i < 5; i++) action();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        long bytes = GC.GetAllocatedBytesForCurrentThread();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++) action();
        watch.Stop();
        Console.WriteLine($"BENCH {label}: {watch.Elapsed.TotalMilliseconds / iterations:F4} ms/op; {(GC.GetAllocatedBytesForCurrentThread() - bytes) / iterations} B/op");
    }
    Measure("Old periodic rebuild, 64 windows", () =>
    {
        foreach (List<Cell> connected in WindowLayout.FindGroups(cells))
        {
            WindowSettings parsed = JsonSerializer.Deserialize<WindowSettings>(json)!;
            _ = JsonSerializer.Serialize(parsed);
            WindowGroup rebuilt = new(connected, parsed);
            foreach (Cell cell in connected) _ = rebuilt.GetPixels(cell);
        }
    }, 100);
    Measure("New unchanged cached lookups, 64 windows", () =>
    {
        foreach (Cell cell in cells) _ = steady.GetRegions(cell);
    }, 10000);
    int rows = 0, merged = 0;
    foreach (Cell cell in cells)
    {
        byte[] pixel = steady.GetPixels(cell);
        for (int y = 0; y < 16; y++)
        for (int x = 0; x < 16;)
        {
            byte role = pixel[y * 16 + x]; x++;
            while (x < 16 && pixel[y * 16 + x] == role) x++;
            if (role != 0) rows++;
        }
        merged += steady.GetRegions(cell).Length;
    }
    Console.WriteLine($"BENCH draw submissions for 64 windows: {rows} row spans -> {merged} merged regions");
    var crop = TrueWindow.Services.ReadbackRegion.FromWorld(128, 64, 640, 192, 1280, 720, 1280, 720);
    Console.WriteLine($"BENCH illustrative readback: {1280L * 720 * 4} bytes -> {(long)crop.Width * crop.Height * 4} bytes; new buffers reused after warm-up");
}

