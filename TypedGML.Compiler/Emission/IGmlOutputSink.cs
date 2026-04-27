namespace TypedGML.Compiler.Emission;

public interface IGmlOutputSink
{
    void Write(string path, string content);
}
