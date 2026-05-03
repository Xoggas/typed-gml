using TypedGML.Compiler.Ast;

namespace TypedGML.Compiler.Verification;

public interface ISemanticCheck
{
    bool Matches(IAstNode node);

    void Check(IAstNode node, VerificationContext ctx);
}
