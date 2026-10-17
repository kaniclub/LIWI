using System.Text.Json;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using StardewValley.GameData.Shops;
using StardewModdingAPI.Utilities;

namespace LinkWindow;

public sealed class ModEntry : Mod
{
    internal const string ItemId = "kaniclub.LinkWindow_Window";
    internal const string SettingsKey = "kaniclub.LinkWindow/SettingsV4";
    internal const string TextureName = "kaniclub.LinkWindow.Texture";
    internal static ModEntry Instance = null!;
    internal readonly Dictionary<Cell, WindowGroup> Groups = new();
    private Texture2D? icon;
    private GameLocation? cachedLocation;
    private ModConfig config = new();
    private bool groupsDirty = true;
    private readonly Dictionary<Furniture, (Cell Position, string? Settings)> windowState = new();

    public override void Entry(IModHelper helper)
    {
        Instance = this;
        config = helper.ReadConfig<ModConfig>();
        config.OpenSettingsKey ??= KeybindList.Parse("F6");
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.Display.RenderedWorld += OnRenderedWorld;
        helper.Events.Content.AssetRequested += OnAssetRequested;
        helper.Events.GameLoop.UpdateTicked += (_, e) =>
        {
            if (Context.IsWorldReady && (groupsDirty || e.IsMultipleOf(30) && HaveWindowsChanged())) RefreshGroups();
        };
        helper.Events.GameLoop.SaveLoaded += (_, _) => RefreshGroups();
        helper.Events.Player.Warped += (_, e) => { if (e.IsLocalPlayer) RefreshGroups(); };
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => { Groups.Clear(); windowState.Clear(); cachedLocation = null; groupsDirty = true; };
        helper.Events.World.FurnitureListChanged += (_, e) =>
        {
            if (e.IsCurrentLocation && (e.Added.Any(IsWindow) || e.Removed.Any(IsWindow))) groupsDirty = true;
        };
        helper.Events.Input.ButtonPressed += OnButtonPressed;
        new Harmony(ModManifest.UniqueID).PatchAll(typeof(ModEntry).Assembly);
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/Furniture"))
            e.Edit(asset => asset.AsDictionary<string, string>().Data[ItemId] =
                $"LinkWindow/window/1 1/1 1/1/100/0/{Helper.Translation.Get("item.name")}/0/{TextureName}/false/truewindow");
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/Shops"))
            e.Edit(asset =>
            {
                if (!asset.AsDictionary<string, ShopData>().Data.TryGetValue(Game1.shop_furnitureCatalogue, out ShopData? shop)) return;
                shop.Items ??= new();
                if (shop.Items.Any(item => item.Id == ItemId)) return;
                shop.Items.Add(new ShopItemData
                {
                    Id = ItemId, ItemId = $"(F){ItemId}", Price = 0,
                    AvailableStock = int.MaxValue, AvoidRepeat = true, IgnoreShopPriceModifiers = true
                });
            });
        else if (e.NameWithoutLocale.IsEquivalentTo(TextureName))
            e.LoadFrom(() =>
            {
                Texture2D texture = new(Game1.graphics.GraphicsDevice, 16, 16);
                texture.SetData(Enumerable.Repeat(Color.Magenta, 256).ToArray());
                return texture;
            }, AssetLoadPriority.Exclusive);
    }

    internal Texture2D GetIcon()
    {
        if (icon is not null) return icon;
        icon = new Texture2D(Game1.graphics.GraphicsDevice, 16, 16);
        icon.SetData(WindowRenderer.GetIconPixels());
        return icon;
    }

    internal static bool IsWindow(Furniture furniture) => furniture.ItemId == ItemId;

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        ITrueWindowApi? trueWindow = Helper.ModRegistry.GetApi<ITrueWindowApi>("kaniclub.TrueWindow");
        if (trueWindow is not null)
        {
            try
            {
                foreach (string path in WindowImageFiles.Find(Path.Combine(Helper.DirectoryPath, "images")))
                    trueWindow.RegisterImageSource(ModManifest.UniqueID, "images/" + Path.GetFileName(path), Path.GetFileName(path), path);
            }
            catch (IOException ex) { Monitor.Log($"Could not read the images folder: {ex.Message}", LogLevel.Warn); }
            catch (UnauthorizedAccessException ex) { Monitor.Log($"Could not read the images folder: {ex.Message}", LogLevel.Warn); }
        }
        IGenericModConfigMenuApi? api = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (api is null) return;
        api.Register(ModManifest, () => config = new(), () => Helper.WriteConfig(config));
        api.AddKeybindList(ModManifest, () => config.OpenSettingsKey, value => config.OpenSettingsKey = value,
            () => Helper.Translation.Get("config.key.name"), () => Helper.Translation.Get("config.key.description"));
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!Context.IsWorldReady) return;
        WindowGroup? group = (Game1.activeClickableMenu as WindowMenu)?.SelectedGroup;
        if (group is null) return;
        foreach (Cell cell in group.Cells)
        {
            Vector2 origin = Game1.GlobalToLocal(Game1.viewport, new Vector2(cell.X * 64, cell.Y * 64));
            Rectangle rect = new((int)origin.X, (int)origin.Y, 64, 64);
            e.SpriteBatch.Draw(Game1.staminaRect, rect, Color.Cyan * 0.08f);
            for (int side = 0; side < 4; side++)
            {
                Cell offset = WindowLayout.Directions[side];
                if (group.Cells.Contains(new Cell(cell.X + offset.X, cell.Y + offset.Y))) continue;
                Rectangle edge = side switch
                {
                    0 => new(rect.X, rect.Y, 64, 2), 1 => new(rect.Right - 2, rect.Y, 2, 64),
                    2 => new(rect.X, rect.Bottom - 2, 64, 2), _ => new(rect.X, rect.Y, 2, 64)
                };
                e.SpriteBatch.Draw(Game1.staminaRect, edge, Color.Cyan * 0.85f);
            }
        }
    }

    internal void RefreshGroups()
    {
        Groups.Clear();
        windowState.Clear();
        groupsDirty = false;
        cachedLocation = Game1.currentLocation;
        if (!Context.IsWorldReady || cachedLocation is null) return;
        Dictionary<Cell, Furniture> windows = cachedLocation.furniture.Where(IsWindow)
            .GroupBy(f => new Cell((int)f.TileLocation.X, (int)f.TileLocation.Y))
            .ToDictionary(g => g.Key, g => g.First());
        foreach (List<Cell> cells in WindowLayout.FindGroups(windows.Keys))
        {
            WindowSettings settings = new();
            foreach (Cell cell in cells)
            {
                if (!windows[cell].modData.TryGetValue(SettingsKey, out string raw)) continue;
                try
                {
                    settings = JsonSerializer.Deserialize<WindowSettings>(raw) ?? new();
                    settings.Normalize();
                    break;
                }
                catch (JsonException) { /* 壊れた設定は次の家具、または既定値へ戻す。 */ }
            }
            WindowGroup group = new(cells, settings);
            string serialized = JsonSerializer.Serialize(settings);
            foreach (Cell cell in cells)
            {
                Groups[cell] = group;
                if (Context.IsMainPlayer && windows[cell].modData.GetValueOrDefault(SettingsKey) != serialized)
                    windows[cell].modData[SettingsKey] = serialized;
                windowState[windows[cell]] = (cell, windows[cell].modData.GetValueOrDefault(SettingsKey));
            }
        }
    }

    private bool HaveWindowsChanged()
    {
        if (!ReferenceEquals(cachedLocation, Game1.currentLocation)) return true;
        int count = 0;
        foreach (Furniture furniture in Game1.currentLocation.furniture)
        {
            if (!IsWindow(furniture)) continue;
            count++;
            if (!windowState.TryGetValue(furniture, out var state)
                || state.Position != new Cell((int)furniture.TileLocation.X, (int)furniture.TileLocation.Y)
                || state.Settings != furniture.modData.GetValueOrDefault(SettingsKey)) return true;
        }
        return count != windowState.Count;
    }

    internal WindowGroup? GetGroup(Furniture furniture)
    {
        if (groupsDirty || !ReferenceEquals(cachedLocation, Game1.currentLocation)) RefreshGroups();
        Groups.TryGetValue(new Cell((int)furniture.TileLocation.X, (int)furniture.TileLocation.Y), out WindowGroup? group);
        return group;
    }

    internal void SaveSettings(WindowGroup group)
    {
        if (!Context.IsMainPlayer) return;
        group.Settings.Normalize();
        string raw = JsonSerializer.Serialize(group.Settings);
        foreach (Furniture furniture in Game1.currentLocation.furniture)
            if (IsWindow(furniture) && group.Cells.Contains(new Cell((int)furniture.TileLocation.X, (int)furniture.TileLocation.Y)))
            {
                furniture.modData[SettingsKey] = raw;
                windowState[furniture] = (new Cell((int)furniture.TileLocation.X, (int)furniture.TileLocation.Y), raw);
            }
        group.ClearPixels();
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsPlayerFree) return;
        if (config.OpenSettingsKey.JustPressed())
        {
            if (!Context.IsMainPlayer)
            {
                Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("host-only")));
                return;
            }
            Furniture? target = Game1.currentLocation.furniture.FirstOrDefault(f => IsWindow(f)
                && f.GetBoundingBox().Contains(e.Cursor.AbsolutePixels.ToPoint()));
            if (target is null) return;
            if (groupsDirty || HaveWindowsChanged()) RefreshGroups();
            WindowGroup? group = GetGroup(target);
            if (group is not null) Game1.activeClickableMenu = new WindowMenu(this, group);
            Helper.Input.SuppressActiveKeybinds(config.OpenSettingsKey);
        }
    }


}


