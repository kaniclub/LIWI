namespace LinkWindow;

internal readonly record struct PixelRegion(int X, int Y, int Width, int Height, byte Role);

internal static class WindowRaster
{
    // 同じ色の横並びと、幅が一致する上下の行を合体。描画時の探索・確保をなくす。
    public static PixelRegion[] BuildRegions(byte[] pixels)
    {
        List<PixelRegion> regions = new();
        Dictionary<(int X, int Width, byte Role), int> last = new();
        for (int y = 0; y < 16; y++)
        for (int x = 0; x < 16;)
        {
            byte role = pixels[y * 16 + x];
            int start = x++;
            while (x < 16 && pixels[y * 16 + x] == role) x++;
            if (role == 0) continue;
            var key = (start, x - start, role);
            if (last.TryGetValue(key, out int index) && regions[index].Y + regions[index].Height == y)
                regions[index] = regions[index] with { Height = regions[index].Height + 1 };
            else
            {
                last[key] = regions.Count;
                regions.Add(new(start, y, x - start, 1, role));
            }
        }
        return regions.ToArray();
    }
}
