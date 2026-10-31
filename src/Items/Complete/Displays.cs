using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ClassicItemsReturns.Items.Complete
{
    /// <summary>
    /// Item displays on survivors, copied from the game's own placements at load time. Every ror2-plus item names a vanilla
    /// "analog" (Meat Nugget uses Bison Steak's spot); void items use their original's spot. For each survivor that has a
    /// placement for the analog, the same bone, position and angle are reused, and the scale is adjusted so our model
    /// takes up the same space as the vanilla display it borrows from. Works for modded survivors that place vanilla items.
    /// </summary>
    public static class Displays
    {
        public static bool enabled = true;

        class Entry { public Object def; public GameObject prefab; public string analog; }
        static readonly List<Entry> entries = new List<Entry>();
        static bool hooked;

        // ror2-plus item/equipment (class name) -> vanilla internal ItemDef/EquipmentDef name whose placement it borrows.
        static readonly Dictionary<string, string> analogs = new Dictionary<string, string>
        {
            { "MeatNugget", "FlatHealth" }, { "Taser", "StunChanceOnHit" }, { "Spikestrip", "SlowOnHit" }, { "HermitsScarf", "Phasing" },
            { "BarbedWire", "Thorns" }, { "VoltaicMitt", "SecondarySkillMagazine" },
            { "TimeKeepersSecret", "FragileDamageBonus" }, { "ChargefieldGenerator", "EquipmentMagazine" }, { "ConcussionGrenade", "StickyBomb" },
            { "DeadMansFoot", "Hoof" }, { "PanicMines", "Mushroom" }, { "ProphetsCape", "Feather" }, { "Insecticide", "SprintBonus" },
            { "DecayingSample", "Seed" }, { "FilialImprinting", "Infusion" }, { "ToxicCentipede", "BleedOnHit" }, { "OldBox", "Medkit" },
            { "BeatingEmbryo", "AutoCastEquipment" }, { "AtgMk2", "Missile" }, { "PlasmaChain", "BounceNearby" }, { "HeavenCracker", "Behemoth" },
            { "RapidMitosis", "AlienHead" }, { "RepulsionArmor", "ArmorPlate" }, { "LaserTurbine", "LaserTurbine" }, { "OlLopper", "ExecuteLowHealthElite" },
            { "PhotonJetpack", "UtilitySkillMagazine" }, { "TelescopicSight", "CritDamage" }, { "FiremansBoots", "FallBoots" }, { "AncientScepter", "Talisman" },
            { "Umbrella", "WarCryOnMultiKill" },
            { "LegendarySpark", "ShockNearby" }, { "ImpTentacle", "BeetleGland" }, { "BurningWitness", "FireballsOnHit" }, { "IfritsHorn", "LightningStrikeOnHit" },
            { "ScorchingShell", "Knurl" },
            { "RottenBrain", "Fruit" }, { "SafeguardLantern", "Lightning" }, { "Snowglobe", "Blackhole" }, { "ExplorersKey", "Recycle" },
            { "InstantMinefield", "CommandMissile" }, { "CarraraMarble", "Gateway" }, { "ShatteredMirror", "Scanner" }, { "GoldPlatedBomb", "GoldGat" },
            { "DynamitePlunger", "FireBallDash" }, { "MaceReplica", "Saw" }, { "CrudelyDrawnBuddy", "TeamWarCry" }, { "ShieldGenerator", "GainArmor" },
            { "UnstableWatch", "Cleanse" }, { "PillagedGold", "CritOnUse" }, { "CaptainsBrooch", "DroneBackup" }, { "SuperMassiveLeech", "LifestealOnHit" },
            { "NematocystNozzle", "BurnNearby" },
        };

        /// <summary>Register a ror2-plus item or equipment by its art/class name.</summary>
        public static void Register(string name, Object def, GameObject prefab)
        {
            if (analogs.TryGetValue(name, out var a)) Add(def, prefab, a);
        }

        /// <summary>Register with an explicit analog (void items pass their original's internal name).</summary>
        public static void Add(Object def, GameObject prefab, string analog)
        {
            if (!enabled || !def || !prefab || string.IsNullOrEmpty(analog)) return;
            entries.Add(new Entry { def = def, prefab = prefab, analog = analog });
            if (!hooked) { hooked = true; RoR2Application.onLoad += Apply; }
        }

        static Object Resolve(string internalName)
        {
            var ii = ItemCatalog.FindItemIndex(internalName);
            if (ii != ItemIndex.None) return ItemCatalog.GetItemDef(ii);
            var ei = EquipmentCatalog.FindEquipmentIndex(internalName);
            return ei != EquipmentIndex.None ? EquipmentCatalog.GetEquipmentDef(ei) : null;
        }

        static readonly Dictionary<GameObject, float> sizeCache = new Dictionary<GameObject, float>();

        /// <summary>Largest dimension of a prefab's visible meshes, in the prefab's own space.</summary>
        static float Size(GameObject prefab)
        {
            if (!prefab) return 0f;
            if (sizeCache.TryGetValue(prefab, out var s)) return s;
            var root = prefab.transform; Bounds? total = null;
            foreach (var mf in prefab.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh) total = Encap(total, mf.sharedMesh.bounds, mf.transform, root);
            foreach (var sk in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (sk.sharedMesh) total = Encap(total, sk.sharedMesh.bounds, sk.transform, root);
            s = total.HasValue ? Mathf.Max(total.Value.size.x, total.Value.size.y, total.Value.size.z) : 0f;
            sizeCache[prefab] = s; return s;
        }

        static Bounds Encap(Bounds? acc, Bounds local, Transform t, Transform root)
        {
            var m = root.worldToLocalMatrix * t.localToWorldMatrix;
            var b = new Bounds(m.MultiplyPoint3x4(local.center), Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                var c = local.center + Vector3.Scale(local.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                b.Encapsulate(m.MultiplyPoint3x4(c));
            }
            if (acc.HasValue) { var a = acc.Value; a.Encapsulate(b); return a; }
            return b;
        }

        static GameObject Follower(ItemDisplayRule r)
        {
            if (r.followerPrefab) return r.followerPrefab;
            try { if (r.followerPrefabAddress != null && r.followerPrefabAddress.RuntimeKeyIsValid()) return r.followerPrefabAddress.LoadAssetAsync<GameObject>().WaitForCompletion(); }
            catch { }
            return null;
        }

        static void Apply()
        {
            int sets = 0, rules = 0;
            var ours = new HashSet<Object>(entries.Select(e => e.def));
            var analogOf = entries.ToDictionary(e => e.def, e => e.analog);
            foreach (var body in BodyCatalog.allBodyPrefabs)
            {
                var model = body ? body.GetComponent<ModelLocator>()?.modelTransform?.GetComponent<CharacterModel>() : null;
                var idrs = model ? model.itemDisplayRuleSet : null;
                if (!idrs) continue;
                bool changed = false;
                foreach (var e in entries)
                {
                    // follow chains: a void of a ror2-plus item uses that item's analog
                    var key = Resolve(e.analog); int guard = 0;
                    while (key && ours.Contains(key) && guard++ < 4) key = Resolve(analogOf[key]);
                    if (!key) continue;
                    var src = idrs.FindDisplayRuleGroup(key);
                    if (src.rules == null || src.rules.Length == 0) continue;
                    float ourSize = Mathf.Max(0.01f, Size(e.prefab));
                    var copy = new List<ItemDisplayRule>();
                    foreach (var r in src.rules)
                    {
                        if (r.ruleType != ItemDisplayRuleType.ParentedPrefab) continue;
                        float theirs = Size(Follower(r));
                        float k = theirs > 0f ? theirs / ourSize : 1f;
                        copy.Add(new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab, followerPrefab = e.prefab, childName = r.childName,
                            localPos = r.localPos, localAngles = r.localAngles, localScale = r.localScale * k, limbMask = r.limbMask,
                        });
                    }
                    if (copy.Count == 0) continue;
                    idrs.SetDisplayRuleGroup(e.def, new DisplayRuleGroup { rules = copy.ToArray() });
                    changed = true; rules++;
                }
                if (changed) { idrs.GenerateRuntimeValues(); sets++; }
            }
            ClassicItemsReturnsPlugin.ModLogger.LogInfo($"ror2-plus displays: placed {entries.Count} items on {sets} bodies ({rules} placements).");
        }
    }
}
