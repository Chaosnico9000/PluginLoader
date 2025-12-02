using System.Text;
using PluginTester.Contracts;

namespace Plugins;

public class FakultaetUndFibonacci : ICodePlugin
{
    public string Name => "Fakultät & Fibonacci";

    public string Category => "Lernen";

    public bool IsEnabled => true;

    public string Run()
    {
        int n = 10;
        StringBuilder sb = new();

        sb.AppendLine($"faciterativ({n}) = {FacIterativ(n)}");
        sb.AppendLine($"facrecursiv({n}) = {FacRecursiv(n)}");
        sb.AppendLine($"fibrecursiv({n}) = {FibRecursiv(n)}");

        return sb.ToString();
    }

    private int FibRecursiv(int n)
    {
        if (n <= 2)
            return n;
        return FibRecursiv(n - 2) + FibRecursiv(n - 1);
    }

    private int FacRecursiv(int n)
    {
        if (n <= 1)
            return 1;
        return n * FacRecursiv(n - 1);
    }

    private int FacIterativ(int n)
    {
        int e = 1;
        for (int i = n; i > 0; i--)
            e *= i;
        return e;
    }
}
