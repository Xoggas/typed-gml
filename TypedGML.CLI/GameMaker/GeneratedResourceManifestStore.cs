using System.Text;
using System.Text.Json;

namespace TypedGML.CLI.GameMaker;

internal sealed class GeneratedResourceManifestStore
{
    private const string FileName = ".typedgml.generated.json";
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public GeneratedResourceManifest Load(string gmRoot)
    {
        var path = Path.Combine(gmRoot, FileName);
        if (!File.Exists(path))
            return GeneratedResourceManifest.CreateEmpty();

        try
        {
            return JsonSerializer.Deserialize<GeneratedResourceManifest>(File.ReadAllText(path), _jsonOptions)
                ?? GeneratedResourceManifest.CreateEmpty();
        }
        catch (JsonException)
        {
            return GeneratedResourceManifest.CreateEmpty();
        }
        catch (IOException)
        {
            return GeneratedResourceManifest.CreateEmpty();
        }
    }

    public void Save(string gmRoot, GeneratedResourceManifest manifest)
    {
        Directory.CreateDirectory(gmRoot);
        File.WriteAllText(
            Path.Combine(gmRoot, FileName),
            JsonSerializer.Serialize(manifest, _jsonOptions),
            new UTF8Encoding(false));
    }
}
