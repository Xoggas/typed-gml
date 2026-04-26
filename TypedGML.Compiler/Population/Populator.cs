using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Population;

public sealed class Populator(
    NamespacePopulator namespacePopulator,
    TypePopulator typePopulator,
    MemberPopulator memberPopulator,
    InheritanceResolver inheritanceResolver,
    GenericParameterBinder genericParameterBinder)
{
    public void Populate(IReadOnlyList<FileNode> files)
    {
        BuiltinTypeRegistry.RegisterInto(namespacePopulator.SymbolTable);
        namespacePopulator.Populate(files);
        typePopulator.Populate(files);
        memberPopulator.Populate(files);
        inheritanceResolver.Populate(files);
        genericParameterBinder.Populate(files);
    }
}
