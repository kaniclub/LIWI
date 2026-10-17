namespace LinkWindow;

/// <summary>ゲームに依存しない、接続した窓全体のピクセル形状。</summary>
internal sealed class WindowGeometry
{
    private readonly HashSet<Cell> cells;
    private readonly WindowSettings settings;
    private readonly int baseLeft, baseTop, baseWidth, baseHeight;
    // 接続グループ全体の外接矩形を中央へ縮める。タイルごとは縮めない。
    private int Inset(int length) => settings.Size == 0 ? 0 : length * settings.Size / 8;
    private int left => baseLeft + Inset(baseWidth);
    private int top => baseTop + Inset(baseHeight);
    private int width => baseWidth - Inset(baseWidth) * 2;
    private int height => baseHeight - Inset(baseHeight) * 2;

    public WindowGeometry(IEnumerable<Cell> cells, WindowSettings settings)
    {
        this.cells = new(cells);
        this.settings = settings;
        this.settings.Normalize();
        baseLeft = this.cells.Min(p => p.X) * 16;
        baseTop = this.cells.Min(p => p.Y) * 16;
        baseWidth = (this.cells.Max(p => p.X) + 1) * 16 - baseLeft;
        baseHeight = (this.cells.Max(p => p.Y) + 1) * 16 - baseTop;
    }

    public bool Contains(int x, int y)
    {
        if (x < left || y < top || x >= left + width || y >= top + height) return false;
        if (!cells.Contains(new Cell((int)Math.Floor(x / 16d), (int)Math.Floor(y / 16d)))) return false;
        double px = x - left + 0.5;
        double py = y - top + 0.5;
        bool right = px >= width / 2d;
        bool bottom = py >= height / 2d;
        int horizontalIndex = bottom ? (right ? 5 : 7) : (right ? 2 : 0);
        int verticalIndex = bottom ? (right ? 4 : 6) : (right ? 3 : 1);
        bool horizontalRound = settings.Corners[horizontalIndex] == 1;
        bool verticalRound = settings.Corners[verticalIndex] == 1;
        if (!horizontalRound && !verticalRound) return true;
        // 接する片辺だけ丸形なら、角形側へ食い込む量を半分にする。
        // 両辺が丸形なら半幅・半高の楕円弧となり、全辺丸形で楕円になる。
        double radiusX = width * (horizontalRound ? 0.5 : 0.25);
        double radiusY = height * (verticalRound ? 0.5 : 0.25);
        double centerX = right ? width - radiusX : radiusX;
        double centerY = bottom ? height - radiusY : radiusY;
        double dx = right ? px - centerX : centerX - px;
        double dy = bottom ? py - centerY : centerY - py;
        if (dx <= 0 || dy <= 0) return true;
        return dx * dx / (radiusX * radiusX) + dy * dy / (radiusY * radiusY) <= 1;
    }

    // 0: 透過、1: 外景、2以降: WindowStyleの素材・陰影パレット。
    public byte Pixel(int x, int y)
    {
        if (!Contains(x, y)) return 0;
        if (settings.Material != 2)
        {
            if (!Contains(x - 1, y) || !Contains(x + 1, y) || !Contains(x, y - 1) || !Contains(x, y + 1)) return 2;
            bool bright = !Contains(x - 2, y) || !Contains(x, y - 2);
            bool shaded = !Contains(x + 2, y) || !Contains(x, y + 2);
            if (bright || shaded)
            {
                if (settings.Material == 0)
                {
                    // 木目は接続タイルではなく窓全体を基準にする。
                    int along = !Contains(x, y - 2) || !Contains(x, y + 2) ? x - left : y - top;
                    int phase = along % 19;
                    if (along > 3 && phase is 11 or 12) return 7;
                    if (phase == 13) return 6;
                }
                return bright ? (byte)4 : (byte)3;
            }
            // 大きい窓は三段のモールディング、小さい窓は二段に抑える。
            if (Math.Min(width, height) >= 32)
            {
                if (!Contains(x - 3, y) || !Contains(x, y - 3)) return 8;
                if (!Contains(x + 3, y) || !Contains(x, y + 3)) return 6;
            }
        }
        bool vertical = OnBar(x - left, width, settings.VerticalCount);
        bool horizontal = OnBar(y - top, height, settings.HorizontalCount);
        if (vertical && horizontal) return 9;
        if (vertical || horizontal) return 3;
        // 間隔に余裕がある格子だけ、隣に1pxの明面を付ける。
        // 密な格子では1px幅を保ってガラスを塞がない。
        bool verticalBevel = settings.VerticalCount > 0 && width / (settings.VerticalCount + 1L) >= 4
            && OnBar(x - left - 1, width, settings.VerticalCount);
        bool horizontalBevel = settings.HorizontalCount > 0 && height / (settings.HorizontalCount + 1L) >= 4
            && OnBar(y - top - 1, height, settings.HorizontalCount);
        return verticalBevel || horizontalBevel ? (settings.Material == 0 ? (byte)6 : (byte)4) : (byte)1;
    }

    private bool OnBar(int position, int length, int count)
    {
        int margin = settings.Material == 2 ? 0 : Math.Min(width, height) >= 32 ? 3 : 2;
        int available = length - margin * 2;
        count = Math.Min(count, available);
        for (int i = 1; i <= count; i++)
            if (position == margin + (int)Math.Floor(available * i / (double)(count + 1))) return true;
        return false;
    }

    public byte[] Rasterize(Cell cell)
    {
        byte[] pixels = new byte[256];
        for (int y = 0; y < 16; y++)
        for (int x = 0; x < 16; x++) pixels[y * 16 + x] = Pixel(cell.X * 16 + x, cell.Y * 16 + y);
        return pixels;
    }
}
