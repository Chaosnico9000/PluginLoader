using System.Diagnostics;
using System.Text;
using PluginTester;
using Spectre.Console;

string pluginDir = @"C:\Users\NHinken\source\repos\PluginTester\PluginTester\Plugins\";
string classLibDir = @"C:\Users\NHinken\source\repos\PluginTester\Plugins\Plugins.csproj";
Console.OutputEncoding = Encoding.UTF8;

AnsiConsole.MarkupLine("[blue]🔨 Baue Projektbibliothek...[/]");
if (!await BuildPluginProjectAsync(classLibDir))
{
    AnsiConsole.MarkupLine("[red]❌ Build fehlgeschlagen. Beende...[/]");
    return;
}

List<PluginItem> plugins = PluginLoader.LoadAll(pluginDir);

void Reload()
{
    AnsiConsole.MarkupLine("[blue]🔄 Änderungen erkannt. Plugins werden neu geladen...[/]");
    plugins = PluginLoader.LoadAll(pluginDir);
}

new PluginWatcher(pluginDir, Reload).Start();

while (true)
{
    string modus = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("▶ [green]Plugin-Modus auswählen[/]")
            .AddChoices("Aktive Plugins nach Kategorie", "Deaktivierte Plugins anzeigen"));

    PluginItem? selected = null;

    if (modus == "Aktive Plugins nach Kategorie")
    {
        List<IGrouping<string, PluginItem>> categoryGroups = plugins.Where(p => p.Plugin.IsEnabled)
            .GroupBy(p => p.Plugin.Category)
            .OrderBy(g => g.Key)
            .ToList();

        if (categoryGroups.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]❌ Keine aktiven Plugins gefunden.[/]");
            continue;
        }

        string selectedCategory = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("▶ [yellow]Kategorie wählen:[/]")
                .AddChoices(categoryGroups.Select(g => $"📁 {g.Key}").Append("⬅ Zurück")))
            .Replace("📁 ", "");

        if (selectedCategory == "Zurück") continue;

        IGrouping<string, PluginItem>? group = categoryGroups.FirstOrDefault(g => g.Key == selectedCategory);
        if (selectedCategory == "⬅ Zurück")
        {
            continue;
        }
        if (group == null)
        {
            AnsiConsole.MarkupLine("[red]❌ Kategorie nicht gefunden.[/]");
            continue;
        }

        List<PluginItem> selectedPlugins = group.ToList();

        selected = AnsiConsole.Prompt(
            new SelectionPrompt<PluginItem>()
                .Title($"▶ Plugin aus Kategorie '{selectedCategory}' auswählen:")
                .UseConverter(p => p.Plugin.Name)
                .AddChoices(selectedPlugins.Append(new PluginItem(new BackPlugin(), "")))
                .HighlightStyle(Style.Plain));

        if (selected.Plugin.Name == "⬅ Zurück") continue;
    }
    else
    {
        List<PluginItem> inactivePlugins = plugins.Where(p => !p.Plugin.IsEnabled)
            .OrderBy(p => p.Plugin.Name)
            .ToList();

        if (inactivePlugins.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]✅ Keine deaktivierten Plugins.[/]");
            continue;
        }

        selected = AnsiConsole.Prompt(
            new SelectionPrompt<PluginItem>()
                .Title("▶ [grey]Deaktiviertes Plugin auswählen:[/]")
                .UseConverter(p => $"{p.Plugin.Name}")
                .AddChoices(inactivePlugins.Append(new PluginItem(new BackPlugin(), "")))
                .HighlightStyle(Style.Plain));

        if (selected.Plugin.Name == "⬅ Zurück") continue;
    }

    if (selected == null) continue;

    if (File.Exists(selected.FilePath) && selected.FilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
    {
        string code = File.ReadAllText(selected.FilePath);
        AnsiConsole.Write(new Panel(new Text(code, new Style(decoration: Decoration.None)))
            .Header($"📝 {Path.GetFileName(selected.FilePath)}").Expand());
    }
    else
    {
        AnsiConsole.MarkupLine($"[grey]📄 Keine Quellcode-Vorschau für {Path.GetFileName(selected.FilePath)}[/]");
    }

    try
    {
        string result = selected.Plugin.Run();
        AnsiConsole.Write(new Panel(Markup.Escape(result)).Header("📤 Plugin-Ausgabe").Expand());
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]❌ Fehler:[/] {Markup.Escape(ex.Message)}");
    }

    AnsiConsole.MarkupLine("[grey]Drücke eine Taste, um fortzufahren...[/]");
    Console.ReadKey(true);
}

static async Task<bool> BuildPluginProjectAsync(string solutionRoot, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(solutionRoot))
        throw new ArgumentException("Pfad darf nicht leer sein.", nameof(solutionRoot));

    string? projectDir = Path.GetDirectoryName(solutionRoot);
    if (string.IsNullOrWhiteSpace(projectDir) || !Directory.Exists(projectDir))
        throw new DirectoryNotFoundException($"Nicht gefunden: {projectDir}");

    TimeSpan maxDuration = timeout ?? TimeSpan.FromMinutes(5);

    ProcessStartInfo psi = new("dotnet", $"build \"{solutionRoot}\" /p:BuildProjectReferences=false")
    {
        WorkingDirectory = projectDir,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
        StandardOutputEncoding = Encoding.UTF8,
        StandardErrorEncoding = Encoding.UTF8
    };

    try
    {
        using Process process = new() { StartInfo = psi, EnableRaisingEvents = true };
        StringBuilder outBuilder = new();
        StringBuilder errBuilder = new();

        process.OutputDataReceived += (_, e) => { if (e.Data != null) outBuilder.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) errBuilder.AppendLine(e.Data); };

        if (!process.Start())
        {
            AnsiConsole.MarkupLine("[red]❌ Build-Prozess konnte nicht gestartet werden.[/]");
            return false;
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(maxDuration);

        try { await process.WaitForExitAsync(linkedCts.Token); }
        catch (OperationCanceledException)
        {
            try { process.Kill(true); } catch { }
            AnsiConsole.MarkupLine($"[red]❌ Build nach {maxDuration.TotalSeconds:F0}s abgebrochen.[/]");
            return false;
        }

        string output = outBuilder.ToString().Trim();
        string error = errBuilder.ToString().Trim();

        if (!string.IsNullOrEmpty(output))
            AnsiConsole.MarkupLine($"[grey]{Markup.Escape(output)}[/]");

        if (process.ExitCode != 0)
        {
            AnsiConsole.MarkupLine($"[red]❌ Build fehlgeschlagen (ExitCode={process.ExitCode}):[/]\n{Markup.Escape(error)}");
            return false;
        }

        return true;
    }
    catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
    {
        AnsiConsole.MarkupLine($"[red]❌ Build-Fehler: {Markup.Escape(ex.Message)}[/]");
        return false;
    }
}