using R2API;
using ClassicItemsReturns.Modules;
using RoR2;
using RoR2.Orbs;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using BepInEx.Configuration;

namespace ClassicItemsReturns.Items.Complete
{
    public class RottenBrain : SimpleEquipment<RottenBrain>
    {
        protected override string UseSound => "Play_item_use_molotov_throw";
        public override string EquipmentName => "Rotten Brain";
        public override string EquipmentLangTokenName => "CIC_ROTTENBRAIN";
        public override float Cooldown => 30f;
        float dmg;
        public override string PickupText => "Throw a bouncing brain.";
        public override string DescriptionText => $"Throw a brain that bounces in place {Dmg("6")} times, dealing {Dmg(Pct(dmg))} damage and {Ut("slowing")} enemies within {Dmg("8m")} each bounce.";
        protected override void CreateConfig(ConfigFile config) { dmg = Cfg("Damage Per Bounce", 2f); }
        static GameObject marker;
        public override void Hooks() { marker = NetMarker.Make("Brain", new Color(0.75f, 0.85f, 0.45f), 1.4f, 2.5f, true, "RottenBrain", 0.7f); }
        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            var b = slot.characterBody; var ray = slot.GetAimRay();
            Vector3 p = Physics.Raycast(ray, out var hit, 60f, LayerIndex.world.mask) ? hit.point : ray.GetPoint(30f);
            var go = NetMarker.Spawn(marker, p + Vector3.up * 0.5f);
            var r = go.AddComponent<Bouncer>(); r.owner = b; r.coefficient = dmg;
            return true;
        }
        public class Bouncer : MonoBehaviour
        {
            public CharacterBody owner; public float coefficient; int left = 6; float t;
            void FixedUpdate()
            {
                t -= Time.fixedDeltaTime;
                if (t > 0) return;
                t = 0.5f;
                if (!owner || left-- <= 0) { NetMarker.Kill(gameObject); return; }
                CIC.Blast(owner, transform.position, 8f, coefficient, 1f, DamageType.SlowOnHit, CIC.ExplosionSmall, owner.RollCrit());
            }
        }
    }

    public class SafeguardLantern : SimpleEquipment<SafeguardLantern>
    {
        protected override string UseSound => "Play_item_use_hellfire_start";
        public override string EquipmentName => "Safeguard Lantern";
        public override string EquipmentLangTokenName => "CIC_LANTERN";
        public override float Cooldown => 45f;
        public override string PickupText => "Drop a lantern that terrifies and burns nearby enemies.";
        public override string DescriptionText => $"Drop a lantern for {Ut("10s")}. Every second it {Ut("terrifies")} enemies within {Ut("15m")} and burns them for {Dmg("100%")} damage.";
        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            var go = NetMarker.Spawn(marker, slot.characterBody.footPosition + Vector3.up);
            go.AddComponent<Lantern>().owner = slot.characterBody;
            return true;
        }
        static GameObject marker;
        public override void Hooks() { marker = NetMarker.Make("Lantern", new Color(1f, 0.75f, 0.35f), 2f, 0.15f, false, "SafeguardLantern", 0.9f, 15f); }
        public class Lantern : MonoBehaviour
        {
            public CharacterBody owner; float life = 10f, t;
            void FixedUpdate()
            {
                life -= Time.fixedDeltaTime; t -= Time.fixedDeltaTime;
                if (life <= 0 || !owner) { NetMarker.Kill(gameObject); return; }
                if (t > 0) return;
                t = 1f;
                CIC.Blast(owner, transform.position, 15f, 1f, 0.3f, DamageType.IgniteOnHit);
                foreach (var hb in CIC.Enemies(transform.position, 15f, CIC.Team(owner))) CIC.Terrify(hb.healthComponent, transform.position, 1.2f);
            }
        }
    }

    public class Snowglobe : SimpleEquipment<Snowglobe>
    {
        protected override string[] ExtraSounds => new[] { "Play_item_proc_iceRingSpear" };
        public override string EquipmentName => "Snowglobe";
        public override string EquipmentLangTokenName => "CIC_SNOWGLOBE";
        public override float Cooldown => 45f;
        public override string PickupText => "Summon a snowstorm that freezes enemies.";
        public override string DescriptionText => $"Summon a snowstorm for {Ut("7s")}. Every second, each enemy within {Ut("40m")} has a {Ut("50%")} chance to be {Ut("frozen")} for {Ut("2s")}. Enemies that can't be frozen are slowed instead.";
        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            slot.characterBody.gameObject.AddComponent<Storm>().owner = slot.characterBody;
            Sfx.Play("Play_item_proc_iceRingSpear", slot.gameObject);
            return true;
        }
        public class Storm : MonoBehaviour
        {
            public CharacterBody owner; float life = 7f, t;
            void FixedUpdate()
            {
                life -= Time.fixedDeltaTime; t -= Time.fixedDeltaTime;
                if (life <= 0 || !owner) { Destroy(this); return; }
                if (t > 0) return;
                t = 1f;
                foreach (var hb in CIC.Enemies(owner.corePosition, 40f, CIC.Team(owner)))
                {
                    var hc = hb.healthComponent; if (!hc || !hc.alive || Random.value > 0.5f) continue;
                    var ssoh = hc.GetComponent<SetStateOnHurt>();
                    if (ssoh && ssoh.canBeFrozen) ssoh.SetFrozen(2f); else hc.body.AddTimedBuff(RoR2Content.Buffs.Slow50, 2f);
                }
            }
        }
    }

    public class ExplorersKey : SimpleEquipment<ExplorersKey>
    {
        protected override string UseSound => "Play_item_void_treasureCache_open";
        public override string EquipmentName => "Explorer's Key";
        public override string EquipmentLangTokenName => "CIC_EXPLORERSKEY";
        public override float Cooldown => 160f;
        public override bool EnigmaCompatible => false;
        public override string PickupText => "Unlock all nearby chests.";
        public override string DescriptionText => $"{Ut("Open")} every chest within {Ut("20m")} for free. Does not work on vaults, terminals or lunar pods.";
        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            var b = slot.characterBody; var interactor = b.GetComponent<Interactor>();
            if (!interactor) return false;
            int opened = 0;
            foreach (var pi in Object.FindObjectsOfType<PurchaseInteraction>())
            {
                if (!pi.available || pi.costType != CostTypeIndex.Money || !pi.GetComponent<ChestBehavior>()) continue;
                if ((pi.transform.position - b.corePosition).sqrMagnitude > 20f * 20f) continue;
                pi.Networkcost = 0;
                pi.OnInteractionBegin(interactor);
                Unlocks.UnlockTracker.NoteExplorersKeyChest(b.master, pi.gameObject);
                opened++;
            }
            return opened > 0;
        }
    }

    public class InstantMinefield : SimpleEquipment<InstantMinefield>
    {
        protected override string UseSound => "Play_item_use_droneDropDynamite_spawn";
        public override string EquipmentName => "Instant Minefield";
        public override string EquipmentLangTokenName => "CIC_MINEFIELD";
        public override float Cooldown => 45f;
        float dmg;
        public override string PickupText => "Drop a cluster of mines at your feet.";
        public override string DescriptionText => $"Drop {Dmg("7")} mines around your feet, each exploding for {Dmg(Pct(dmg))} damage.";
        protected override void CreateConfig(ConfigFile config) { dmg = Cfg("Damage Per Mine", 4f); }
        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            var b = slot.characterBody;
            for (int i = 0; i < 7; i++)
            {
                float a = i / 7f * Mathf.PI * 2f;
                CIC.FireEngiMine(b, b.footPosition + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (i == 0 ? 0 : 3f), dmg, DamageTypeCombo.Generic);
            }
            return true;
        }
    }

    public class CarraraMarble : SimpleEquipment<CarraraMarble>
    {
        protected override string[] ExtraSounds => new[] { "Play_item_proc_crit_cooldown", "Play_item_use_gateway_rideEnd" };
        public override string EquipmentName => "Carrara Marble";
        public override string EquipmentLangTokenName => "CIC_CARRARAMARBLE";
        public override float Cooldown => 30f;
        public override bool EnigmaCompatible => false;
        public override bool CanBeRandomlyTriggered => false;
        static readonly Dictionary<CharacterMaster, (Vector3 pos, GameObject marker)> gates = new Dictionary<CharacterMaster, (Vector3, GameObject)>();
        public override string PickupText => "Place a marble gate. Activate again to return to it.";
        public override string DescriptionText => $"Place a {Ut("marble gate")} (no cooldown). Activate again to {Ut("teleport back")} to it. Gates are cleared between stages.";
        static GameObject gatePrefab;
        public override void Hooks() { Stage.onServerStageBegin += _ => gates.Clear(); gatePrefab = NetMarker.Make("Gate", new Color(0.85f, 0.9f, 1f), 2.5f, 0.2f, false, "CarraraMarble", 1.3f, 6f); }
        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            var b = slot.characterBody; var m = b.master;
            if (m && gates.TryGetValue(m, out var g))
            {
                gates.Remove(m);
                NetMarker.Kill(g.marker);
                TeleportHelper.TeleportBody(b, g.pos, false);
                Sfx.Play("Play_item_use_gateway_rideEnd", g.pos);
                CIC.Fx(CIC.ExplosionSmall, g.pos, 4f);
                return true;
            }
            if (m && gates.TryGetValue(m, out var old)) NetMarker.Kill(old.marker);
            var marker = NetMarker.Spawn(gatePrefab, b.footPosition + Vector3.up * 1.5f);
            if (m) gates[m] = (b.footPosition, marker);
            Sfx.Play("Play_item_proc_crit_cooldown", b.gameObject);
            return false;
        }
    }

    public class ShatteredMirror : SimpleEquipment<ShatteredMirror>
    {
        protected override string UseSound => "Play_item_use_gummyClone_spawn";
        public override string EquipmentName => "Shattered Mirror";
        public override string EquipmentLangTokenName => "CIC_MIRROR";
        public override float Cooldown => 100f;
        float dur;
        public override string PickupText => "Create a shadow of yourself.";
        public override string DescriptionText => $"Summon a {Ut("shadow")} of yourself that carries {Ut("all of your items")} and fights alongside you for {Ut(dur + "s")}.";
        protected override void CreateConfig(ConfigFile config) { dur = Cfg("Duration", 15f); }
        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            var ghost = Util.TryToCreateGhost(slot.characterBody, slot.characterBody, Mathf.RoundToInt(dur));
            return ghost;
        }
    }

    public class GoldPlatedBomb : SimpleEquipment<GoldPlatedBomb>
    {
        protected override string UseSound => "Play_item_proc_moneyOnKill_spawn";
        protected override string[] ExtraSounds => new[] { "Play_item_use_molotov_impact_big" };
        public override string EquipmentName => "Gold-Plated Bomb";
        public override string EquipmentLangTokenName => "CIC_GOLDBOMB";
        public override float Cooldown => 45f;
        public override bool CanBeRandomlyTriggered => false;
        float perChunk;
        public override string PickupText => "Turn half your money into an explosion.";
        public override string DescriptionText => $"Spend {Ut("50%")} of your gold to detonate a bomb, dealing {Dmg(Pct(perChunk))} damage for every {Ut("$25")} spent (scales with time) to enemies within {Dmg("16m")}.";
        protected override void CreateConfig(ConfigFile config) { perChunk = Cfg("Damage Per $25", 1f); }
        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            var b = slot.characterBody; var m = b.master;
            if (!m || m.money < 2) return false;
            uint spent = m.money / 2; m.money -= spent;
            Unlocks.UnlockTracker.NoteGoldBomb(m);
            float chunk = Mathf.Max(1, Run.instance.GetDifficultyScaledCost(25));
            CIC.Blast(b, b.corePosition, 16f, perChunk * spent / chunk, 1f, null, CIC.Explosion, b.RollCrit(), force: 1500f);
            Sfx.Play("Play_item_use_molotov_impact_big", b.gameObject);
            return true;
        }
    }

    public class DynamitePlunger : SimpleEquipment<DynamitePlunger>
    {
        protected override string UseSound => "Play_item_use_droneDropDynamite_explosion";
        public override string EquipmentName => "Dynamite Plunger";
        public override string EquipmentLangTokenName => "CIC_DYNAMITEPLUNGER";
        public override float Cooldown => 20f;
        public override bool CanBeRandomlyTriggered => false;
        float chance, dmg; const int max = 20;
        static GameObject stickPrefab;
        static readonly Dictionary<CharacterBody, List<GameObject>> sticks = new Dictionary<CharacterBody, List<GameObject>>();
        public override string PickupText => "Hitting enemies drops dynamite. Activate to detonate it all.";
        public override string DescriptionText => $"While held, hits have a {Dmg(chance + "%")} chance to drop {Dmg("dynamite")} (up to {max}). Activate to detonate all of it, each stick dealing {Dmg(Pct(dmg))} damage within {Dmg("6m")}.";
        protected override void CreateConfig(ConfigFile config) { chance = Cfg("Drop Chance", 20f); dmg = Cfg("Damage Per Stick", 3f); }
        public override void Hooks()
        {
            CICHooks.onHitServer += (di, a, v, inv) =>
            {
                if (inv.currentEquipmentIndex != EquipmentDef.equipmentIndex || !Util.CheckRoll(chance * di.procCoefficient, a.master)) return;
                if (!sticks.TryGetValue(a, out var list)) sticks[a] = list = new List<GameObject>();
                list.RemoveAll(x => !x);
                if (list.Count >= max) return;
                var s = NetMarker.Spawn(stickPrefab, v.footPosition + Vector3.up * 0.3f);
                list.Add(s);
            };
            Stage.onServerStageBegin += _ => sticks.Clear();
            stickPrefab = NetMarker.Make("Dynamite", new Color(1f, 0.35f, 0.2f), 0.7f, 0.08f, false, null, 1f, 3f);
        }
        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            var b = slot.characterBody;
            if (!sticks.TryGetValue(b, out var list) || list.Count == 0) return false;
            foreach (var s in list) if (s) { CIC.Blast(b, s.transform.position, 6f, dmg, 1f, null, CIC.ExplosionSmall, b.RollCrit()); NetMarker.Kill(s); }
            list.Clear();
            return true;
        }
    }

    public class MaceReplica : SimpleEquipment<MaceReplica>
    {
        protected override string UseSound => "Play_item_use_sawmerang_throw";
        protected override string[] ExtraSounds => new[] { "Play_loader_m1_impact" };
        public override string EquipmentName => "Mace Replica";
        public override string EquipmentLangTokenName => "CIC_MACE";
        public override float Cooldown => 30f;
        float dmg;
        public override string PickupText => "Swing a heavy mace that is very likely to trigger your on-hit items.";
        public override string DescriptionText => $"Swing a mace for {Dmg(Pct(dmg))} damage, knocking enemies away. Hits have a {Dmg("300% proc coefficient")}, making on-hit items far more likely to trigger.";
        protected override void CreateConfig(ConfigFile config) { dmg = Cfg("Damage", 2.5f); }
        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            var b = slot.characterBody; var ray = slot.GetAimRay();
            CIC.Blast(b, b.corePosition + ray.direction * 4f, 6f, dmg, 3f, null, CIC.Explosion, b.RollCrit(), force: 4000f, fxScale: 4f);
            Sfx.Play("Play_loader_m1_impact", b.gameObject);
            return true;
        }
    }

    public class CrudelyDrawnBuddy : SimpleEquipment<CrudelyDrawnBuddy>
    {
        protected override string UseSound => "Play_item_proc_squidTurret_spawn";
        public override string EquipmentName => "Crudely-Drawn Buddy";
        public override string EquipmentLangTokenName => "CIC_BUDDY";
        public override float Cooldown => 45f;
        static GameObject turretMaster;
        public override string PickupText => "Drop a decoy that draws enemy attention, then explodes.";
        public override string DescriptionText => $"Drop a sturdy {Ut("decoy")} for {Ut("8s")} that {Ut("taunts")} enemies within {Ut("50m")}. When it expires it explodes for {Dmg("400%")} damage.";
        public override void Hooks() { turretMaster = CIC.Load<GameObject>("RoR2/Base/Engi/EngiTurretMaster.prefab"); }
        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            var b = slot.characterBody;
            if (!turretMaster) return false;
            var m = new MasterSummon { masterPrefab = turretMaster, position = b.footPosition + b.transform.forward * 2f, rotation = b.transform.rotation, summonerBodyObject = b.gameObject, ignoreTeamMemberLimit = true, useAmbientLevel = true }.Perform();
            if (!m) return false;
            m.inventory.GiveItem(RoR2Content.Items.BoostHp, 40);
            m.inventory.GiveItem(RoR2Content.Items.BoostDamage, 0);
            var d = m.gameObject.AddComponent<Decoy>(); d.master = m; d.owner = b;
            return true;
        }
        public class Decoy : MonoBehaviour
        {
            public CharacterMaster master; public CharacterBody owner; float life = 8f, t;
            void FixedUpdate()
            {
                if (!master) { Destroy(this); return; }
                var body = master.GetBody();
                life -= Time.fixedDeltaTime; t -= Time.fixedDeltaTime;
                if (life <= 0 || !body)
                {
                    if (body && owner) CIC.Blast(owner, body.corePosition, 12f, 4f, 1f, null, CIC.Explosion, owner.RollCrit());
                    master.TrueKill(); Destroy(this); return;
                }
                if (t > 0) return;
                t = 1f;
                foreach (var ai in InstanceTracker.GetInstancesList<RoR2.CharacterAI.BaseAI>())
                {
                    var aib = ai.body;
                    if (!aib || CIC.Team(aib) == CIC.Team(body) || (aib.corePosition - body.corePosition).sqrMagnitude > 50f * 50f) continue;
                    ai.currentEnemy.gameObject = body.gameObject;
                    ai.currentEnemy.bestHurtBox = body.mainHurtBox;
                }
            }
        }
    }

    public class ShieldGenerator : SimpleEquipment<ShieldGenerator>
    {
        protected override string UseSound => "Play_item_use_gainArmor";
        public override string EquipmentName => "Shield Generator";
        public override string EquipmentLangTokenName => "CIC_SHIELDGEN";
        public override float Cooldown => 60f;
        float dur;
        public override string PickupText => "Become invulnerable for a few seconds.";
        public override string DescriptionText => $"Become {Ut("invulnerable")} for {Ut(dur + "s")}.";
        protected override void CreateConfig(ConfigFile config) { dur = Cfg("Duration", 6f); }
        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            slot.characterBody.AddTimedBuff(RoR2Content.Buffs.Immune, dur);
            return true;
        }
    }

    public class UnstableWatch : SimpleEquipment<UnstableWatch>
    {
        protected override string UseSound => "Play_item_use_blackhole_start";
        protected override string[] ExtraSounds => new[] { "Play_item_proc_crit_cooldown" };
        public override string EquipmentName => "Unstable Watch";
        public override string EquipmentLangTokenName => "CIC_WATCH";
        public override float Cooldown => 90f;
        float dur;
        public override string PickupText => "Stop time.";
        public override string DescriptionText => $"{Ut("Stop time")} for enemies within {Ut("100m")} for {Ut(dur + "s")}: they are frozen in place and their projectiles vanish.";
        protected override void CreateConfig(ConfigFile config) { dur = Cfg("Duration", 5f); }
        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            TimeStop.Stop(slot.characterBody.corePosition, 100f, CIC.Team(slot.characterBody), dur);
            Sfx.Play("Play_item_proc_crit_cooldown", slot.gameObject);
            return true;
        }
    }

    public class PillagedGold : SimpleEquipment<PillagedGold>
    {
        protected override string UseSound => "Play_item_proc_goldOnHurt";
        public override string EquipmentName => "Pillaged Gold";
        public override string EquipmentLangTokenName => "CIC_PILLAGEDGOLD";
        public override float Cooldown => 45f;
        static BuffDef buff;
        public override string PickupText => "For a short time, every hit earns gold.";
        public override string DescriptionText => $"For {Ut("14s")}, every hit earns {Ut("$1")} of gold (scales with time and proc coefficient).";
        public override void Hooks()
        {
            buff = CIC.Buff("PillagedGold", new Color(1f, 0.85f, 0.2f), false, false, "RoR2/Base/CritOnUse/texBuffFullCritIcon.tif");
            CICHooks.onHitServer += (di, a, v, inv) =>
            {
                if (!a.HasBuff(buff) || !a.master) return;
                a.master.GiveMoney((uint)Mathf.Max(1, Mathf.RoundToInt(Run.instance.GetDifficultyScaledCost(1) * di.procCoefficient)));
            };
        }
        protected override bool ActivateEquipment(EquipmentSlot slot) { slot.characterBody.AddTimedBuff(buff, 14f); return true; }
    }

    public class CaptainsBrooch : SimpleEquipment<CaptainsBrooch>
    {
        protected override string UseSound => "Play_item_use_radio";
        public override string EquipmentName => "Captain's Brooch";
        public override string EquipmentLangTokenName => "CIC_CAPTAINSBROOCH";
        public override float Cooldown => 135f;
        public override bool CanBeRandomlyTriggered => false;
        static SpawnCard chest;
        public override string PickupText => "Call down a chest. It costs more than usual.";
        public override string DescriptionText => $"Call down a {Ut("small chest")} nearby. Its price is {Ut("doubled")}.";
        public override void Hooks() { chest = CIC.Load<SpawnCard>("RoR2/Base/Chest1/iscChest1.asset"); }
        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            if (!chest || !DirectorCore.instance) return false;
            var b = slot.characterBody;
            var go = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(chest, new DirectorPlacementRule { placementMode = DirectorPlacementRule.PlacementMode.Approximate, position = b.footPosition, minDistance = 3f, maxDistance = 15f }, RoR2Application.rng));
            if (!go) return false;
            var pi = go.GetComponent<PurchaseInteraction>();
            if (pi) pi.Networkcost = pi.cost * 2;
            CIC.Fx(CIC.ExplosionSmall, go.transform.position, 4f);
            return true;
        }
    }

    public class SuperMassiveLeech : SimpleEquipment<SuperMassiveLeech>
    {
        protected override string UseSound => "Play_item_proc_crit_heal";
        public override string EquipmentName => "Super Massive Leech";
        public override string EquipmentLangTokenName => "CIC_LEECH";
        public override float Cooldown => 45f;
        float heal;
        static BuffDef buff;
        public override string PickupText => "For a short time, every hit heals you.";
        public override string DescriptionText => $"For {Ut("10s")}, every hit {Heal("heals")} you for {Heal(Pct(heal))} of your maximum health.";
        protected override void CreateConfig(ConfigFile config) { heal = Cfg("Heal Per Hit", 0.015f); }
        public override void Hooks()
        {
            buff = CIC.Buff("Leech", new Color(0.8f, 0.2f, 0.3f), false, false, "RoR2/Base/Croco/texBuffRegenBoostIcon.tif");
            CICHooks.onHitServer += (di, a, v, inv) => { if (a.HasBuff(buff)) a.healthComponent.HealFraction(heal * di.procCoefficient, di.procChainMask); };
        }
        protected override bool ActivateEquipment(EquipmentSlot slot) { slot.characterBody.AddTimedBuff(buff, 10f); return true; }
    }

    public class NematocystNozzle : SimpleEquipment<NematocystNozzle>
    {
        protected override string UseSound => "Play_item_proc_squidTurret_shoot";
        public override string EquipmentName => "Nematocyst Nozzle";
        public override string EquipmentLangTokenName => "CIC_NEMATOCYST";
        public override float Cooldown => 45f;
        public override bool IsBoss => true;
        float dmg;
        public override string PickupText => "Fire a spray of homing stingers.";
        public override string DescriptionText => $"Fire {Dmg("6")} homing stingers at nearby enemies, each dealing {Dmg(Pct(dmg))} damage.";
        protected override void CreateConfig(ConfigFile config) { dmg = Cfg("Damage Per Stinger", 3f); }
        public override void Hooks() { if (EquipmentDef) BossDrops.Add("VagrantBody", EquipmentDef); }
        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            var b = slot.characterBody;
            var targets = CIC.Enemies(b.corePosition, 60f, CIC.Team(b));
            for (int i = 0; i < 6; i++)
            {
                var t = targets.Count > 0 ? targets[i % targets.Count].healthComponent.gameObject : null;
                MissileUtils.FireMissile(b.corePosition + Vector3.up, b, default, t, b.damage * dmg, b.RollCrit(), GlobalEventManager.CommonAssets.missilePrefab, DamageColorIndex.Item, false);
            }
            return true;
        }
    }
}
