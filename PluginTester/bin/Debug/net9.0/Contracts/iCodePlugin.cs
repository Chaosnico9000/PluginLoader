namespace PluginTester.Contracts;

public interface ICodePlugin
{
    string Name { get; }
    string Category { get; }
    bool IsEnabled { get; }
    string Run();
}
