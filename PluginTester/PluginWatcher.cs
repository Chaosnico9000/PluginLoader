namespace PluginTester;

public class PluginWatcher(string pluginDir, Action onChanged)
{
    private readonly string _pluginDir = pluginDir;
    private readonly Action _onChanged = onChanged;

    public void Start()
    {
        FileSystemWatcher watcher = new(_pluginDir, "*.cs")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        watcher.Changed += (_, _) => _onChanged();
        watcher.Created += (_, _) => _onChanged();
        watcher.Renamed += (_, _) => _onChanged();
        watcher.Deleted += (_, _) => _onChanged();
    }
}
