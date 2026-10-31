# Vanilla item spec (measured from the game's own bundles, Oct 2026 build)

Sample: 29 items across base, SotV (DLC1), SotS (DLC2). Extracted with tools/study_vanilla.py; raw data in reference/vanilla/summary.json (not shipped).

## Pickup meshes
- Triangles: 220-3700, median ~700. Simple objects 200-600 (Seed 242, Knurl 260, Medkit 464), detailed 1500-3000 (Feather 3320, Headhunter 3076, Frost Relic 3036).
- Separate Display meshes exist for some items (simplified for bodies).

## Textures
- Diffuse is FLAT COLOUR, very small: Crowbar 8px, Bandolier 16px, Firework 16px, Infusion 2px, Medkit 128px. Detailed hero items 256-1024px (Dagger 256, Meteor 1024, DLC2 items 256-1024).
- Normal maps on ~70% of items (64-512px). Emission textures for glowing parts (Syringe, Beetle Gland, void items).
- The painted look comes from the shader (HGStandard ramp lighting), not baked lighting.

## Material (HGStandard) - typical values
- `_NormalStrength` 1.0-1.9, `_SpecularStrength` 0.1 (matte: Crowbar) to 1.0 (Syringe glass), `_SpecularExponent` 1-13, `_Smoothness` ~0.02.
- `_EmColor` + `_EmTex` for glow, `_EmPower` ~0.8-1. Void items: `_EmColor` hot pink (0.99, 0.08, 0.51).
- Keywords: `DITHER` (always), `CUTOUT` where alpha-tested, `FORCE_SPEC` on shiny metal.

## Icons
- 128x128 (base/DLC1), 256x256 (DLC2). Object fills 20-70% of the canvas.
- Thick tier-coloured outer stroke, thin dark inner line, small splats/drips scattered around the object.
- Tier stroke colours (median of measured edge pixels):
  - white `#A4B7B7`  green `#6EB33C`  red `#EF593C`  boss `#ADC525`  lunar `#34C2E3`  void `#9C66C5`
- Void items: small void glyph top-left; the object is the original reworked in purple/blue with hot-pink glow.

## Displays
- Each survivor's ItemDisplayRuleSet holds ~250 groups. Copy placements from an analogous vanilla item at runtime rather than hand-authoring.
