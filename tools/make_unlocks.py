"""Generates src/Items/Complete/Unlocks.Generated.cs from the table below.
Each row: (id, item/equipment lang token, is_equipment, required body or None, client_check or None, name, description)
Conditions marked client_check are evaluated on the player's own machine from synced stats;
everything else is decided server-side in UnlockTracker.cs."""
import os

ROWS = [
    ("TASER", "CIC_TASER", 0, "LoaderBody", None, "Grapple Happy", "As Loader, use your Secondary skill 150 times in a single run."),
    ("BITTERROOT", "BITTERROOT", 0, None, "b.maxHealth >= 1000f", "Big Boned", "Reach 1,000 maximum health."),
    ("SNAKEEYES", "SNAKEEYES", 0, None, None, "Hot Streak", "Win at a Shrine of Chance 4 times in a row."),
    ("HERMITSCARF", "CIC_HERMITSCARF", 0, "HuntressBody", "b.attackSpeed >= b.baseAttackSpeed * 2f", "Quickdraw", "As Huntress, reach 200% attack speed."),
    ("SPIKESTRIP", "CIC_SPIKESTRIP", 0, None, None, "Punching Bag", "Take 5,000 damage in a single stage and live."),
    ("VOLTAICMITT", "CIC_VOLTAICMITT", 0, None, "Airborne(b) >= 15f", "Frequent Flyer", "Stay in the air for 15 seconds straight."),
    ("TOXIN", "WEAKENONCONTACT", 0, None, None, "Hidden Cargo", "Open a Cloaked Chest."),
    ("MUCONSTRUCT", "MUCONSTRUCT", 0, None, None, "Against the Clock", "Open a Timed Security Chest."),
    ("PENNY", "PENNY", 0, None, None, "Money Shot", "Kill 12 enemies within 1 second of using the Gold-Plated Bomb."),
    ("ARCANEBLADES", "ARCANEBLADES", 0, None, "b.moveSpeed >= 30f", "Blur", "Reach a movement speed of 30 m/s."),
    ("CHARGEFIELD", "CIC_CHARGEFIELD", 0, "MercBody", None, "Thousand Cuts", "As Mercenary, kill 50 enemies with your Special skill in a single run."),
    ("ARMSRACE", "ARMSRACE", 0, "ToolbotBody", None, "Heavy Machinery", "As MUL-T, kill 10 enemies within 1 second."),
    ("GOLDENGUN", "GOLDENGUN", 0, None, "b.master && b.master.money >= 5000", "Nest Egg", "Hold $5,000 at once."),
    ("CLOVER", "56LEAFCLOVER", 0, None, None, "Junk Dealer", "Defeat a Scavenger."),
    ("CONCUSSION", "CIC_CONCUSSION", 0, "EngiBody", None, "Efficiency Expert", "As Engineer, kill a boss within 15 seconds of it appearing."),
    ("FILIAL", "CIC_FILIAL", 0, None, None, "Crab Season", "Kill 20 Hermit Crabs in a single run."),
    ("DEADMANSFOOT", "CIC_DEADMANSFOOT", 0, None, None, "Offering", "Activate a Newt Altar."),
    ("CENTIPEDE", "CIC_CENTIPEDE", 0, "CrocoBody", None, "Leapfrog", "As Acrid, use your Utility skill 75 times in a single run."),
    ("PANICMINES", "CIC_PANICMINES", 0, None, None, "Cool Under Fire", "Finish a Teleporter event without dropping below 50% health."),
    ("MEDALLION", "ROYALMEDALLION", 0, None, None, "Big Game Hunter", "Defeat 5 Teleporter bosses in a single run."),
    ("CAPE", "CIC_PROPHETSCAPE", 0, "CommandoBody", None, "Untouchable", "As Commando, finish a Teleporter event without taking damage."),
    ("JEWEL", "JEWEL", 0, None, "b.healthComponent.fullCombinedHealth > 0f && b.healthComponent.barrier >= b.healthComponent.fullCombinedHealth * 0.99f", "Fully Shielded", "Fill your barrier to your maximum health."),
    ("INSECTICIDE", "CIC_INSECTICIDE", 0, None, None, "Pest Control", "Kill 100 Beetles in a single run."),
    ("DECAYINGSAMPLE", "CIC_DECAYINGSAMPLE", 0, None, None, "Hands Off", "Finish a Teleporter event without using your Primary skill."),
    ("LASERTURBINE", "CIC_LASERTURBINE", 0, "ChefBody", None, "Rush Order", "As CHEF, kill 20 enemies within 3 seconds."),
    ("WICKEDRING", "WICKEDRING", 0, None, None, "Locksmith", "Open 4 Rusty Lockboxes in a single run."),
    ("LOPPER", "CIC_LOPPER", 0, None, None, "Long Haul", "Survive for 40 minutes in a single run."),
    ("HITLIST", "HITLIST", 0, "Bandit2Body", None, "Bounty Hunter", "As Bandit, kill 15 enemies with your Special skill in a single run."),
    ("PHOTONJETPACK", "CIC_PHOTONJETPACK", 0, None, None, "Clean Sweep", "Finish a Teleporter event with no enemies left alive."),
    ("TELESCOPE", "CIC_TELESCOPE", 0, "RailgunnerBody", None, "One Shot", "As Railgunner, kill 10 enemies in a row, each with its first hit."),
    ("FIREMANSBOOTS", "CIC_FIREMANSBOOTS", 0, None, "OnFire(b) >= 30f", "Hot Foot", "Stay on fire for 30 seconds straight."),
    ("HYPERTHREADER", "HYPERTHREADER", 0, None, None, "Express Lane", "Reach stage 5 in under 20 minutes."),
    ("SCEPTER", "CIC_SCEPTER", 0, "MercBody", None, "Master of Blades", "As Mercenary, beat the game or obliterate on Monsoon or harder."),
    ("USB", "USB", 0, None, None, "Blitz", "Defeat a boss within 15 seconds of it appearing."),
    ("UMBRELLA", "CIC_UMBRELLA", 0, None, None, "Weather Report", "Kill 20 enemies with the Glowing Meteorite in a single run."),
    ("MACE", "CIC_MACE", 1, "CommandoBody", None, "Clean Record", "As Commando, reach stage 4 without taking any damage."),
    ("AMETHYST", "RESETSKILLCOOLDOWN", 1, "LoaderBody", None, "Short Circuit", "As Loader, defeat an Overloading Worm."),
    ("BUDDY", "CIC_BUDDY", 1, "RailgunnerBody", None, "Marksman", "As Railgunner, kill 25 enemies with your Special skill in a single run."),
    ("DRUGS", "DRUGS", 1, None, "b.attackSpeed >= b.baseAttackSpeed * 3f", "Overclocked", "Reach 300% attack speed."),
    ("SHIELDGEN", "CIC_SHIELDGEN", 1, "ToolbotBody", None, "Built Tough", "As MUL-T, reach stage 4 without ever dropping below 60% health."),
    ("WATCH", "CIC_WATCH", 1, None, None, "Speedrunner", "Finish the first stage in under 5 minutes."),
    ("LOSTDOLL", "LOSTDOLL", 1, None, None, "Last Stand", "Defeat a Teleporter boss while below 20% health."),
    ("PILLAGEDGOLD", "CIC_PILLAGEDGOLD", 1, "Bandit2Body", None, "Showdown", "As Bandit, kill a boss with your Special skill."),
    ("BROOCH", "CIC_CAPTAINSBROOCH", 1, None, None, "Special Delivery", "Open a Legendary Chest with the Explorer's Key."),
    ("LEECH", "CIC_LEECH", 1, "CrocoBody", None, "Outbreak", "As Acrid, kill 25 enemies with your Special skill in a single run."),
]

def cls(r): return "Ach_" + r[0]

out = ["// <auto-generated> by tools/make_unlocks.py — edit the table there, not this file.",
       "using RoR2;", "", "namespace ClassicItemsReturns.Items.Complete.Unlocks", "{",
       "    public static partial class UnlockTable", "    {",
       "        public static readonly (string id, string token, bool equip, string name, string desc)[] Rows =", "        {"]
for r in ROWS:
    out.append(f'            ("{r[0]}", "{r[1]}", {"true" if r[2] else "false"}, "{r[5]}", "{r[6]}"),')
out += ["        };", "    }", ""]
for r in ROWS:
    id_, token, equip, body, check, name, desc = r
    out.append(f'    [RegisterAchievement("CIC_{id_}", "ror2plus.{id_}", null, 5u, {"null" if check else "typeof(CICServerAchievement)"})]')
    out.append(f'    public class {cls(r)} : CICAchievement')
    out.append("    {")
    if body: out.append(f'        protected override string RequiredBody => "{body}";')
    if check:
        out.append("        protected override bool ServerTracked => false;")
        out.append(f"        protected override bool ClientCheck(CharacterBody b) => {check};")
    out.append("    }")
    out.append("")
out.append("}")
p = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "src", "Items", "Complete", "Unlocks.Generated.cs")
open(p, "w").write("\n".join(out) + "\n")
print(len(ROWS), "achievements")
