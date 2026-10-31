using R2API;
using ClassicItemsReturns.Modules;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace ClassicItemsReturns.Items.Complete
{
    public class MeatNugget : SimpleItem<MeatNugget>
    {
        public override string ItemName => "Meat Nugget";
        public override string ItemLangTokenName => "CIC_MEATNUGGET";
        public override ItemTier Tier => ItemTier.Tier1;
        public override ItemTag[] ItemTags => new[] { ItemTag.Healing, ItemTag.CanBeTemporary };
        float chance, heal, healStack;
        public override string PickupText => "Enemies drop chunks of healing meat.";
        public override string DescriptionText => $"{Dmg(chance + "%")} chance on hit to drop {Heal("2 meat nuggets")} that each {Heal("heal")} for {Heal(heal)} {Stack("+" + healStack)} health.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config)
        {
            chance = Cfg("Chance", 8f); heal = Cfg("Heal Per Nugget", 8f); healStack = Cfg("Heal Per Stack", 8f);
        }
        static GameObject pack;
        public override void Hooks()
        {
            pack = CIC.Load<GameObject>("RoR2/Base/Tooth/HealPack.prefab");
            Sfx.Register("Play_item_proc_monsterTooth_spawn");
            CICHooks.onHitServer += (di, a, v, inv) =>
            {
                int c = GetCount(a);
                if (c <= 0 || !pack || !Util.CheckRoll(chance * di.procCoefficient, a.master)) return;
                for (int i = 0; i < 2; i++)
                {
                    var go = Object.Instantiate(pack, v.corePosition, Random.rotation);
                    go.GetComponent<TeamFilter>().teamIndex = CIC.Team(a);
                    var hp = go.GetComponentInChildren<HealthPickup>();
                    hp.flatHealing = heal + healStack * (c - 1); hp.fractionalHealing = 0f;
                    go.transform.localScale = Vector3.one * 0.8f;
                    var rb = go.GetComponent<Rigidbody>();
                    if (rb) rb.velocity = new Vector3(Random.Range(-6f, 6f), 12f, Random.Range(-6f, 6f));
                    NetworkServer.Spawn(go);
                }
                Sfx.Play("Play_item_proc_monsterTooth_spawn", v.corePosition);
            };
        }
    }

    public class Taser : SimpleItem<Taser>
    {
        public override string ItemName => "Taser";
        public override string ItemLangTokenName => "CIC_TASER";
        public override ItemTier Tier => ItemTier.Tier1;
        public override ItemTag[] ItemTags => new[] { ItemTag.Utility, ItemTag.CanBeTemporary };
        float chance, dur, durStack;
        public override string PickupText => "Chance to snare enemies on hit.";
        public override string DescriptionText => $"{Ut(chance + "%")} chance on hit to {Ut("root")} enemies for {Ut(dur + "s")} {Stack("+" + durStack + "s")}.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { chance = Cfg("Chance", 7f); dur = Cfg("Duration", 1.5f); durStack = Cfg("Duration Per Stack", 0.5f); }
        static GameObject zap;
        public override void Hooks()
        {
            zap = CIC.Load<GameObject>("RoR2/Base/Common/VFX/OmniImpactVFXLightning.prefab");
            Sfx.Register("Play_item_proc_chain_lightning");
            CICHooks.onHitServer += (di, a, v, inv) =>
            {
                int c = GetCount(a);
                if (c <= 0 || v.HasBuff(RoR2Content.Buffs.Entangle) || !Util.CheckRoll(chance * di.procCoefficient, a.master)) return;
                v.AddTimedBuff(RoR2Content.Buffs.Entangle, dur + durStack * (c - 1));
                CIC.Fx(zap, v.corePosition, v.radius * 2f);
                Sfx.Play("Play_item_proc_chain_lightning", v.corePosition);
            };
        }
    }

    public class Spikestrip : SimpleItem<Spikestrip>
    {
        public override string ItemName => "Spikestrip";
        public override string ItemLangTokenName => "CIC_SPIKESTRIP";
        public override ItemTier Tier => ItemTier.Tier1;
        public override ItemTag[] ItemTags => new[] { ItemTag.Utility, ItemTag.CanBeTemporary };
        float radius, dur, durStack;
        public override string PickupText => "Getting hit scatters caltrops that slow enemies.";
        public override string DescriptionText => $"Getting hit scatters caltrops, {Ut("slowing")} enemies within {Ut(radius + "m")} by {Ut("50%")} for {Ut(dur + "s")} {Stack("+" + durStack + "s")}.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { radius = Cfg("Radius", 12f); dur = Cfg("Duration", 2f); durStack = Cfg("Duration Per Stack", 1f); }
        public override void Hooks()
        {
            var burst = CIC.Load<GameObject>("RoR2/Base/Common/VFX/OmniImpactVFXSlash.prefab");
            Sfx.Register("Play_item_proc_slowOnHitChains");
            var last = new System.Collections.Generic.Dictionary<CharacterBody, float>();
            CICHooks.onPostDamageServer += (hc, di, before) =>
            {
                int c = GetCount(hc.body);
                if (c <= 0) return;
                if (!last.TryGetValue(hc.body, out var t) || Time.time - t > 0.75f)
                {
                    last[hc.body] = Time.time;
                    CIC.Fx(burst, hc.body.footPosition, radius * 0.5f);
                    Sfx.Play("Play_item_proc_slowOnHitChains", hc.body.footPosition);
                }
                foreach (var hb in CIC.Enemies(hc.body.footPosition, radius, CIC.Team(hc.body)))
                    if (hb.healthComponent && hb.healthComponent.body) hb.healthComponent.body.AddTimedBuff(RoR2Content.Buffs.Slow50, dur + durStack * (c - 1));
            };
        }
    }

    public class HermitsScarf : SimpleItem<HermitsScarf>
    {
        public override string ItemName => "Hermit's Scarf";
        public override string ItemLangTokenName => "CIC_HERMITSCARF";
        public override ItemTier Tier => ItemTier.Tier1;
        public override ItemTag[] ItemTags => new[] { ItemTag.Utility, ItemTag.CanBeTemporary };
        float chance, speed, speedDur;
        static BuffDef evadeBuff;
        static GameObject fx;
        public override string PickupText => "Chance to evade damage. Evading gives a burst of speed.";
        public override string DescriptionText => $"{Ut(chance + "%")} {Stack("+" + chance + "%")} chance to {Ut("evade")} incoming damage. Evading grants {Ut(Pct(speed))} movement speed for {Ut(speedDur + "s")}. Stacks hyperbolically.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { chance = Cfg("Chance Per Stack", 10f); speed = Cfg("Speed Bonus", 0.4f); speedDur = Cfg("Speed Duration", 2f); }
        public override void Hooks()
        {
            evadeBuff = CIC.Buff("ScarfEvade", new Color(0.6f, 0.85f, 0.5f), false, false, "RoR2/Base/Common/texMovespeedBuffIcon.tif");
            fx = CIC.Load<GameObject>("RoR2/Base/Bear/BearProc.prefab");
            RecalculateStatsAPI.GetStatCoefficients += (b, args) => { if (b.HasBuff(evadeBuff)) args.moveSpeedMultAdd += speed; };
            CICHooks.onPreDamageServer += (hc, di, attacker) =>
            {
                if (di.rejected || (di.damageType.damageType & DamageType.BypassBlock) != 0) return;
                int c = GetCount(hc.body);
                if (c <= 0 || !Util.CheckRoll(CIC.Hyperbolic(chance / 100f, c) * 100f, hc.body.master)) return;
                di.rejected = true;
                hc.body.AddTimedBuff(evadeBuff, speedDur);
                if (fx) EffectManager.SpawnEffect(fx, new EffectData { origin = di.position, rotation = Util.QuaternionSafeLookRotation(di.force != Vector3.zero ? di.force : Random.onUnitSphere) }, true);
            };
        }
    }

    public class BarbedWire : SimpleItem<BarbedWire>
    {
        public override string ItemName => "Barbed Wire";
        public override string ItemLangTokenName => "CIC_BARBEDWIRE";
        public override ItemTier Tier => ItemTier.Tier1;
        public override ItemTag[] ItemTags => new[] { ItemTag.Damage, ItemTag.CanBeTemporary };
        public static float radius, radiusStack, dps, dpsStack;
        public override string PickupText => "Hurt nearby enemies.";
        public override string DescriptionText => $"Deal {Dmg(Pct(dps))} {Stack("+" + Pct(dpsStack))} damage per second to enemies within {Dmg(radius + "m")} {Stack("+" + radiusStack + "m")}.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { radius = Cfg("Radius", 6f); radiusStack = Cfg("Radius Per Stack", 1.5f); dps = Cfg("Damage Per Second", 0.5f); dpsStack = Cfg("Damage Per Second Per Stack", 0.25f); }
        public override void Hooks() { CharacterBody.onBodyInventoryChangedGlobal += b => b.AddItemBehavior<Behavior>(GetCount(b)); }
        public class Behavior : CharacterBody.ItemBehavior
        {
            float t; GameObject ring; int shown;
            void Start() { ring = Aura.Attach(body.transform, radius, new Color(0.75f, 0.78f, 0.82f, 1f)); }
            void OnDestroy() { if (ring) Destroy(ring); }
            void FixedUpdate()
            {
                if (ring && shown != stack) { shown = stack; Aura.SetRadius(ring, radius + radiusStack * (stack - 1)); }
                if (!NetworkServer.active) return;
                t += Time.fixedDeltaTime;
                if (t < 0.5f || !body || !body.healthComponent.alive) return;
                t = 0;
                CIC.Blast(body, body.corePosition, radius + radiusStack * (stack - 1), (dps + dpsStack * (stack - 1)) * 0.5f, 0.1f);
            }
        }
    }

    public class VoltaicMitt : SimpleItem<VoltaicMitt>
    {
        public override string ItemName => "Voltaic Mitt";
        public override string ItemLangTokenName => "CIC_VOLTAICMITT";
        public override ItemTier Tier => ItemTier.Tier1;
        public override ItemTag[] ItemTags => new[] { ItemTag.Damage, ItemTag.CanBeTemporary };
        public static float dps, dpsStack;
        static GameObject fx;
        public override string PickupText => "Electrify the air around you while airborne.";
        public override string DescriptionText => $"While {Ut("airborne")}, shock enemies within {Dmg("6m")} for {Dmg(Pct(dps))} {Stack("+" + Pct(dpsStack))} damage per second.";
        public override void CreateConfig(BepInEx.Configuration.ConfigFile config) { dps = Cfg("Damage Per Second", 0.8f); dpsStack = Cfg("Damage Per Second Per Stack", 0.5f); }
        public override void Hooks()
        {
            fx = CIC.Load<GameObject>("RoR2/Base/Common/VFX/OmniImpactVFXLightning.prefab");
            Sfx.Register("Play_item_proc_shocknearby_start");
            CharacterBody.onBodyInventoryChangedGlobal += b => b.AddItemBehavior<Behavior>(GetCount(b));
        }
        public class Behavior : CharacterBody.ItemBehavior
        {
            float t, sfx; GameObject ring;
            void Start() { ring = Aura.Attach(body.transform, 6f, new Color(0.45f, 0.75f, 1f, 1f)); }
            void OnDestroy() { if (ring) Destroy(ring); }
            void FixedUpdate()
            {
                bool air = body && body.characterMotor && !body.characterMotor.isGrounded;
                if (ring && ring.activeSelf != air) ring.SetActive(air);
                if (!NetworkServer.active) return;
                t += Time.fixedDeltaTime; sfx -= Time.fixedDeltaTime;
                if (t < 0.25f || !air) return;
                t = 0;
                var r = CIC.Blast(body, body.corePosition, 6f, (dps + dpsStack * (stack - 1)) * 0.25f, 0.2f, DamageTypeCombo.Generic);
                if (r.hitCount > 0) { CIC.Fx(fx, body.corePosition, 3f); if (sfx <= 0) { sfx = 1f; Sfx.Play("Play_item_proc_shocknearby_start", body.corePosition); } }
            }
        }
    }
}
