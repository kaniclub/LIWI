using StardewModdingAPI.Utilities;

namespace LinkWindow;

internal sealed class ModConfig
{
    public KeybindList OpenSettingsKey { get; set; } = KeybindList.Parse("F6");
}

public interface IGenericModConfigMenuApi
{
    void Register(StardewModdingAPI.IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
    void AddKeybindList(StardewModdingAPI.IManifest mod, Func<KeybindList> getValue, Action<KeybindList> setValue,
        Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
}
