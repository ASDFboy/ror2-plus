// Makes every type, method and field public (skipping fields named like events, as BepInEx's publicizer does).
using Mono.Cecil; using System.Linq;
var res = new DefaultAssemblyResolver();
foreach (var d in args.Skip(2)) res.AddSearchDirectory(d);
var m = ModuleDefinition.ReadModule(args[0], new ReaderParameters { AssemblyResolver = res, ReadingMode = ReadingMode.Immediate });
foreach (var t in m.GetTypes()) {
  if (t.IsNested) t.IsNestedPublic = true; else t.IsPublic = true;
  var evs = new System.Collections.Generic.HashSet<string>(t.Events.Select(e => e.Name));
  foreach (var me in t.Methods) me.IsPublic = true;
  foreach (var f in t.Fields) if (!evs.Contains(f.Name)) f.IsPublic = true;
}
m.Write(args[1]);
System.Console.WriteLine("ok");
