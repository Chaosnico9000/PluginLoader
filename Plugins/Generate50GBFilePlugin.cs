using System.Diagnostics;
using System.Text;
using PluginTester.Contracts;
using Spectre.Console;

namespace Plugins;
public class Generate50GBFilePlugin : ICodePlugin
{
    public string Name => "50GB-Testdatei generieren";

    public string Category => "Tools";

    public bool IsEnabled => true;

    public string Run()
    {
        AnsiConsole.MarkupLine("[green]Erstelle 50GB-Testdatei auf dem Desktop...[/]");

        string filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "50GBTest.txt");

        GenerateFile(filePath);

        return $"✅ Datei erstellt unter: {filePath}";
    }

    private void GenerateFile(string path)
    {
        const long MaxFileSizeBytes = 50L * 1024 * 1024 * 1024; // 50 GB
        long writtenBytes = 0;
        int number = 0;
        StringBuilder buffer = new(1048576); // 1 MB Buffer
        Stopwatch stopwatch = Stopwatch.StartNew();

        using FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.None, 1048576, FileOptions.WriteThrough);
        using StreamWriter writer = new(fs, Encoding.UTF8, 1048576);

        AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn(),
            })
            .Start(ctx =>
            {
                ProgressTask task = ctx.AddTask("[yellow]Schreibe Daten...[/]", new ProgressTaskSettings { MaxValue = MaxFileSizeBytes });

                while (writtenBytes < MaxFileSizeBytes)
                {
                    buffer.Append(++number).Append(' ');

                    if (buffer.Length >= 1048576)
                    {
                        string data = buffer.ToString();
                        writer.Write(data);
                        writer.Flush();

                        long bytesThisChunk = Encoding.UTF8.GetByteCount(data);
                        writtenBytes += bytesThisChunk;
                        task.Increment(bytesThisChunk);
                        buffer.Clear();

                        if (number % 1_000_000 == 0)
                        {
                            AnsiConsole.MarkupLine(
                                $"[grey](Zwischenstand)[/] [blue]{writtenBytes / (1024 * 1024):N0} MB[/] von [green]{MaxFileSizeBytes / (1024 * 1024):N0} MB[/] geschrieben...");
                        }
                    }
                }

                if (buffer.Length > 0)
                {
                    string data = buffer.ToString();
                    writer.Write(data);
                    writtenBytes += Encoding.UTF8.GetByteCount(data);
                    task.Value = writtenBytes;
                }
            });

        stopwatch.Stop();

        AnsiConsole.MarkupLine($"\n[green]✅ Datei erfolgreich erstellt![/]");
        AnsiConsole.MarkupLine($"Pfad: [blue]{path}[/]");
        AnsiConsole.MarkupLine($"Größe: [yellow]{writtenBytes / (1024 * 1024):N0} MB[/]");
        AnsiConsole.MarkupLine($"Dauer: [cyan]{stopwatch.Elapsed.TotalSeconds:F1} Sekunden[/]");
    }
}
