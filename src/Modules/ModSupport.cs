using RoR2;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ClassicItemsReturns.Modules
{
    internal class ModSupport
    {
        internal static void CheckForModSupport()
        {
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(ModCompatRiskOfOptions.guid))
            {
                ModCompatRiskOfOptions.Init();
            }
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(ModCompatRiskyMod.guid))
            {
                ModCompatRiskyMod.Init();
            }
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(ModCompatAssistManager.guid))
            {
                ModCompatAssistManager.Init();
            }
            LinearDamage.loaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(LinearDamage.guid);
        }

        internal class LinearDamage
        {
            internal const string guid = "com.RiskyLives.LinearDamage";
            internal static bool loaded = false;
        }

        internal class ModCompatRiskOfOptions
        {
            internal const string guid = "com.rune580.riskofoptions";
            internal static bool loaded = false;

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            internal static void Init()
            {
                loaded = true;

                RiskOfOptions.ModSettingsManager.SetModDescription(ClassicItemsReturnsPlugin.ModDescription, ClassicItemsReturnsPlugin.ModGuid, ClassicItemsReturnsPlugin.ModName);
                var icon = RuntimeArt.Icon("ModIcon");
                if (icon) RiskOfOptions.ModSettingsManager.SetModIcon(icon, ClassicItemsReturnsPlugin.ModGuid, ClassicItemsReturnsPlugin.ModName);
            }

            /// <summary>
            /// Exposes every bound config entry in the in-game settings menu. Called after all items, artifacts and
            /// void items have bound their settings. Nearly everything is read at startup, so all options need a restart.
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            internal static void AddAll(BepInEx.Configuration.ConfigFile config)
            {
                if (!loaded) return;
                string guid = ClassicItemsReturnsPlugin.ModGuid, name = ClassicItemsReturnsPlugin.ModName;
                var entries = new System.Collections.Generic.List<BepInEx.Configuration.ConfigEntryBase>();
                foreach (var kv in config) entries.Add(kv.Value);
                foreach (var e in System.Linq.Enumerable.OrderBy(entries, x => Category(x.Definition) + x.Definition.Section + x.Definition.Key))
                {
                    try { Add(e, guid, name); }
                    catch (System.Exception ex) { ClassicItemsReturnsPlugin.ModLogger.LogWarning($"Risk of Options: skipped {e.Definition}: {ex.Message}"); }
                }
            }

            static string Category(BepInEx.Configuration.ConfigDefinition d)
            {
                string s = d.Section;
                if (s == "General" || s == "Void Items") return "General";
                if (s.StartsWith("Artifact: ")) return "Artifacts";
                if (s.StartsWith("Void: ")) return "Void Items";
                if (s.StartsWith("Lunar ")) return "Lunars";
                if (d.Key == "Enable Item?" || d.Key == "Enable Equipment?" || d.Key.StartsWith("Blacklist")) return "Items";
                return "Tuning";
            }

            static void Add(BepInEx.Configuration.ConfigEntryBase e, string guid, string modName)
            {
                var d = e.Definition; string cat = Category(d);
                string label = cat == "General" ? d.Key
                    : cat == "Void Items" ? d.Section.Substring(6)
                    : cat == "Items" && d.Key.StartsWith("Enable") ? StripPrefix(d.Section)
                    : StripPrefix(d.Section) + ": " + d.Key.TrimEnd('?');
                string desc = e.Description?.Description ?? "";
                RiskOfOptions.Options.BaseOption opt = null;
                switch (e)
                {
                    case BepInEx.Configuration.ConfigEntry<bool> b:
                        opt = new RiskOfOptions.Options.CheckBoxOption(b, new RiskOfOptions.OptionConfigs.CheckBoxConfig { name = label, description = desc, category = cat, restartRequired = true });
                        break;
                    case BepInEx.Configuration.ConfigEntry<float> f:
                    {
                        float def = System.Math.Abs((float)f.DefaultValue);
                        float max = Mathf.Max(1f, def * 5f), inc = max > 50 ? 1f : max > 5 ? 0.1f : 0.01f;
                        opt = new RiskOfOptions.Options.StepSliderOption(f, new RiskOfOptions.OptionConfigs.StepSliderConfig { name = label, description = desc, category = cat, restartRequired = true, min = 0f, max = max, increment = inc, formatString = inc >= 1 ? "{0:0}" : inc >= 0.1f ? "{0:0.0}" : "{0:0.00}" });
                        break;
                    }
                    case BepInEx.Configuration.ConfigEntry<int> i:
                        opt = new RiskOfOptions.Options.IntSliderOption(i, new RiskOfOptions.OptionConfigs.IntSliderConfig { name = label, description = desc, category = cat, restartRequired = true, min = 0, max = Mathf.Max(10, (int)i.DefaultValue * 5) });
                        break;
                    case BepInEx.Configuration.ConfigEntry<string> str:
                        opt = new RiskOfOptions.Options.StringInputFieldOption(str, new RiskOfOptions.OptionConfigs.InputFieldConfig { name = label, description = desc, category = cat, restartRequired = true });
                        break;
                }
                if (opt != null) RiskOfOptions.ModSettingsManager.AddOption(opt, guid, modName);
            }

            static string StripPrefix(string section)
            {
                foreach (var p in new[] { "Item: ", "Equipment: ", "Artifact: ", "Void: " }) if (section.StartsWith(p)) return section.Substring(p.Length);
                return section;
            }
        }

        internal class ModCompatRiskyMod
        {
            internal const string guid = "com.RiskyLives.RiskyMod";
            internal static bool loaded = false;

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            internal static void Init()
            {
                loaded = true;
            }

            internal static bool IsAtgEnabled()
            {
                if (!loaded) return false;
                return IsAtgEnabledInternal();
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            private static bool IsAtgEnabledInternal()
            {
                return RiskyMod.Items.ItemsCore.enabled && RiskyMod.Items.Uncommon.AtG.enabled;
            }

            internal static BuffDef GetFreezeDebuff()
            {
                if (!loaded) return null;
                return GetFreezeDebuffInternal();
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            private static BuffDef GetFreezeDebuffInternal()
            {
                return RiskyMod.Tweaks.CharacterMechanics.FreezeChampionExecute.FreezeDebuff;
            }
        }

        internal class ModCompatAssistManager
        {
            internal const string guid = "com.Moffein.AssistManager";
            internal static bool loaded = false;

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            internal static void Init()
            {
                loaded = true;
            }
        }
    }
}