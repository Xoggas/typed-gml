// const string tgmlProjectFileName = "tgmlMetadata.json";
//
// if (args.Length == 0)
// {
//     Console.WriteLine("Specify the project file path");
//     return;
// }
//
// var projectFolderPath = args[0];
// var projectFilePath = Path.Combine(projectFolderPath, tgmlProjectFileName);
//
// if (File.Exists(projectFilePath) is false)
// {
//     Console.WriteLine("Project file not found");
//     return;
// }
//
// var tgmlProjectFile = JsonConvert.DeserializeObject<TgmlProjectFile>(File.ReadAllText(projectFilePath));
//
// if (tgmlProjectFile is null)
// {
//     Console.WriteLine("Invalid project file");
//     return;
// }

using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using TypedGML.Transpiler.GameMaker;

const string projectPath = @"C:\Users\xogga\OneDrive\Documents\Projects\TestGame";
const string codePath = @"C:\Users\xogga\OneDrive\Documents\Projects\TestGame\tgml";

// const string content = "show_debug_message(\"Hello\")";

// for (var i = 0; i < 1000; i++)
// {
//     var path = Path.Combine(codePath, $"Script{i:0000}.tgml");
//
//     File.WriteAllText(path, content);
// }

// var api = GameMakerApi.Init(projectPath);
//
// var files = Directory.GetFiles(codePath, "*.tgml", SearchOption.AllDirectories);
//
// foreach (var file in files)
// {
//     var localFilePath = Path.GetRelativePath(codePath, file).Replace("\\", "/");
//     var localFolderPath = Path.Combine("Scripts", Path.GetDirectoryName(localFilePath)!).Replace("\\", "/");
//     var fileName = Path.GetFileNameWithoutExtension(localFilePath);
//     var content = File.ReadAllText(file);
//     api.WriteScript(localFolderPath, fileName, content);
// }
//
// api.PerformCleanup();
// api.CommitChanges();

BenchmarkRunner.Run<Benchmarks>();

public class Benchmarks
{
    private List<string> _strings = [];

    [GlobalSetup]
    public void Setup()
    {
        _strings = Enumerable.Range(0, 1_000_000).Select(_ => Guid.NewGuid().ToString()).ToList();
    }

    [Benchmark]
    public List<string> ListBenchmark()
    {
        var list = new List<string>();

        foreach (var item in _strings)
        {
            list.Add(item);
        }

        return list;
    }

    [Benchmark]
    public ConcurrentBag<string> BagBenchmark()
    {
        var bag = new ConcurrentBag<string>();

        Parallel.ForEach(_strings, x => bag.Add(x));

        return bag;
    }
}