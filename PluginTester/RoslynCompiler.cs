using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace PluginTester;

public static class RoslynCompiler
{
    public static ICodePlugin? Compile(string filePath)
    {
        string code = File.ReadAllText(filePath);
        if (!code.Contains("using PluginTester.Contracts;"))
            code = "using PluginTester.Contracts;\n" + code;

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
        List<PortableExecutableReference> refs = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToList();

        string contractsDll = typeof(ICodePlugin).Assembly.Location;
        if (!string.IsNullOrWhiteSpace(contractsDll) && File.Exists(contractsDll))
            refs.Add(MetadataReference.CreateFromFile(contractsDll));

        CSharpCompilation compilation = CSharpCompilation.Create(
            Path.GetRandomFileName(),
            new[] { syntaxTree },
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using MemoryStream ms = new();
        Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(ms);

        if (!result.Success)
        {
            foreach (Diagnostic? d in result.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Fehler in {Path.GetFileName(filePath)}: {d.GetMessage()}");
                Console.ResetColor();
            }
            return null;
        }

        ms.Seek(0, SeekOrigin.Begin);
        Assembly asm = Assembly.Load(ms.ToArray());
        Type? type = asm.GetTypes().FirstOrDefault(t => typeof(ICodePlugin).IsAssignableFrom(t));
        return type != null ? Activator.CreateInstance(type) as ICodePlugin : null;
    }
}
