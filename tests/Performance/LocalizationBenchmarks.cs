using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using PokeNET.Core.Localization;

namespace PokeNET.Tests.Performance;

/// <summary>
/// Performance benchmarks for localization system.
/// Run with: dotnet run -c Release --project tests/PokeNET.Tests.csproj --filter *Benchmarks*
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class LocalizationBenchmarks
{
    private List<string> _supportedCultureNames = new();

    [GlobalSetup]
    public void Setup()
    {
        var cultures = LocalizationManager.GetSupportedCultures();
        _supportedCultureNames = cultures.Select(c => c.Name).ToList();
    }

    [Benchmark]
    public List<System.Globalization.CultureInfo> GetSupportedCultures()
    {
        return LocalizationManager.GetSupportedCultures();
    }

    [Benchmark]
    public void SetCulture_DefaultCulture()
    {
        LocalizationManager.SetCulture(LocalizationManager.DEFAULT_CULTURE_CODE);
    }

    [Benchmark]
    public void SetCulture_MultipleCultures()
    {
        foreach (var cultureName in _supportedCultureNames)
        {
            LocalizationManager.SetCulture(cultureName);
        }
    }

    [Benchmark]
    public void SetCulture_Repeated()
    {
        for (int i = 0; i < 100; i++)
        {
            LocalizationManager.SetCulture(LocalizationManager.DEFAULT_CULTURE_CODE);
        }
    }
}

/// <summary>
/// Benchmark runner program.
/// </summary>
public class BenchmarkRunner
{
    public static void RunBenchmarks()
    {
        BenchmarkRunner.Run<LocalizationBenchmarks>();
    }
}
