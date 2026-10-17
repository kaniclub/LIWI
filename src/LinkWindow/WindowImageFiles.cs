namespace LinkWindow;

internal static class WindowImageFiles
{
    // 起動時にだけ呼ぶ。画像そのものは選択されるまで読み込まない。
    public static IEnumerable<string> Find(string directory)
    {
        if (!Directory.Exists(directory)) return Array.Empty<string>();
        return Directory.EnumerateFiles(directory)
            .Where(path => Path.GetExtension(path).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg")
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
