using TypedGML.Transpiler;

const string codePath = @"C:\Users\xoggas\Documents\GitHub\typed-gml\TypedGML.Transpiler\Test.tgml";
const string resultPath = @"C:\Users\xoggas\Documents\GitHub\typed-gml\TypedGML.Transpiler\Generated";

var result = TranspilerApi.Transpile([
    new TgmlSourceFile("Test.tgml", File.ReadAllText(codePath))
]);

if (result.Success is false)
{
    foreach (var diagnostic in result.Diagnostics)
    {
        Console.WriteLine(diagnostic);
    }

    return;
}

foreach (var f in result.Files)
{
    var path = Path.Combine(resultPath, f.Path);

    if (!Directory.Exists(path))
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException());
    }

    File.WriteAllText(path, f.Content);
}
