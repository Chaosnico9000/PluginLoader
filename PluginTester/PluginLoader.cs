using System.Reflection;

namespace PluginTester;

public static class PluginLoader
{

    public static List<PluginItem> LoadAll(string pluginDir)
    {
        List<PluginItem> all = [];

        foreach (string file in Directory.GetFiles(pluginDir, "*.cs"))
        {
            ICodePlugin? p = RoslynCompiler.Compile(file);
            if (p != null)
                all.Add(new PluginItem(p, file));
        }

        foreach (string file in Directory.GetFiles(pluginDir, "*.dll"))
        {
            Assembly asm = Assembly.LoadFrom(file);
            foreach (Type? type in asm.GetTypes().Where(t => typeof(ICodePlugin).IsAssignableFrom(t)))
            {
                if (Activator.CreateInstance(type) is ICodePlugin plugin)
                    all.Add(new PluginItem(plugin, file));
            }
        }

        return all;
    }
}

