namespace LinkWindow;

/// <summary>Basic Window / Metal Windowの配色を基準とする、共通の描画パレット。</summary>
internal static class WindowStyle
{
    // 透過・仮ガラス・輪郭・主面・明面・陰面・縁の反射・木目・内側溝・交差部。
    private static readonly uint[] Wood =
    {
        0x000000, 0x8CC3CF, 0x744030, 0x905938, 0xBA8849,
        0x6B3C2F, 0xAD6F4B, 0x854B30, 0x744030, 0xBA8849
    };
    private static readonly uint[] Iron =
    {
        0x000000, 0x8CC3CF, 0x443345, 0x8A848A, 0x9A9D9A,
        0x625463, 0x736974, 0x625463, 0x625463, 0x9A9D9A
    };

    internal static uint Rgb(int material, byte role) => (material == 0 ? Wood : Iron)[role];
}
