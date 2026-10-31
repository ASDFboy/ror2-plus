using R2API;
using ClassicItemsReturns.Modules;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace ClassicItemsReturns.Items.Complete
{
    public class OldBox : LowHealthMineItem<OldBox>
    {
        public override string ItemName => "Old Box";
        public override string ItemLangTokenName => "CIC_OLDBOX";
        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags => new[] { ItemTag.Utility, ItemTag.LowHealth };
        float dur, durStack;
        protected override float BaseChance => 10f;
        public override string PickupText => "Chance when hurt to release a jack-in-the-box that terrifies enemies.";
        public override string DescriptionText => $"Chance when hurt to release a jack-in-the-box, {Ut("terrifying")} enemies within {Ut("20m")}: they are {Ut("stunned")} and thrown back for {Ut(dur + "s")} {Stack("+" + durStack + "s")}. Starts at {Ut("10%")} and rises as your health falls.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { dur = Cfg("Duration", 2f); durStack = Cfg("Duration Per Stack", 1f); }
        public override void Hooks() { base.Hooks(); Sfx.Register("Play_item_proc_slug_emerge", "Play_item_proc_warCry"); }
        protected override void Drop(CharacterBody owner, int c)
        {
            CIC.Fx(CIC.ExplosionSmall, owner.corePosition, 8f);
            Sfx.Play("Play_item_proc_slug_emerge", owner.corePosition);
            Sfx.Play("Play_item_proc_warCry", owner.corePosition);
            foreach (var hb in CIC.Enemies(owner.corePosition, 20f, CIC.Team(owner))) CIC.Terrify(hb.healthComponent, owner.corePosition, dur + durStack * (c - 1));
        }
    }

    public class BeatingEmbryo : SimpleItem<BeatingEmbryo>
    {
        public override string ItemName => "Beating Embryo";
        public override string ItemLangTokenName => "CIC_EMBRYO";
        public override ItemTier Tier => ItemTier.Tier3;
        public override ItemTag[] ItemTags => new[] { ItemTag.Utility, ItemTag.EquipmentRelated, ItemTag.AIBlacklist };
        float chance;
        // Toggles, portals and one-shot consumables misbehave when fired twice.
        static readonly HashSet<string> blacklist = new HashSet<string> { "Recycle", "Gateway", "GoldGat", "LunarPortalOnUse", "BossHunter", "BossHunterConsumed", "QuestVolatileBattery", "HealAndRevive", "HealAndReviveConsumed", "MultiShopCard", "CLASSICITEMSRETURNS_EQUIPMENT_CIC_CARRARAMARBLE", "CLASSICITEMSRETURNS_EQUIPMENT_CIC_DYNAMITEPLUNGER", "CLASSICITEMSRETURNS_EQUIPMENT_CIC_CAPTAINSBROOCH", "CLASSICITEMSRETURNS_EQUIPMENT_CIC_GOLDBOMB" };
        bool firing;
        public override string PickupText => "Equipment has a chance to activate twice.";
        public override string DescriptionText => $"Activating your Equipment has a {Ut(chance + "%")} {Stack("+" + chance + "%")} chance to {Ut("trigger it a second time")}.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { chance = Cfg("Chance Per Stack", 30f); }
        public override void Hooks()
        {
            Sfx.Register("Play_item_proc_equipMag");
            On.RoR2.EquipmentSlot.PerformEquipmentAction += (orig, self, def) =>
            {
                bool ok = orig(self, def);
                if (!ok || firing || !def || blacklist.Contains(def.name) || !self.characterBody) return ok;
                int c = GetCount(self.characterBody);
                if (c > 0 && Util.CheckRoll(Mathf.Min(100f, chance * c), self.characterBody.master))
                {
                    firing = true;
                    try { orig(self, def); } finally { firing = false; }
                    Sfx.Play("Play_item_proc_equipMag", self.characterBody.corePosition);
                }
                return ok;
            };
        }
    }

    public class AtgMk2 : SimpleItem<AtgMk2>
    {
        public override string ItemName => "AtG Missile Mk. 2";
        public override string ItemLangTokenName => "CIC_ATGMK2";
        public override ItemTier Tier => ItemTier.Tier3;
        public override ItemTag[] ItemTags => new[] { ItemTag.Damage };
        float chance, dmg;
        public override string PickupText => "Chance to fire a volley of three missiles.";
        public override string DescriptionText => $"{Dmg(chance + "%")} chance on hit to fire {Dmg("three missiles")} that each deal {Dmg(Pct(dmg))} {Stack("+" + Pct(dmg))} TOTAL damage.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { chance = Cfg("Chance", 10f); dmg = Cfg("Damage Per Missile", 2f); }
        public override void Hooks()
        {
            Sfx.Register("Play_item_proc_missile_fire");
            CICHooks.onHitServer += (di, a, v, inv) =>
            {
                int c = GetCount(a);
                if (c <= 0 || di.procChainMask.HasProc(ProcType.Missile) || !Util.CheckRoll(chance * di.procCoefficient, a.master)) return;
                Sfx.Play("Play_item_proc_missile_fire", a.corePosition);
                float d = Util.OnHitProcDamage(di.damage, a.damage, dmg * c);
                for (int i = 0; i < 3; i++)
                    MissileUtils.FireMissile(a.corePosition + Random.insideUnitSphere, a, di.procChainMask, v.gameObject, d, di.crit, GlobalEventManager.CommonAssets.missilePrefab, DamageColorIndex.Item, true);
            };
        }
    }

    public class PlasmaChain : SimpleItem<PlasmaChain>
    {
        public override string ItemName => "Plasma Chain";
        public override string ItemLangTokenName => "CIC_PLASMACHAIN";
        public override ItemTier Tier => ItemTier.Tier3;
        public override ItemTag[] ItemTags => new[] { ItemTag.Damage };
        public static float chance, dps, dur;
        public static BuffDef tether;
        public override string PickupText => "Chance on hit to tether to enemies, burning everything along the chain.";
        public override string DescriptionText => $"{Dmg(chance + "%")} chance on hit to {Dmg("tether")} onto up to {Dmg("1")} {Stack("+1")} enemy for {Dmg(dur + "s")}. The chain deals {Dmg(Pct(dps))} damage per second to the target and to every enemy it passes through.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { chance = Cfg("Chance", 12f); dps = Cfg("Damage Per Second", 1.2f); dur = Cfg("Duration", 4f); }
        public override void Hooks()
        {
            tether = CIC.Buff("PlasmaTether", new Color(0.5f, 0.85f, 1f), false, true, null, hidden: false);
            Sfx.Register("Play_item_proc_bounceChain");
            CharacterBody.onBodyInventoryChangedGlobal += b => b.AddItemBehavior<Behavior>(GetCount(b));
            CICHooks.onHitServer += (di, a, v, inv) =>
            {
                if (GetCount(a) > 0 && a.TryGetComponent(out Behavior b) && Util.CheckRoll(chance * di.procCoefficient, a.master)) b.Tether(v);
            };
        }
        public class Behavior : CharacterBody.ItemBehavior
        {
            readonly Dictionary<CharacterBody, float> links = new Dictionary<CharacterBody, float>();
            readonly List<LineRenderer> lines = new List<LineRenderer>();
            float tick;
            static Material mat;
            public void Tether(CharacterBody v)
            {
                if (links.ContainsKey(v)) { links[v] = dur; return; }
                if (links.Count >= stack) return;
                links[v] = dur; v.AddTimedBuff(tether, dur);
                Sfx.Play("Play_item_proc_bounceChain", v.corePosition);
            }
            void FixedUpdate()
            {
                if (!NetworkServer.active || !body) return;
                foreach (var k in links.Keys.ToList()) { links[k] -= Time.fixedDeltaTime; if (links[k] <= 0 || !k || !k.healthComponent.alive) links.Remove(k); }
                tick += Time.fixedDeltaTime;
                if (tick < 0.5f || links.Count == 0) return;
                tick = 0;
                var team = CIC.Team(body);
                var hit = new HashSet<HealthComponent>();
                foreach (var target in links.Keys)
                {
                    Vector3 a = body.corePosition, b = target.corePosition;
                    hit.Add(target.healthComponent);
                    foreach (var rh in Physics.SphereCastAll(a, 1.5f, (b - a).normalized, (b - a).magnitude, LayerIndex.entityPrecise.mask))
                    {
                        var hb = rh.collider.GetComponent<HurtBox>();
                        if (hb && hb.healthComponent && hb.teamIndex != team) hit.Add(hb.healthComponent);
                    }
                }
                foreach (var hc in hit)
                    hc.TakeDamage(new DamageInfo { attacker = body.gameObject, inflictor = body.gameObject, damage = body.damage * dps * 0.5f, crit = body.RollCrit(), position = hc.body.corePosition, procCoefficient = 0.2f, damageColorIndex = DamageColorIndex.Item, damageType = DamageTypeCombo.Generic });
            }
            void Update()
            {
                // Visual: draw chains to tethered enemies near us (buff is synced, so clients see them too).
                if (!body) return;
                if (!mat) mat = CIC.Load<Material>("RoR2/Base/BFG/matBeamSphereBeam.mat") ?? new Material(Shader.Find("Sprites/Default"));
                int n = 0;
                foreach (var hc in CIC.Enemies(body.corePosition, 60f, CIC.Team(body)))
                {
                    var t = hc.healthComponent ? hc.healthComponent.body : null;
                    if (!t || !t.HasBuff(tether) || (NetworkServer.active && !links.ContainsKey(t))) continue;
                    if (n >= lines.Count)
                    {
                        var go = new GameObject("CIC_Chain"); var lr = go.AddComponent<LineRenderer>();
                        lr.material = mat; lr.startColor = lr.endColor = new Color(0.5f, 0.9f, 1f, 1f); lr.widthMultiplier = 0.6f; lr.positionCount = 2; lr.textureMode = LineTextureMode.Tile;
                        lines.Add(lr);
                    }
                    lines[n].enabled = true; lines[n].SetPosition(0, body.corePosition); lines[n].SetPosition(1, t.corePosition); n++;
                    if (n >= stack) break;
                }
                for (int i = n; i < lines.Count; i++) lines[i].enabled = false;
            }
            void OnDestroy() { foreach (var l in lines) if (l) Destroy(l.gameObject); }
        }
    }

    /// <summary>Counts skill uses on the body's authority and fires attacks from there, like normal skills do.</summary>
    public class HeavenCracker : SimpleItem<HeavenCracker>
    {
        public override string ItemName => "Heaven Cracker";
        public override string ItemLangTokenName => "CIC_HEAVENCRACKER";
        public override ItemTier Tier => ItemTier.Tier3;
        public override ItemTag[] ItemTags => new[] { ItemTag.Damage };
        float dmg; int every;
        static GameObject tracer;
        public override string PickupText => "Every few Primary attacks fire a piercing beam.";
        public override string DescriptionText => $"Every {Dmg(every)} {Stack("-1, minimum 1")} uses of your {Dmg("Primary skill")} also fires a {Dmg("piercing beam")} for {Dmg(Pct(dmg))} damage.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { dmg = Cfg("Damage", 3f); every = Cfg("Uses Per Beam", 4); }
        readonly Dictionary<CharacterBody, int> counts = new Dictionary<CharacterBody, int>();
        public override void Hooks()
        {
            tracer = CIC.Load<GameObject>("RoR2/Base/Huntress/TracerHuntressSnipe.prefab");
            Sfx.Register("Play_huntress_R_snipe_shoot");
            CICHooks.onSkillActivated += (body, skill) =>
            {
                if (!body.hasEffectiveAuthority || !body.skillLocator || skill != body.skillLocator.primary) return;
                int c = GetCount(body); if (c <= 0) return;
                counts.TryGetValue(body, out int n); n++;
                if (n < Mathf.Max(1, every - (c - 1))) { counts[body] = n; return; }
                counts[body] = 0;
                var ray = body.inputBank ? body.inputBank.GetAimRay() : new Ray(body.corePosition, body.transform.forward);
                new BulletAttack
                {
                    owner = body.gameObject, weapon = body.gameObject, origin = ray.origin, aimVector = ray.direction,
                    damage = body.damage * dmg, procCoefficient = 1f, isCrit = body.RollCrit(), radius = 1.5f, maxDistance = 300f,
                    stopperMask = LayerIndex.world.mask, tracerEffectPrefab = tracer, falloffModel = BulletAttack.FalloffModel.None,
                    damageColorIndex = DamageColorIndex.Item, smartCollision = true
                }.Fire();
                Sfx.Play("Play_huntress_R_snipe_shoot", body.corePosition, body.gameObject);
            };
        }
    }

    public class RapidMitosis : SimpleItem<RapidMitosis>
    {
        public override string ItemName => "Rapid Mitosis";
        public override string ItemLangTokenName => "CIC_MITOSIS";
        public override ItemTier Tier => ItemTier.Tier3;
        public override ItemTag[] ItemTags => new[] { ItemTag.Utility, ItemTag.EquipmentRelated };
        float reduction, reductionStack, refund;
        public override string PickupText => "Reduce equipment cooldown. Using equipment refreshes your skills a little.";
        public override string DescriptionText => $"Reduce {Ut("equipment cooldown")} by {Ut(Pct(reduction))} {Stack("+" + Pct(reductionStack))}. Activating your Equipment reduces all {Ut("skill cooldowns")} by {Ut(refund + "s")}.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { reduction = Cfg("Cooldown Reduction", 0.25f); reductionStack = Cfg("Cooldown Reduction Per Stack", 0.15f); refund = Cfg("Skill Refund Seconds", 4f); }
        public override void Hooks()
        {
            On.RoR2.Inventory.CalculateEquipmentCooldownScale += (orig, self) =>
            {
                float s = orig(self);
                int c = self.GetItemCountEffective(ItemDef);
                return c > 0 ? s * (1f - reduction) * Mathf.Pow(1f - reductionStack, c - 1) : s;
            };
            Sfx.Register("Play_item_proc_crit_cooldown");
            On.RoR2.EquipmentSlot.OnEquipmentExecuted += (orig, self) =>
            {
                orig(self);
                var b = self.characterBody;
                if (b && GetCount(b) > 0 && b.skillLocator) { b.skillLocator.DeductCooldownFromAllSkillsServer(refund); Sfx.Play("Play_item_proc_crit_cooldown", b.corePosition); }
            };
        }
    }

    public class RepulsionArmor : SimpleItem<RepulsionArmor>
    {
        public override string ItemName => "Repulsion Armor";
        public override string ItemLangTokenName => "CIC_REPULSION";
        public override ItemTier Tier => ItemTier.Tier3;
        public override ItemTag[] ItemTags => new[] { ItemTag.Utility, ItemTag.Damage };
        int hits; float armor, dur, durStack, reflect;
        static BuffDef charge, active;
        public override string PickupText => "After being hit several times, reflect damage and gain armor.";
        public override string DescriptionText => $"After taking {Ut(hits)} hits, become {Ut("repulsive")} for {Ut(dur + "s")} {Stack("+" + durStack + "s")}: gain {Ut("+" + armor + " armor")} and {Dmg("reflect")} {Dmg(Pct(reflect))} damage back at attackers.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { hits = Cfg("Hits To Trigger", 6); armor = Cfg("Armor", 100f); dur = Cfg("Duration", 4f); durStack = Cfg("Duration Per Stack", 1f); reflect = Cfg("Reflect Damage", 4f); }
        public override void Hooks()
        {
            charge = CIC.Buff("RepulsionCharge", new Color(0.6f, 0.65f, 0.75f), true, false, "RoR2/Base/Common/texBuffGenericShield.tif");
            active = CIC.Buff("RepulsionActive", new Color(0.4f, 0.8f, 1f), false, false, "RoR2/Base/Common/texBuffGenericShield.tif");
            RecalculateStatsAPI.GetStatCoefficients += (b, args) => { if (b.HasBuff(active)) args.armorAdd += armor; };
            var flash = CIC.Load<GameObject>("RoR2/Base/Common/VFX/OmniImpactVFXLarge.prefab");
            Sfx.Register("Play_item_proc_repulsionArmor", "Play_item_proc_thorns");
            CICHooks.onPostDamageServer += (hc, di, before) =>
            {
                var b = hc.body; int c = GetCount(b);
                if (c <= 0 || !hc.alive) return;
                if (b.HasBuff(active))
                {
                    var ab = di.attacker ? di.attacker.GetComponent<CharacterBody>() : null;
                    if (ab && ab.healthComponent && ab != b && (di.damageType.damageType & DamageType.DoT) == 0 && di.procCoefficient > 0)
                    {
                        ab.healthComponent.TakeDamage(new DamageInfo { attacker = b.gameObject, inflictor = b.gameObject, damage = b.damage * reflect, position = ab.corePosition, procCoefficient = 0f, damageColorIndex = DamageColorIndex.Item, damageType = DamageTypeCombo.Generic });
                        Sfx.Play("Play_item_proc_thorns", ab.corePosition);
                    }
                    return;
                }
                if ((di.damageType.damageType & DamageType.DoT) != 0) return;
                b.AddBuff(charge);
                if (b.GetBuffCount(charge) >= hits)
                {
                    b.SetBuffCount(charge.buffIndex, 0);
                    b.AddTimedBuff(active, dur + durStack * (c - 1));
                    CIC.Fx(flash, b.corePosition, b.radius * 3f);
                    Sfx.Play("Play_item_proc_repulsionArmor", b.corePosition);
                }
            };
        }
    }

    public class LaserTurbine : SimpleItem<LaserTurbine>
    {
        public override string ItemName => "Laser Turbine";
        public override string ItemLangTokenName => "CIC_LASERTURBINE";
        public override ItemTier Tier => ItemTier.Tier3;
        public override ItemTag[] ItemTags => new[] { ItemTag.Damage };
        float chargePerSkill, chargeStack, dmg;
        static GameObject tracer;
        readonly Dictionary<CharacterBody, float> charge = new Dictionary<CharacterBody, float>();
        public override string PickupText => "Using skills charges up a huge laser blast.";
        public override string DescriptionText => $"Every skill use charges the turbine by {Dmg(Pct(chargePerSkill))} {Stack("+" + Pct(chargeStack))}. At full charge, fire a {Dmg("piercing laser")} for {Dmg(Pct(dmg))} damage.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { chargePerSkill = Cfg("Charge Per Skill", 0.1f); chargeStack = Cfg("Charge Per Skill Per Stack", 0.05f); dmg = Cfg("Damage", 20f); }
        public override void Hooks()
        {
            tracer = CIC.Load<GameObject>("RoR2/Base/Golem/TracerGolem.prefab");
            Sfx.Register("Play_item_proc_laserTurbine_shoot");
            CICHooks.onSkillActivated += (body, skill) =>
            {
                if (!body.hasEffectiveAuthority) return;
                int c = GetCount(body); if (c <= 0) return;
                charge.TryGetValue(body, out float v);
                v += chargePerSkill + chargeStack * (c - 1);
                if (v < 1f) { charge[body] = v; return; }
                charge[body] = 0f;
                var ray = body.inputBank ? body.inputBank.GetAimRay() : new Ray(body.corePosition, body.transform.forward);
                new BulletAttack
                {
                    owner = body.gameObject, weapon = body.gameObject, origin = ray.origin, aimVector = ray.direction,
                    damage = body.damage * dmg, procCoefficient = 1f, isCrit = body.RollCrit(), radius = 2.5f, maxDistance = 500f,
                    stopperMask = LayerIndex.world.mask, tracerEffectPrefab = tracer, falloffModel = BulletAttack.FalloffModel.None,
                    damageColorIndex = DamageColorIndex.Item, smartCollision = true, force = 2000f
                }.Fire();
                Sfx.Play("Play_item_proc_laserTurbine_shoot", body.corePosition, body.gameObject);
            };
        }
    }

    public class OlLopper : SimpleItem<OlLopper>
    {
        public override string ItemName => "The Ol' Lopper";
        public override string ItemLangTokenName => "CIC_LOPPER";
        public override ItemTier Tier => ItemTier.Tier3;
        public override ItemTag[] ItemTags => new[] { ItemTag.Damage };
        float bonus;
        public override string PickupText => "Deal more damage to enemies the more hurt they are.";
        public override string DescriptionText => $"Deal up to {Dmg("+" + Pct(bonus))} {Stack("+" + Pct(bonus))} damage to enemies, scaling with how much health they are {Dmg("missing")}.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { bonus = Cfg("Max Bonus Per Stack", 1.0f); }
        public override void Hooks()
        {
            CICHooks.onPreDamageServer += (hc, di, attacker) =>
            {
                if (!attacker || di.rejected) return;
                int c = GetCount(attacker);
                if (c > 0) di.damage *= 1f + bonus * c * (1f - hc.combinedHealthFraction);
            };
        }
    }

    public class PhotonJetpack : SimpleItem<PhotonJetpack>
    {
        public override string ItemName => "Photon Jetpack";
        public override string ItemLangTokenName => "CIC_PHOTONJETPACK";
        public override ItemTier Tier => ItemTier.Tier3;
        public override ItemTag[] ItemTags => new[] { ItemTag.Utility, ItemTag.AIBlacklist };
        public static float fuel, fuelStack;
        public override string PickupText => "Hold jump in the air to fly.";
        public override string DescriptionText => $"Hold {Ut("Jump")} while falling (or out of jumps) to {Ut("fly")} for up to {Ut(fuel + "s")} {Stack("+" + fuelStack + "s")}. Fuel recharges while grounded.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { fuel = Cfg("Fuel Seconds", 3f); fuelStack = Cfg("Fuel Per Stack", 1.5f); }
        public override void Hooks() { CharacterBody.onBodyInventoryChangedGlobal += b => b.AddItemBehavior<Behavior>(GetCount(b)); }
        public class Behavior : CharacterBody.ItemBehavior
        {
            float tank;
            void FixedUpdate()
            {
                if (!body || !body.hasEffectiveAuthority || !body.characterMotor || !body.inputBank) return;
                var m = body.characterMotor; float max = fuel + fuelStack * (stack - 1);
                if (m.isGrounded) { tank = Mathf.Min(max, tank + Time.fixedDeltaTime * max / 1.6f * 2f); return; }
                bool wants = body.inputBank.jump.down && (m.velocity.y <= 0f || m.jumpCount >= body.maxJumpCount);
                if (!wants || tank <= 0) return;
                tank -= Time.fixedDeltaTime;
                var v = m.velocity; v.y = Mathf.MoveTowards(v.y, 7f, 60f * Time.fixedDeltaTime); m.velocity = v;
            }
        }
    }

    public class TelescopicSight : SimpleItem<TelescopicSight>
    {
        public override string ItemName => "Telescopic Sight";
        public override string ItemLangTokenName => "CIC_TELESCOPE";
        public override ItemTier Tier => ItemTier.Tier3;
        public override ItemTag[] ItemTags => new[] { ItemTag.Damage };
        float chance, chanceStack;
        static GameObject fx;
        public override string PickupText => "Chance to instantly kill an enemy.";
        public override string DescriptionText => $"{Dmg(chance + "%")} {Stack("+" + chanceStack + "%")} chance on hit to {Dmg("instantly kill")} an enemy. Does not work on bosses.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { chance = Cfg("Chance", 2f); chanceStack = Cfg("Chance Per Stack", 1f); }
        public override void Hooks()
        {
            fx = CIC.Load<GameObject>("RoR2/Base/Common/VFX/OmniExplosionVFXQuick.prefab");
            Sfx.Register("Play_item_proc_guillotine");
            CICHooks.onHitServer += (di, a, v, inv) =>
            {
                int c = GetCount(a);
                if (c <= 0 || v.isBoss || !v.healthComponent.alive || !Util.CheckRoll((chance + chanceStack * (c - 1)) * di.procCoefficient, a.master)) return;
                CIC.Fx(fx, v.corePosition, 3f);
                Sfx.Play("Play_item_proc_guillotine", v.corePosition);
                v.healthComponent.TakeDamage(new DamageInfo { attacker = a.gameObject, inflictor = a.gameObject, damage = v.healthComponent.fullCombinedHealth * 2f, crit = true, position = v.corePosition, procCoefficient = 0f, damageColorIndex = DamageColorIndex.WeakPoint, damageType = DamageType.BypassArmor | DamageType.BypassBlock | DamageType.BypassOneShotProtection });
            };
        }
    }

    public class FiremansBoots : SimpleItem<FiremansBoots>
    {
        public override string ItemName => "Fireman's Boots";
        public override string ItemLangTokenName => "CIC_FIREMANSBOOTS";
        public override ItemTier Tier => ItemTier.Tier3;
        public override ItemTag[] ItemTags => new[] { ItemTag.Damage };
        public static float dps, dpsStack;
        public override string PickupText => "Walking leaves a trail of fire.";
        public override string DescriptionText => $"Walking leaves a {Dmg("trail of fire")} that burns enemies for {Dmg(Pct(dps))} {Stack("+" + Pct(dpsStack))} damage per second.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { dps = Cfg("Damage Per Second", 1.5f); dpsStack = Cfg("Damage Per Second Per Stack", 1f); }
        public override void Hooks()
        {
            CharacterBody.onBodyInventoryChangedGlobal += b =>
            {
                int c = GetCount(b);
                var t = b.GetComponents<CICFireTrail>().FirstOrDefault(x => x.key == "boots");
                if (c > 0 && !t) { t = b.gameObject.AddComponent<CICFireTrail>(); t.body = b; t.key = "boots"; }
                if (t) { if (c > 0) t.damagePerSecondCoefficient = dps + dpsStack * (c - 1); else Object.Destroy(t); }
            };
        }
    }

    public class AncientScepter : SimpleItem<AncientScepter>
    {
        public override string ItemName => "Ancient Scepter";
        public override string ItemLangTokenName => "CIC_SCEPTER";
        public override ItemTier Tier => ItemTier.Tier3;
        public override ItemTag[] ItemTags => new[] { ItemTag.Damage, ItemTag.Utility };
        float cdr, dmgBuff, buffDur, buffDurStack;
        static BuffDef might;
        public override string PickupText => "Empower your Special skill.";
        public override string DescriptionText => $"Reduce your {Ut("Special skill")} cooldown by {Ut(Pct(cdr))} {Stack("+" + Pct(cdr))}, stacking hyperbolically. Using your Special grants {Dmg("+" + Pct(dmgBuff) + " damage")} for {Dmg(buffDur + "s")} {Stack("+" + buffDurStack + "s")}.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { cdr = Cfg("Special Cooldown Reduction", 0.3f); dmgBuff = Cfg("Damage Bonus", 0.5f); buffDur = Cfg("Buff Duration", 4f); buffDurStack = Cfg("Buff Duration Per Stack", 1f); }
        public override void Hooks()
        {
            might = CIC.Buff("ScepterMight", new Color(0.7f, 0.4f, 1f), false, false, "RoR2/Base/CritOnUse/texBuffFullCritIcon.tif");
            Sfx.Register("Play_item_proc_increasePrimaryDamage");
            RecalculateStatsAPI.GetStatCoefficients += (b, args) =>
            {
                int c = GetCount(b);
                if (c > 0) args.specialCooldownMultAdd -= CIC.Hyperbolic(cdr / (1f - cdr), c);
                if (b.HasBuff(might)) args.damageMultAdd += dmgBuff;
            };
            CICHooks.onSkillActivated += (body, skill) =>
            {
                if (!body.hasEffectiveAuthority || !body.skillLocator || skill != body.skillLocator.special) return;
                int c = GetCount(body);
                if (c > 0) { body.AddTimedBuffAuthority(might.buffIndex, buffDur + buffDurStack * (c - 1)); Sfx.Play("Play_item_proc_increasePrimaryDamage", body.corePosition, body.gameObject); }
            };
        }
    }

    public class Umbrella : SimpleItem<Umbrella>
    {
        public override string ItemName => "Umbrella";
        public override string ItemLangTokenName => "CIC_UMBRELLA";
        public override ItemTier Tier => ItemTier.Tier3;
        public override ItemTag[] ItemTags => new[] { ItemTag.Utility, ItemTag.HoldoutZoneRelated, ItemTag.AIBlacklist };
        public static float dur, durStack, dps;
        public override string PickupText => "Activating the Teleporter calls down a protective rain.";
        public override string DescriptionText => $"Activating the Teleporter starts a {Ut("rain")} for {Ut(dur + "s")} {Stack("+" + durStack + "s")}. While it rains you are {Ut("invulnerable")}, and enemies within {Dmg("40m")} are {Ut("slowed")} and take {Dmg(Pct(dps))} damage per second.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { dur = Cfg("Duration", 12f); durStack = Cfg("Duration Per Stack", 5f); dps = Cfg("Damage Per Second", 1f); }
        static BuffDef raining;
        static Material rainMat;
        public override void Hooks()
        {
            raining = CIC.Buff("UmbrellaRain", new Color(0.5f, 0.7f, 1f), false, false, "RoR2/Base/Common/texBuffGenericShield.tif");
            rainMat = CIC.Load<Material>("RoR2/Base/Common/VFX/matRain.mat");
            Sfx.Register("Play_item_use_meteor_storm_start", "Play_item_use_meteor_storm_end");
            CharacterBody.onBodyInventoryChangedGlobal += b => b.AddItemBehavior<Visual>(GetCount(b));
            Utils.IsTeleActivatedTracker.OnTPActivatedActions += () =>
            {
                if (!NetworkServer.active) return;
                foreach (var b in CharacterBody.readOnlyInstancesList)
                {
                    int c = GetCount(b);
                    if (c <= 0 || !b.healthComponent.alive) continue;
                    var r = b.gameObject.AddComponent<Rain>(); r.body = b; r.time = dur + durStack * (c - 1);
                    b.AddTimedBuff(RoR2Content.Buffs.Immune, r.time);
                    b.AddTimedBuff(raining, r.time);
                    Sfx.Play("Play_item_use_meteor_storm_start", b.corePosition);
                }
            };
        }
        public class Rain : MonoBehaviour
        {
            public CharacterBody body; public float time; float tick;
            void FixedUpdate()
            {
                time -= Time.fixedDeltaTime; tick += Time.fixedDeltaTime;
                if (time <= 0 || !body) { if (body) Sfx.Play("Play_item_use_meteor_storm_end", body.corePosition); Destroy(this); return; }
                if (tick < 1f) return;
                tick = 0;
                CIC.Blast(body, body.corePosition, 40f, dps, 0.2f, DamageType.SlowOnHit);
            }
        }

        /// <summary>Client-side: a rain shower over the holder and a 40m ring while the rain buff is up.</summary>
        public class Visual : CharacterBody.ItemBehavior
        {
            GameObject rain, ring;
            void Update()
            {
                bool on = body && body.HasBuff(raining);
                if (on && !rain) Build();
                if (rain && rain.activeSelf != on) { rain.SetActive(on); if (ring) ring.SetActive(on); }
            }
            void Build()
            {
                rain = new GameObject("CIC_UmbrellaRain");
                rain.transform.SetParent(body.transform, false); rain.transform.localPosition = Vector3.up * 14f;
                var ps = rain.AddComponent<ParticleSystem>();
                var main = ps.main; main.startLifetime = 0.9f; main.startSpeed = 0f; main.startSize = 0.12f; main.maxParticles = 1500;
                main.simulationSpace = ParticleSystemSimulationSpace.World; main.startColor = new Color(0.75f, 0.85f, 1f, 0.7f);
                var em = ps.emission; em.rateOverTime = 900f;
                var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
                vel.x = new ParticleSystem.MinMaxCurve(0f); vel.y = new ParticleSystem.MinMaxCurve(-28f); vel.z = new ParticleSystem.MinMaxCurve(0f);
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 16f; sh.rotation = new Vector3(90f, 0f, 0f);
                var r = rain.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = 6f;
                if (rainMat) r.sharedMaterial = rainMat;
                ring = Aura.Attach(body.transform, 40f, new Color(0.5f, 0.7f, 1f, 1f));
            }
            void OnDestroy() { if (rain) Destroy(rain); if (ring) Destroy(ring); }
        }
    }
}
