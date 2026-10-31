using ClassicItemsReturns.Modules;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace ClassicItemsReturns.Items.Complete
{
    /// <summary>One set of hooks shared by every Complete item, so 50 items don't add 50 detours.</summary>
    public static class CICHooks
    {
        public delegate void OnHit(DamageInfo damageInfo, CharacterBody attacker, CharacterBody victim, Inventory inventory);
        /// <summary>Server only. Fired after a successful hit with procCoefficient &gt; 0.</summary>
        public static event OnHit onHitServer;

        public delegate void PreDamage(HealthComponent victim, DamageInfo damageInfo, CharacterBody attacker);
        /// <summary>Server only. Fired before damage is applied; may modify or reject damageInfo.</summary>
        public static event PreDamage onPreDamageServer;

        public delegate void PostDamage(HealthComponent victim, DamageInfo damageInfo, float healthFractionBefore);
        /// <summary>Server only. Fired after non-rejected damage was taken.</summary>
        public static event PostDamage onPostDamageServer;

        public delegate void Kill(DamageReport report);
        public static event Kill onKillServer;

        public delegate void SkillUsed(CharacterBody body, GenericSkill skill);
        /// <summary>Runs where the skill was activated (the body's authority).</summary>
        public static event SkillUsed onSkillActivated;

        private static bool init;
        public static void Init()
        {
            if (init) return;
            init = true;
            On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, di, victim) =>
            {
                orig(self, di, victim);
                if (!NetworkServer.active || onHitServer == null || di.rejected || di.procCoefficient <= 0f || !di.attacker || !victim) return;
                var ab = di.attacker.GetComponent<CharacterBody>();
                var vb = victim.GetComponent<CharacterBody>();
                if (ab && ab.inventory && vb) onHitServer(di, ab, vb, ab.inventory);
            };
            On.RoR2.HealthComponent.TakeDamageProcess += (orig, self, di) =>
            {
                if (!NetworkServer.active) { orig(self, di); return; }
                CharacterBody ab = di.attacker ? di.attacker.GetComponent<CharacterBody>() : null;
                onPreDamageServer?.Invoke(self, di, ab);
                float before = self.combinedHealthFraction;
                orig(self, di);
                if (!di.rejected && di.damage > 0f) onPostDamageServer?.Invoke(self, di, before);
            };
            GlobalEventManager.onCharacterDeathGlobal += r => { if (NetworkServer.active) onKillServer?.Invoke(r); };
            On.RoR2.CharacterBody.OnSkillActivated += (orig, self, skill) =>
            {
                orig(self, skill);
                onSkillActivated?.Invoke(self, skill);
            };
        }
    }

    public static class CIC
    {
        public static T Load<T>(string path) where T : UnityEngine.Object
        {
            try { return Addressables.LoadAssetAsync<T>(path).WaitForCompletion(); }
            catch (Exception e) { ClassicItemsReturnsPlugin.ModLogger.LogWarning("Missing asset " + path + ": " + e.Message); return null; }
        }

        private static GameObject _explosion, _explosionSmall;
        public static GameObject Explosion => _explosion ? _explosion : (_explosion = Load<GameObject>("RoR2/Base/Common/VFX/OmniExplosionVFX.prefab"));
        public static GameObject ExplosionSmall => _explosionSmall ? _explosionSmall : (_explosionSmall = Load<GameObject>("RoR2/Base/Common/VFX/OmniExplosionVFXQuick.prefab"));

        public static void Fx(GameObject prefab, Vector3 pos, float scale = 1f)
        {
            if (prefab) EffectManager.SpawnEffect(prefab, new EffectData { origin = pos, scale = scale }, true);
        }

        /// <summary>Server-side radial attack from a body.</summary>
        public static BlastAttack.Result Blast(CharacterBody owner, Vector3 pos, float radius, float damageCoefficient, float procCoefficient = 1f,
            DamageTypeCombo? type = null, GameObject fx = null, bool crit = false, ProcChainMask procChain = default, float force = 0f, float fxScale = -1f)
        {
            var ba = new BlastAttack
            {
                attacker = owner.gameObject,
                inflictor = owner.gameObject,
                teamIndex = owner.teamComponent ? owner.teamComponent.teamIndex : TeamIndex.Player,
                position = pos,
                radius = radius,
                baseDamage = owner.damage * damageCoefficient,
                crit = crit,
                procCoefficient = procCoefficient,
                procChainMask = procChain,
                falloffModel = BlastAttack.FalloffModel.None,
                damageColorIndex = DamageColorIndex.Item,
                damageType = type ?? DamageTypeCombo.Generic,
                baseForce = force,
                attackerFiltering = AttackerFiltering.NeverHitSelf
            };
            Fx(fx, pos, fxScale > 0 ? fxScale : radius);
            return ba.Fire();
        }

        public static List<HurtBox> Enemies(Vector3 pos, float radius, TeamIndex team)
        {
            var sphere = new SphereSearch { origin = pos, radius = radius, mask = LayerIndex.entityPrecise.mask, queryTriggerInteraction = QueryTriggerInteraction.UseGlobal };
            var list = new List<HurtBox>();
            sphere.RefreshCandidates().FilterCandidatesByHurtBoxTeam(TeamMask.GetEnemyTeams(team)).FilterCandidatesByDistinctHurtBoxEntities().GetHurtBoxes(list);
            return list;
        }

        public static TeamIndex Team(CharacterBody b) => b.teamComponent ? b.teamComponent.teamIndex : TeamIndex.None;

        /// <summary>Chance that ramps up as health drops: base at full, base+bonus at zero.</summary>
        public static float LowHealthChance(CharacterBody body, float baseChance, float bonusAtZero)
        {
            float frac = body.healthComponent ? body.healthComponent.combinedHealthFraction : 1f;
            return baseChance + bonusAtZero * (1f - frac);
        }

        public static float Hyperbolic(float perStack, int count) => 1f - 1f / (1f + perStack * count);

        /// <summary>Stuns (or slows, if unstunnable) a body and knocks it away from a point — our stand-in for RoR1's fear.</summary>
        public static void Terrify(HealthComponent hc, Vector3 from, float duration)
        {
            if (!hc || !hc.alive || !hc.body) return;
            var ssoh = hc.GetComponent<SetStateOnHurt>();
            if (ssoh && ssoh.canBeStunned) ssoh.SetStun(duration);
            else hc.body.AddTimedBuff(RoR2Content.Buffs.Slow50, duration);
            var dir = hc.transform.position - from; dir.y = 0; dir = dir.normalized;
            var motor = hc.body.characterMotor;
            if (motor && !hc.body.isBoss) motor.ApplyForce(dir * 2500f * Mathf.Max(1f, motor.mass / 100f), true, false);
        }

        public static void FireEngiMine(CharacterBody owner, Vector3 pos, float damageCoefficient, DamageTypeCombo type)
        {
            var prefab = Mine;
            if (!prefab) { Blast(owner, pos, 8f, damageCoefficient, 1f, type, Explosion); return; }
            ProjectileManager.instance.FireProjectile(new FireProjectileInfo
            {
                projectilePrefab = prefab,
                position = pos + Vector3.up,
                rotation = Quaternion.identity,
                owner = owner.gameObject,
                damage = owner.damage * damageCoefficient,
                crit = owner.RollCrit(),
                force = 0f,
                damageTypeOverride = type,
                damageColorIndex = DamageColorIndex.Item
            });
        }
        private static GameObject _mine;
        public static GameObject Mine => _mine ? _mine : (_mine = Load<GameObject>("RoR2/Base/Engi/EngiMine.prefab"));

        private static Sprite _glow;
        /// <summary>Soft round sprite generated at runtime for simple orbs, auras and markers.</summary>
        public static Sprite Glow
        {
            get
            {
                if (_glow) return _glow;
                const int n = 64;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2 - 1, dy = (y + 0.5f) / n * 2 - 1;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d); a = a * a * (3 - 2 * a);
                    float core = Mathf.Clamp01(1.6f - d * 2.2f);
                    px[y * n + x] = new Color(1, 1, 1, Mathf.Max(a, core));
                }
                tex.SetPixels32(px); tex.Apply();
                _glow = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
                return _glow;
            }
        }

        public static GameObject GlowObject(Transform parent, Color color, float size)
        {
            var go = new GameObject("CIC_Glow");
            if (parent) go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Glow; sr.color = color;
            go.transform.localScale = Vector3.one * size;
            go.AddComponent<Modules.CICBillboard>();
            return go;
        }

        public static BuffDef Buff(string name, Color color, bool stack, bool debuff, string iconPath, bool cooldown = false, bool hidden = false)
        {
            return Buffs.CreateBuffInternal("CIC_" + name, color, stack, null, iconPath != null ? Load<Sprite>(iconPath) : null, cooldown, debuff, hidden, null);
        }

        public static ItemDef VanillaItem(string addressablePath) => Load<ItemDef>(addressablePath);
    }

    /// <summary>
    /// Freezes enemies in place: stunned where possible, otherwise rooted and slowed. Shared by
    /// Time Keeper's Secret and Unstable Watch.
    /// </summary>
    public static class TimeStop
    {
        public static BuffDef buff;
        public static void Init()
        {
            if (buff) return;
            buff = CIC.Buff("TimeStopped", new Color(0.6f, 0.8f, 1f), false, true, "RoR2/Base/Common/texBuffSlow50Icon.tif");
            R2API.RecalculateStatsAPI.GetStatCoefficients += (body, args) =>
            {
                if (body.HasBuff(buff)) { args.moveSpeedRootCount += 1; args.attackSpeedReductionMultAdd += 9f; }
            };
        }

        public static void Stop(Vector3 center, float radius, TeamIndex ownerTeam, float duration)
        {
            foreach (var hb in CIC.Enemies(center, radius, ownerTeam))
            {
                var hc = hb.healthComponent;
                if (!hc || !hc.alive) continue;
                hc.body.AddTimedBuff(buff, duration);
                var ssoh = hc.GetComponent<SetStateOnHurt>();
                if (ssoh && ssoh.canBeStunned) ssoh.SetStun(duration);
            }
            // Hostile projectiles in range simply vanish.
            var list = InstanceTracker.GetInstancesList<ProjectileController>();
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var pc = list[i];
                if (pc && pc.teamFilter && pc.teamFilter.teamIndex != ownerTeam && (pc.transform.position - center).sqrMagnitude < radius * radius)
                    UnityEngine.Object.Destroy(pc.gameObject);
            }
        }
    }

    /// <summary>Blazing-elite style fire trail attached to a body (Fireman's Boots, Burning Witness).</summary>
    public class CICFireTrail : MonoBehaviour
    {
        public CharacterBody body;
        public string key;
        public float damagePerSecondCoefficient;
        private DamageTrail trail;
        private static GameObject prefab;

        private void Start()
        {
            if (!prefab) prefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/FireTrail");
            if (!prefab || !body) { enabled = false; return; }
            trail = Instantiate(prefab, body.transform).GetComponent<DamageTrail>();
            trail.transform.position = body.footPosition;
            trail.owner = body.gameObject;
            trail.radius *= body.radius;
        }

        private void FixedUpdate()
        {
            if (!trail || !body) return;
            trail.damagePerSecond = body.damage * damagePerSecondCoefficient;
            trail.active = body.characterMotor ? body.characterMotor.isGrounded : true;
        }

        private void OnDestroy() { if (trail) Destroy(trail.gameObject); }
    }
}

namespace ClassicItemsReturns.Items.Complete
{
    /// <summary>
    /// Networked vanilla sounds. Util.PlaySound from server code is only heard by the host; a registered
    /// NetworkSoundEventDef played through EffectManager is heard by everyone. Register at load (in Hooks), play anywhere.
    /// </summary>
    public static class Sfx
    {
        static readonly System.Collections.Generic.Dictionary<string, NetworkSoundEventDef> defs = new System.Collections.Generic.Dictionary<string, NetworkSoundEventDef>();

        public static void Register(params string[] events)
        {
            foreach (var ev in events)
            {
                if (defs.ContainsKey(ev)) continue;
                var d = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
                d.eventName = ev; d.name = "nse" + ev;
                R2API.ContentAddition.AddNetworkSoundEventDef(d);
                defs[ev] = d;
            }
        }

        /// <summary>Play at a position for all players (server), or locally (client-side code).</summary>
        public static void Play(string ev, Vector3 pos, GameObject fallback = null)
        {
            // Works from the server and from an authority client (EffectManager forwards it to everyone).
            if (defs.TryGetValue(ev, out var d) && (int)d.index >= 0)
                EffectManager.SimpleSoundEffect(d.index, pos, true);
            else if (fallback) Util.PlaySound(ev, fallback);
        }

        public static void Play(string ev, GameObject at) { if (at) Play(ev, at.transform.position, at); }
    }
}

namespace ClassicItemsReturns.Items.Complete
{
    /// <summary>Radius ring like Focus Crystal's, built from the game's own indicator, tinted per item. Client-side visual.</summary>
    public static class Aura
    {
        static GameObject indicator;
        const float BaseRadius = 13f;   // Focus Crystal's radius, which the vanilla indicator is sized for

        public static GameObject Attach(Transform parent, float radius, Color tint)
        {
            if (!indicator) indicator = CIC.Load<GameObject>("RoR2/Base/NearbyDamageBonus/NearbyDamageBonusIndicator.prefab");
            if (!indicator || !parent) return null;
            var go = UnityEngine.Object.Instantiate(indicator, parent.position, Quaternion.identity, parent);
            foreach (var nb in go.GetComponents<UnityEngine.Networking.NetworkBehaviour>()) UnityEngine.Object.Destroy(nb);
            var ni = go.GetComponent<UnityEngine.Networking.NetworkIdentity>(); if (ni) UnityEngine.Object.Destroy(ni);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var m = r.material;
                if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", tint);
                else if (m.HasProperty("_Color")) m.SetColor("_Color", tint);
            }
            SetRadius(go, radius);
            return go;
        }

        public static void SetRadius(GameObject go, float radius)
        {
            if (go) go.transform.localScale = Vector3.one * (radius / BaseRadius);
        }
    }
}
