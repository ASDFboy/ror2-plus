using BepInEx.Configuration;
using ClassicItemsReturns.Equipment;
using ClassicItemsReturns.Modules;
using R2API;
using RoR2;
using UnityEngine;

namespace ClassicItemsReturns.Items.Complete
{
    /// <summary>
    /// Base for items added by ror2-plus. Text lives in code (LanguageAPI) and art is
    /// loaded from embedded PNGs, so each item is a single self-contained file.
    /// </summary>
    public abstract class SimpleItem<T> : ItemBase<T> where T : SimpleItem<T>
    {
        /// <summary>Embedded icon name (art/icons/{ArtName}.png).</summary>
        public virtual string ArtName => GetType().Name;
        public abstract string PickupText { get; }
        public abstract string DescriptionText { get; }
        public virtual string LoreText => Lore.Item(GetType().Name);

        public override GameObject ItemModel => RuntimeArt.PickupModel(ArtName);
        public override Sprite ItemIcon => RuntimeArt.Icon(ArtName);
        public override ItemDisplayRuleDict CreateItemDisplayRules() { Displays.Register(ArtName, ItemDef, RuntimeArt.PickupModel(ArtName)); return new ItemDisplayRuleDict(); }

        protected ConfigFile cfg;
        protected float Cfg(string key, float value, string desc = "") => cfg.Bind(ConfigCategory, key, value, desc).Value;
        protected int Cfg(string key, int value, string desc = "") => cfg.Bind(ConfigCategory, key, value, desc).Value;

        public override void Init(ConfigFile config)
        {
            cfg = config;
            CreateConfig(config);
            CreateItem();
            // Text is built after config so numbers always match the player's settings.
            string p = LanguageOverrides.LanguageTokenPrefixItem + ItemLangTokenName;
            LanguageAPI.Add(p + "_NAME", ItemName);
            LanguageAPI.Add(ItemPickupToken, PickupText);
            LanguageAPI.Add(ItemDescriptionToken, DescriptionText);
            LanguageAPI.Add(ItemLoreToken, LoreText);
            Hooks();
        }

        // Shorthand for description markup.
        protected static string Dmg(object s) => $"<style=cIsDamage>{s}</style>";
        protected static string Heal(object s) => $"<style=cIsHealing>{s}</style>";
        protected static string Ut(object s) => $"<style=cIsUtility>{s}</style>";
        protected static string Stack(object s) => $"<style=cStack>({s} per stack)</style>";
        protected static string Pct(float f) => (f * 100f).ToString("0.#") + "%";
    }

    public abstract class SimpleEquipment<T> : EquipmentBase<T> where T : SimpleEquipment<T>
    {
        public virtual string ArtName => GetType().Name;
        public abstract string PickupText { get; }
        public abstract string DescriptionText { get; }
        public virtual string LoreText => Lore.Item(GetType().Name);

        public override GameObject EquipmentModel => RuntimeArt.PickupModel(ArtName);
        public override Sprite EquipmentIcon => RuntimeArt.Icon(ArtName);
        public override ItemDisplayRuleDict CreateItemDisplayRules() { Displays.Register(ArtName, EquipmentDef, RuntimeArt.PickupModel(ArtName)); return new ItemDisplayRuleDict(); }

        protected ConfigFile cfg;
        protected float Cfg(string key, float value, string desc = "") => cfg.Bind(ConfigCategory, key, value, desc).Value;

        public override void Init(ConfigFile config)
        {
            cfg = config;
            base.Init(config);
            string p = LanguageOverrides.LanguageTokenPrefixEquipment + EquipmentLangTokenName;
            LanguageAPI.Add(p + "_NAME", EquipmentName);
            LanguageAPI.Add(EquipmentPickupToken, PickupText);
            LanguageAPI.Add(EquipmentDescriptionToken, DescriptionText);
            LanguageAPI.Add(p + "_LORE", LoreText);
            foreach (var ev in ExtraSounds) Sfx.Register(ev);
            if (UseSound != null && EquipmentDef)
            {
                Sfx.Register(UseSound);
                useSounds[EquipmentDef] = UseSound;
                if (!useHooked)
                {
                    useHooked = true;
                    On.RoR2.EquipmentSlot.PerformEquipmentAction += (orig, self, def) =>
                    {
                        bool ok = orig(self, def);
                        if (ok && def && self.characterBody && useSounds.TryGetValue(def, out var s)) Sfx.Play(s, self.characterBody.corePosition);
                        return ok;
                    };
                }
            }
        }

        /// <summary>Vanilla sound event played (for everyone) whenever this equipment fires successfully.</summary>
        protected virtual string UseSound => null;
        /// <summary>Other vanilla sound events this equipment plays through Sfx.Play.</summary>
        protected virtual string[] ExtraSounds => new string[0];
        static readonly System.Collections.Generic.Dictionary<EquipmentDef, string> useSounds = new System.Collections.Generic.Dictionary<EquipmentDef, string>();
        static bool useHooked;

        protected static string Dmg(object s) => $"<style=cIsDamage>{s}</style>";
        protected static string Heal(object s) => $"<style=cIsHealing>{s}</style>";
        protected static string Ut(object s) => $"<style=cIsUtility>{s}</style>";
        protected static string Pct(float f) => (f * 100f).ToString("0.#") + "%";
    }
}
