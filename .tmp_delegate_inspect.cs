using System;
using System.IO;
using System.Linq;
using TypedGML.Transpiler;
using TypedGML.Transpiler.Population;

var source = new TgmlSourceFile("Delegates.tgml", File.ReadAllText("TypedGML.Transpiler/Bcl/Delegates.tgml"));
var populated = Populator.Populate(new[] { source });
foreach (var type in populated.Files[0].TypeDecls)
{
    Console.WriteLine($"{type.GetType().Name} {type.Name} arity={type.TypeParams.Count}");
}
