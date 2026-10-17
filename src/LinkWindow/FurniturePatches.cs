using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;

namespace LinkWindow;

[HarmonyPatch(typeof(Furniture), nameof(Furniture.GetModifiedWallTilePosition))]
internal static class WallPositionPatch
{
    private static bool Prefix(Furniture __instance, int tile_y, ref int __result)
    {
        if (!ModEntry.IsWindow(__instance)) return true;
        __result = tile_y;
        return false;
    }
}

[HarmonyPatch(typeof(Furniture), nameof(Furniture.GetAdditionalFurniturePlacementStatus))]
internal static class WallPlacementPatch
{
    private static bool Prefix(Furniture __instance, GameLocation location, int x, int y, ref int __result)
    {
        if (!ModEntry.IsWindow(__instance)) return true;
        __result = !location.CanPlaceThisFurnitureHere(__instance) ? 4
            : location is not DecoratableLocation room || !room.isTileOnWall(x / 64, y / 64) ? 1
            : location.doesTileHaveProperty(x / 64, y / 64, "NoFurniture", "Back") is not null ? 2 : 0;
        return false;
    }
}

[HarmonyPatch(typeof(Furniture), nameof(Furniture.draw), new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) })]
internal static class WindowDrawPatch
{
    private static bool Prefix(Furniture __instance, SpriteBatch spriteBatch, int x, int y, float alpha)
    {
        if (!ModEntry.IsWindow(__instance)) return true;
        if (__instance.isTemporarilyInvisible) return false;
        if (Furniture.isDrawingLocationFurniture)
        {
            // 設置済み家具には x/y=-1 が渡される。実座標は家具から取得する。
            Cell cell = new((int)__instance.TileLocation.X, (int)__instance.TileLocation.Y);
            WindowGroup group = ModEntry.Instance.GetGroup(__instance) ?? new WindowGroup(new[] { cell }, new());
            Vector2 origin = Game1.GlobalToLocal(Game1.viewport, new Vector2(cell.X * 64, cell.Y * 64));
            WindowRenderer.Draw(spriteBatch, __instance, cell, group, origin, alpha);
            return false;
        }
        // 未設置のプレビューには予約色や他の家具用の外景スロットを表示しない。
        spriteBatch.Draw(ModEntry.Instance.GetIcon(), Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, y * 64)),
            null, Color.White * alpha, 0, Vector2.Zero, 4, SpriteEffects.None,
            (__instance.GetBoundingBox().Bottom - 48) / 10000f);
        return false;
    }

}

[HarmonyPatch(typeof(Furniture), nameof(Furniture.drawInMenu))]
internal static class WindowIconPatch
{
    private static bool Prefix(Furniture __instance, SpriteBatch spriteBatch, Vector2 location, float scaleSize,
        float transparency, float layerDepth, Color color)
    {
        if (!ModEntry.IsWindow(__instance)) return true;
        spriteBatch.Draw(ModEntry.Instance.GetIcon(), location + new Vector2(32), null, color * transparency,
            0, new Vector2(8), 4 * scaleSize, SpriteEffects.None, layerDepth);
        return false;
    }
}

[HarmonyPatch(typeof(Furniture), nameof(Furniture.drawWhenHeld))]
internal static class WindowHeldPatch
{
    private static bool Prefix(Furniture __instance, SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f)
    {
        if (!ModEntry.IsWindow(__instance)) return true;
        spriteBatch.Draw(ModEntry.Instance.GetIcon(), objectPosition, null, Color.White, 0, Vector2.Zero,
            4, SpriteEffects.None, Math.Max(0, (f.StandingPixel.Y + 1) / 10000f));
        return false;
    }
}
