namespace PluginTester;

public class PluginItem(ICodePlugin plugin, string? filePath)
{
    public ICodePlugin Plugin { get; } = plugin;
    public string? FilePath { get; } = filePath;

    public override string ToString() =>
        $"{Plugin.Category} → {Plugin.Name}{(Plugin.IsEnabled ? "" : " (deaktiviert)")}";
}
