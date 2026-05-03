namespace TypedGML.Compiler.Emission;

public sealed class GmlOutputFormatter
{
    public string Normalize(string content)
    {
        var lines = content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var output = new List<string>(lines.Length);
        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();
            if (line.StartsWith("function ", StringComparison.Ordinal) &&
                output.Count > 0 &&
                !string.IsNullOrEmpty(output[^1]))
                output.Add(string.Empty);
            output.Add(line);
        }

        while (output.Count > 0 && string.IsNullOrEmpty(output[^1]))
            output.RemoveAt(output.Count - 1);

        return string.Join(Environment.NewLine, output);
    }
}
