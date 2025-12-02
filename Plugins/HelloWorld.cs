using PluginTester.Contracts;
namespace Plugins;
public class HelloWorld : ICodePlugin
{
    public string Name => "Hallo Welt";
    public string Category => "Test";
    public bool IsEnabled => true;

    public string Run()
    {
        return "👋 Hallo aus HelloWorld.cs!";
    }
}
