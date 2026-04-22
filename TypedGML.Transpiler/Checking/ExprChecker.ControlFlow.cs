using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public sealed partial class ExprChecker
{
    private static bool BlockAlwaysReturns(TgmlBlock block)
    {
        foreach (var stmt in block.Statements)
        {
            if (StmtAlwaysReturns(stmt)) return true;
        }

        return false;
    }

    private static bool StmtAlwaysReturns(TgmlStatement stmt) =>
        stmt switch
        {
            TgmlReturnStmt => true,
            TgmlBlock b => BlockAlwaysReturns(b),
            TgmlIfStmt i =>
                i.ElseBlock is not null &&
                i.Branches.All(br => BlockAlwaysReturns(br.Body)) &&
                BlockAlwaysReturns(i.ElseBlock),
            _ => false
        };
}
