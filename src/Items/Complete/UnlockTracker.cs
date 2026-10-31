using R2API;
using RoR2;
using RoR2.Achievements;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace ClassicItemsReturns.Items.Complete.Unlocks
{
    /// <summary>Client side of every Complete achievement. Server-decided ones just switch server tracking on.</summary>
    public abstract class CICAchievement : BaseAchievement
    {
        protected virtual string RequiredBody => null;
        protected virtual bool ServerTracked => true;
        protected virtual bool ClientCheck(CharacterBody b) => false;

        public override BodyIndex LookUpRequiredBodyIndex() => RequiredBody != null ? BodyCatalog.FindBodyIndex(RequiredBody) : BodyIndex.None;

        public override void OnInstall()
        {
            base.OnInstall();
            if (!ServerTracked) RoR2Application.onFixedUpdate += Tick;
        }
        public override void OnUninstall()
        {
            RoR2Application.onFixedUpdate -= Tick;
            base.OnUninstall();
        }
        public override void OnBodyRequirementMet() { base.OnBodyRequirementMet(); if (ServerTracked) SetServerTracked(true); }
        public override void OnBodyRequirementBroken() { if (ServerTracked) SetServerTracked(false); base.OnBodyRequirementBroken(); }

        float airborne, onFire;
        protected float Airborne(CharacterBody b) => airborne;
        protected float OnFire(CharacterBody b) => onFire;

        void Tick()
        {
            var b = localUser?.cachedBody;
            if (!b || !meetsBodyRequirement || !b.healthComponent || !b.healthComponent.alive) { airborne = onFire = 0; return; }
            airborne = b.characterMotor && !b.characterMotor.isGrounded ? airborne + Time.fixedDeltaTime : 0f;
            onFire = b.HasBuff(RoR2Content.Buffs.OnFire) ? onFire + Time.fixedDeltaTime : 0f;
            if (ClientCheck(b)) Grant();
        }
    }

    public class CICServerAchievement : BaseServerAchievement
    {
        internal static readonly List<CICServerAchievement> active = new List<CICServerAchievement>();
        public override void OnInstall() { base.OnInstall(); active.Add(this); }
        public override void OnUninstall() { active.Remove(this); base.OnUninstall(); }
        public CharacterMaster Master => networkUser ? networkUser.master : null;
        public void Award() => Grant();
    }

    /// <summary>
    /// Creates the unlockables, links them to items, and decides the server-side conditions.
    /// Granting is a no-op unless that player's achievement is currently tracked (not yet earned,
    /// and playing the required survivor).
    /// </summary>
    public static class UnlockTracker
    {
        public static bool requireUnlocks = true;

        public static void Setup()
        {
            var items = ClassicItemsReturnsPlugin.Items;
            var equips = ClassicItemsReturnsPlugin.Equipments;
            foreach (var r in UnlockTable.Rows)
            {
                string key = r.id;
                var item = r.equip ? null : items.FirstOrDefault(i => i.ItemLangTokenName == r.token);
                var equip = r.equip ? equips.FirstOrDefault(e => e.EquipmentLangTokenName == r.token) : null;
                var def = ScriptableObject.CreateInstance<UnlockableDef>();
                def.cachedName = "ror2plus." + key;
                def.nameToken = item != null && item.ItemDef ? item.ItemDef.nameToken : equip != null && equip.EquipmentDef ? equip.EquipmentDef.nameToken : r.name;
                def.achievementIcon = Modules.RuntimeArt.Icon("Ach_" + key) ?? (item != null && item.ItemDef ? item.ItemDef.pickupIconSprite : equip != null && equip.EquipmentDef ? equip.EquipmentDef.pickupIconSprite : null);
                Modules.PluginContentPack.unlockableDefs.Add(def);
                LanguageAPI.Add("ACHIEVEMENT_CIC_" + key + "_NAME", r.name);
                LanguageAPI.Add("ACHIEVEMENT_CIC_" + key + "_DESCRIPTION", r.desc);
                if (!requireUnlocks) continue;
                if (item != null && item.ItemDef) item.ItemDef.unlockableDef = def;
                if (equip != null && equip.EquipmentDef) equip.EquipmentDef.unlockableDef = def;
            }
            Hooks();
        }

        static void Grant(string id, CharacterMaster m)
        {
            if (!m) return;
            id = "CIC_" + id;
            foreach (var a in CICServerAchievement.active.ToArray())
                if (a.Master == m && a.achievementDef != null && a.achievementDef.identifier == id) a.Award();
        }
        static void GrantAll(string id) { foreach (var pcmc in PlayerCharacterMasterController.instances) Grant(id, pcmc.master); }

        // ---- per-run / per-stage player state (server) ----
        class State
        {
            public int secondaryUses, utilityUses, specialKills, crabKills, beetleKills, lockboxes, meteorKills, shrineStreak, oneShotStreak;
            public bool tookDamageRun, tookDamageTele, usedPrimaryTele;
            public float minHealthRun = 1f, minHealthTele = 1f, damageThisStage, goldBombTime = -99f;
            public readonly Queue<float> kills = new Queue<float>();
            public readonly Queue<float> bombKills = new Queue<float>();
        }
        static readonly Dictionary<CharacterMaster, State> states = new Dictionary<CharacterMaster, State>();
        static State S(CharacterMaster m) { if (!states.TryGetValue(m, out var s)) states[m] = s = new State(); return s; }
        static int tpBossKills;
        static readonly Dictionary<CharacterBody, float> bossSpawnTime = new Dictionary<CharacterBody, float>();
        static readonly HashSet<HealthComponent> damagedBefore = new HashSet<HealthComponent>();
        static readonly Dictionary<HealthComponent, bool> firstHit = new Dictionary<HealthComponent, bool>();
        static bool teleCharging;

        internal static void NoteGoldBomb(CharacterMaster m) { if (m) S(m).goldBombTime = Time.time; }
        internal static void NoteExplorersKeyChest(CharacterMaster m, GameObject chest) { if (m && chest && chest.name.Contains("GoldChest")) Grant("BROOCH", m); }

        static bool IsPlayer(CharacterMaster m) => m && m.playerCharacterMasterController;
        static bool IsSpecial(DamageInfo di) => (di.damageType.damageSource & DamageSource.Special) != 0;

        static void Hooks()
        {
            Run.onRunStartGlobal += _ => { states.Clear(); tpBossKills = 0; bossSpawnTime.Clear(); damagedBefore.Clear(); firstHit.Clear(); };
            Stage.onServerStageBegin += _ =>
            {
                damagedBefore.Clear(); firstHit.Clear(); teleCharging = false;
                foreach (var s in states.Values) s.damageThisStage = 0;
                if (!Run.instance) return;
                int cleared = Run.instance.stageClearCount;
                float time = Run.instance.GetRunStopwatch();
                foreach (var pc in PlayerCharacterMasterController.instances)
                {
                    var m = pc.master; if (!m) continue; var s = S(m);
                    if (cleared == 1 && time < 300f) Grant("WATCH", m);
                    if (cleared == 4 && time < 1200f) Grant("HYPERTHREADER", m);
                    if (cleared == 3 && !s.tookDamageRun) Grant("MACE", m);
                    if (cleared == 3 && s.minHealthRun >= 0.6f) Grant("SHIELDGEN", m);
                }
            };

            // Skill use counts (server copy of activations).
            On.RoR2.CharacterBody.OnSkillActivated += (orig, self, skill) =>
            {
                orig(self, skill);
                if (!NetworkServer.active || !self.skillLocator || !IsPlayer(self.master)) return;
                var s = S(self.master);
                if (skill == self.skillLocator.secondary && ++s.secondaryUses >= 150) Grant("TASER", self.master);
                if (skill == self.skillLocator.utility && ++s.utilityUses >= 75) Grant("CENTIPEDE", self.master);
                if (skill == self.skillLocator.primary && teleCharging) s.usedPrimaryTele = true;
            };

            CICHooks.onPreDamageServer += (hc, di, attacker) =>
            {
                if (!damagedBefore.Contains(hc)) { firstHit[hc] = true; damagedBefore.Add(hc); } else firstHit[hc] = false;
            };
            CICHooks.onPostDamageServer += (hc, di, before) =>
            {
                var m = hc.body ? hc.body.master : null;
                if (!IsPlayer(m)) return;
                var s = S(m);
                s.tookDamageRun = true; if (teleCharging) s.tookDamageTele = true;
                s.minHealthRun = Mathf.Min(s.minHealthRun, hc.combinedHealthFraction);
                if (teleCharging) s.minHealthTele = Mathf.Min(s.minHealthTele, hc.combinedHealthFraction);
                s.damageThisStage += di.damage;
                if (s.damageThisStage >= 5000f && hc.alive) Grant("SPIKESTRIP", m);
            };

            CharacterBody.onBodyStartGlobal += b => { if (NetworkServer.active && b.isBoss) bossSpawnTime[b] = Time.time; };

            CICHooks.onKillServer += r =>
            {
                var v = r.victimBody; var m = r.attackerMaster;
                if (v && v.isBoss)
                {
                    if (bossSpawnTime.TryGetValue(v, out float t0) && Time.time - t0 <= 15f) { GrantAll("USB"); GrantAll("CONCUSSION"); }
                    bossSpawnTime.Remove(v);
                    if (IsPlayer(m) && IsSpecial(r.damageInfo)) Grant("PILLAGEDGOLD", m);
                }
                string vn = v ? BodyCatalog.GetBodyName(v.bodyIndex) : "";
                if (vn == "ScavBody") GrantAll("CLOVER");
                if (vn == "ElectricWormBody") GrantAll("AMETHYST");
                if (!IsPlayer(m)) return;
                var s = S(m);
                if (IsSpecial(r.damageInfo))
                {
                    s.specialKills++;
                    if (s.specialKills >= 50) Grant("CHARGEFIELD", m);
                    if (s.specialKills >= 15) Grant("HITLIST", m);
                    if (s.specialKills >= 25) { Grant("BUDDY", m); Grant("LEECH", m); }
                }
                if (vn == "HermitCrabBody" && ++s.crabKills >= 20) Grant("FILIAL", m);
                if (vn.Contains("Beetle") && ++s.beetleKills >= 100) Grant("INSECTICIDE", m);
                if (r.damageInfo.inflictor && r.damageInfo.inflictor.name.Contains("Meteor") && ++s.meteorKills >= 20) Grant("UMBRELLA", m);

                float now = Time.time;
                s.kills.Enqueue(now);
                while (s.kills.Count > 0 && now - s.kills.Peek() > 3f) s.kills.Dequeue();
                if (s.kills.Count >= 20) Grant("LASERTURBINE", m);
                if (s.kills.Count(k => now - k <= 1f) >= 10) Grant("ARMSRACE", m);
                if (now - s.goldBombTime <= 1f && s.kills.Count(k => k >= s.goldBombTime) >= 12) Grant("PENNY", m);

                bool wasFirst = r.victim && firstHit.TryGetValue(r.victim, out bool f) && f;
                s.oneShotStreak = wasFirst ? s.oneShotStreak + 1 : 0;
                if (s.oneShotStreak >= 10) Grant("TELESCOPE", m);
            };

            ShrineChanceBehavior.onShrineChancePurchaseGlobal += (failed, interactor) =>
            {
                var m = interactor ? interactor.GetComponent<CharacterBody>()?.master : null;
                if (!IsPlayer(m)) return;
                var s = S(m);
                s.shrineStreak = failed ? 0 : s.shrineStreak + 1;
                if (s.shrineStreak >= 4) Grant("SNAKEEYES", m);
            };

            GlobalEventManager.OnInteractionsGlobal += (interactor, interactable, go) =>
            {
                var m = interactor ? interactor.GetComponent<CharacterBody>()?.master : null;
                if (!IsPlayer(m) || !go) return;
                string n = go.name;
                if (n.Contains("Stealth")) Grant("TOXIN", m);
                if (n.Contains("TimedChest")) Grant("MUCONSTRUCT", m);
                if (n.Contains("NewtStatue")) Grant("DEADMANSFOOT", m);
                if (n.Contains("Lockbox") && ++S(m).lockboxes >= 4) Grant("WICKEDRING", m);
            };

            TeleporterInteraction.onTeleporterBeginChargingGlobal += _ =>
            {
                if (!NetworkServer.active) return;
                teleCharging = true;
                foreach (var s in states.Values) { s.tookDamageTele = false; s.usedPrimaryTele = false; s.minHealthTele = 1f; }
                foreach (var pc in PlayerCharacterMasterController.instances) if (pc.master) S(pc.master);
            };
            TeleporterInteraction.onTeleporterChargedGlobal += _ =>
            {
                if (!NetworkServer.active) return;
                teleCharging = false;
                bool noEnemies = !TeamComponent.GetTeamMembers(TeamIndex.Monster).Any(t => t && t.body && t.body.healthComponent && t.body.healthComponent.alive);
                foreach (var pc in PlayerCharacterMasterController.instances)
                {
                    var m = pc.master; if (!m || !m.hasBody) continue; var s = S(m);
                    if (!s.tookDamageTele) Grant("CAPE", m);
                    if (s.minHealthTele >= 0.5f) Grant("PANICMINES", m);
                    if (!s.usedPrimaryTele) Grant("DECAYINGSAMPLE", m);
                    if (noEnemies) Grant("PHOTONJETPACK", m);
                }
            };

            BossGroup.onBossGroupDefeatedServer += group =>
            {
                var tp = TeleporterInteraction.instance;
                if (!tp || tp.bossGroup != group) return;
                if (++tpBossKills >= 5) GrantAll("MEDALLION");
                foreach (var pc in PlayerCharacterMasterController.instances)
                {
                    var b = pc.master ? pc.master.GetBody() : null;
                    if (b && b.healthComponent.alive && b.healthComponent.combinedHealthFraction < 0.2f) Grant("LOSTDOLL", pc.master);
                }
            };

            Run.onServerGameOver += (run, ending) =>
            {
                if (!ending || !ending.isWin) return;
                if (run.selectedDifficulty >= DifficultyIndex.Hard) GrantAll("SCEPTER");
            };

            RoR2Application.onFixedUpdate += () =>
            {
                if (NetworkServer.active && Run.instance && Run.instance.GetRunStopwatch() >= 2400f) GrantAll("LOPPER");
            };
        }
    }
}
