using BepInEx.Configuration;
using ClassicItemsReturns.Modules;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace ClassicItemsReturns.Items.Complete.Voids
{
    public enum Trig { None, Hit, Crit, Kill, EliteKill, Hurt, LowHp, Primary, Secondary, Utility, Special, Pulse, Tp, Interact, Equip, Stage }

    public class Eff { public string kind, stat; public float a, b, c; }

    public class VoidSpec
    {
        public string id, original, name; public int tier;
        public Trig trig; public float tp;
        public readonly List<Eff> effects = new List<Eff>();
        public readonly Dictionary<string, float> stats = new Dictionary<string, float>();
        public ItemDef def;
    }

    /// <summary>
    /// Data-driven void items: every row in VoidTable becomes an item that corrupts its original.
    /// Effects come from a shared library, so each item is a single line of the table.
    /// </summary>
    public static class VoidSystem
    {
        static readonly List<VoidSpec> specs = new List<VoidSpec>();
        static readonly Dictionary<Trig, List<VoidSpec>> byTrig = new Dictionary<Trig, List<VoidSpec>>();
        static readonly List<VoidSpec> statSpecs = new List<VoidSpec>();
        static readonly Dictionary<string, BuffDef> buffs = new Dictionary<string, BuffDef>();
        static readonly Dictionary<string, float> buffPerStack = new Dictionary<string, float> { { "dmg", 0.05f }, { "atk", 0.08f }, { "ms", 0.08f }, { "armor", 8f }, { "crit", 4f }, { "regen", 2f } };
        static readonly Dictionary<string, string> buffNames = new Dictionary<string, string> { { "dmg", "Void Might" }, { "atk", "Void Haste" }, { "ms", "Void Stride" }, { "armor", "Void Shell" }, { "crit", "Void Focus" }, { "regen", "Void Mend" } };
        static BuffDef rot;
        static GameObject blastFx, lightningFx;
        const float AreaRadius = 15f, SeekRadius = 40f, LowHpCooldown = 20f;

        public static void Init(ConfigFile config)
        {
            if (!config.Bind("Void Items", "Enable Void Items", true, "Adds a void version of every white, green and red item that lacks one. Requires Survivors of the Void.").Value) return;
            var dlc1 = CIC.Load<ExpansionDef>("RoR2/DLC1/Common/DLC1.asset");
            Sfx.Register(new List<string>(voidSounds.Values).ToArray());
            blastFx = CIC.Load<GameObject>("RoR2/DLC1/ExplodeOnDeathVoid/ExplodeOnDeathVoidExplosionEffect.prefab") ?? CIC.ExplosionSmall;
            rot = CIC.Buff("VoidRot", new Color(0.6f, 0.2f, 0.8f), true, true, "RoR2/Base/ArmorReductionOnHit/texBuffPulverizeIcon.tif");
            foreach (var s in buffPerStack.Keys) buffs[s] = CIC.Buff("Void_" + s, new Color(0.75f, 0.35f, 1f), true, false, "RoR2/Base/CritOnUse/texBuffFullCritIcon.tif");

            foreach (var row in VoidTable.Rows)
            {
                VoidSpec spec;
                try { spec = Parse(row); }
                catch (Exception e) { ClassicItemsReturnsPlugin.ModLogger.LogError($"Void item {row.id}: bad spec '{row.spec}': {e.Message}"); continue; }
                if (!config.Bind("Void: " + spec.name, "Enable Item?", true, "Should this void item appear in runs?").Value) continue;
                CreateItem(spec, dlc1);
                specs.Add(spec);
                if (spec.trig != Trig.None) { if (!byTrig.TryGetValue(spec.trig, out var l)) byTrig[spec.trig] = l = new List<VoidSpec>(); l.Add(spec); }
                if (spec.stats.Count > 0) statSpecs.Add(spec);
            }
            Hooks();
            ClassicItemsReturnsPlugin.ModLogger.LogInfo($"ror2-plus: registered {specs.Count} void items.");
        }

        // ------------------------------------------------------------ parsing
        static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);
        static VoidSpec Parse((string id, string original, int tier, string name, string spec) r)
        {
            var spec = new VoidSpec { id = r.id, original = r.original, name = r.name, tier = r.tier };
            var halves = r.spec.Split(';');
            var left = halves[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (left.Length > 0)
            {
                var t = left[0].Split(':');
                spec.trig = (Trig)Enum.Parse(typeof(Trig), t[0], true);
                spec.tp = t.Length > 1 ? F(t[1]) : 100f;
                foreach (var tok in left.Skip(1))
                {
                    var p = tok.Split(':');
                    var e = new Eff { kind = p[0] };
                    if (e.kind == "buff") { e.stat = p[1]; e.a = F(p[2]); e.b = F(p[3]); if (!buffs.ContainsKey(e.stat)) throw new Exception("unknown buff " + e.stat); }
                    else { if (p.Length > 1) e.a = F(p[1]); if (p.Length > 2) e.b = F(p[2]); }
                    spec.effects.Add(e);
                }
            }
            if (halves.Length > 1)
                foreach (var tok in halves[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                { var p = tok.Split(':'); spec.stats[p[0]] = F(p[1]); }
            return spec;
        }

        // ------------------------------------------------------------ item creation
        static void CreateItem(VoidSpec s, ExpansionDef dlc1)
        {
            string tok = LanguageOverrides.LanguageTokenPrefixItem + "VOID_" + s.id;
            var def = ScriptableObject.CreateInstance<ItemDef>();
            def.name = tok;
            def.nameToken = tok + "_NAME"; def.pickupToken = tok + "_PICKUP"; def.descriptionToken = tok + "_DESCRIPTION"; def.loreToken = tok + "_LORE";
            def.deprecatedTier = s.tier == 1 ? ItemTier.VoidTier1 : s.tier == 2 ? ItemTier.VoidTier2 : ItemTier.VoidTier3;
            def.pickupIconSprite = RuntimeArt.Icon("Void_" + s.id);
            def.pickupModelPrefab = RuntimeArt.PickupModel("Void_" + s.id);
            def.canRemove = true;
            def.requiredExpansion = dlc1;
            def.tags = Tags(s);
            s.def = def;
            ItemAPI.Add(new CustomItem(def, new ItemDisplayRuleDict()));
            Displays.Add(def, def.pickupModelPrefab, s.original.StartsWith("rp:") ? LanguageOverrides.LanguageTokenPrefixItem + s.original.Substring(3) : s.original);
            LanguageAPI.Add(def.nameToken, s.name);
            string desc = Describe(s);
            LanguageAPI.Add(def.pickupToken, desc.Split(new[] { ". " }, StringSplitOptions.None)[0].Replace("<style=cStack>", "").Replace("</style>", "") + ". <style=cIsVoid>Corrupts all " + OriginalDisplay(s) + "</style>.");
            LanguageAPI.Add(def.descriptionToken, desc + " <style=cIsVoid>Corrupts all " + OriginalDisplay(s) + "</style>.");
            LanguageAPI.Add(def.loreToken, Lore.Void(s.id));
        }

        static ItemTag[] Tags(VoidSpec s)
        {
            var t = new HashSet<ItemTag>();
            foreach (var e in s.effects)
                switch (e.kind)
                {
                    case "heal": case "barrier": t.Add(ItemTag.Healing); break;
                    case "gold": case "cdr": case "root": case "slow": t.Add(ItemTag.Utility); break;
                    case "buff": t.Add(e.stat == "regen" || e.stat == "armor" ? ItemTag.Utility : ItemTag.Damage); break;
                    default: t.Add(ItemTag.Damage); break;
                }
            foreach (var k in s.stats.Keys) t.Add(k == "regen" || k == "hp" || k == "shield" ? ItemTag.Healing : k == "dmg" || k == "crit" || k == "critdmg" || k == "atk" ? ItemTag.Damage : ItemTag.Utility);
            if (s.trig == Trig.Kill || s.trig == Trig.EliteKill) t.Add(ItemTag.OnKillEffect);
            if (s.trig == Trig.LowHp) t.Add(ItemTag.LowHealth);
            if (s.trig == Trig.Interact || s.trig == Trig.Tp || s.trig == Trig.Stage) t.Add(ItemTag.AIBlacklist);
            if (s.trig == Trig.Equip) { t.Add(ItemTag.EquipmentRelated); t.Add(ItemTag.AIBlacklist); }
            return t.ToArray();
        }

        // Display names for the "Corrupts all X" line, resolved from the original's language token at runtime.
        static readonly Dictionary<string, string> originalNames = new Dictionary<string, string>();
        static string OriginalDisplay(VoidSpec s) => "{ORIG_" + s.id + "}";

        // ------------------------------------------------------------ description text
        static string N(float f) => f.ToString("0.##", CultureInfo.InvariantCulture);
        static string P(float f) => N(f * 100f) + "%";
        static string D(object s) => $"<style=cIsDamage>{s}</style>";
        static string H(object s) => $"<style=cIsHealing>{s}</style>";
        static string U(object s) => $"<style=cIsUtility>{s}</style>";
        static string V(object s) => $"<style=cIsVoid>{s}</style>";
        static string St(object s) => $" <style=cStack>(+{s} per stack)</style>";

        static string Describe(VoidSpec s)
        {
            var parts = new List<string>();
            if (s.trig != Trig.None && s.effects.Count > 0)
            {
                bool infinitive; string lead = Lead(s, out infinitive);
                var effs = s.effects.Select(e => EffText(s, e, infinitive)).ToList();
                string joined = effs.Count == 1 ? effs[0] : string.Join(", ", effs.Take(effs.Count - 1)) + " and " + effs.Last();
                parts.Add(lead + " " + joined + ".");
            }
            foreach (var kv in s.stats) parts.Add(StatText(kv.Key, kv.Value));
            return string.Join(" ", parts);
        }

        static string Lead(VoidSpec s, out bool inf)
        {
            inf = true;
            switch (s.trig)
            {
                case Trig.Hit: return $"{D(N(s.tp) + "%")} chance on hit to";
                case Trig.Crit: inf = false; return "Critical strikes";
                case Trig.Kill: inf = false; return "Killing an enemy";
                case Trig.EliteKill: inf = false; return "Killing an elite enemy";
                case Trig.Hurt: return s.tp >= 100 ? "When hit," : $"{U(N(s.tp) + "%")} chance when hit to";
                case Trig.LowHp: return $"Falling below {H("25% health")} (recharges every {LowHpCooldown}s) causes you to";
                case Trig.Primary: case Trig.Secondary: case Trig.Utility: case Trig.Special:
                    string slot = s.trig.ToString();
                    if (s.trig == Trig.Primary && s.tp < 100) return $"{D(N(s.tp) + "%")} chance on using your {U("Primary skill")} to";
                    inf = false; return $"Using your {U(slot + " skill")}";
                case Trig.Pulse: return $"Every {U(N(s.tp) + "s")},";
                case Trig.Tp: inf = false; return $"Activating the {U("Teleporter")}";
                case Trig.Interact: inf = false; return $"Buying or using an {U("interactable")}";
                case Trig.Equip: inf = false; return $"Activating your {U("Equipment")}";
                case Trig.Stage: return "At the start of each stage,";
            }
            return "";
        }

        static string Where(VoidSpec s) => s.trig == Trig.Hit || s.trig == Trig.Crit ? "the target" : s.trig == Trig.Kill || s.trig == Trig.EliteKill ? $"enemies within {AreaRadius}m of the victim" : $"enemies within {AreaRadius}m";

        static string EffText(VoidSpec s, Eff e, bool inf)
        {
            string v(string baseVerb, string third) => inf ? baseVerb : third;
            switch (e.kind)
            {
                case "collapse": return $"{v("inflict", "inflicts")} {D(N(e.a))}{St(N(e.a))} stack(s) of {V("Collapse")} on {Where(s)}";
                case "blast": return $"{v("detonate", "detonates")} a {V("void blast")} for {D(P(e.a))}{St(P(e.a))} base damage within {D(N(e.b) + "m")}";
                case "lightning": return $"{v("call", "calls")} {V("void lightning")} that strikes {D(N(e.b))} times for {D(P(e.a))}{St(P(e.a))} base damage each";
                case "missile": return $"{v("fire", "fires")} {D(N(e.b))} {V("void missiles")} for {D(P(e.a))}{St(P(e.a))} base damage each";
                case "root": return $"{v("root", "roots")} {Where(s)} for {U(N(e.a) + "s")}{St(N(e.a) + "s")}";
                case "slow": return $"{v("slow", "slows")} {Where(s)} by {U("50%")} for {U(N(e.a) + "s")}{St(N(e.a) + "s")}";
                case "rot": return $"{v("apply", "applies")} {D(N(e.a))}{St(N(e.a))} {V("Rot")} (-10 armor each) to {Where(s)} for {D(N(e.b) + "s")}";
                case "barrier": return $"{v("gain", "grants")} {H(P(e.a))}{St(P(e.a))} of your maximum health as {H("barrier")}";
                case "heal": return $"{v("heal", "heals you")} for {H(P(e.a))}{St(P(e.a))} of your maximum health";
                case "gold": return $"{v("gain", "grants")} {U("$" + N(e.a))}{St("$" + N(e.a))} (scales with time)";
                case "cdr": return $"{v("reduce", "reduces")} all skill cooldowns by {U(N(e.a) + "s")}{St(N(e.a) + "s")}";
                case "buff":
                    string amt = e.stat == "armor" ? "+" + N(buffPerStack["armor"]) + " armor" : e.stat == "regen" ? "+" + N(buffPerStack["regen"]) + " hp/s" : "+" + P(buffPerStack[e.stat]) + " " + StatWord(e.stat);
                    return $"{v("gain", "grants")} {U(N(e.a))}{St(N(e.a))} stack(s) of {V(buffNames[e.stat])} ({amt} each, up to {U(N(Mathf.Max(3f, e.a)))}{St(N(Mathf.Max(3f, e.a)))}) for {U(N(e.b) + "s")}";
            }
            return e.kind;
        }

        static string StatWord(string k) => k == "dmg" ? "damage" : k == "atk" ? "attack speed" : k == "ms" ? "movement speed" : k == "crit" ? "critical chance" : k;

        static string StatText(string k, float v)
        {
            bool neg = v < 0; float a = Mathf.Abs(v);
            string verb = neg ? "Lose" : "Gain";
            switch (k)
            {
                case "dmg": case "atk": case "ms": case "crit": return $"{verb} {(neg ? D("-" + N(a) + "%") : D("+" + N(a) + "%"))}{St(N(v) + "%")} {(k == "dmg" ? "damage" : k == "atk" ? "attack speed" : k == "ms" ? "movement speed" : "critical chance")}.";
                case "critdmg": return $"Gain {D("+" + N(a) + "%")}{St(N(a) + "%")} critical damage.";
                case "cdr": return $"Reduce all skill cooldowns by {U(N(a) + "%")}{St(N(a) + "%")} (hyperbolic).";
                case "hp": return $"{verb} {H((neg ? "-" : "+") + N(a) + "%")}{St(N(v) + "%")} maximum health.";
                case "shield": return $"Gain a {H("shield")} equal to {H(N(a) + "%")}{St(N(a) + "%")} of your maximum health.";
                case "curse": return $"{V("Lose " + N(a) + "%")}{St(N(a) + "%")} of your maximum health.";
                case "armor": return $"{verb} {U((neg ? "-" : "+") + N(a))}{St(N(v))} armor.";
                case "regen": return $"{verb} {H((neg ? "-" : "+") + N(a) + " hp/s")}{St(N(v) + " hp/s")} base health regeneration.";
                case "jump": return $"Gain {U("+" + N(a))}{St(N(a))} maximum jump count.";
            }
            return "";
        }

        // ------------------------------------------------------------ runtime
        static int Count(CharacterBody b, VoidSpec s) => b && b.inventory && s.def ? b.inventory.GetItemCountEffective(s.def) : 0;
        static List<VoidSpec> Of(Trig t) => byTrig.TryGetValue(t, out var l) ? l : null;

        static void Hooks()
        {
            On.RoR2.Items.ContagiousItemManager.Init += orig =>
            {
                var key = DLC1Content.ItemRelationshipTypes.ContagiousItem;
                var pairs = ItemCatalog.itemRelationships.TryGetValue(key, out var arr) ? arr.ToList() : new List<ItemDef.Pair>();
                foreach (var s in specs)
                {
                    if (!s.def || s.def.itemIndex == ItemIndex.None) continue;
                    string name = s.original.StartsWith("rp:") ? LanguageOverrides.LanguageTokenPrefixItem + s.original.Substring(3) : s.original;
                    var idx = ItemCatalog.FindItemIndex(name);
                    if (idx == ItemIndex.None) { ClassicItemsReturnsPlugin.ModLogger.LogWarning($"Void item {s.name}: original '{name}' not found; it will still drop from void sources."); continue; }
                    var od = ItemCatalog.GetItemDef(idx);
                    if (pairs.Any(p => p.itemDef1 == od)) { ClassicItemsReturnsPlugin.ModLogger.LogWarning($"Void item {s.name}: {name} already has a void version; skipping corruption."); continue; }
                    pairs.Add(new ItemDef.Pair { itemDef1 = od, itemDef2 = s.def });
                }
                ItemCatalog.itemRelationships[key] = pairs.ToArray();
                orig();
            };

            // Fill in "Corrupts all X" with the original's real in-game name, in whatever language is active.
            Language.onCurrentLanguageChanged += FillOriginalNames;
            RoR2Application.onLoad += FillOriginalNames;

            RecalculateStatsAPI.GetStatCoefficients += Stats;
            On.RoR2.CharacterBody.RecalculateStats += (orig, self) =>
            {
                orig(self);
                if (!self.inventory) return;
                foreach (var s in statSpecs) if (s.stats.TryGetValue("jump", out float j)) { int n = Count(self, s); if (n > 0) self.maxJumpCount += Mathf.RoundToInt(j * n); }
            };
            CICHooks.onHitServer += OnHit;
            CICHooks.onKillServer += OnKill;
            CICHooks.onPostDamageServer += OnHurt;
            CICHooks.onSkillActivated += OnSkill;
            On.RoR2.EquipmentSlot.OnEquipmentExecuted += (orig, self) => { orig(self); if (NetworkServer.active && self.characterBody) Fire(Trig.Equip, self.characterBody, null, self.characterBody.corePosition, 1f, false, default); };
            GlobalEventManager.OnInteractionsGlobal += (interactor, interactable, go) =>
            {
                if (!go || !go.GetComponent<PurchaseInteraction>()) return;
                var b = interactor ? interactor.GetComponent<CharacterBody>() : null;
                if (b) Fire(Trig.Interact, b, null, b.corePosition, 1f, false, default);
            };
            Utils.IsTeleActivatedTracker.OnTPActivatedActions += () =>
            {
                if (!NetworkServer.active) return;
                foreach (var b in CharacterBody.readOnlyInstancesList.ToArray()) if (b.isPlayerControlled || b.teamComponent.teamIndex == TeamIndex.Player) Fire(Trig.Tp, b, null, b.corePosition, 1f, false, default);
            };
            CharacterBody.onBodyStartGlobal += b => { if (NetworkServer.active && b.isPlayerControlled) Fire(Trig.Stage, b, null, b.corePosition, 1f, false, default); };
            CharacterBody.onBodyInventoryChangedGlobal += b =>
            {
                if (!NetworkServer.active) return;
                var pulses = Of(Trig.Pulse);
                bool any = pulses != null && pulses.Any(s => Count(b, s) > 0);
                var comp = b.GetComponent<VoidPulse>();
                if (any && !comp) b.gameObject.AddComponent<VoidPulse>().body = b;
                else if (!any && comp) UnityEngine.Object.Destroy(comp);
            };
        }

        static void FillOriginalNames()
        {
            var lang = Language.currentLanguage;
            if (lang == null || ItemCatalog.itemCount == 0) return;
            foreach (var s in specs)
            {
                string name = s.original.StartsWith("rp:") ? LanguageOverrides.LanguageTokenPrefixItem + s.original.Substring(3) : s.original;
                var idx = ItemCatalog.FindItemIndex(name);
                string disp = idx != ItemIndex.None ? Language.GetString(ItemCatalog.GetItemDef(idx).nameToken) : name;
                foreach (var t in new[] { s.def.pickupToken, s.def.descriptionToken })
                {
                    string cur = lang.GetLocalizedStringByToken(t);
                    if (cur.Contains("{ORIG_")) lang.SetStringByToken(t, cur.Replace("{ORIG_" + s.id + "}", disp + "s"));
                }
            }
        }

        static void Stats(CharacterBody b, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (!b.inventory) return;
            foreach (var s in statSpecs)
            {
                int n = Count(b, s); if (n <= 0) continue;
                foreach (var kv in s.stats)
                {
                    float v = kv.Value * n;
                    switch (kv.Key)
                    {
                        case "dmg": args.damageMultAdd += v / 100f; break;
                        case "atk": args.attackSpeedMultAdd += v / 100f; break;
                        case "ms": args.moveSpeedMultAdd += v / 100f; break;
                        case "crit": args.critAdd += v; break;
                        case "critdmg": args.critDamageMultAdd += v / 100f; break;
                        case "armor": args.armorAdd += v; break;
                        case "regen": args.baseRegenAdd += v * (1f + 0.2f * (b.level - 1f)); break;
                        case "hp": args.healthMultAdd += v / 100f; break;
                        case "shield": args.baseShieldAdd += (b.baseMaxHealth + b.levelMaxHealth * (b.level - 1f)) * v / 100f; break;
                        case "cdr": args.cooldownMultAdd -= 1f - 1f / (1f + kv.Value / 100f * n); break;
                        case "curse": { float f = Mathf.Min(0.9f, v / 100f); args.baseCurseAdd += f / (1f - f); break; }
                    }
                }
            }
            foreach (var kv in buffs)
            {
                int c = b.GetBuffCount(kv.Value); if (c <= 0) continue;
                float v = buffPerStack[kv.Key] * c;
                switch (kv.Key)
                {
                    case "dmg": args.damageMultAdd += v; break;
                    case "atk": args.attackSpeedMultAdd += v; break;
                    case "ms": args.moveSpeedMultAdd += v; break;
                    case "armor": args.armorAdd += v; break;
                    case "crit": args.critAdd += v; break;
                    case "regen": args.baseRegenAdd += v * (1f + 0.2f * (b.level - 1f)); break;
                }
            }
            int r = b.GetBuffCount(rot);
            if (r > 0) args.armorAdd -= 10f * r;
        }

        static void OnHit(DamageInfo di, CharacterBody a, CharacterBody v, Inventory inv)
        {
            if (di.damageColorIndex == DamageColorIndex.Void) return; // void effects don't chain into each other
            foreach (var t in new[] { Trig.Hit, Trig.Crit })
            {
                var list = Of(t); if (list == null) continue;
                foreach (var s in list)
                {
                    int n = Count(a, s); if (n <= 0) continue;
                    if (t == Trig.Crit ? !di.crit : !Util.CheckRoll(s.tp * di.procCoefficient, a.master)) continue;
                    Apply(s, n, a, v, v.corePosition, di.crit, di.procChainMask);
                }
            }
        }

        static void OnKill(DamageReport r)
        {
            var a = r.attackerBody; if (!a || !a.inventory || !r.victimBody) return;
            Fire(Trig.Kill, a, r.victimBody, r.victimBody.corePosition, 1f, false, default);
            if (r.victimBody.isElite) Fire(Trig.EliteKill, a, r.victimBody, r.victimBody.corePosition, 1f, false, default);
        }

        static readonly Dictionary<(CharacterBody, VoidSpec), float> lowHpReady = new Dictionary<(CharacterBody, VoidSpec), float>();
        static void OnHurt(HealthComponent hc, DamageInfo di, float before)
        {
            var b = hc.body; if (!b || !b.inventory || !hc.alive) return;
            var hurt = Of(Trig.Hurt);
            if (hurt != null && (di.damageType.damageType & DamageType.DoT) == 0)
                foreach (var s in hurt) { int n = Count(b, s); if (n > 0 && Util.CheckRoll(s.tp, b.master)) Apply(s, n, b, null, b.corePosition, b.RollCrit(), default); }
            var low = Of(Trig.LowHp);
            if (low != null && before >= 0.25f && hc.combinedHealthFraction < 0.25f)
                foreach (var s in low)
                {
                    int n = Count(b, s); if (n <= 0) continue;
                    if (lowHpReady.TryGetValue((b, s), out float t) && Time.time < t) continue;
                    lowHpReady[(b, s)] = Time.time + LowHpCooldown;
                    Apply(s, n, b, null, b.corePosition, b.RollCrit(), default);
                }
        }

        static void OnSkill(CharacterBody b, GenericSkill skill)
        {
            if (!NetworkServer.active || !b.skillLocator || !b.inventory) return;
            var sl = b.skillLocator;
            Trig t = skill == sl.primary ? Trig.Primary : skill == sl.secondary ? Trig.Secondary : skill == sl.utility ? Trig.Utility : skill == sl.special ? Trig.Special : Trig.None;
            var list = Of(t); if (list == null) return;
            foreach (var s in list) { int n = Count(b, s); if (n > 0 && Util.CheckRoll(s.tp, b.master)) Apply(s, n, b, null, b.corePosition, b.RollCrit(), default); }
        }

        static void Fire(Trig t, CharacterBody owner, CharacterBody victim, Vector3 pos, float procCoef, bool crit, ProcChainMask mask)
        {
            var list = Of(t); if (list == null || !owner.inventory) return;
            foreach (var s in list) { int n = Count(owner, s); if (n > 0) Apply(s, n, owner, victim, pos, crit || owner.RollCrit(), mask); }
        }

        internal static void Pulse(CharacterBody b, Dictionary<VoidSpec, float> timers)
        {
            var list = Of(Trig.Pulse); if (list == null) return;
            foreach (var s in list)
            {
                int n = Count(b, s); if (n <= 0) continue;
                timers.TryGetValue(s, out float t); t += Time.fixedDeltaTime;
                if (t >= s.tp) { t = 0; Apply(s, n, b, null, b.corePosition, b.RollCrit(), default); }
                timers[s] = t;
            }
        }

        // Vanilla void-item sounds per effect type, rate-limited per owner so fast procs don't stack into noise.
        static readonly Dictionary<string, string> voidSounds = new Dictionary<string, string>
        {
            { "collapse", "Play_item_void_bleedOnHit_start" }, { "blast", "Play_item_void_explodeOnDeath" }, { "lightning", "Play_item_void_chainLightning" },
            { "missile", "Play_item_void_missle_fire" }, { "root", "Play_item_void_slowOnHit" }, { "slow", "Play_item_void_slowOnHit" },
            { "rot", "Play_item_void_bleedOnHit_buildup" }, { "barrier", "Play_item_void_bear" }, { "heal", "Play_item_void_mushroom" },
            { "gold", "Play_item_void_treasureCache_open" }, { "cdr", "Play_item_void_equipmag" }, { "buff", "Play_item_void_critGlasses" },
        };
        static readonly Dictionary<(CharacterBody, string), float> lastSound = new Dictionary<(CharacterBody, string), float>();
        static void PlayVoidSound(string kind, CharacterBody owner, Vector3 pos)
        {
            if (!owner || !voidSounds.TryGetValue(kind, out var ev)) return;
            var key = (owner, kind);
            if (lastSound.TryGetValue(key, out var t) && Time.time - t < 0.35f) return;
            lastSound[key] = Time.time;
            if (lastSound.Count > 512) lastSound.Clear();
            Sfx.Play(ev, pos);
        }

        static void Apply(VoidSpec s, int n, CharacterBody owner, CharacterBody victim, Vector3 pos, bool crit, ProcChainMask mask)
        {
            var team = CIC.Team(owner);
            bool single = s.trig == Trig.Hit || s.trig == Trig.Crit;
            foreach (var e in s.effects)
            {
                float a = e.a * n;
                PlayVoidSound(e.kind, owner, single && victim ? victim.corePosition : pos);
                switch (e.kind)
                {
                    case "collapse":
                        foreach (var hc in Targets(single, victim, pos, team))
                            for (int i = 0; i < Mathf.RoundToInt(a); i++)
                            { var info = new InflictDotInfo { attackerObject = owner.gameObject, victimObject = hc.gameObject, dotIndex = DotController.DotIndex.Fracture, duration = 3f, damageMultiplier = 1f }; DotController.InflictDot(ref info); }
                        break;
                    case "blast":
                        { var ba = new BlastAttack { attacker = owner.gameObject, inflictor = owner.gameObject, teamIndex = team, position = pos, radius = e.b, baseDamage = owner.damage * a, crit = crit, procCoefficient = 0.5f, procChainMask = mask, falloffModel = BlastAttack.FalloffModel.None, damageColorIndex = DamageColorIndex.Void, damageType = DamageTypeCombo.Generic, attackerFiltering = AttackerFiltering.NeverHitSelf }; ba.Fire(); CIC.Fx(blastFx, pos, e.b); }
                        break;
                    case "lightning":
                        { var t = Seek(victim, pos, team); if (t) OrbManager.instance.AddOrb(new VoidLightningOrb { origin = owner.corePosition, damageValue = owner.damage * a, isCrit = crit, totalStrikes = Mathf.Max(1, (int)e.b), secondsPerStrike = 0.1f, teamIndex = team, attacker = owner.gameObject, procCoefficient = 0.3f, damageColorIndex = DamageColorIndex.Void, target = t }); }
                        break;
                    case "missile":
                        for (int i = 0; i < Mathf.Max(1, (int)e.b); i++)
                        { var t = Seek(victim, pos, team); if (t) OrbManager.instance.AddOrb(new MissileVoidOrb { origin = owner.corePosition + UnityEngine.Random.insideUnitSphere * 0.5f, damageValue = owner.damage * a, isCrit = crit, teamIndex = team, attacker = owner.gameObject, procChainMask = mask, procCoefficient = 0.3f, damageColorIndex = DamageColorIndex.Void, target = t }); }
                        break;
                    case "root":
                        foreach (var hc in Targets(single, victim, pos, team)) hc.body.AddTimedBuff(RoR2Content.Buffs.Entangle, a);
                        break;
                    case "slow":
                        foreach (var hc in Targets(single, victim, pos, team)) hc.body.AddTimedBuff(RoR2Content.Buffs.Slow50, a);
                        break;
                    case "rot":
                        foreach (var hc in Targets(single, victim, pos, team))
                            for (int i = 0; i < Mathf.Min(20, Mathf.RoundToInt(a)); i++) if (hc.body.GetBuffCount(rot) < 30) hc.body.AddTimedBuff(rot, e.b);
                        break;
                    case "barrier": owner.healthComponent.AddBarrier(owner.healthComponent.fullCombinedHealth * a); break;
                    case "heal": owner.healthComponent.HealFraction(a, default); break;
                    case "gold": if (owner.master) owner.master.GiveMoney((uint)Mathf.Max(1, Run.instance.GetDifficultyScaledCost(Mathf.RoundToInt(a)))); break;
                    case "cdr": if (owner.skillLocator) owner.skillLocator.DeductCooldownFromAllSkillsServer(a); break;
                    case "buff":
                        { var bd = buffs[e.stat]; int add = Mathf.RoundToInt(a); int cap = Mathf.Max(3 * n, add); for (int i = 0; i < add && owner.GetBuffCount(bd) < cap; i++) owner.AddTimedBuff(bd, e.b); }
                        break;
                }
            }
        }

        static IEnumerable<HealthComponent> Targets(bool single, CharacterBody victim, Vector3 pos, TeamIndex team)
        {
            if (single) { if (victim && victim.healthComponent && victim.healthComponent.alive) yield return victim.healthComponent; yield break; }
            foreach (var hb in CIC.Enemies(pos, AreaRadius, team)) if (hb.healthComponent && hb.healthComponent.alive && hb.healthComponent.body) yield return hb.healthComponent;
        }

        static HurtBox Seek(CharacterBody victim, Vector3 pos, TeamIndex team)
        {
            if (victim && victim.healthComponent && victim.healthComponent.alive && victim.mainHurtBox) return victim.mainHurtBox;
            HurtBox best = null; float bd = float.MaxValue;
            foreach (var hb in CIC.Enemies(pos, SeekRadius, team))
            {
                if (!hb.healthComponent || !hb.healthComponent.alive) continue;
                float d = (hb.transform.position - pos).sqrMagnitude;
                if (d < bd) { bd = d; best = hb.healthComponent.body ? hb.healthComponent.body.mainHurtBox : hb; }
            }
            return best;
        }
    }

    public class VoidPulse : MonoBehaviour
    {
        public CharacterBody body;
        readonly Dictionary<VoidSpec, float> timers = new Dictionary<VoidSpec, float>();
        void FixedUpdate() { if (body && body.healthComponent && body.healthComponent.alive) VoidSystem.Pulse(body, timers); }
    }
}
