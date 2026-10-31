using R2API;
using ClassicItemsReturns.Modules;
using RoR2;
using RoR2.Projectile;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace ClassicItemsReturns.Items.Complete
{
    public class TimeKeepersSecret : SimpleItem<TimeKeepersSecret>
    {
        public override string ItemName => "Time Keeper's Secret";
        public override string ItemLangTokenName => "CIC_TIMEKEEPER";
        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags => new[] { ItemTag.Utility, ItemTag.LowHealth };
        float threshold, dur, durStack, cooldown;
        static BuffDef cd;
        static GameObject fx;
        public override string PickupText => "Falling to low health stops time.";
        public override string DescriptionText => $"Falling below {Heal(Pct(threshold))} health {Ut("stops time")} for enemies within {Ut("60m")} for {Ut(dur + "s")} {Stack("+" + durStack + "s")}: they are frozen in place and their projectiles vanish. Recharges after {Ut(cooldown + "s")}.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { threshold = Cfg("Health Threshold", 0.25f); dur = Cfg("Duration", 3f); durStack = Cfg("Duration Per Stack", 1f); cooldown = Cfg("Cooldown", 60f); }
        public override void Hooks()
        {
            cd = CIC.Buff("TimeKeeperCooldown", new Color(0.5f, 0.5f, 0.6f), true, false, "RoR2/Base/Common/texBuffSlow50Icon.tif", cooldown: true);
            fx = CIC.Load<GameObject>("RoR2/Base/Common/VFX/OmniExplosionVFXQuick.prefab");
            Sfx.Register("Play_item_use_blackhole_start");
            CICHooks.onPostDamageServer += (hc, di, before) =>
            {
                int c = GetCount(hc.body);
                if (c <= 0 || !hc.alive || hc.combinedHealthFraction >= threshold || hc.body.HasBuff(cd)) return;
                TimeStop.Stop(hc.body.corePosition, 60f, CIC.Team(hc.body), dur + durStack * (c - 1));
                for (int i = 1; i <= cooldown; i++) hc.body.AddTimedBuff(cd, i);
                CIC.Fx(fx, hc.body.corePosition, 20f);
                Sfx.Play("Play_item_use_blackhole_start", hc.body.corePosition);
            };
        }
    }

    public class ChargefieldGenerator : SimpleItem<ChargefieldGenerator>
    {
        public override string ItemName => "Chargefield Generator";
        public override string ItemLangTokenName => "CIC_CHARGEFIELD";
        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags => new[] { ItemTag.Damage, ItemTag.OnKillEffect };
        public static float dmg, dmgStack, startRadius, growth, maxRadius, life;
        public override string PickupText => "Killing an enemy creates a growing ring of lightning.";
        public override string DescriptionText => $"Killing an enemy creates a {Dmg("ring of lightning")} around you that deals {Dmg(Pct(dmg))} {Stack("+" + Pct(dmgStack))} damage every {Dmg("0.5s")} to enemies inside. The ring starts at {Ut(startRadius + "m")} and grows {Ut(growth + "m")} per kill, up to {Ut(maxRadius + "m")}. Lasts {Ut(life + "s")} without a kill.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { dmg = Cfg("Damage", 0.5f); dmgStack = Cfg("Damage Per Stack", 0.2f); startRadius = Cfg("Start Radius", 8f); growth = Cfg("Growth Per Kill", 1.5f); maxRadius = Cfg("Max Radius", 30f); life = Cfg("Duration", 6f); }
        static GameObject fx;
        static BuffDef radiusBuff;   // hidden; stack count = current ring radius in metres, so clients can draw it
        public override void Hooks()
        {
            fx = CIC.Load<GameObject>("RoR2/Base/Common/VFX/OmniImpactVFXLightning.prefab");
            radiusBuff = CIC.Buff("ChargefieldRadius", Color.cyan, true, false, null, hidden: true);
            Sfx.Register("Play_item_proc_shocknearby_start", "Play_item_proc_chain_lightning");
            CharacterBody.onBodyInventoryChangedGlobal += b => b.AddItemBehavior<Behavior>(GetCount(b));
            CICHooks.onKillServer += r =>
            {
                if (r.attackerBody && r.attackerBody.TryGetComponent(out Behavior b) && b.enabled) b.OnKill();
            };
        }
        public class Behavior : CharacterBody.ItemBehavior
        {
            float radius, timeLeft, tick; GameObject ring;
            public void OnKill()
            {
                if (timeLeft <= 0) Sfx.Play("Play_item_proc_shocknearby_start", body.corePosition);
                radius = timeLeft > 0 ? Mathf.Min(maxRadius, radius + growth) : startRadius; timeLeft = life;
                body.SetBuffCount(radiusBuff.buffIndex, Mathf.RoundToInt(radius));
            }
            void Start() { ring = Aura.Attach(body.transform, startRadius, new Color(0.35f, 0.7f, 1f, 1f)); if (ring) ring.SetActive(false); }
            void OnDestroy() { if (ring) Destroy(ring); if (body && NetworkServer.active) body.SetBuffCount(radiusBuff.buffIndex, 0); }
            void Update()
            {
                if (!ring || !body) return;
                int r = body.GetBuffCount(radiusBuff);
                if (ring.activeSelf != r > 0) ring.SetActive(r > 0);
                if (r > 0) Aura.SetRadius(ring, Mathf.Lerp(ring.transform.localScale.x * 13f, r, Time.deltaTime * 8f));
            }
            void FixedUpdate()
            {
                if (!NetworkServer.active || timeLeft <= 0 || !body) return;
                timeLeft -= Time.fixedDeltaTime; tick += Time.fixedDeltaTime;
                if (timeLeft <= 0) { body.SetBuffCount(radiusBuff.buffIndex, 0); return; }
                if (tick < 0.5f) return;
                tick = 0;
                var r = CIC.Blast(body, body.corePosition, radius, dmg + dmgStack * (stack - 1), 0.3f);
                if (r.hitCount > 0) { CIC.Fx(fx, body.corePosition, radius); Sfx.Play("Play_item_proc_chain_lightning", body.corePosition); }
            }
        }
    }

    public class ConcussionGrenade : SimpleItem<ConcussionGrenade>
    {
        public override string ItemName => "Concussion Grenade";
        public override string ItemLangTokenName => "CIC_CONCUSSION";
        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags => new[] { ItemTag.Utility, ItemTag.Damage };
        float chance, chanceStack, dmg;
        public override string PickupText => "Chance on hit to set off a stunning blast.";
        public override string DescriptionText => $"{Ut(chance + "%")} {Stack("+" + chanceStack + "%")} chance on hit to set off a concussive blast, dealing {Dmg(Pct(dmg))} damage and {Ut("stunning")} enemies within {Ut("6m")} for {Ut("2s")}.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { chance = Cfg("Chance", 6f); chanceStack = Cfg("Chance Per Stack", 6f); dmg = Cfg("Damage", 1.2f); }
        public override void Hooks()
        {
            Sfx.Register("Play_item_proc_knockBackHitEnemies");
            CICHooks.onHitServer += (di, a, v, inv) =>
            {
                int c = GetCount(a);
                if (c <= 0 || di.procChainMask.HasProc(ProcType.Missile) || !Util.CheckRoll((chance + chanceStack * (c - 1)) * di.procCoefficient, a.master)) return;
                var mask = di.procChainMask; mask.AddProc(ProcType.Missile);
                Sfx.Play("Play_item_proc_knockBackHitEnemies", v.corePosition);
                CIC.Blast(a, v.corePosition, 6f, dmg, 0.5f, DamageType.Stun1s, CIC.ExplosionSmall, di.crit, mask, fxScale: 6f);
                foreach (var hb in CIC.Enemies(v.corePosition, 6f, CIC.Team(a)))
                {
                    var ssoh = hb.healthComponent ? hb.healthComponent.GetComponent<SetStateOnHurt>() : null;
                    if (ssoh && ssoh.canBeStunned) ssoh.SetStun(2f);
                }
            };
        }
    }

    /// <summary>Shared "chance when hurt, higher at low health" trigger for the mine items.</summary>
    public abstract class LowHealthMineItem<T> : SimpleItem<T> where T : LowHealthMineItem<T>
    {
        protected abstract float BaseChance { get; }
        protected abstract void Drop(CharacterBody owner, int count);
        public override void Hooks()
        {
            CICHooks.onPostDamageServer += (hc, di, before) =>
            {
                int c = GetCount(hc.body);
                if (c <= 0 || !hc.alive || di.attacker == hc.gameObject) return;
                if (Util.CheckRoll(CIC.LowHealthChance(hc.body, BaseChance, 35f), hc.body.master)) Drop(hc.body, c);
            };
        }
    }

    public class DeadMansFoot : LowHealthMineItem<DeadMansFoot>
    {
        public override string ItemName => "Dead Man's Foot";
        public override string ItemLangTokenName => "CIC_DEADMANSFOOT";
        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags => new[] { ItemTag.Damage, ItemTag.LowHealth };
        float dmg, dmgStack;
        protected override float BaseChance => 15f;
        public override string PickupText => "Drop a poison mine when hurt.";
        public override string DescriptionText => $"Chance when hurt to drop a {Dmg("poison mine")} that explodes for {Dmg(Pct(dmg))} {Stack("+" + Pct(dmgStack))} damage and {Dmg("poisons")} enemies. Starts at {Ut("15%")} and rises as your health falls.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { dmg = Cfg("Damage", 1.5f); dmgStack = Cfg("Damage Per Stack", 1.0f); }
        protected override void Drop(CharacterBody owner, int c) => CIC.FireEngiMine(owner, owner.footPosition, dmg + dmgStack * (c - 1), DamageType.PoisonOnHit);
    }

    public class PanicMines : LowHealthMineItem<PanicMines>
    {
        public override string ItemName => "Panic Mines";
        public override string ItemLangTokenName => "CIC_PANICMINES";
        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags => new[] { ItemTag.Damage, ItemTag.LowHealth };
        float dmg;
        protected override float BaseChance => 15f;
        public override string PickupText => "Drop mines when hurt.";
        public override string DescriptionText => $"Chance when hurt to drop {Dmg("1")} {Stack("+1")} mine that explodes for {Dmg(Pct(dmg))} damage. Starts at {Ut("15%")} and rises as your health falls.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { dmg = Cfg("Damage", 4f); }
        protected override void Drop(CharacterBody owner, int c)
        {
            for (int i = 0; i < c; i++) CIC.FireEngiMine(owner, owner.footPosition + Random.insideUnitSphere * (i == 0 ? 0 : 2f), dmg, DamageTypeCombo.Generic);
        }
    }

    public class ProphetsCape : SimpleItem<ProphetsCape>
    {
        public override string ItemName => "Prophet's Cape";
        public override string ItemLangTokenName => "CIC_PROPHETSCAPE";
        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags => new[] { ItemTag.Utility, ItemTag.Healing };
        float window, windowStack, barrier, cooldown;
        static BuffDef ready, active;
        public override string PickupText => "Being struck briefly blocks all damage. Blocked hits grant barrier.";
        public override string DescriptionText => $"Being struck makes you {Ut("invulnerable")} for {Ut(window + "s")} {Stack("+" + windowStack + "s")}. Every hit blocked this way grants {Heal(Pct(barrier))} of your maximum health as {Heal("barrier")}. Recharges after {Ut(cooldown + "s")}.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { window = Cfg("Block Window", 0.75f); windowStack = Cfg("Block Window Per Stack", 0.25f); barrier = Cfg("Barrier Per Block", 0.05f); cooldown = Cfg("Cooldown", 15f); }
        public override void Hooks()
        {
            ready = CIC.Buff("CapeReady", new Color(0.6f, 0.4f, 0.9f), false, false, "RoR2/Base/Common/texBuffGenericShield.tif");
            active = CIC.Buff("CapeActive", new Color(0.8f, 0.6f, 1f), false, false, "RoR2/Base/Common/texBuffGenericShield.tif");
            var blockFx = CIC.Load<GameObject>("RoR2/Base/Bear/BearProc.prefab");
            Sfx.Register("Play_item_proc_negateAttack");
            CharacterBody.onBodyInventoryChangedGlobal += b => { if (NetworkServer.active) b.AddItemBehavior<Behavior>(GetCount(b)); };
            CICHooks.onPreDamageServer += (hc, di, attacker) =>
            {
                var b = hc.body;
                if (di.rejected || GetCount(b) <= 0) return;
                if (b.HasBuff(active)) { di.rejected = true; b.healthComponent.AddBarrier(b.healthComponent.fullCombinedHealth * barrier); CIC.Fx(blockFx, di.position, 1f); return; }
                if (!b.HasBuff(ready) || (di.damageType.damageType & DamageType.DoT) != 0) return;
                b.RemoveBuff(ready);
                b.AddTimedBuff(active, window + windowStack * (GetCount(b) - 1));
                di.rejected = true; b.healthComponent.AddBarrier(b.healthComponent.fullCombinedHealth * barrier);
                b.GetComponent<Behavior>()?.Trigger();
                CIC.Fx(blockFx, di.position, 1f);
                Sfx.Play("Play_item_proc_negateAttack", b.corePosition);
            };
        }
        public class Behavior : CharacterBody.ItemBehavior
        {
            float timer;
            public void Trigger() => timer = Instance.cooldown;
            void FixedUpdate()
            {
                if (!body) return;
                if (timer > 0) { timer -= Time.fixedDeltaTime; return; }
                if (!body.HasBuff(ready)) body.AddBuff(ready);
            }
            void OnDisable() { if (body && NetworkServer.active && body.HasBuff(ready)) body.RemoveBuff(ready); }
        }
    }

    public class Insecticide : SimpleItem<Insecticide>
    {
        public override string ItemName => "Insecticide";
        public override string ItemLangTokenName => "CIC_INSECTICIDE";
        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags => new[] { ItemTag.Damage, ItemTag.Healing };
        float chance, chanceStack, dps, heal;
        const int maxStacks = 10;
        static BuffDef sprayBuff;
        static DotController.DotIndex dot;
        public override string PickupText => "Chance to spray enemies with a stacking poison. Heal when they die.";
        public override string DescriptionText => $"{Dmg(chance + "%")} {Stack("+" + chanceStack + "%")} chance on hit to spray an enemy for {Dmg(Pct(dps))} damage per second for {Dmg("5s")}. Stacks up to {Dmg(maxStacks)} times. Killing a sprayed enemy {Heal("heals")} you for {Heal(Pct(heal))} of your maximum health per spray.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { chance = Cfg("Chance", 10f); chanceStack = Cfg("Chance Per Stack", 5f); dps = Cfg("Damage Per Second", 0.3f); heal = Cfg("Heal Per Spray", 0.01f); }
        public override void Hooks()
        {
            sprayBuff = CIC.Buff("Insecticide", new Color(0.4f, 0.8f, 0.3f), true, true, "RoR2/Base/Treebot/texBuffWeakIcon.tif");
            dot = DotAPI.RegisterDotDef(new DotController.DotDef { associatedBuff = sprayBuff, damageCoefficient = dps * 0.5f, interval = 0.5f, damageColorIndex = DamageColorIndex.Poison, resetTimerOnAdd = false });
            Sfx.Register("Play_item_proc_triggerEnemyDebuffs", "Play_item_proc_regenOnKill");
            CICHooks.onHitServer += (di, a, v, inv) =>
            {
                int c = GetCount(a);
                if (c <= 0 || di.dotIndex == dot || !Util.CheckRoll((chance + chanceStack * (c - 1)) * di.procCoefficient, a.master)) return;
                if (!v.HasBuff(sprayBuff)) Sfx.Play("Play_item_proc_triggerEnemyDebuffs", v.corePosition);
                var info = new InflictDotInfo { attackerObject = a.gameObject, victimObject = v.gameObject, dotIndex = dot, duration = 5f, damageMultiplier = 1f, maxStacksFromAttacker = maxStacks };
                DotController.InflictDot(ref info);
            };
            CICHooks.onKillServer += r =>
            {
                if (!r.attackerBody || !r.victimBody || GetCount(r.attackerBody) <= 0) return;
                int sprays = r.victimBody.GetBuffCount(sprayBuff);
                if (sprays > 0) { r.attackerBody.healthComponent.HealFraction(heal * sprays, default); Sfx.Play("Play_item_proc_regenOnKill", r.attackerBody.corePosition); }
            };
        }
    }

    public class DecayingSample : SimpleItem<DecayingSample>
    {
        public override string ItemName => "Decaying Sample";
        public override string ItemLangTokenName => "CIC_DECAYINGSAMPLE";
        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags => new[] { ItemTag.Damage };
        public static float dmg, dmgStack;
        public override string PickupText => "Gain orbiting spheres that strike nearby enemies.";
        public override string DescriptionText => $"Two spheres orbit you, striking enemies they touch for {Dmg(Pct(dmg))} {Stack("+" + Pct(dmgStack))} damage every {Dmg("0.5s")}.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { dmg = Cfg("Damage", 0.5f); dmgStack = Cfg("Damage Per Stack", 0.3f); }
        public override void Hooks() { CharacterBody.onBodyInventoryChangedGlobal += b => b.AddItemBehavior<Behavior>(GetCount(b)); }
        public class Behavior : CharacterBody.ItemBehavior
        {
            readonly GameObject[] orbs = new GameObject[2];
            float tick;
            void Start() { for (int i = 0; i < 2; i++) orbs[i] = RuntimeArt.Spawn("DecayingSample", 0.45f); }
            void OnDestroy() { foreach (var o in orbs) if (o) Destroy(o); }
            Vector3 Pos(int i)
            {
                float a = Time.time * 3f + i * Mathf.PI;
                float r = 2.5f + body.radius;
                return body.corePosition + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(Time.time * 2f + i) * 0.4f, Mathf.Sin(a) * r);
            }
            void Update()
            {
                if (!body) return;
                for (int i = 0; i < 2; i++) if (orbs[i]) { orbs[i].transform.position = Pos(i); orbs[i].transform.Rotate(0f, 180f * Time.deltaTime, 90f * Time.deltaTime); }
            }
            void FixedUpdate()
            {
                if (!NetworkServer.active || !body) return;
                tick += Time.fixedDeltaTime;
                if (tick < 0.5f) return;
                tick = 0;
                for (int i = 0; i < 2; i++) CIC.Blast(body, Pos(i), 1.8f, dmg + dmgStack * (stack - 1), 0.15f);
            }
        }
    }

    public class FilialImprinting : SimpleItem<FilialImprinting>
    {
        public override string ItemName => "Filial Imprinting";
        public override string ItemLangTokenName => "CIC_FILIAL";
        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags => new[] { ItemTag.Utility, ItemTag.Healing };
        public static float interval, buffDur;
        public static BuffDef speedBuff, atkBuff, regenBuff;
        public override string PickupText => "Hatch a strange creature that drops buffs.";
        public override string DescriptionText => $"Hatch {Ut("1")} {Stack("+1")} strange creature that drops a buff every {Ut(interval + "s")}, granting {Ut("+30% movement speed")}, {Dmg("+30% attack speed")} or {Heal("+3 hp/s regeneration")} for {Ut(buffDur + "s")}.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { interval = Cfg("Interval", 15f); buffDur = Cfg("Buff Duration", 10f); }
        public override void Hooks()
        {
            speedBuff = CIC.Buff("FilialSpeed", new Color(0.4f, 0.8f, 1f), true, false, "RoR2/Base/Common/texMovespeedBuffIcon.tif");
            atkBuff = CIC.Buff("FilialAttack", new Color(1f, 0.6f, 0.3f), true, false, "RoR2/Base/AttackSpeedOnCrit/texBuffAttackSpeedOnCritIcon.tif");
            regenBuff = CIC.Buff("FilialRegen", new Color(0.5f, 1f, 0.4f), true, false, "RoR2/Base/Croco/texBuffRegenBoostIcon.tif");
            RecalculateStatsAPI.GetStatCoefficients += (b, args) =>
            {
                args.moveSpeedMultAdd += 0.3f * b.GetBuffCount(speedBuff);
                args.attackSpeedMultAdd += 0.3f * b.GetBuffCount(atkBuff);
                args.baseRegenAdd += 3f * (1 + 0.2f * (b.level - 1)) * b.GetBuffCount(regenBuff);
            };
            CharacterBody.onBodyInventoryChangedGlobal += b => b.AddItemBehavior<Behavior>(GetCount(b));
        }
        public class Behavior : CharacterBody.ItemBehavior
        {
            GameObject critter; float timer;
            void Start() { critter = RuntimeArt.Spawn("FilialImprinting", 0.35f); }
            void OnDestroy() { if (critter) Destroy(critter); }
            void Update()
            {
                if (!body || !critter) return;
                var target = body.corePosition + body.transform.right * -1.5f + Vector3.up * (1f + Mathf.Sin(Time.time * 4f) * 0.25f);
                critter.transform.position = Vector3.Lerp(critter.transform.position, target, Time.deltaTime * 6f);
                var look = body.corePosition - critter.transform.position; look.y = 0;
                if (look.sqrMagnitude > 0.01f) critter.transform.rotation = Quaternion.Slerp(critter.transform.rotation, Quaternion.LookRotation(look), Time.deltaTime * 5f);
            }
            void FixedUpdate()
            {
                if (!NetworkServer.active || !body) return;
                timer += Time.fixedDeltaTime;
                if (timer < interval) return;
                timer = 0;
                var pool = new[] { speedBuff, atkBuff, regenBuff };
                for (int i = 0; i < stack; i++) body.AddTimedBuff(pool[Random.Range(0, 3)], buffDur);
                Sfx.Play("Play_item_proc_regenOnKill", body.corePosition);
            }
        }
    }

    public class ToxicCentipede : SimpleItem<ToxicCentipede>
    {
        public override string ItemName => "Toxic Centipede";
        public override string ItemLangTokenName => "CIC_CENTIPEDE";
        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags => new[] { ItemTag.Damage };
        public static float dur, durStack, dps, recharge;
        public static BuffDef infectBuff;
        public static DotController.DotIndex dot;
        public override string PickupText => "Infect a nearby enemy. The infection jumps when they die.";
        public override string DescriptionText => $"Infect an enemy within {Dmg("6m")} for {Dmg(dur + "s")} {Stack("+" + durStack + "s")}, dealing {Dmg(Pct(dps))} damage per second. If the host dies, the infection {Dmg("jumps")} to a nearby enemy. Recharges {Ut(recharge + "s")} {Stack("-33%")} after the infection ends.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { dur = Cfg("Duration", 6f); durStack = Cfg("Duration Per Stack", 2f); dps = Cfg("Damage Per Second", 1f); recharge = Cfg("Recharge", 6f); }
        public override void Hooks()
        {
            infectBuff = CIC.Buff("Centipede", new Color(0.5f, 0.9f, 0.2f), false, true, "RoR2/Base/Treebot/texBuffWeakIcon.tif");
            dot = DotAPI.RegisterDotDef(new DotController.DotDef { associatedBuff = infectBuff, damageCoefficient = dps * 0.5f, interval = 0.5f, damageColorIndex = DamageColorIndex.Poison });
            Sfx.Register("Play_item_proc_triggerEnemyDebuffs", "Play_item_proc_bounceChain");
            CharacterBody.onBodyInventoryChangedGlobal += b => { if (NetworkServer.active) b.AddItemBehavior<Behavior>(GetCount(b)); };
            CICHooks.onKillServer += r =>
            {
                if (!r.victimBody || !r.victimBody.HasBuff(infectBuff)) return;
                foreach (var b in InstanceTracker.GetInstancesList<Behavior>()) b.OnHostDied(r.victimBody);
            };
        }
        public class Behavior : CharacterBody.ItemBehavior
        {
            CharacterBody host; float infectionLeft, cooldown;
            void OnEnable() => InstanceTracker.Add(this);
            void OnDisable() => InstanceTracker.Remove(this);
            float Duration => dur + durStack * (stack - 1);
            void Infect(CharacterBody target, float time)
            {
                Sfx.Play(host ? "Play_item_proc_bounceChain" : "Play_item_proc_triggerEnemyDebuffs", target.corePosition);
                host = target; infectionLeft = time;
                var info = new InflictDotInfo { attackerObject = body.gameObject, victimObject = target.gameObject, dotIndex = dot, duration = time, damageMultiplier = 1f, maxStacksFromAttacker = 1 };
                DotController.InflictDot(ref info);
            }
            public void OnHostDied(CharacterBody victim)
            {
                if (victim != host || !body || infectionLeft <= 0) return;
                HealthComponent best = null; float bestD = 30f * 30f;
                foreach (var hb in CIC.Enemies(victim.corePosition, 30f, CIC.Team(body)))
                {
                    var hc = hb.healthComponent; if (!hc || !hc.alive || hc.body == victim) continue;
                    float d = (hc.transform.position - victim.corePosition).sqrMagnitude;
                    if (d < bestD) { bestD = d; best = hc; }
                }
                if (best) Infect(best.body, infectionLeft); else { host = null; infectionLeft = 0; }
            }
            void FixedUpdate()
            {
                if (!body) return;
                if (infectionLeft > 0)
                {
                    infectionLeft -= Time.fixedDeltaTime;
                    if (infectionLeft <= 0 || !host || !host.healthComponent.alive) { infectionLeft = 0; host = null; cooldown = recharge * Mathf.Pow(0.67f, stack - 1); }
                    return;
                }
                if (cooldown > 0) { cooldown -= Time.fixedDeltaTime; return; }
                foreach (var hb in CIC.Enemies(body.corePosition, 6f + body.radius, CIC.Team(body)))
                    if (hb.healthComponent && hb.healthComponent.alive) { Infect(hb.healthComponent.body, Duration); break; }
            }
        }
    }
}
