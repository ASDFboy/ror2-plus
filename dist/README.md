# ror2-plus

Every item and equipment from Risk of Rain (2013) and Risk of Rain Returns that Risk of Rain 2 never got, adapted to fit RoR2's 3D combat, scaling and proc system.

This is a fork of **ClassicItemsReturns** by DestroyedClone, Moffein and contributors, used with their permission. It includes everything ClassicItemsReturns has, plus 52 new items and equipment. Don't install both — this mod replaces it.

## What's new on top of ClassicItemsReturns

**Common:** Meat Nugget, Taser, Spikestrip, Hermit's Scarf, Barbed Wire, Voltaic Mitt

**Uncommon:** Time Keeper's Secret, Chargefield Generator, Concussion Grenade, Dead Man's Foot, Panic Mines, Prophet's Cape, Insecticide, Decaying Sample, Filial Imprinting, Toxic Centipede, Old Box

**Legendary:** Beating Embryo, AtG Missile Mk. 2, Plasma Chain, Heaven Cracker, Rapid Mitosis, Repulsion Armor, Laser Turbine, The Ol' Lopper, Photon Jetpack, Telescopic Sight, Fireman's Boots, Ancient Scepter, Umbrella

**Boss:** Legendary Spark (Overloading Worm), Imp Overlord's Tentacle (Imp Overlord), Burning Witness (Magma Worm), Ifrit's Horn (Grandparent), Scorching Shell Piece (Beetle Queen), Nematocyst Nozzle (Wandering Vagrant, boss equipment)

**Equipment:** Rotten Brain, Safeguard Lantern, Snowglobe, Explorer's Key, Instant Minefield, Carrara Marble, Shattered Mirror, Gold-Plated Bomb, Dynamite Plunger, Mace Replica, Crudely-Drawn Buddy, Shield Generator, Unstable Watch, Pillaged Gold, Captain's Brooch, Super Massive Leech

**Artifacts:** four more artifacts from the original game, adapted for RoR2:
- **Distortion** — every minute one of your skills is locked at random; your other skills recharge 25% faster.
- **Spirit** — everyone moves faster the more health they're missing, up to double speed.
- **Origin** — from stage 2 on, an Imp invasion tears in a few minutes into each stage. The Imp Overlord drops a legendary item.
- **Tempus** — picking up an item also gives you 2 temporary copies of it, which fade away over time (using the game's own temporary items).

Every new item, equipment and void item has a 3D pickup model, shows up on survivors, has its own logbook entry, and uses the game's own effects and sounds.

**A proper expansion.** ror2-plus shows up in the lobby's Expansions row next to the official DLCs, with its own emblem. Switch it off there and its items, equipment, artifacts, lunar rework and lunar acquisition rules all sit that run out, no restart needed.

## Notable changes from the originals
- **Fear** doesn't exist in RoR2, so "terrify" effects stun and knock enemies back instead.
- **Time stop** (Time Keeper's Secret, Unstable Watch) stuns and roots enemies and deletes their projectiles in range. Time Keeper recharges in 90s instead of 7 minutes.
- **Hermit's Scarf** also gives a speed burst on evade, so it isn't just Tougher Times.
- **Prophet's Cape** gives a short invulnerability window that turns blocked hits into barrier, so it isn't just Safer Spaces.
- **Heaven Cracker** works off any survivor's Primary skill.
- **Ancient Scepter** can't hand-craft upgraded specials for every survivor, so it cuts your Special cooldown and empowers you after using it.
- **Shattered Mirror** summons a temporary shadow copy of you with all your items.
- **Voltaic Mitt** works while airborne, since RoR2 has no ropes.
- Every number is configurable in the config file, and every item can be disabled.

## Unlocks
Like in Risk of Rain and Returns, many items must be unlocked first. Requirements that couldn't carry over to RoR2 (Providence Trials, ropes, survivors RoR2 doesn't have) were swapped for something in the same spirit. Turn off `Require Unlocks` in the config to have everything available from the start. Each challenge has its own icon in the base game's unlock style.

- **Taser** — Grapple Happy: As Loader, use your Secondary skill 150 times in a single run.
- **Bitter Root** — Big Boned: Reach 1,000 maximum health.
- **Snake Eyes** — Hot Streak: Win at a Shrine of Chance 4 times in a row.
- **Hermit's Scarf** — Quickdraw: As Huntress, reach 200% attack speed.
- **Spikestrip** — Punching Bag: Take 5,000 damage in a single stage and live.
- **Voltaic Mitt** — Frequent Flyer: Stay in the air for 15 seconds straight.
- **The Toxin** — Hidden Cargo: Open a Cloaked Chest.
- **Mu Construct** — Against the Clock: Open a Timed Security Chest.
- **Razor Penny** — Money Shot: Kill 12 enemies within 1 second of using the Gold-Plated Bomb.
- **Arcane Blades** — Blur: Reach a movement speed of 30 m/s.
- **Chargefield Generator** — Thousand Cuts: As Mercenary, kill 50 enemies with your Special skill in a single run.
- **Arms Race** — Heavy Machinery: As MUL-T, kill 10 enemies within 1 second.
- **Golden Gun** — Nest Egg: Hold $5,000 at once.
- **56 Leaf Clover** — Junk Dealer: Defeat a Scavenger.
- **Concussion Grenade** — Efficiency Expert: As Engineer, kill a boss within 15 seconds of it appearing.
- **Filial Imprinting** — Crab Season: Kill 20 Hermit Crabs in a single run.
- **Dead Man's Foot** — Offering: Activate a Newt Altar.
- **Toxic Centipede** — Leapfrog: As Acrid, use your Utility skill 75 times in a single run.
- **Panic Mines** — Cool Under Fire: Finish a Teleporter event without dropping below 50% health.
- **Royal Medallion** — Big Game Hunter: Defeat 5 Teleporter bosses in a single run.
- **Prophet's Cape** — Untouchable: As Commando, finish a Teleporter event without taking damage.
- **Locked Jewel** — Fully Shielded: Fill your barrier to your maximum health.
- **Insecticide** — Pest Control: Kill 100 Beetles in a single run.
- **Decaying Sample** — Hands Off: Finish a Teleporter event without using your Primary skill.
- **Laser Turbine** — Rush Order: As CHEF, kill 20 enemies within 3 seconds.
- **Wicked Ring** — Locksmith: Open 4 Rusty Lockboxes in a single run.
- **The Ol' Lopper** — Long Haul: Survive for 40 minutes in a single run.
- **The Hit List** — Bounty Hunter: As Bandit, kill 15 enemies with your Special skill in a single run.
- **Photon Jetpack** — Clean Sweep: Finish a Teleporter event with no enemies left alive.
- **Telescopic Sight** — One Shot: As Railgunner, kill 10 enemies in a row, each with its first hit.
- **Fireman's Boots** — Hot Foot: Stay on fire for 30 seconds straight.
- **Hyper-Threader** — Express Lane: Reach stage 5 in under 20 minutes.
- **Ancient Scepter** — Master of Blades: As Mercenary, beat the game or obliterate on Monsoon or harder.
- **Classified Access Codes** — Blitz: Defeat a boss within 15 seconds of it appearing.
- **Umbrella** — Weather Report: Kill 20 enemies with the Glowing Meteorite in a single run.
- **Mace Replica** — Clean Record: As Commando, reach stage 4 without taking any damage.
- **Gigantic Amethyst** — Short Circuit: As Loader, defeat an Overloading Worm.
- **Crudely-Drawn Buddy** — Marksman: As Railgunner, kill 25 enemies with your Special skill in a single run.
- **Prescriptions** — Overclocked: Reach 300% attack speed.
- **Shield Generator** — Built Tough: As MUL-T, reach stage 4 without ever dropping below 60% health.
- **Unstable Watch** — Speedrunner: Finish the first stage in under 5 minutes.
- **Lost Doll** — Last Stand: Defeat a Teleporter boss while below 20% health.
- **Pillaged Gold** — Showdown: As Bandit, kill a boss with your Special skill.
- **Captain's Brooch** — Special Delivery: Open a Legendary Chest with the Explorer's Key.
- **Super Massive Leech** — Outbreak: As Acrid, kill 25 enemies with your Special skill in a single run.

## Void items
Every white, green and red item that didn't already have a void version now has one, including ror2-plus's own items: 156 in total. Like vanilla void items, picking one up corrupts all copies of its original, and they need Survivors of the Void. They can be turned off as a group (`Enable Void Items`) or one at a time in the config. Full effects are in the in-game logbook.

**Void common:** Abyssal Syringe (Soldier's Syringe), Gnawing Maw (Monster Tooth), Hollow Hoof (Paul's Goat Hoof), Fissure Bar (Crowbar), Dread Standard (Warbanner), Shelled Polyp (Cautious Slug), Null Carapace (Personal Shield Generator), Mending Spore (Medkit), Ichor Flask (Gasoline), Stillness Pearl (Stun Grenade), Starless Rockets (Bundle of Fireworks), Void Nectar (Energy Drink), Echoing Clip (Backup Magazine), Clinging Spawn (Sticky Bomb), Piercing Hollow (Armor-Piercing Rounds), Onyx Brooch (Topaz Brooch), Gravity Lens (Focus Crystal), Marrow Cut (Bison Steak), Fractal Plating (Repulsion Armor Plate), Nightshade Brew (Mocha), Tidal Draught (Power Elixir), Cracked Hourglass (Delicate Watch), Lightless Opal (Oddly-shaped Opal), Tarnished Coins (Roll of Pennies), Antlers of Silence (Elusive Antlers), Smothered Lantern (Bolstering Lantern), Collapsing Star (Chronic Expansion), Resounding Shell (Warped Echo), Umbral Disc (Eclipse Lite), Descent Treads (Hiker's Boots), Eclipsed Blades (Arcane Blades), Hollow Root (Bitter Root), Null Shield (Fire Shield), Sunken Savings (Life Savings), Void Construct (Mu Construct), Murky Vial (Mysterious Vial), Hollow Penny (Razor Penny), Blind Dice (Snake Eyes), Seeping Toxin (The Toxin), Rotten Morsel (Meat Nugget), Null Prod (Taser), Grasping Caltrops (Spikestrip), Shadow Scarf (Hermit's Scarf), Thorned Void (Barbed Wire), Static Glove (Voltaic Mitt)

**Void uncommon:** Ink-Stained Quill (Hopoo Feather), Parasite Bloom (Leeching Seed), Ravenous Eye (Predatory Instincts), Lash of Night (Red Whip), Veil Wrap (Old War Stealthkit), Reaper's Hook (Harvester's Scythe), Hungering Vessel (Infusion), Spent Casings (Bandolier), Howling Mantle (Berzerker's Pauldron), Abyssal Aegis (Rose Buckler), Frozen Abyss Band (Runald's Band), Hollow Wing (Wax Quail), Null Cleaver (Old Guillotine), Silent Horn (War Horn), Wilting Daisy (Lepton Daisy), Barbed Coil (Razorwire), Hoarder's Codex (Ghor's Tome), Drowned Polyp (Squid Polyp), Brand of Ruin (Death Mark), Undertow Hook (Hunter's Harpoon), Smothering Tank (Ignition Tank), Void Star (Shuriken), Reforming Husk (Regenerating Scrap), Unmarked Parcel (Shipping Request Form), Undertow Fin (Breaching Fin), Hollow Effigy (Chance Doll), Dim Shot (Luminous Shot), Spreading Blight (Noxious Thorn), Rosary of Silence (Prayer Beads), Black Star (Sale Star), Rift Beacon (Unstable Transmitter), Pressure Charge (Box of Dynamite), Collector's Void (Collector's Compulsion), Static Hollow (Faraday Spur), Kinetic Sink (Kinetic Dampener), Arms of the Deep (Arms Race), Hard Shell (Tough Times), Crushing Grip (Boxing Gloves), Wilted Clover (56 Leaf Clover), Drained Cell (Energy Cell), Tarnished Gun (Golden Gun), Hollow Heart (Guardian's Heart), Clouded Jewel (Locked Jewel), Void Mortar (Mortar Tube), Abyssal Chains (Prison Shackles), Fallen Medallion (Royal Medallion), Sunken Jetpack (Rusty Jetpack), Hoarder's Purse (Smart Shopper), Stopped Clock (Time Keeper's Secret), Null Field Generator (Chargefield Generator), Silencing Grenade (Concussion Grenade), Drowned Foot (Dead Man's Foot), Void Mines (Panic Mines), Heretic's Cape (Prophet's Cape), Black Spray (Insecticide), Rotting Sample (Decaying Sample), Abandoned Brood (Filial Imprinting), Void Crawler (Toxic Centipede)

**Void legendary:** Imploding Core (Brilliant Behemoth), Hollow Fang (Ceremonial Dagger), Abyssal Maelstrom (Frost Relic), Lament Mask (Happiest Mask), Gravity Well (H3AD-5T v2), Devouring Opinion (N'kuhana's Opinion), Null Coil (Unstable Tesla Coil), Grasping Hook (Sentient Meat Hook), Starless Mind (Alien Head), Hollow Catalyst (Soulbound Catalyst), Void Thrusters (Hardlight Afterburner), Carrion Crown (Wake of Vultures), Hollow Thoughts (Brainstalks), Empty Rack (Rejuvenation Rack), Shattered Aegis (Aegis), Unmaking Gavel (Shattering Justice), Silent Disc (Resonance Disc), Withered Planter (Interstellar Desk Plant), Null Field (Defensive Microbots), Void Sight (Laser Scope), Abyssal Payload (Pocket I.C.B.M.), Cannibal Circuitry (Spare Drone Parts), Hollow Stinger (Symbiotic Scorpion), Shroud of Stillness (Ben's Raincoat), Unbottled Void (Bottled Chaos), Returning Eclipse (Electric Boomerang), Rotting Nectar (Growth Nectar), Fallen Rune (Runic Lens), Muted Whispers (Sonorous Whispers), Void Debt (War Bonds), Severed Network (Networked Suffering), Abandoned Nucleus (Orphaned Core), Hollow Copy (Substandard Duplicator), Death Ledger (The Hit List), Unravelling Thread (Hyper-Threader), Abyssal Frost (Permafrost), Void Thallium (Thallium), Corrupted Codes (Classified Access Codes), Hollow Ring (Wicked Ring), Hollow Box (Old Box), Stillborn Embryo (Beating Embryo), Plasma Barrage (AtG Missile Mk. 2), Severing Chain (Plasma Chain), Abyss Cracker (Heaven Cracker), Arrested Mitosis (Rapid Mitosis), Absorption Armor (Repulsion Armor), Void Turbine (Laser Turbine), Rusted Cleaver (The Ol' Lopper), Graviton Pack (Photon Jetpack), Obsidian Scope (Telescopic Sight), Ashen Treads (Fireman's Boots), Hollow Scepter (Ancient Scepter), Black Parasol (Umbrella)

## Lunar rework
Lunars are harder to get, but their downsides are gentler. Each one still has the same trade-off, just less harsh. Every change has its own on/off switch and number in the config (`Lunar Rework`), and logbook text updates to match.

| Lunar | Vanilla downside | ror2-plus |
|---|---|---|
| Shaped Glass | -50% max health per stack | -30% per stack (multiplicative) |
| Transcendence | Health set to 1 | Keeps 10% of your health as health |
| Light Flux Pauldron | Attack speed ÷(1 + stacks) | ÷(1 + 0.25 × stacks) |
| Stone Flux Pauldron | Move speed -50% per stack | ÷(1 + 0.33 × stacks), -25% at one stack |
| Purity | +1 unlucky reroll per stack | 50% chance of an unlucky reroll per stack |
| Neutronium Weight | -30% stats, -35 armor | -15% (-5%/stack), -20 armor (-10/stack) |
| Brittle Crown | Lose 100% (+100%) gold scaling | 50% (+25% per stack) |
| Defiant Gouge | Full-strength shrine spawns | 60% strength |
| Corpsebloom | 10% heal cap, halved per stack | 15% cap, shrinks more slowly |
| Egocentrism | Any item converted every 60s | Only white/green items, every 120s |
| Focused Convergence | Zone -50% (halved per stack) | Zone ÷(1 + 0.33 × stacks) |
| Gesture of the Drowned | Always fires | Only fires with an enemy within 60m; never auto-fires Glowing Meteorite, Helfire, Effigy, Recycler, Crowdfunder, Elegy, Vase, Trophy Hunter's Tricorn or Executive Card |
| Visions/Hooks/Strides/Essence of Heresy | Full cooldown added per extra stack | Half a cooldown per extra stack |
| Mercurial Rachis | Enemies get +50% damage | Enemies get +25% (allies still +50%) |
| Longstanding Solitude | Costs +50% per stack | +25% per stack |
| Glowing Meteorite | Full damage to you and allies | 25% damage to your team |
| Helfire Tincture | 5% max health/s self burn | 2.5%/s; enemies burn exactly as before |
| Effigy of Grief | Slows you 50%, -20 armor | Your team is slowed 25% and keeps its armor |
| Spinel Tonic | 20% affliction chance | 10% |

Beads of Fealty, Eulogy Zero and Elegy of Extinction have no real downside, so they're unchanged.

**Harder to get** (`Lunar Acquisition`, one master switch, each part adjustable): Lunar Pods cost 2 coins, Bazaar lunar prices ×1.5, lunar coins drop 40% less often, Lunar Pods appear half as often. Off by default: **Bazaar Only** (no pods on stages) and **Trade-In** (taking a lunar costs you a random white or green item, if you have one).

## Settings
The quickest switch is in the lobby: turn the **ror2-plus** expansion off for a run, like any DLC.

Everything is in the config file (`BepInEx/config/com.ASDFboy.ror2plus.cfg`). If you have **Risk of Options** installed, the same settings show up in the in-game Mod Options menu, grouped into General, Items, Void Items, Lunars, Artifacts and Tuning. Most settings take effect after a restart.

Useful switches under General: `Require Unlocks`, `Enable Void Items`, `Use 3d Models` (off = flat icon cards), `Item Displays` (items shown on survivors).

## Known limitations (v1.0.0)
- Item displays reuse the placement of a similar vanilla item on each survivor (void items use their original's spot), so they sit where the game puts things. A few may still look odd for unusual shapes; turn them off with `Item Displays` if needed.
- Glass parts of models (vials, the snow globe's dome) are left off the 3D pickups.

## Credits
- New items, icons and code: ASDFboy and Claude
- ClassicItemsReturns (base mod, models, sounds): DestroyedClone, Moffein and contributors
