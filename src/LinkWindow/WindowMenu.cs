using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace LinkWindow;

internal sealed class WindowMenu : IClickableMenu
{
    private readonly ModEntry mod;
    private readonly WindowGroup group;
    private readonly List<Rectangle> buttons = new();
    private Rectangle preview;
    internal WindowGroup SelectedGroup => group;

    internal WindowMenu(ModEntry mod, WindowGroup group) : base(0, 0, 720, 656, true)
    {
        this.mod = mod;
        this.group = group;
        Reposition();
    }

    private void Reposition()
    {
        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;
        initializeUpperRightCloseButton();
        int x = xPositionOnScreen, y = yPositionOnScreen;
        buttons.Clear();
        buttons.Add(new Rectangle(x + 40, y + 32, 416, 64));
        // 左上の上・左、右上の上・右、右下の右・下、左下の左・下。
        buttons.Add(new Rectangle(x + 192, y + 112, 144, 64));
        buttons.Add(new Rectangle(x + 32, y + 192, 144, 64));
        buttons.Add(new Rectangle(x + 384, y + 112, 144, 64));
        buttons.Add(new Rectangle(x + 544, y + 192, 144, 64));
        buttons.Add(new Rectangle(x + 544, y + 304, 144, 64));
        buttons.Add(new Rectangle(x + 384, y + 384, 144, 64));
        buttons.Add(new Rectangle(x + 32, y + 304, 144, 64));
        buttons.Add(new Rectangle(x + 192, y + 384, 144, 64));
        buttons.Add(new Rectangle(x + 40, y + 464, 304, 64));
        buttons.Add(new Rectangle(x + 376, y + 464, 304, 64));
        buttons.Add(new Rectangle(x + 240, y + 544, 240, 64));
        buttons.Add(new Rectangle(x + 472, y + 32, 208, 64));
        preview = new Rectangle(x + 192, y + 192, 336, 176);
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds) => Reposition();

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        for (int i = 0; i < buttons.Count; i++)
        {
            if (!buttons[i].Contains(x, y)) continue;
            if (i == 0) group.Settings.Material = (group.Settings.Material + 1) % 3;
            else if (i <= 8) group.Settings.Corners[i - 1] = 1 - group.Settings.Corners[i - 1];
            else if (i is 9 or 10)
            {
                int delta = MinusButton(buttons[i]).Contains(x, y) ? -1 : PlusButton(buttons[i]).Contains(x, y) ? 1 : 0;
                if (delta == 0) return;
                group.Settings.AdjustCount(i == 9, delta);
            }
            else if (i == 12) group.Settings.Size = (group.Settings.Size + 1) % 2;
            else { exitThisMenu(); return; }
            mod.SaveSettings(group);
            group.ClearPixels();
            Game1.playSound("smallSelect");
            return;
        }
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.55f);
        drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);
        // パネル自体のタイトルと角ごとの見出しは付けず、配置で位置を示す。
        WindowRenderer.DrawPreview(b, group, preview);
        string[] labels = new string[13];
        labels[0] = mod.Helper.Translation.Get("menu.material") + ": " + mod.Helper.Translation.Get($"material.{group.Settings.Material}");
        for (int i = 0; i < 8; i++) labels[i + 1] = mod.Helper.Translation.Get($"shape.{group.Settings.Corners[i]}");
        labels[9] = mod.Helper.Translation.Get("menu.vertical", new { count = group.Settings.VerticalCount });
        labels[10] = mod.Helper.Translation.Get("menu.horizontal", new { count = group.Settings.HorizontalCount });
        labels[11] = mod.Helper.Translation.Get("menu.close");
        labels[12] = mod.Helper.Translation.Get("menu.size") + ": " + mod.Helper.Translation.Get($"size.{group.Settings.Size}");
        Point mouse = new(Game1.getMouseX(), Game1.getMouseY());
        for (int i = 0; i < buttons.Count; i++)
        {
            Rectangle rect = buttons[i];
            if (i is 9 or 10)
            {
                // 本数表示に親ボタンは描かず、独立した増減ボタンを並べる。
                DrawStep(b, MinusButton(rect), "-", mouse, (i == 9 ? group.Settings.VerticalCount : group.Settings.HorizontalCount) > 0);
                DrawStep(b, PlusButton(rect), "+", mouse, true);
                DrawCenteredText(b, labels[i], new Rectangle(rect.X + 64, rect.Y, rect.Width - 128, rect.Height));
            }
            else
            {
                drawTextureBox(b, rect.X, rect.Y, rect.Width, rect.Height, rect.Contains(mouse) ? Color.Wheat : Color.White);
                DrawCenteredText(b, labels[i], rect);
            }
        }
        b.DrawString(Game1.smallFont, mod.Helper.Translation.Get("menu.saved"),
            new Vector2(xPositionOnScreen + 40, yPositionOnScreen + height - 40), Game1.textColor);
        base.draw(b);
        drawMouse(b);
    }

    private static Rectangle MinusButton(Rectangle row) => new(row.X, row.Y, 48, row.Height);
    private static Rectangle PlusButton(Rectangle row) => new(row.Right - 48, row.Y, 48, row.Height);
    private static void DrawCenteredText(SpriteBatch b, string text, Rectangle rect)
    {
        Vector2 size = Game1.smallFont.MeasureString(text);
        float scale = Math.Min(1, Math.Min((rect.Width - 16) / Math.Max(1f, size.X), (rect.Height - 24) / Math.Max(1f, size.Y)));
        Vector2 origin = new(rect.Center.X - size.X * scale / 2, rect.Center.Y - size.Y * scale / 2);
        b.DrawString(Game1.smallFont, text, origin, Game1.textColor, 0, Vector2.Zero, scale, SpriteEffects.None, 1);
    }
    private static void DrawStep(SpriteBatch b, Rectangle rect, string text, Point mouse, bool enabled)
    {
        drawTextureBox(b, rect.X, rect.Y, rect.Width, rect.Height, enabled && rect.Contains(mouse) ? Color.Wheat : Color.White);
        // フォントにないマイナス記号が「*」になる問題を避け、記号を線で描く。
        Color color = enabled ? Game1.textColor : Color.Gray;
        b.Draw(Game1.staminaRect, new Rectangle(rect.Center.X - 8, rect.Center.Y - 2, 16, 4), color);
        if (text == "+")
            b.Draw(Game1.staminaRect, new Rectangle(rect.Center.X - 2, rect.Center.Y - 8, 4, 16), color);
    }
}


