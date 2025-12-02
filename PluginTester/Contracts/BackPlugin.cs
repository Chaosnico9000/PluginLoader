namespace PluginTester.Contracts;
public class BackPlugin : ICodePlugin
{
    public string Name => "⬅ Zurück";
    public string Category => string.Empty;
    public bool IsEnabled => false;
    public string Run() => string.Empty;
}
