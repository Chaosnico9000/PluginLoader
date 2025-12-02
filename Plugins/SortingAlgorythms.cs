using PluginTester.Contracts;
using Spectre.Console;

namespace Plugins
{
    public class SortingAlgorythms : ICodePlugin
    {
        public string Name => "SortingAlgorythm";
        public string Category => "Lernen";
        public bool IsEnabled => true;

        private Raum[] raeume;
        private readonly int anzraum = 20;
        private int delay;
        private int comparisonCount;
        private int swapCount;

        public string Run()
        {
            // Initialisierung
            raeume = GenerateRooms();
            comparisonCount = 0;
            swapCount = 0;

            // Geschwindigkeit wählen
            string speed = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]Wähle die Animation-Geschwindigkeit:[/]")
                    .AddChoices("Langsam", "Mittel", "Schnell"));
            delay = speed switch
            {
                "Langsam" => 500,
                "Mittel" => 200,
                _ => 50
            };

            // Anzeige unsortierter Zustand
            AnsiConsole.MarkupLine("[underline yellow]Unsortierte Räume:[/]");
            PrintChart();

            // Algorithmus wählen
            string algorithmus = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]Wähle den Sortieralgorithmus:[/]")
                    .AddChoices("SelectionSort", "InsertionSort", "BubbleSort", "MergeSort"));

            // Sortieren
            switch (algorithmus)
            {
                case "SelectionSort": SelectionSort(); break;
                case "InsertionSort": InsertionSort(); break;
                case "BubbleSort": BubbleSort(); break;
                case "MergeSort": MergeSort(); break;
            }

            // Sortiert
            AnsiConsole.MarkupLine("\n[underline green]Sortierte Räume:[/]");
            PrintChart();
            AnsiConsole.MarkupLine($"[grey]Vergleiche: {comparisonCount}   Swaps: {swapCount}[/]");

            return "\n[Sortierung abgeschlossen]";
        }

        private void PrintChart(int idx1 = -1, int idx2 = -1, bool isSwap = false)
        {
            Console.Clear();
            BarChart chart = new BarChart()
                .Width(60)
                .Label("[yellow]Raum-Belegungen[/]")
                .CenterLabel();

            // Maximalwert für Skala
            int maxVal = raeume.Max(r => r.Belegung);

            for (int i = 0; i < raeume.Length; i++)
            {
                int belegung = raeume[i].Belegung;
                // Farbe für Hervorhebung
                Color color = Color.Grey;
                if (i == idx1 || i == idx2)
                    color = isSwap ? Color.Green : Color.Yellow;

                chart.AddItem(
                    new BarChartItem(i.ToString(), belegung, color)
                );
            }

            AnsiConsole.Write(chart);
            Thread.Sleep(delay);
        }

        private void SelectionSort()
        {
            for (int i = 0; i < anzraum - 1; i++)
            {
                int minIdx = i;
                for (int j = i + 1; j < anzraum; j++)
                {
                    comparisonCount++;
                    PrintChart(minIdx, j);
                    if (raeume[j].Belegung < raeume[minIdx].Belegung)
                        minIdx = j;
                }
                if (minIdx != i)
                    Swap(i, minIdx);
            }
        }

        private void InsertionSort()
        {
            for (int i = 1; i < raeume.Length; i++)
            {
                Raum key = raeume[i];
                int j = i - 1;
                while (j >= 0)
                {
                    comparisonCount++;
                    PrintChart(j, j + 1);
                    if (raeume[j].Belegung <= key.Belegung)
                        break;
                    raeume[j + 1] = raeume[j];
                    swapCount++;
                    PrintChart(j, j + 1, true);
                    j--;
                }
                raeume[j + 1] = key;
                PrintChart(j + 1, i, true);
            }
        }

        private void BubbleSort()
        {
            bool swapped;
            do
            {
                swapped = false;
                for (int i = 1; i < raeume.Length; i++)
                {
                    comparisonCount++;
                    PrintChart(i - 1, i);
                    if (raeume[i - 1].Belegung > raeume[i].Belegung)
                    {
                        Swap(i - 1, i);
                        swapped = true;
                    }
                }
            } while (swapped);
        }

        private void MergeSort() => MergeSortRec(0, raeume.Length - 1);

        private void MergeSortRec(int l, int r)
        {
            if (l >= r) return;
            int m = (l + r) / 2;
            MergeSortRec(l, m);
            MergeSortRec(m + 1, r);
            Merge(l, m, r);
        }

        private void Merge(int l, int m, int r)
        {
            Raum[] aux = raeume.Skip(l).Take(r - l + 1).ToArray();
            int i = 0, j = m - l + 1, k = l;
            while (i <= m - l && j < aux.Length)
            {
                comparisonCount++;
                PrintChart(k, l + (aux[i].Belegung <= aux[j].Belegung ? i : (j - (m - l + 1))));
                if (aux[i].Belegung <= aux[j].Belegung)
                    raeume[k++] = aux[i++];
                else
                    raeume[k++] = aux[j++];
                swapCount++;
                PrintChart(k - 1, k - 1, true);
            }
            while (i <= m - l)
            {
                raeume[k++] = aux[i++];
                swapCount++;
                PrintChart(k - 1, k - 1, true);
            }
            while (j < aux.Length)
            {
                raeume[k++] = aux[j++];
                swapCount++;
                PrintChart(k - 1, k - 1, true);
            }
        }

        private void Swap(int a, int b)
        {
            (raeume[a], raeume[b]) = (raeume[b], raeume[a]);
            swapCount++;
            PrintChart(a, b, true);
        }

        private Raum[] GenerateRooms()
        {
            Random rnd = new();
            return Enumerable.Range(0, anzraum)
                             .Select(_ => new Raum(rnd.Next(100)))
                             .ToArray();
        }

        private class Raum
        {
            public int Belegung { get; set; }
            public Raum(int belegung) => Belegung = belegung;
        }
    }
}


///ORIGINAL
///

//class Program
//{
//    static Raum[] raeume;
//    private static readonly int anzraum = 20;
//    public static void Main(string[] args)
//    {
//        raeume = GenerateRooms();
//        Console.WriteLine("Unsortierte Räume:");
//        foreach (Raum raum in raeume)
//            Console.Write(raum.Belegung + " ");
//        //SelectionSort();
//        //InsertionSort();
//        //BubbleSort();
//        MergeSort();
//        Console.WriteLine("\nSortierte Räume:");
//        foreach (Raum raum in raeume)
//            Console.Write(raum.Belegung + " ");
//    }

//    private static void MergeSort()
//    {
//        int anzahl = raeume.Length;
//        Raum[] tmp = new Raum[anzahl];
//        for (int größe = 1; größe < anzahl; größe *= 2)
//            for (int links = 0; links < anzahl - 1; links += 2 * größe)
//            {
//                int mitte = Math.Min(links + größe - 1, anzahl - 1);
//                int rechts = Math.Min(links + 2 * größe - 1, anzahl - 1);
//                Merge(tmp, links, mitte, rechts);
//            }
//    }

//    private static void Merge(Raum[] hilfsArray, int links, int mitte, int rechts)
//    {
//        int i = links, j = mitte + 1, k = links;
//        while (i <= mitte && j <= rechts) hilfsArray[k++] = raeume[i].Belegung <= raeume[j].Belegung ? raeume[i++] : raeume[j++];
//        while (i <= mitte) hilfsArray[k++] = raeume[i++];
//        while (j <= rechts) hilfsArray[k++] = raeume[j++];
//        for (int index = links; index <= rechts; index++) raeume[index] = hilfsArray[index];
//    }

//    private static void SelectionSort()
//    {

//        for (int i = 0; i < anzraum - 1; i++)
//        {
//            int minIndex = i;
//            for (int j = i + 1; j < anzraum; j++)
//                if (raeume[j].Belegung < raeume[minIndex].Belegung)
//                    minIndex = j;
//            if (minIndex != i)
//                (raeume[i], raeume[minIndex]) = (raeume[minIndex], raeume[i]);
//        }
//    }

//    private static void InsertionSort()
//    {
//        while (true)
//        {
//            bool success = false;
//            for (int i = 0; i < raeume.Length; i++)
//            {
//                if (raeume[i] != raeume[0])
//                    while (true)
//                        if (raeume[i].Belegung < raeume[i - 1].Belegung)
//                        {
//                            (raeume[i], raeume[i - 1]) = (raeume[i - 1], raeume[i]);
//                            success = true;
//                        }
//                        else
//                            break;
//            }
//            if (!success)
//                break;
//        }
//    }

//    private static Raum[] GenerateRooms()
//    {
//        Raum[] raeume = new Raum[anzraum];
//        Random rng = new();
//        for (int i = 0; i < raeume.Length; i++)
//            raeume[i] = new Raum(rng.Next(100));
//        return raeume;
//    }

//    private static void BubbleSort()
//    {
//        while (true)
//        {
//            bool success = false;
//            for (int i = 0; i < raeume.Length; i++)
//                if (raeume[i] != raeume[0])
//                    if (raeume[i].Belegung < raeume[i - 1].Belegung)
//                    {
//                        (raeume[i], raeume[i - 1]) = (raeume[i - 1], raeume[i]);
//                        success = true;
//                    }
//            if (!success)
//                break;
//        }
//    }
//}

//class Raum(int belegung)
//{
//    public int Belegung { get; set; } = belegung;
//}