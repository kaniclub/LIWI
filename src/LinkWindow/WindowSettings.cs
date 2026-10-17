namespace LinkWindow;

internal sealed class WindowSettings
{
    // 0: 木、1: 鉄、2: 外枠なし。
    public int Material { get; set; }
    // 0: 大（100%）、1: 中（75%）。
    public int Size { get; set; }
    // 左上の上・左、右上の上・右、右下の右・下、左下の左・下。
    // 0: 角形、1: 丸形。
    public int[] Corners { get; set; } = new int[8];
    public int VerticalCount { get; set; }
    public int HorizontalCount { get; set; }

    public void Normalize()
    {
        Material = Math.Clamp(Material, 0, 2);
        Size = Math.Clamp(Size, 0, 1);
        VerticalCount = Math.Max(VerticalCount, 0);
        HorizontalCount = Math.Max(HorizontalCount, 0);
        if (Corners is null || Corners.Length != 8) Corners = new int[8];
        for (int i = 0; i < 8; i++) Corners[i] = Math.Clamp(Corners[i], 0, 1);
    }

    public void AdjustCount(bool vertical, int delta)
    {
        int current = vertical ? VerticalCount : HorizontalCount;
        int value = (int)Math.Clamp((long)current + delta, 0, int.MaxValue);
        if (vertical) VerticalCount = value;
        else HorizontalCount = value;
    }
}

