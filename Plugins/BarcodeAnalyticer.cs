using System.Text;
using System.Xml;
using PluginTester.Contracts;
using Spectre.Console;
using Rule = Spectre.Console.Rule;

namespace Plugins;
internal class BarcodeAnalyticer : ICodePlugin
{
    public string Name => "Barcode Analyticer";
    public string Category => "Tools";
    public bool IsEnabled => true;

    public string Run()
    {
        // Desktop-Pfade für die CSVs
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string zeroPath = Path.Combine(desktop, "Zero.csv");
        string singlePath = Path.Combine(desktop, "Single.csv");
        string doublePath = Path.Combine(desktop, "Double.csv");

        // Zähler
        int eanSingleZero = 0;
        int eanDoubleZero = 0;
        int itscopeSingleZero = 0;
        int itscopeDoubleZero = 0;

        // Collections für Datensätze
        List<DataSet> zeroZeroSets = new();
        List<DataSet> singleZeroSets = new();
        List<DataSet> doubleZeroSets = new();

        // 1) Pfad vom Benutzer erfragen und validieren
        string folder = AnsiConsole.Prompt(
            new TextPrompt<string>("Bitte gebe den Pfad zum Ordner an, der durchsucht werden soll:")
                .PromptStyle("green")
                .Validate(path =>
                    Directory.Exists(path.Trim('"'))
                        ? ValidationResult.Success()
                        : ValidationResult.Error("[red]Dieser Ordner existiert nicht![/]")
                )
        ).Trim('"');

        // 2) Alle Dateien einlesen und verarbeiten
        string[] files = Directory.GetFiles(folder, "*");
        XmlDocument xml = new();
        AnsiConsole.Status()
            .Start("Verarbeite XML-Dateien…", ctx =>
            {
                foreach (string file in files)
                {
                    ctx.Status($"Lade {Path.GetFileName(file)}");
                    xml.Load(file);

                    // Annahme: deine Struktur bleibt so, wie im Original
                    XmlNodeList nodes = xml
                        .LastChild
                        .ChildNodes.Item(1)
                        .FirstChild
                        .ChildNodes.Item(1)
                        .ChildNodes;

                    foreach (XmlNode node in nodes)
                    {
                        if (node.Name != "ns2:INTERNATIONAL_PID")
                            continue;

                        string typeAttr = node.Attributes[0].Value;    // "ean" oder "itscope"
                        string code = node.InnerText;
                        bool has1Zero = code.StartsWith("0");
                        bool has2Zero = code.StartsWith("00");

                        // Zähler
                        switch (typeAttr)
                        {
                            case "ean" when has2Zero: eanDoubleZero++; break;
                            case "ean" when has1Zero: eanSingleZero++; break;
                            case "itscope" when has2Zero: itscopeDoubleZero++; break;
                            case "itscope" when has1Zero: itscopeSingleZero++; break;
                        }

                        // Supplier-PID auslesen
                        XmlNode? supplierNode = node.ParentNode?.ChildNodes[0];
                        if (supplierNode?.Name != "ns2:SUPPLIER_PID")
                            throw new Exception("Unerwarteter XML-Knoten");

                        DataSet ds = new(
                            barCode: code,
                            type: typeAttr,
                            supplierPid: supplierNode.InnerText,
                            counter: code.Length
                        );

                        // In die passende Liste einsortieren
                        if (has2Zero)
                            doubleZeroSets.Add(ds);
                        else if (has1Zero)
                            singleZeroSets.Add(ds);
                        else
                            zeroZeroSets.Add(ds);
                    }
                }
            });

        // 3) CSVs schreiben
        WriteCsv(singlePath, singleZeroSets);
        WriteCsv(doublePath, doubleZeroSets);
        WriteCsv(zeroPath, zeroZeroSets);

        // 4) Zusammenfassung per Spectre.Console-Tabelle
        Table table = new Table()
            .AddColumn("Typ")
            .AddColumn("Einzelnull")
            .AddColumn("Doppelnullen")
            .AddRow("EAN", eanSingleZero.ToString(), eanDoubleZero.ToString())
            .AddRow("ITScope", itscopeSingleZero.ToString(), itscopeDoubleZero.ToString());

        AnsiConsole.Write(new Rule("[yellow]Zusammenfassung[/]").RuleStyle("grey"));
        AnsiConsole.Write(table);

        return "Verarbeitung abgeschlossen.";
    }
    private void WriteCsv(string path, List<DataSet> data)
    {
        try
        {
            using StreamWriter w = new(path, false, Encoding.UTF8);
            w.WriteLine("Artikelnummer;Barcode;Typ;Stellen");
            foreach (DataSet d in data)
                w.WriteLine($"{d.SUPPLIER_PID};{d.BarCode};{d.Type};{d.Counter}");

            AnsiConsole.MarkupLine($"[green]CSV '{path}' erfolgreich erstellt.[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Fehler beim Schreiben von '{path}': {ex.Message}[/]");
        }
    }
}

internal class DataSet
{
    public DataSet(string barCode, string type, string supplierPid, int counter)
    {
        BarCode = barCode;
        Type = type;
        SUPPLIER_PID = supplierPid;
        Counter = counter;
    }
    public string BarCode { get; }
    public string Type { get; }
    public string SUPPLIER_PID { get; }
    public int Counter { get; }
}


///ORIGINAL

//using System.Xml;

//public class Program
//{
//    private static readonly string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
//    static string root = @"";
//    static string Zero = Path.Combine(DesktopPath, "Zero.csv");
//    static string Single = Path.Combine(DesktopPath, "Single.csv");
//    static string Double = Path.Combine(DesktopPath, "Double.csv");
//    static int eanDoubleZeroCounter;
//    static int eanSingleZeroCounter;
//    static int itscopeDoubleZeroCounter;
//    static int itscopeSingleZeroCounter;
//    static List<DataSet> ZeroZeroDataSets = [];
//    static List<DataSet> SingleZeroDataSets = [];
//    static List<DataSet> DoubleZeroDataSets = [];
//    public static void Main(string[] args)
//    {
//        Console.WriteLine("Bitte gebe den Pfad zum Ordner an der Durchsucht werden soll");
//        root = Console.ReadLine()?.Trim();
//        root = root.Trim('"');
//        if (Directory.Exists(root))
//        {
//            Console.WriteLine($"Der eingegebene Pfad existiert: {root}");
//        }
//        else
//        {
//            Console.WriteLine("Fehler: Der eingegebene Pfad existiert nicht. Bitte erneut eingeben.");
//        }
//        var files = Directory.GetFiles(root, "*");
//        XmlDocument xmlDoc = new();
//        foreach (var file in files)
//        {
//            xmlDoc.Load(file);
//            XmlNodeList nodes = xmlDoc.LastChild.ChildNodes.Item(1).FirstChild.ChildNodes.Item(1).ChildNodes;
//            foreach (XmlNode node in nodes)
//            {
//                if (node.Name == "ns2:SUPPLIER_PID")
//                {
//                    //
//                }
//                if (node.Name == "ns2:SUPPLIER_IDREF")
//                {
//                    //
//                }
//                if (node.Name == "ns2:INTERNATIONAL_PID")
//                {
//                    switch (node.Attributes.Item(0).Value)
//                    {
//                        case "ean":
//                            bool singlecheck = false;
//                            bool doublecheck = false;
//                            if (node.InnerText[0].ToString() == "0")
//                            {
//                                eanSingleZeroCounter++;
//                                singlecheck = true;
//                            }
//                            if (node.InnerText[0].ToString() == "0" && node.InnerText[1].ToString() == "0")
//                            {
//                                eanDoubleZeroCounter++;
//                                eanSingleZeroCounter--;
//                                singlecheck = false;
//                                doublecheck = true;
//                            }
//                            if (singlecheck)
//                            {
//                                var temp = node.ParentNode.ChildNodes.Item(0);
//                                if (temp.Name != "ns2:SUPPLIER_PID")
//                                {
//                                    throw new Exception("Ein Fehler ist aufgetreten");
//                                }
//                                var tmp = new DataSet(node.InnerText, node.Attributes.Item(0).Value, temp.InnerText, node.InnerText.Length);
//                                SingleZeroDataSets.Add(tmp);
//                            }
//                            if (doublecheck)
//                            {
//                                var temp = node.ParentNode.ChildNodes.Item(0);
//                                if (temp.Name != "ns2:SUPPLIER_PID")
//                                {
//                                    throw new Exception("Ein Fehler ist aufgetreten");
//                                }
//                                var tmp = new DataSet(node.InnerText, node.Attributes.Item(0).Value, temp.InnerText, node.InnerText.Length);
//                                DoubleZeroDataSets.Add(tmp);
//                            }
//                            if (!singlecheck && !doublecheck)
//                            {
//                                var temp = node.ParentNode.ChildNodes.Item(0);
//                                if (temp.Name != "ns2:SUPPLIER_PID")
//                                {
//                                    throw new Exception("Ein Fehler ist aufgetreten");
//                                }
//                                var tmp = new DataSet(node.InnerText, node.Attributes.Item(0).Value, temp.InnerText, node.InnerText.Length);
//                                ZeroZeroDataSets.Add(tmp);
//                            }
//                            break;
//                        case "itscope":
//                            bool singlecheckSecond = false;
//                            bool doublecheckSecond = false;
//                            if (node.InnerText[0].ToString() == "0")
//                            {
//                                itscopeSingleZeroCounter++;
//                                singlecheckSecond = true;
//                            }
//                            if (node.InnerText[0].ToString() == "0" && node.InnerText[1].ToString() == "0")
//                            {
//                                itscopeDoubleZeroCounter++;
//                                itscopeSingleZeroCounter--;
//                                singlecheckSecond = false;
//                                doublecheckSecond = true;
//                            }
//                            if (singlecheckSecond)
//                            {
//                                var temp = node.ParentNode.ChildNodes.Item(0);
//                                if (temp.Name != "ns2:SUPPLIER_PID")
//                                {
//                                    throw new Exception("Ein Fehler ist aufgetreten");
//                                }
//                                var tmp = new DataSet(node.InnerText, node.Attributes.Item(0).Value, temp.InnerText, node.InnerText.Length);
//                                SingleZeroDataSets.Add(tmp);
//                            }
//                            if (doublecheckSecond)
//                            {
//                                var temp = node.ParentNode.ChildNodes.Item(0);
//                                if (temp.Name != "ns2:SUPPLIER_PID")
//                                {
//                                    throw new Exception("Ein Fehler ist aufgetreten");
//                                }
//                                var tmp = new DataSet(node.InnerText, node.Attributes.Item(0).Value, temp.InnerText, node.InnerText.Length);
//                                DoubleZeroDataSets.Add(tmp);
//                            }
//                            if (!singlecheckSecond && !doublecheckSecond)
//                            {
//                                var temp = node.ParentNode.ChildNodes.Item(0);
//                                if (temp.Name != "ns2:SUPPLIER_PID")
//                                {
//                                    throw new Exception("Ein Fehler ist aufgetreten");
//                                }
//                                var tmp = new DataSet(node.InnerText, node.Attributes.Item(0).Value, temp.InnerText, node.InnerText.Length);
//                                ZeroZeroDataSets.Add(tmp);
//                            }
//                            break;
//                        default:
//                            break;
//                    }
//                }
//            }
//        }

//        WriteCSV(Single, SingleZeroDataSets);
//        WriteCSV(Double, DoubleZeroDataSets);
//        WriteCSV(Zero, ZeroZeroDataSets);


//        Console.WriteLine("________________");

//        Console.WriteLine(eanSingleZeroCounter + ": Anzahl an einer null beim Allgemeinen Barcode");
//        Console.WriteLine(eanDoubleZeroCounter + ": Anzahl an Zwei nullen beim Allgemeinen Barcode");
//        Console.WriteLine(itscopeSingleZeroCounter + ": Anzahl an einer null beim ITScope Barcode");
//        Console.WriteLine(itscopeDoubleZeroCounter + ": Anzahl an Zwei nullen beim ITScope Barcode");
//        Console.WriteLine("________________");
//    }

//    public static void WriteCSV(string filePath, List<DataSet> DataSets)
//    {
//        try
//        {
//            using (StreamWriter writer = new(filePath))
//            {
//                writer.WriteLine("Artikelnummer;Barcode;Typ;Stellen");
//                foreach (var Dataset in DataSets)
//                {
//                    writer.WriteLine($"{Dataset.SUPPLIER_PID};{Dataset.BarCode};{Dataset.Type};{Dataset.Counter}");
//                }
//            }
//            Console.WriteLine("CSV-Datei wurde erfolgreich erstellt.");
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Fehler beim Schreiben der CSV-Datei: {ex.Message}");
//        }
//    }
//}

//public class DataSet(string barCode, string type, string sUPPLIER_PID, int counter)
//{
//    public string BarCode { get; set; } = barCode;
//    public string Type { get; set; } = type;
//    public string SUPPLIER_PID { get; set; } = sUPPLIER_PID;
//    public int Counter { get; set; } = counter;
//}
