using Mono.Cecil; using MonoMod.Cil; using System; using System.Linq; using System.Reflection; using System.Runtime.Loader;
string R = System.Environment.GetEnvironmentVariable("REPO") ?? System.IO.Path.GetFullPath("../.."); // run from tools/check_lunar_patches
string L = R + "/libs";
var dirs = new[]{L, L+"/BepInEx", L+"/R2API", R + "/bin/Release/netstandard2.1"};
AssemblyLoadContext.Default.Resolving += (ctx, n) => { foreach (var d in dirs) { var p = System.IO.Path.Combine(d, n.Name + ".dll"); if (System.IO.File.Exists(p)) return ctx.LoadFromAssemblyPath(p); } return null; };
var mod = Assembly.LoadFrom(R + "/bin/Release/netstandard2.1/ror2-plus.dll");
var lp = mod.GetType("ClassicItemsReturns.Lunar.LunarPatches");
var m = ModuleDefinition.ReadModule(L + "/RoR2.dll");
MethodDefinition M(string t, string n) => m.GetTypes().First(x => x.FullName == t).Methods.Where(x => x.Name == n && x.HasBody).OrderByDescending(x => x.Body.Instructions.Count).First();
int bad = 0; bool APPLY = args.Length > 0;
void Check(MethodDefinition md, params string[] names) {
  foreach (var n in names) {
    bool ok; string err = "";
    try { var il = new ILContext(md); ok = (bool)lp.GetMethod(n).Invoke(null, new object[]{ il, APPLY }); }
    catch (Exception e) { ok = false; err = (e.InnerException ?? e).Message; }
    if (!ok) bad++;
    Console.WriteLine($"{(ok ? "OK  " : "FAIL")} {n,-20} {md.DeclaringType.Name}.{md.Name} {err}");
  }
}
Check(M("RoR2.CharacterBody","RecalculateStats"), "ShapedGlass","Transcendence","LightFlux","StoneFlux","Purity","Neutronium");
Check(M("RoR2.HealthComponent","TakeDamageProcess"), "BrittleCrown");
Check(M("RoR2.GlobalEventManager","OnInteractionBegin"), "DefiantGouge");
Check(M("RoR2.HealthComponent/RepeatHealComponent","RecalculateStats"), "Corpsebloom");
Check(M("RoR2.LunarSunBehavior","FixedUpdate"), "Egocentrism");
Check(M("RoR2.HoldoutZoneController/FocusConvergenceController","ApplyRadius"), "FocusedConvergence");
Check(M("RoR2.EquipmentSlot","FireTonic"), "SpinelTonic");
Check(M("RoR2.EquipmentSlot","MyFixedUpdate"), "Gesture");
var pc = m.GetTypes().First(x => x.FullName == "RoR2.PlayerCharacterMasterController");
int coin = 0;
foreach (var t in new[]{pc}.Concat(pc.NestedTypes)) foreach (var md in t.Methods.Where(x => x.HasBody && x.Name.Contains("<Init>")))
  try { if ((bool)lp.GetMethod("CoinDrop").Invoke(null, new object[]{ new ILContext(md), APPLY })) { coin++; Console.WriteLine("OK   CoinDrop             " + t.Name + "." + md.Name); } } catch {}
if (coin != 1) { bad++; Console.WriteLine("FAIL CoinDrop matches=" + coin); }
// On-hook target signatures in the current game
var asm = Assembly.LoadFrom(L + "/RoR2.dll");
void Sig(string t, string n) { var ms = asm.GetType(t)?.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static).Where(x => x.Name == n).ToList();
  if (ms == null || ms.Count == 0) { bad++; Console.WriteLine("FAIL hook " + t + "." + n); return; }
  foreach (var x in ms) Console.WriteLine("hook " + t.Split('.').Last() + "." + n + "(" + string.Join(", ", x.GetParameters().Select(p => p.ParameterType.Name)) + ") -> " + x.ReturnType.Name); }
Sig("RoR2.Util","CheckRoll"); Sig("RoR2.Skills.LunarPrimaryReplacementSkill","GetRechargeInterval"); Sig("RoR2.Skills.LunarSecondaryReplacementSkill","GetRechargeInterval");
Sig("RoR2.Skills.LunarDetonatorSkill","GetRechargeInterval"); Sig("RoR2.LunarSunBehavior","TransformationFilter"); Sig("RoR2.TeamManager","GetLongstandingSolitudeItemCostScale");
Sig("RoR2.HelfireController","Awake"); Sig("RoR2.PurchaseInteraction","Awake"); Sig("RoR2.GenericPickupController","AttemptGrant");
if (APPLY) { m.Write(args[0]); Console.WriteLine("written"); }
Console.WriteLine(bad == 0 ? "ALL PASS" : bad + " FAILURES");
