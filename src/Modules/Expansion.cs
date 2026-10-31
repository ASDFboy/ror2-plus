using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using System.Linq;
using UnityEngine;

namespace ClassicItemsReturns.Modules
{
    /// <summary>
    /// ror2-plus as a game expansion, the way the official DLCs are set up: it gets its own emblem and an
    /// on/off choice in the lobby's Expansions row, and every ror2-plus item, equipment and artifact belongs to it,
    /// so switching it off in the lobby removes them from the run. Void items stay on Survivors of the Void,
    /// which they need to work.
    /// </summary>
    public static class Expansion
    {
        public static ExpansionDef def;

        public static void Init()
        {
            def = ScriptableObject.CreateInstance<ExpansionDef>();
            def.name = "ror2plus";
            def.nameToken = "ROR2PLUS_EXPANSION_NAME";
            def.descriptionToken = "ROR2PLUS_EXPANSION_DESCRIPTION";
            def.iconSprite = RuntimeArt.Icon("ExpansionIcon");
            def.disabledIconSprite = RuntimeArt.Icon("ExpansionIconDisabled");
            LanguageAPI.Add(def.nameToken, "ror2-plus");
            LanguageAPI.Add(def.descriptionToken, "Adds content from the 'ror2-plus' expansion to the game: every Risk of Rain and Risk of Rain Returns item and equipment missing from Risk of Rain 2, their unlocks, four classic artifacts, and a void version of every item that lacked one.");
            PluginContentPack.expansionDefs.Add(def);
        }

        /// <summary>Called once every item, equipment and artifact exists (end of the plugin's Awake).</summary>
        public static void Assign()
        {
            if (!def) return;
            int n = 0;
            foreach (var i in ClassicItemsReturnsPlugin.Items)
                if (i?.ItemDef && !i.ItemDef.requiredExpansion) { i.ItemDef.requiredExpansion = def; n++; }
            foreach (var e in ClassicItemsReturnsPlugin.Equipments)
                if (e?.EquipmentDef && !e.EquipmentDef.requiredExpansion) { e.EquipmentDef.requiredExpansion = def; n++; }
            foreach (var a in ClassicItemsReturnsPlugin.Artifacts)
                if (a?.ArtifactDef && !a.ArtifactDef.requiredExpansion) { a.ArtifactDef.requiredExpansion = def; n++; }
            ClassicItemsReturnsPlugin.ModLogger.LogInfo($"ror2-plus expansion: {n} items, equipment and artifacts assigned.");
        }

        /// <summary>True unless a run is going with the ror2-plus expansion switched off in the lobby.</summary>
        public static bool Active => !def || !Run.instance || Run.instance.IsExpansionEnabled(def);
    }
}
