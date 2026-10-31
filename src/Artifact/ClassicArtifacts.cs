using ClassicItemsReturns.Items.Complete;
using ClassicItemsReturns.Modules;
using R2API;
using RoR2;
using RoR2.Skills;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace ClassicItemsReturns.Artifact
{
    /// <summary>Base for ror2-plus artifacts: language via LanguageAPI, original icons from embedded PNGs.</summary>
    public abstract class SimpleArtifact<T> : ArtifactBase<T> where T : SimpleArtifact<T>
    {
        public abstract string Description { get; }
        public override Sprite ArtifactEnabledIcon => RuntimeArt.Icon("Art_" + GetType().Name + "_On");
        public override Sprite ArtifactDisabledIcon => RuntimeArt.Icon("Art_" + GetType().Name + "_Off");
        protected BepInEx.Configuration.ConfigFile Config;
        public override void Init(BepInEx.Configuration.ConfigFile config)
        {
            Config = config;
            base.Init(config);
            LanguageAPI.Add(ArtifactDef.nameToken, ArtifactName);
            LanguageAPI.Add(ArtifactDef.descriptionToken, Description);
        }
        public bool Enabled => RunArtifactManager.instance && ArtifactDef && RunArtifactManager.instance.IsArtifactEnabled(ArtifactDef);
    }

    // ------------------------------------------------------------------ Distortion
    public class Distortion : SimpleArtifact<Distortion>
    {
        public override string ArtifactName => "Artifact of Distortion";
        public override string ArtifactLangTokenName => "CIC_DISTORTION";
        public override string Description => "Every minute, one of your skills is locked at random. All other skills recharge 25% faster.";
        static BuffDef[] locks = new BuffDef[4];
        static SkillDef disabled;
        public override void Hooks()
        {
            for (int i = 0; i < 4; i++) locks[i] = CIC.Buff("DistortLock" + i, new Color(0.5f, 0.3f, 0.7f), false, true, "RoR2/Base/Common/texBuffSlow50Icon.tif", hidden: false);
            disabled = CIC.Load<SkillDef>("RoR2/Base/Captain/CaptainSkillDisconnected.asset");
            R2API.RecalculateStatsAPI.GetStatCoefficients += (b, args) => { if (Enabled && b.isPlayerControlled) args.cooldownMultAdd -= 0.25f; };
            CharacterBody.onBodyStartGlobal += b => { if (Enabled && b.isPlayerControlled && !b.GetComponent<Lock>()) b.gameObject.AddComponent<Lock>().body = b; };
        }
        public override void OnArtifactEnabled() { }
        public override void OnArtifactDisabled() { }

        /// <summary>Server picks the slot (synced as a buff); the body's owner applies the actual lock.</summary>
        public class Lock : MonoBehaviour
        {
            public CharacterBody body; float timer = 10f; int applied = -1;
            void FixedUpdate()
            {
                if (!body || !Instance.Enabled) return;
                if (NetworkServer.active)
                {
                    timer -= Time.fixedDeltaTime;
                    if (timer <= 0) { timer = 60f; body.AddTimedBuff(locks[Random.Range(0, 4)], 60f); }
                }
                if (!body.hasEffectiveAuthority || !body.skillLocator || !disabled) return;
                int want = -1; for (int i = 0; i < 4; i++) if (body.HasBuff(locks[i])) want = i;
                if (want == applied) return;
                if (applied >= 0) Slot(applied)?.UnsetSkillOverride(this, disabled, GenericSkill.SkillOverridePriority.Contextual);
                if (want >= 0) Slot(want)?.SetSkillOverride(this, disabled, GenericSkill.SkillOverridePriority.Contextual);
                applied = want;
            }
            GenericSkill Slot(int i) { var s = body.skillLocator; return i == 0 ? s.primary : i == 1 ? s.secondary : i == 2 ? s.utility : s.special; }
            void OnDestroy() { if (applied >= 0 && body && body.skillLocator) Slot(applied)?.UnsetSkillOverride(this, disabled, GenericSkill.SkillOverridePriority.Contextual); }
        }
    }

    // ------------------------------------------------------------------ Spirit
    public class Spirit : SimpleArtifact<Spirit>
    {
        public override string ArtifactName => "Artifact of Spirit";
        public override string ArtifactLangTokenName => "CIC_SPIRIT";
        public override string Description => "All characters move faster the more health they are missing, up to double speed.";
        public override void Hooks()
        {
            R2API.RecalculateStatsAPI.GetStatCoefficients += (b, args) =>
            {
                if (Enabled && b.healthComponent) args.moveSpeedMultAdd += 1f - b.healthComponent.combinedHealthFraction;
            };
            CharacterBody.onBodyStartGlobal += b => { if (Enabled && !b.GetComponent<Refresh>()) b.gameObject.AddComponent<Refresh>().body = b; };
        }
        public override void OnArtifactEnabled() { }
        public override void OnArtifactDisabled() { }
        /// <summary>Health changes don't trigger a stat recalculation, so nudge it when health moves noticeably.</summary>
        public class Refresh : MonoBehaviour
        {
            public CharacterBody body; float last = 1f, t;
            void FixedUpdate()
            {
                if (!body || !body.healthComponent) return;
                t += Time.fixedDeltaTime; if (t < 0.25f) return; t = 0;
                float f = body.healthComponent.combinedHealthFraction;
                if (Mathf.Abs(f - last) > 0.02f) { last = f; body.MarkAllStatsDirty(); }
            }
        }
    }

    // ------------------------------------------------------------------ Origin
    public class Origin : SimpleArtifact<Origin>
    {
        public override string ArtifactName => "Artifact of Origin";
        public override string ArtifactLangTokenName => "CIC_ORIGIN";
        public override string Description => "Imp invasions tear into each stage after a few minutes. Their Overlord carries a legendary item.";
        static SpawnCard imp, impBoss;
        static float delay;
        public override void Hooks()
        {
            delay = Config.Bind("Artifact: " + ArtifactName, "Invasion Delay", 240f, "Seconds into each stage before the imps arrive.").Value;
            imp = CIC.Load<SpawnCard>("RoR2/Base/Imp/cscImp.asset");
            impBoss = CIC.Load<SpawnCard>("RoR2/Base/ImpBoss/cscImpBoss.asset");
            Stage.onServerStageBegin += s => { if (Enabled && Run.instance.stageClearCount >= 1) new GameObject("CIC_OriginTimer").AddComponent<Invasion>(); };
            CICHooks.onKillServer += r =>
            {
                if (!r.victimBody || !r.victimBody.GetComponent<Carrier>()) return;
                var drop = PickupDropTable.GenerateDropFromWeightedSelection(Run.instance.treasureRng, Run.instance.availableTier3DropList.Count > 0 ? WeightedTier3() : null);
                if (drop.isValid) PickupDropletController.CreatePickupDroplet(drop, r.victimBody.corePosition, Vector3.up * 20f);
            };
        }
        static WeightedSelection<PickupIndex> WeightedTier3()
        {
            var w = new WeightedSelection<PickupIndex>();
            foreach (var p in Run.instance.availableTier3DropList) w.AddChoice(p, 1f);
            return w;
        }
        public override void OnArtifactEnabled() { }
        public override void OnArtifactDisabled() { }
        public class Carrier : MonoBehaviour { }
        public class Invasion : MonoBehaviour
        {
            float t;
            void FixedUpdate()
            {
                t += Time.fixedDeltaTime;
                if (t < delay) return;
                Spawn(); Destroy(gameObject);
            }
            void Spawn()
            {
                if (!DirectorCore.instance || !imp) return;
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = "<style=cIsVoid>The air splits open. The imps have come.</style>" });
                var players = PlayerCharacterMasterController.instances.Where(p => p.master && p.master.GetBody()).Select(p => p.master.GetBody()).ToList();
                if (players.Count == 0) return;
                var rng = new Xoroshiro128Plus(Run.instance.stageRng.nextUlong);
                int count = 3 + Mathf.RoundToInt(Run.instance.stageClearCount * 1.5f);
                for (int i = 0; i < count; i++) Place(imp, players[i % players.Count], rng);
                if (impBoss) { var boss = Place(impBoss, players[0], rng); if (boss) boss.AddComponent<CarrierMarker>(); }
            }
            GameObject Place(SpawnCard card, CharacterBody near, Xoroshiro128Plus rng)
            {
                var req = new DirectorSpawnRequest(card, new DirectorPlacementRule { placementMode = DirectorPlacementRule.PlacementMode.Approximate, position = near.footPosition, minDistance = 15f, maxDistance = 40f }, rng) { teamIndexOverride = TeamIndex.Monster, ignoreTeamMemberLimit = true };
                return DirectorCore.instance.TrySpawnObject(req);
            }
        }
        /// <summary>Attached to the Overlord's master; moves the carrier tag onto its body once it exists.</summary>
        public class CarrierMarker : MonoBehaviour
        {
            void FixedUpdate()
            {
                var m = GetComponent<CharacterMaster>(); var b = m ? m.GetBody() : null;
                if (b) { b.gameObject.AddComponent<Carrier>(); Destroy(this); }
            }
        }
    }

    // ------------------------------------------------------------------ Tempus
    public class Tempus : SimpleArtifact<Tempus>
    {
        public override string ArtifactName => "Artifact of Tempus";
        public override string ArtifactLangTokenName => "CIC_TEMPUS";
        public override string Description => "Picking up an item also grants 2 temporary copies of it, which fade away over time.";
        public override void Hooks()
        {
            // Uses the game's own temporary items, so the copies show and decay in the normal inventory UI.
            On.RoR2.GenericPickupController.AttemptGrant += (orig, self, body) =>
            {
                var pickup = self.pickupIndex;
                orig(self, body);
                if (!NetworkServer.active || !Enabled || !self.consumed || !body || !body.inventory) return;
                var def = PickupCatalog.GetPickupDef(pickup);
                if (def == null || def.itemIndex == ItemIndex.None) return;
                var item = ItemCatalog.GetItemDef(def.itemIndex);
                if (!item || item.hidden || item.tier == ItemTier.NoTier || item.tier == ItemTier.Lunar) return;
                body.inventory.GiveItemTemp(item.itemIndex, 2f);            };
        }
        public override void OnArtifactEnabled() { }
        public override void OnArtifactDisabled() { }
    }
}
