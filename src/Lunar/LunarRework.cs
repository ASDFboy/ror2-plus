using BepInEx.Configuration;
using ClassicItemsReturns.Items.Complete;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace ClassicItemsReturns.Lunar
{
    /// <summary>All lunar tuning in one place, bound from the config ("Lunar Rework" / "Lunar Acquisition").</summary>
    public static class LunarSettings
    {
        public static bool rework, acquisition;
        static readonly Dictionary<string, bool> items = new Dictionary<string, bool>();
        public static bool Enabled(string key) => rework && items.TryGetValue(key, out var on) && on && Modules.Expansion.Active;
        /// <summary>Acquisition rules apply only while the ror2-plus expansion is on for the run.</summary>
        public static bool AcquisitionActive => acquisition && Modules.Expansion.Active;

        // rework values
        public static float glassHealthKeep, transcendenceHealthKeep, lightFluxPerStack, stoneFluxSlowPerStack, purityLuckPerStack;
        public static float neutroniumStatPenalty, neutroniumStatPenaltyPerStack; public static int neutroniumArmor, neutroniumArmorPerStack;
        public static float crownLossBase, crownLossPerStack, gougeCreditMult, corpsebloomCap, corpsebloomCapLossPerStack, egoInterval;
        public static float convergenceShrinkPerStack, tonicAfflictionChance, heresyPenaltyPerStack, rachisEnemyBonus, solitudeCostPerStack;
        public static float meteorAllyDamage, helfireSelfBurn, effigyAllySlow, gestureRange;
        // acquisition values
        public static int podCost; public static float bazaarCostMult, coinDropMult, podSpawnMult; public static bool bazaarOnly, tradeIn;

        public static readonly string[] Keys = { "BrittleCrown", "Corpsebloom", "DefiantGouge", "FocusedConvergence", "Gesture", "Heresy", "MercurialRachis",
            "Purity", "ShapedGlass", "Transcendence", "LightFlux", "StoneFlux", "Egocentrism", "LongstandingSolitude", "Neutronium",
            "GlowingMeteorite", "HelfireTincture", "EffigyOfGrief", "SpinelTonic" };

        public static void Bind(ConfigFile cfg)
        {
            const string R = "Lunar Rework", A = "Lunar Acquisition";
            rework = cfg.Bind(R, "Enable Lunar Rework", true, "Softer downsides for lunar items and equipment (each one keeps its identity). Turn off to restore vanilla behaviour.").Value;
            foreach (var k in Keys) items[k] = cfg.Bind(R, "Rework " + k, true, "Use the softer version of this lunar.").Value;
            glassHealthKeep = cfg.Bind(R, "Shaped Glass Health Kept Per Stack", 0.7f, "Vanilla 0.5: each stack keeps this fraction of max health.").Value;
            transcendenceHealthKeep = cfg.Bind(R, "Transcendence Health Kept", 0.1f, "Vanilla 0 (1 HP): fraction of max health that stays as health.").Value;
            lightFluxPerStack = cfg.Bind(R, "Light Flux Attack Speed Loss Per Stack", 0.25f, "Vanilla 1.0: attack speed is divided by (1 + this x stacks).").Value;
            stoneFluxSlowPerStack = cfg.Bind(R, "Stone Flux Slow Per Stack", 0.33f, "Vanilla 1.0+: movement speed is divided by (1 + this x stacks).").Value;
            purityLuckPerStack = cfg.Bind(R, "Purity Bad Luck Per Stack", 0.5f, "Vanilla 1: unlucky rerolls per stack. Fractions roll as a chance (0.5 = half the time).").Value;
            neutroniumStatPenalty = cfg.Bind(R, "Neutronium Stat Penalty", 0.15f, "Vanilla 0.3").Value;
            neutroniumStatPenaltyPerStack = cfg.Bind(R, "Neutronium Stat Penalty Per Stack", 0.05f, "Vanilla 0.1").Value;
            neutroniumArmor = cfg.Bind(R, "Neutronium Armor Penalty", 20, "Vanilla 35").Value;
            neutroniumArmorPerStack = cfg.Bind(R, "Neutronium Armor Penalty Per Stack", 10, "Vanilla 15").Value;
            crownLossBase = cfg.Bind(R, "Brittle Crown Gold Loss", 0.5f, "Vanilla 1.0").Value;
            crownLossPerStack = cfg.Bind(R, "Brittle Crown Gold Loss Per Stack", 0.25f, "Vanilla 1.0").Value;
            gougeCreditMult = cfg.Bind(R, "Defiant Gouge Monster Strength", 0.6f, "Vanilla 1.0").Value;
            corpsebloomCap = cfg.Bind(R, "Corpsebloom Heal Cap", 0.15f, "Vanilla 0.10 of max health per second").Value;
            corpsebloomCapLossPerStack = cfg.Bind(R, "Corpsebloom Cap Loss Per Stack", 0.5f, "Vanilla 1.0 (cap / stacks)").Value;
            egoInterval = cfg.Bind(R, "Egocentrism Conversion Interval", 120f, "Vanilla 60 seconds").Value;
            convergenceShrinkPerStack = cfg.Bind(R, "Focused Convergence Shrink Per Stack", 0.33f, "Vanilla 1.0: zone radius / (1 + this x stacks)").Value;
            tonicAfflictionChance = cfg.Bind(R, "Spinel Tonic Affliction Chance", 10f, "Vanilla 20%").Value;
            heresyPenaltyPerStack = cfg.Bind(R, "Heresy Cooldown Increase Per Extra Stack", 0.5f, "Vanilla 1.0 (each extra stack adds a full base cooldown)").Value;
            rachisEnemyBonus = cfg.Bind(R, "Mercurial Rachis Enemy Damage Bonus", 0.25f, "Vanilla 0.5 (allies keep +50%)").Value;
            solitudeCostPerStack = cfg.Bind(R, "Longstanding Solitude Cost Increase Per Stack", 0.25f, "Vanilla 0.5").Value;
            meteorAllyDamage = cfg.Bind(R, "Glowing Meteorite Damage To Allies", 0.25f, "Vanilla 1.0").Value;
            helfireSelfBurn = cfg.Bind(R, "Helfire Tincture Self Burn", 0.5f, "Multiplier on the burn you and allies take. Enemies are unaffected.").Value;
            effigyAllySlow = cfg.Bind(R, "Effigy Of Grief Ally Slow", 0.25f, "Vanilla 0.5; allies also keep their armor").Value;
            gestureRange = cfg.Bind(R, "Gesture Enemy Range", 60f, "Gesture only auto-fires when an enemy is within this many metres.").Value;

            acquisition = cfg.Bind(A, "Harder Lunar Acquisition", true, "Master switch for every setting in this section.").Value;
            podCost = cfg.Bind(A, "Lunar Pod Cost", 2, "Lunar coins per Lunar Pod. Vanilla 1.").Value;
            bazaarCostMult = cfg.Bind(A, "Bazaar Lunar Cost Multiplier", 1.5f, "Multiplier on Bazaar lunar shop prices (rounded up).").Value;
            coinDropMult = cfg.Bind(A, "Lunar Coin Drop Chance Multiplier", 0.6f, "Multiplier on the chance enemies drop lunar coins.").Value;
            podSpawnMult = cfg.Bind(A, "Lunar Pod Spawn Multiplier", 0.5f, "Multiplier on how often Lunar Pods appear on stages.").Value;
            bazaarOnly = cfg.Bind(A, "Bazaar Only", false, "No Lunar Pods on stages at all; lunars come from the Bazaar and special sources only.").Value;
            tradeIn = cfg.Bind(A, "Trade-In Cost", false, "Taking a lunar item or equipment also costs you one random white or green item (if you have any).").Value;
        }
    }

    public static class LunarRework
    {
        static readonly List<IDisposable> hooks = new List<IDisposable>();
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public static void Init(ConfigFile cfg)
        {
            LunarSettings.Bind(cfg);
            if (LunarSettings.rework) InstallRework();
            if (LunarSettings.acquisition) InstallAcquisition();
            if (LunarSettings.rework) RoR2Application.onLoad += Descriptions;
        }

        static void IL(Type t, string method, string label, params Func<ILContext, bool, bool>[] patches)
        {
            var mi = t?.GetMethods(All).Where(m => m.Name == method && m.GetMethodBody() != null).OrderByDescending(m => m.GetMethodBody().GetILAsByteArray().Length).FirstOrDefault();
            if (mi == null) { ClassicItemsReturnsPlugin.ModLogger.LogWarning($"Lunar rework: {t?.Name}.{method} not found; {label} stays vanilla."); return; }
            try
            {
                hooks.Add(new ILHook(mi, il =>
                {
                    foreach (var p in patches)
                    {
                        bool ok; try { ok = p(il, true); } catch (Exception e) { ok = false; ClassicItemsReturnsPlugin.ModLogger.LogWarning($"Lunar rework {p.Method.Name}: {e.Message}"); }
                        if (!ok) ClassicItemsReturnsPlugin.ModLogger.LogWarning($"Lunar rework: couldn't find {p.Method.Name}'s spot in this game version; it stays vanilla.");
                    }
                }));
            }
            catch (Exception e) { ClassicItemsReturnsPlugin.ModLogger.LogError($"Lunar rework: hooking {t.Name}.{method} failed: {e}"); }
        }

        static void InstallRework()
        {
            IL(typeof(CharacterBody), "RecalculateStats", "stat lunars", LunarPatches.ShapedGlass, LunarPatches.Transcendence, LunarPatches.LightFlux,
               LunarPatches.StoneFlux, LunarPatches.Purity, LunarPatches.Neutronium);
            IL(typeof(HealthComponent), "TakeDamageProcess", "Brittle Crown", LunarPatches.BrittleCrown);
            IL(typeof(GlobalEventManager), "OnInteractionBegin", "Defiant Gouge", LunarPatches.DefiantGouge);
            IL(typeof(HealthComponent).GetNestedType("RepeatHealComponent", All), "RecalculateStats", "Corpsebloom", LunarPatches.Corpsebloom);
            IL(typeof(LunarSunBehavior), "FixedUpdate", "Egocentrism", LunarPatches.Egocentrism);
            IL(typeof(HoldoutZoneController).GetNestedType("FocusConvergenceController", All), "ApplyRadius", "Focused Convergence", LunarPatches.FocusedConvergence);
            IL(typeof(EquipmentSlot), "FireTonic", "Spinel Tonic", LunarPatches.SpinelTonic);
            IL(typeof(EquipmentSlot), "MyFixedUpdate", "Gesture", LunarPatches.Gesture);

            // Purity: fractional bad luck becomes a chance of one extra unlucky reroll
            // (vanilla rounds fractional luck up; only negative luck is changed, so other sources of positive luck are untouched)
            if (LunarSettings.Enabled("Purity"))
                On.RoR2.Util.CheckRoll_float_float_CharacterMaster += (orig, pct, luck, master) =>
                {
                    if (luck >= 0f) return orig(pct, luck, master);
                    float a = -luck, whole = Mathf.Floor(a);
                    if (a == whole) return orig(pct, luck, master);
                    return orig(pct, -(whole + (UnityEngine.Random.value < a - whole ? 1f : 0f)), master);
                };
            // Heresy: extra stacks raise the cooldown by heresyPenaltyPerStack x base instead of a full base each
            On.RoR2.Skills.LunarPrimaryReplacementSkill.GetRechargeInterval += (orig, self, slot) => Heresy(orig(self, slot), self.baseRechargeInterval);
            On.RoR2.Skills.LunarSecondaryReplacementSkill.GetRechargeInterval += (orig, self, slot) => Heresy(orig(self, slot), self.baseRechargeInterval);
            On.RoR2.Skills.LunarDetonatorSkill.GetRechargeInterval += (orig, self, slot) => Heresy(orig(self, slot), self.baseRechargeInterval);
            // Egocentrism: only white and green items get converted
            On.RoR2.LunarSunBehavior.TransformationFilter += (orig, args) =>
            {
                var r = orig(args);
                if (r == ItemIndex.None || !LunarSettings.Enabled("Egocentrism")) return r;
                var d = ItemCatalog.GetItemDef(args.itemIndex);
                return d && (d.tier == ItemTier.Tier1 || d.tier == ItemTier.Tier2) ? r : ItemIndex.None;
            };
            // Longstanding Solitude: hook the public cost function (the tiny scale helper it calls can be inlined by the JIT)
            On.RoR2.TeamManager.AdjustCostForLongstandingSolitude += (orig, costType, cost, viewer) =>
            {
                if (!LunarSettings.Enabled("LongstandingSolitude") || costType != CostTypeIndex.Money || cost == 0) return orig(costType, cost, viewer);
                if (viewer && viewer.HasBuff(DLC2Content.Buffs.FreeUnlocks)) return 0;
                return (int)(cost * (1f + TeamManager.LongstandingSolitudesInParty() * LunarSettings.solitudeCostPerStack));
            };
            On.RoR2.HelfireController.Awake += (orig, self) =>
            {
                orig(self);
                if (!LunarSettings.Enabled("HelfireTincture")) return;
                // lower the base burn, then raise the enemy multiplier so enemies take exactly what they did before
                float k = Mathf.Clamp(LunarSettings.helfireSelfBurn, 0.05f, 1f);
                self.healthFractionPerSecond *= k; self.enemyDamageScalar /= k;
            };
            RecalculateStatsAPI.GetStatCoefficients += (body, args) =>
            {
                // Mercurial Rachis: enemies get a smaller share of the ward's +50% damage
                if (LunarSettings.Enabled("MercurialRachis") && body.HasBuff(RoR2Content.Buffs.PowerBuff) && body.teamComponent && body.teamComponent.teamIndex != TeamIndex.Player)
                    args.damageMultAdd -= 0.5f - LunarSettings.rachisEnemyBonus;
                // Effigy of Grief: your own team is slowed less and keeps its armor
                if (LunarSettings.Enabled("EffigyOfGrief") && body.HasBuff(RoR2Content.Buffs.Cripple) && body.teamComponent && body.teamComponent.teamIndex == TeamIndex.Player && EffigyTag.Near(body.corePosition))
                {
                    float s = Mathf.Clamp(LunarSettings.effigyAllySlow, 0f, 0.9f);
                    args.moveSpeedReductionMultAdd -= 1f - s / (1f - s);
                    args.armorAdd += 20f;
                }
            };
            var ward = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<GameObject>("RoR2/Base/CrippleWard/CrippleWard.prefab").WaitForCompletion();
            if (ward && !ward.GetComponent<EffigyTag>()) ward.AddComponent<EffigyTag>();
            // Glowing Meteorite: much less damage to you and your team
            CICHooks.onPreDamageServer += (hc, di, attackerBody) =>
            {
                if (!LunarSettings.Enabled("GlowingMeteorite") || !di.inflictor || !di.inflictor.GetComponent<MeteorStormController>()) return;
                var victimTeam = hc.body && hc.body.teamComponent ? hc.body.teamComponent.teamIndex : TeamIndex.None;
                var attackerTeam = attackerBody && attackerBody.teamComponent ? attackerBody.teamComponent.teamIndex : TeamIndex.Player;
                if (victimTeam == attackerTeam) di.damage *= LunarSettings.meteorAllyDamage;
            };
        }

        static float Heresy(float vanilla, float baseInterval)
        {
            if (!LunarSettings.Enabled("Heresy") || baseInterval <= 0f) return vanilla;
            float stacks = Mathf.Round(vanilla / baseInterval);
            return stacks <= 1f ? vanilla : baseInterval * (1f + LunarSettings.heresyPenaltyPerStack * (stacks - 1f));
        }

        // ---------------------------------------------------------------- Gesture of the Drowned
        static readonly HashSet<string> gestureNever = new HashSet<string> { "Meteor", "BurnNearby", "CrippleWard", "Recycle", "GoldGat", "LunarPortalOnUse", "Gateway", "BossHunter", "MultiShopCard" };
        static readonly Dictionary<EquipmentSlot, (float t, bool ok)> gestureCache = new Dictionary<EquipmentSlot, (float, bool)>();

        /// <summary>Gesture still forces activation, but holds dangerous/wasteful equipment and waits while no enemy is near.</summary>
        public static int GestureCount(int count, EquipmentSlot slot)
        {
            if (count <= 0 || !LunarSettings.Enabled("Gesture") || !slot) return count;
            var def = EquipmentCatalog.GetEquipmentDef(slot.equipmentIndex);
            if (def && gestureNever.Contains(def.name)) return 0;
            if (gestureCache.TryGetValue(slot, out var c) && Time.time - c.t < 0.25f) return c.ok ? count : 0;
            bool ok = false; var body = slot.characterBody;
            if (body && body.teamComponent)
            {
                float r2 = LunarSettings.gestureRange * LunarSettings.gestureRange;
                foreach (var tc in TeamComponent.GetTeamMembers(TeamIndex.Monster).Concat(TeamComponent.GetTeamMembers(TeamIndex.Void)).Concat(TeamComponent.GetTeamMembers(TeamIndex.Lunar)))
                    if (tc && tc.teamIndex != body.teamComponent.teamIndex && tc.body && tc.body.healthComponent && tc.body.healthComponent.alive && (tc.body.corePosition - body.corePosition).sqrMagnitude < r2) { ok = true; break; }
            }
            if (gestureCache.Count > 64) gestureCache.Clear();
            gestureCache[slot] = (Time.time, ok);
            return ok ? count : 0;
        }

        // ---------------------------------------------------------------- acquisition
        static readonly MethodInfo cloneCard = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
        static bool IsPod(DirectorCard c) => c != null && c.spawnCard && c.spawnCard.name.IndexOf("LunarChest", StringComparison.OrdinalIgnoreCase) >= 0;

        static void InstallAcquisition()
        {
            On.RoR2.PurchaseInteraction.Awake += (orig, self) =>
            {
                orig(self);
                if (!NetworkServer.active || self.costType != CostTypeIndex.LunarCoin || !LunarSettings.AcquisitionActive) return;
                if (self.name.StartsWith("LunarChest")) self.Networkcost = Mathf.Max(0, LunarSettings.podCost);
                else if (self.name.StartsWith("LunarShopTerminal")) self.Networkcost = Mathf.CeilToInt(self.cost * LunarSettings.bazaarCostMult);
            };
            SceneDirector.onGenerateInteractableCardSelection += (director, dccs) =>
            {
                if (!dccs || dccs.categories == null || !LunarSettings.AcquisitionActive) return;
                // The selection's card arrays are copies but the DirectorCards inside are the stage's shared assets,
                // so cards are cloned before editing (otherwise the change would compound every stage).
                // Weights are ints: the category is scaled x10 so a weight-1 pod can still be halved.
                foreach (var cat in dccs.categories)
                {
                    var cards = cat.cards; if (cards == null) continue;
                    if (!cards.Any(IsPod)) continue;
                    for (int i = 0; i < cards.Length; i++)
                    {
                        if (cards[i] == null) continue;
                        var copy = (DirectorCard)cloneCard.Invoke(cards[i], null);
                        copy.selectionWeight = IsPod(cards[i])
                            ? (LunarSettings.bazaarOnly ? 0 : Mathf.Max(1, Mathf.RoundToInt(cards[i].selectionWeight * 10f * LunarSettings.podSpawnMult)))
                            : cards[i].selectionWeight * 10;
                        cards[i] = copy;
                    }
                }
            };
            var pcmc = typeof(PlayerCharacterMasterController);
            foreach (var t in new[] { pcmc }.Concat(pcmc.GetNestedTypes(All)))
                foreach (var m in t.GetMethods(All).Where(x => x.Name.Contains("<Init>") && x.GetMethodBody() != null))
                    try { hooks.Add(new ILHook(m, il => LunarPatches.CoinDrop(il, true))); } catch { }
            if (LunarSettings.tradeIn)
                On.RoR2.GenericPickupController.AttemptGrant += (orig, self, body) =>
                {
                    var pickup = PickupCatalog.GetPickupDef(self.pickupIndex);
                    orig(self, body);
                    if (!NetworkServer.active || !self.consumed || pickup == null || !pickup.isLunar || !body || !body.inventory || !LunarSettings.AcquisitionActive) return;
                    if (pickup.itemIndex == ItemIndex.None && pickup.equipmentIndex == EquipmentIndex.None) return; // coins and other misc pickups
                    var pool = body.inventory.itemAcquisitionOrder.Where(i =>
                    {
                        var d = ItemCatalog.GetItemDef(i);
                        return d && d.canRemove && !d.hidden && (d.tier == ItemTier.Tier1 || d.tier == ItemTier.Tier2) && body.inventory.GetItemCountPermanent(i) > 0;
                    }).ToList();
                    if (pool.Count == 0) return;
                    var white = pool.Where(i => ItemCatalog.GetItemDef(i).tier == ItemTier.Tier1).ToList();
                    var take = (white.Count > 0 ? white : pool)[UnityEngine.Random.Range(0, white.Count > 0 ? white.Count : pool.Count)];
                    body.inventory.RemoveItemPermanent(take, 1);
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = "<style=cIsLunar>The moon takes its price: {0}</style>", paramTokens = new[] { Language.GetString(ItemCatalog.GetItemDef(take).nameToken) } });
                };
        }

        // ---------------------------------------------------------------- logbook / pickup text
        static void Descriptions()
        {
            string P(float f) => (f * 100f).ToString("0.#") + "%";
            void D(string key, string token, string text) { if (LunarSettings.Enabled(key)) LanguageAPI.Add(token, text); }
            var S = LunarSettings.Keys;
            D("BrittleCrown", "ITEM_GOLDONHIT_DESC", $"<style=cIsDamage>30%</style> chance on hit to gain <style=cIsUtility>2</style> <style=cStack>(+2 per stack)</style> gold. <style=cIsUtility>Scales over time.</style>\n\nOn taking damage, lose gold equal to <style=cIsHealth>{P(LunarSettings.crownLossBase)}</style> <style=cStack>(+{P(LunarSettings.crownLossPerStack)} per stack)</style> of the maximum health percentage you lost.");
            D("Corpsebloom", "ITEM_REPEATHEAL_DESC", $"<style=cIsHealing>Heal +100%</style> <style=cStack>(+100% per stack)</style> more. <style=cIsHealing>All healing is applied over time</style>. Can heal for a maximum of <style=cIsHealing>{P(LunarSettings.corpsebloomCap)}</style> of your <style=cIsHealing>health per second</style>, reduced with extra stacks.");
            D("DefiantGouge", "ITEM_MONSTERSONSHRINEUSE_DESC", $"Using a Shrine summons <style=cIsHealth>enemies</style> nearby ({P(LunarSettings.gougeCreditMult)} of the vanilla strength). <style=cIsUtility>Scales over time.</style>");
            D("FocusedConvergence", "ITEM_FOCUSEDCONVERGENCE_DESC", $"Teleporters charge <style=cIsUtility>30%</style> <style=cStack>(+30% per stack)</style> <style=cIsUtility>faster</style>, but the size of the Teleporter zone is <style=cIsHealth>{P(1f - 1f / (1f + LunarSettings.convergenceShrinkPerStack))}</style> smaller, more with stacks.");
            D("Gesture", "ITEM_AUTOCASTEQUIPMENT_DESC", $"<style=cIsUtility>Reduce Equipment cooldown</style> by <style=cIsUtility>50%</style> <style=cStack>(+15% per stack)</style>. Forces your Equipment to <style=cIsUtility>activate</style> whenever it is off cooldown and an enemy is within {LunarSettings.gestureRange:0}m. Never forces equipment that would harm you or waste a one-time use.");
            D("MercurialRachis", "ITEM_RANDOMDAMAGEZONE_DESC", $"Creates a Ward of Power in a random location nearby that buffs both enemies and allies within <style=cIsUtility>16m</style> <style=cStack>(+50% per stack)</style>, causing allies to deal <style=cIsDamage>+50% damage</style> and enemies <style=cIsHealth>+{P(LunarSettings.rachisEnemyBonus)}</style>.");
            D("Purity", "ITEM_LUNARBADLUCK_DESC", $"All skill cooldowns are reduced by <style=cIsUtility>2</style> <style=cStack>(+1 per stack)</style> seconds. Random effects have a <style=cIsHealth>{P(LunarSettings.purityLuckPerStack)}</style> <style=cStack>(+{P(LunarSettings.purityLuckPerStack)} per stack)</style> chance to be rolled again for an unfavorable outcome.");
            D("ShapedGlass", "ITEM_LUNARDAGGER_DESC", $"Increase base damage by <style=cIsDamage>100%</style> <style=cStack>(+100% per stack)</style>. Reduce maximum health by <style=cIsHealth>{P(1f - LunarSettings.glassHealthKeep)}</style> <style=cStack>(+{P(1f - LunarSettings.glassHealthKeep)} per stack, multiplicative)</style>.");
            D("Transcendence", "ITEM_SHIELDONLY_DESC", $"Convert all but <style=cIsHealing>{P(LunarSettings.transcendenceHealthKeep)} of your health</style> into <style=cIsHealing>regenerating shields</style>. Gain <style=cIsHealing>50% <style=cStack>(+25% per stack)</style> maximum health</style>.");
            D("LightFlux", "ITEM_HALFATTACKSPEEDHALFCOOLDOWNS_DESC", $"<style=cIsUtility>Decrease skill cooldowns by 50%</style> <style=cStack>(+50% per stack)</style>. <style=cIsHealth>Decrease attack speed by {P(1f - 1f / (1f + LunarSettings.lightFluxPerStack))}</style>, more with stacks.");
            D("StoneFlux", "ITEM_HALFSPEEDDOUBLEHEALTH_DESC", $"<style=cIsHealing>Increase max health by 100%</style> <style=cStack>(+100% per stack)</style>. <style=cIsHealth>Reduce movement speed by {P(1f - 1f / (1f + LunarSettings.stoneFluxSlowPerStack))}</style>, more with stacks.");
            D("Egocentrism", "ITEM_LUNARSUN_DESC", $"Every <style=cIsUtility>3</style> <style=cStack>(-50% per stack)</style> seconds, gain an <style=cIsDamage>orbiting bomb</style> that detonates on impact for <style=cIsDamage>360%</style> damage, up to a maximum of <style=cIsUtility>3 <style=cStack>(+1 per stack)</style> bombs</style>. Every <style=cIsHealth>{LunarSettings.egoInterval:0}</style> seconds, a random <style=cIsHealth>white or green item</style> is <style=cIsHealth>converted</style> into this item.");
            D("LongstandingSolitude", "ITEM_ONLEVELUPFREEUNLOCK_DESC", $"On level up gain a <style=cIsUtility>free unlock</style> for the next purchase <style=cStack>(+1 per stack)</style> but all <style=cIsHealth>gold is converted to experience</style>. Anything that costs gold is increased by <style=cIsHealth>{P(LunarSettings.solitudeCostPerStack)}</style> <style=cStack>(+{P(LunarSettings.solitudeCostPerStack)} per stack)</style>.");
            D("Neutronium", "ITEM_TRANSFERDEBUFFONHIT_DESC", $"<style=cIsHealth>Reduces your stats while in combat</style>, but <style=cIsUtility>send this item to an enemy on hit</style>. On death, this item will be sent to its holder's killer.\n\nReduces damage, attack speed and movement speed by <style=cIsHealth>{P(LunarSettings.neutroniumStatPenalty)}</style> <style=cStack>(+{P(LunarSettings.neutroniumStatPenaltyPerStack)} per stack)</style>, and armor by <style=cIsHealth>{LunarSettings.neutroniumArmor}</style> <style=cStack>(+{LunarSettings.neutroniumArmorPerStack} per stack)</style>.");
            D("GlowingMeteorite", "EQUIPMENT_METEOR_DESC", $"Rain meteors from the sky, dealing <style=cIsDamage>600% damage</style> per blast to enemies and <style=cIsHealth>{P(LunarSettings.meteorAllyDamage)}</style> of that to you and your allies. Lasts 20 seconds.");
            D("HelfireTincture", "EQUIPMENT_BURNNEARBY_DESC", $"Ignite ALL characters within 15m. Deal <style=cIsHealth>{P(0.05f * LunarSettings.helfireSelfBurn)} of your maximum health/second as burning</style> to yourself. Allies burn at half that; enemies burn as fiercely as ever.");
            D("EffigyOfGrief", "EQUIPMENT_CRIPPLEWARD_DESC", $"ALL characters within are <style=cIsUtility>slowed</style>: enemies by 50% with their <style=cIsDamage>armor reduced by 20</style>, you and your allies only by <style=cIsHealth>{P(LunarSettings.effigyAllySlow)}</style>. Can place up to 5.");
            D("SpinelTonic", "EQUIPMENT_TONIC_DESC", $"Drink the Tonic, gaining a boost for 20 seconds. Increases <style=cIsDamage>damage</style> by <style=cIsDamage>+100%</style>. Increases <style=cIsDamage>attack speed</style> by <style=cIsDamage>+70%</style>. Increases <style=cIsHealing>armor</style> by <style=cIsHealing>+20</style>. Increases <style=cIsHealing>maximum health</style> by <style=cIsHealing>+50%</style>. Increases <style=cIsHealing>passive health regeneration</style> by <style=cIsHealing>+300%</style>. Increases <style=cIsUtility>movespeed</style> by <style=cIsUtility>+30%</style>.\n\nWhen the Tonic wears off, you have a <style=cIsHealth>{LunarSettings.tonicAfflictionChance:0}%</style> chance to gain a <style=cIsHealth>Tonic Affliction</style>, reducing all of your stats by <style=cIsHealth>-5%</style> <style=cStack>(-5% per stack)</style>.");
        }
    }

    /// <summary>Marks Effigy of Grief wards so allies standing in one can be told apart from other cripple sources.</summary>
    public class EffigyTag : MonoBehaviour
    {
        static readonly List<EffigyTag> live = new List<EffigyTag>();
        BuffWard ward;
        void OnEnable() { live.Add(this); ward = GetComponent<BuffWard>(); }
        void OnDisable() { live.Remove(this); }
        public static bool Near(Vector3 p)
        {
            foreach (var t in live) if (t && (t.transform.position - p).sqrMagnitude <= Mathf.Pow((t.ward ? t.ward.radius : 16f) + 2f, 2f)) return true;
            return false;
        }
    }
}
