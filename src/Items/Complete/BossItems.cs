using R2API;
using ClassicItemsReturns.Modules;
using RoR2;
using RoR2.Orbs;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace ClassicItemsReturns.Items.Complete
{
    /// <summary>
    /// Boss items only drop from the bosses they belong to. RoR1/Returns bosses that don't exist in
    /// RoR2 are mapped onto the closest RoR2 boss, and the item is added to that boss's drop table.
    /// </summary>
    public static class BossDrops
    {
        static readonly List<(string body, Object pickup)> pending = new List<(string, Object)>();
        static bool hooked;
        public static void Add(string bodyName, Object itemOrEquipmentDef)
        {
            pending.Add((bodyName, itemOrEquipmentDef));
            if (hooked) return;
            hooked = true;
            RoR2Application.onLoad += () =>
            {
                foreach (var (body, pickup) in pending)
                {
                    var prefab = BodyCatalog.FindBodyPrefab(body);
                    var rewards = prefab ? prefab.GetComponent<DeathRewards>() : null;
                    if (!rewards) { ClassicItemsReturnsPlugin.ModLogger.LogWarning("BossDrops: no DeathRewards on " + body); continue; }
                    if (rewards.bossDropTable is ExplicitPickupDropTable table)
                    {
                        var list = table.pickupEntries?.ToList() ?? new List<ExplicitPickupDropTable.PickupDefEntry>();
                        list.Add(new ExplicitPickupDropTable.PickupDefEntry { pickupDef = pickup, pickupWeight = 1f });
                        table.pickupEntries = list.ToArray();
                    }
                    else
                    {
                        var t = ScriptableObject.CreateInstance<ExplicitPickupDropTable>();
                        t.pickupEntries = new[] { new ExplicitPickupDropTable.PickupDefEntry { pickupDef = pickup, pickupWeight = 1f } };
                        rewards.bossDropTable = t;
                    }
                }
            };
        }
    }

    public abstract class BossItem<T> : SimpleItem<T> where T : BossItem<T>
    {
        public override ItemTier Tier => ItemTier.Boss;
        protected abstract string DropsFrom { get; }
        public override void Init(BepInEx.Configuration.ConfigFile config)
        {
            base.Init(config);
            if (ItemDef) BossDrops.Add(DropsFrom, ItemDef);
        }
    }

    public class LegendarySpark : BossItem<LegendarySpark>
    {
        public override string ItemName => "Legendary Spark";
        public override string ItemLangTokenName => "CIC_LEGENDARYSPARK";
        protected override string DropsFrom => "ElectricWormBody";
        public override ItemTag[] ItemTags => new[] { ItemTag.Damage, ItemTag.WorldUnique };
        float chance, dmg;
        public override string PickupText => "Smite them. Smite them all.";
        public override string DescriptionText => $"{Dmg(chance + "%")} chance on hit to call down {Dmg("2")} {Stack("+1")} lightning strikes for {Dmg(Pct(dmg))} TOTAL damage each.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { chance = Cfg("Chance", 10f); dmg = Cfg("Damage Per Strike", 2.5f); }
        public override void Hooks()
        {
            CICHooks.onHitServer += (di, a, v, inv) =>
            {
                int c = GetCount(a);
                if (c <= 0 || di.procChainMask.HasProc(ProcType.LightningStrikeOnHit) || !Util.CheckRoll(chance * di.procCoefficient, a.master) || !v.mainHurtBox) return;
                var mask = di.procChainMask; mask.AddProc(ProcType.LightningStrikeOnHit);
                for (int i = 0; i < c + 1; i++)
                    OrbManager.instance.AddOrb(new SimpleLightningStrikeOrb { attacker = a.gameObject, damageValue = Util.OnHitProcDamage(di.damage, a.damage, dmg), isCrit = di.crit, procChainMask = mask, procCoefficient = 0.5f, damageColorIndex = DamageColorIndex.Item, target = v.mainHurtBox });
            };
        }
    }

    public class ImpTentacle : BossItem<ImpTentacle>
    {
        public override string ItemName => "Imp Overlord's Tentacle";
        public override string ItemLangTokenName => "CIC_IMPTENTACLE";
        protected override string DropsFrom => "ImpBossBody";
        public override ItemTag[] ItemTags => new[] { ItemTag.Utility, ItemTag.WorldUnique, ItemTag.AIBlacklist, ItemTag.CannotCopy };
        public static float respawn, respawnStack, boost;
        public static GameObject impMaster;
        public override string PickupText => "Summon an imp bodyguard.";
        public override string DescriptionText => $"Summon an {Ut("imp bodyguard")} that inherits nothing but loyalty. It returns {Ut(respawn + "s")} {Stack("-" + respawnStack + "s")} after dying. Each stack gives it {Dmg("+" + Pct(boost) + " health and damage")}.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { respawn = Cfg("Respawn Time", 30f); respawnStack = Cfg("Respawn Reduction Per Stack", 5f); boost = Cfg("Boost Per Stack", 0.15f); }
        public override void Hooks()
        {
            impMaster = CIC.Load<GameObject>("RoR2/Base/Imp/ImpMaster.prefab");
            Sfx.Register("Play_imp_overlord_teleport_end");
            CharacterBody.onBodyInventoryChangedGlobal += b => { if (NetworkServer.active && b.isPlayerControlled) b.AddItemBehavior<Behavior>(GetCount(b)); };
        }
        public class Behavior : CharacterBody.ItemBehavior
        {
            CharacterMaster minion; float timer = 2f;
            void FixedUpdate()
            {
                if (!body || !impMaster) return;
                if (minion && minion.hasBody) { SyncBoost(); return; }
                timer -= Time.fixedDeltaTime;
                if (timer > 0) return;
                timer = Mathf.Max(10f, respawn - respawnStack * (stack - 1));
                minion = new MasterSummon { masterPrefab = impMaster, position = body.footPosition + body.transform.forward * -3f, rotation = body.transform.rotation, summonerBodyObject = body.gameObject, ignoreTeamMemberLimit = true, useAmbientLevel = true }.Perform();
                if (minion) Sfx.Play("Play_imp_overlord_teleport_end", body.footPosition);
                SyncBoost();
            }
            void SyncBoost()
            {
                if (!minion || !minion.inventory) return;
                int want = Mathf.RoundToInt(boost * 10f * stack);
                minion.inventory.ResetItem(RoR2Content.Items.BoostHp); minion.inventory.GiveItem(RoR2Content.Items.BoostHp, want);
                minion.inventory.ResetItem(RoR2Content.Items.BoostDamage); minion.inventory.GiveItem(RoR2Content.Items.BoostDamage, want);
            }
            void OnDisable() { if (minion && NetworkServer.active) minion.TrueKill(); }
        }
    }

    public class BurningWitness : BossItem<BurningWitness>
    {
        public override string ItemName => "Burning Witness";
        public override string ItemLangTokenName => "CIC_BURNINGWITNESS";
        protected override string DropsFrom => "MagmaWormBody";
        public override ItemTag[] ItemTags => new[] { ItemTag.Utility, ItemTag.Damage, ItemTag.OnKillEffect, ItemTag.WorldUnique };
        float speed, dur, durStack, dps;
        static BuffDef buff;
        public override string PickupText => "The worm's eye still watches. Kills set your path ablaze.";
        public override string DescriptionText => $"Killing an enemy grants a {Dmg("fire trail")} ({Dmg(Pct(dps))} damage per second) and {Ut("+" + Pct(speed) + " movement speed")} for {Ut(dur + "s")} {Stack("+" + durStack + "s")}.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { speed = Cfg("Speed Bonus", 0.3f); dur = Cfg("Duration", 6f); durStack = Cfg("Duration Per Stack", 2f); dps = Cfg("Trail Damage Per Second", 1.5f); }
        public override void Hooks()
        {
            buff = CIC.Buff("BurningWitness", new Color(1f, 0.5f, 0.2f), false, false, "RoR2/Base/Common/texMovespeedBuffIcon.tif");
            Sfx.Register("Play_item_proc_igniteOnKill");
            RecalculateStatsAPI.GetStatCoefficients += (b, args) => { if (b.HasBuff(buff)) args.moveSpeedMultAdd += speed; };
            CICHooks.onKillServer += r =>
            {
                var a = r.attackerBody; int c = GetCount(a);
                if (c <= 0) return;
                if (!a.HasBuff(buff)) Sfx.Play("Play_item_proc_igniteOnKill", a.corePosition);
                a.AddTimedBuff(buff, dur + durStack * (c - 1));
            };
            // Trail follows the (synced) buff so every client sees it.
            On.RoR2.CharacterBody.OnBuffFirstStackGained += (orig, self, def) =>
            {
                orig(self, def);
                if (def != buff) return;
                var t = self.gameObject.AddComponent<CICFireTrail>(); t.body = self; t.key = "witness"; t.damagePerSecondCoefficient = dps;
            };
            On.RoR2.CharacterBody.OnBuffFinalStackLost += (orig, self, def) =>
            {
                orig(self, def);
                if (def != buff) return;
                var t = self.GetComponents<CICFireTrail>().FirstOrDefault(x => x.key == "witness");
                if (t) Object.Destroy(t);
            };
        }
    }

    public class IfritsHorn : BossItem<IfritsHorn>
    {
        public override string ItemName => "Ifrit's Horn";
        public override string ItemLangTokenName => "CIC_IFRITSHORN";
        protected override string DropsFrom => "GrandParentBody";
        public override ItemTag[] ItemTags => new[] { ItemTag.Damage, ItemTag.WorldUnique };
        float chance, dmg;
        public override string PickupText => "Chance on hit to unleash a burst of flame.";
        public override string DescriptionText => $"{Dmg(chance + "%")} chance on hit to unleash a {Dmg("wave of fire")} that {Dmg("ignites")} enemies within {Dmg("10m")} for {Dmg(Pct(dmg))} {Stack("+" + Pct(dmg))} TOTAL damage.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { chance = Cfg("Chance", 10f); dmg = Cfg("Damage", 3f); }
        static GameObject fx;
        public override void Hooks()
        {
            fx = CIC.Load<GameObject>("RoR2/Base/FireballsOnHit/FireMeatBallExplosion.prefab") ?? CIC.Explosion;
            Sfx.Register("Play_magmaWorm_M1_explo");
            CICHooks.onHitServer += (di, a, v, inv) =>
            {
                int c = GetCount(a);
                if (c <= 0 || di.procChainMask.HasProc(ProcType.Behemoth) || !Util.CheckRoll(chance * di.procCoefficient, a.master)) return;
                var mask = di.procChainMask; mask.AddProc(ProcType.Behemoth);
                float total = Util.OnHitProcDamage(di.damage, a.damage, dmg * c) / a.damage;
                CIC.Blast(a, v.corePosition, 10f, total, 0.5f, DamageType.IgniteOnHit, fx, di.crit, mask);
                Sfx.Play("Play_magmaWorm_M1_explo", v.corePosition);
            };
        }
    }

    public class ScorchingShell : BossItem<ScorchingShell>
    {
        public override string ItemName => "Scorching Shell Piece";
        public override string ItemLangTokenName => "CIC_SCORCHINGSHELL";
        protected override string DropsFrom => "BeetleQueen2Body";
        public override ItemTag[] ItemTags => new[] { ItemTag.Damage, ItemTag.WorldUnique };
        float chance, dmg;
        public override string PickupText => "Getting hit can fling molten shards at nearby enemies.";
        public override string DescriptionText => $"{Dmg(chance + "%")} chance when hurt to fling {Dmg("3")} {Stack("+1")} molten shards at nearby enemies, each dealing {Dmg(Pct(dmg))} damage.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { chance = Cfg("Chance", 20f); dmg = Cfg("Damage Per Shard", 1.5f); }
        public override void Hooks()
        {
            Sfx.Register("Play_beetle_queen_attack2_shoot");
            CICHooks.onPostDamageServer += (hc, di, before) =>
            {
                var b = hc.body; int c = GetCount(b);
                if (c <= 0 || !hc.alive || !Util.CheckRoll(chance, b.master)) return;
                Sfx.Play("Play_beetle_queen_attack2_shoot", b.corePosition);
                var targets = CIC.Enemies(b.corePosition, 35f, CIC.Team(b));
                if (targets.Count == 0) return;
                for (int i = 0; i < c + 2; i++)
                    OrbManager.instance.AddOrb(new DevilOrb { origin = b.corePosition, damageValue = b.damage * dmg, teamIndex = CIC.Team(b), attacker = b.gameObject, procCoefficient = 0.5f, effectType = DevilOrb.EffectType.Wisp, isCrit = b.RollCrit(), target = targets[i % targets.Count] });
            };
        }
    }
}
