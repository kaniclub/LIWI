using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Objects;

namespace LinkWindow;

internal static class WindowRenderer
{
    internal static readonly Color Shadow = RoleColor(0, 2);
    internal static readonly Color Wood = RoleColor(0, 3);
    internal static readonly Color Highlight = RoleColor(0, 4);
    private static readonly Color[][] Palettes = { BuildPalette(0), BuildPalette(1) };

    private static Color RoleColor(int material, byte role)
    {
        uint rgb = WindowStyle.Rgb(material, role);
        return new Color((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
    }
    private static Color[] BuildPalette(int material) => Enumerable.Range(0, 10)
        .Select(role => RoleColor(material, (byte)role)).ToArray();

    internal static Color[] GetIconPixels()
    {
        byte[] roles = new WindowGeometry(new[] { new Cell(0, 0) }, new()).Rasterize(new Cell(0, 0));
        return roles.Select(role => role == 0 ? Color.Transparent : Palettes[0][role]).ToArray();
    }

    internal static void DrawPreview(SpriteBatch batch, WindowGroup group, Rectangle area)
    {
        int width = (group.Right - group.Left) * 16;
        int height = (group.Bottom - group.Top) * 16;
        float scale = Math.Min(area.Width / (float)width, area.Height / (float)height);
        float originX = area.Center.X - width * scale / 2;
        float originY = area.Center.Y - height * scale / 2;
        Color[] colors = Palettes[group.Settings.Material == 0 ? 0 : 1];
        foreach (Cell cell in group.Cells)
        {
            byte[] pixels = group.GetPixels(cell);
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16;)
            {
                byte kind = pixels[y * 16 + x];
                int start = x++;
                while (x < 16 && pixels[y * 16 + x] == kind) x++;
                if (kind == 0) continue;
                int globalX = (cell.X - group.Left) * 16;
                int globalY = (cell.Y - group.Top) * 16 + y;
                int dx = (int)Math.Round(originX + (globalX + start) * scale);
                int dy = (int)Math.Round(originY + globalY * scale);
                int right = (int)Math.Round(originX + (globalX + x) * scale);
                int bottom = (int)Math.Round(originY + (globalY + 1) * scale);
                if (right > dx && bottom > dy)
                    batch.Draw(Game1.staminaRect, new Rectangle(dx, dy, right - dx, bottom - dy), colors[kind]);
            }
        }
    }

    internal static void Draw(SpriteBatch batch, Furniture furniture, Cell cell, WindowGroup group, Vector2 origin, float alpha)
    {
        PixelRegion[] regions = group.GetRegions(cell);
        Texture2D exterior = ItemRegistry.GetDataOrErrorItem(furniture.QualifiedItemId).GetTexture();
        Rectangle source = furniture.sourceRect.Value;
        bool exteriorReady = source.X >= 16 && source.Right <= exterior.Width && source.Bottom <= exterior.Height;
        float depth = (furniture.GetBoundingBox().Bottom - 48) / 10000f + 0.000001f;
        Color[] colors = Palettes[group.Settings.Material == 0 ? 0 : 1];
        foreach (PixelRegion region in regions)
        {
            Rectangle destination = new((int)origin.X + region.X * 4, (int)origin.Y + region.Y * 4, region.Width * 4, region.Height * 4);
            if (region.Role == 1 && exteriorReady)
                batch.Draw(exterior, destination, new Rectangle(source.X + region.X, source.Y + region.Y, region.Width, region.Height),
                    Color.White * alpha, 0, Vector2.Zero, SpriteEffects.None, depth);
            else
                batch.Draw(Game1.staminaRect, destination, null, colors[region.Role] * alpha,
                    0, Vector2.Zero, SpriteEffects.None, depth);
        }
    }
}
