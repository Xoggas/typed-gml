using System.Diagnostics.CodeAnalysis;

namespace TypedGML.Transpiler.GameMaker.IO;

public sealed class ScriptModule
{
    private readonly Project _project;
    private readonly ChangesTracker _changesTracker;

    public ScriptModule(Project project, ChangesTracker changesTracker)
    {
        _project = project;
        _changesTracker = changesTracker;
    }

    public bool TryFindScript(string name, [NotNullWhen(true)] out Script? script)
    {
        throw new NotImplementedException();
    }

    public Script CreateScript(string name, Reference folderReference)
    {
        throw new NotImplementedException();
    }

    public void DeleteScript(string name)
    {
        throw new NotImplementedException();
    }
}