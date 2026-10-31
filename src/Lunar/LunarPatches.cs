using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;

namespace ClassicItemsReturns.Lunar
{
    /// <summary>
    /// IL edits that soften vanilla lunar downsides. Each patch first *finds* its exact spot in the game's code and only
    /// then edits; with apply = false it just reports whether the spot exists, so the patches can be checked offline
    /// against the real RoR2.dll (tools/check_lunar_patches) before shipping. Values come from LunarSettings.
    /// </summary>
    public static class LunarPatches
    {
        static bool LdLoc(Instruction i, int idx) => i.MatchLdloc(idx);

        static int ItemCountLocal(ILContext il, string itemField)
        {
            var c = new ILCursor(il); int loc = -1;
            if (c.TryGotoNext(x => x.MatchLdsfld(out Mono.Cecil.FieldReference f) && f.Name == itemField && f.DeclaringType.Name == "Items",
                              x => x.MatchCallOrCallvirt(out Mono.Cecil.MethodReference m) && m.Name == "GetItemCountEffective",
                              x => x.MatchStloc(out loc))) return loc;
            return -1;
        }

        // ---------------------------------------------------------------- CharacterBody.RecalculateStats
        /// <summary>Shaped Glass: max health x0.5 per stack -> x(glassHealthKeep) per stack.</summary>
        public static bool ShapedGlass(ILContext il, bool apply)
        {
            int loc = ItemCountLocal(il, "LunarDagger"); if (loc < 0) return false;
            var c = new ILCursor(il); bool found = false;
            // 2^stacks is used twice (damage and curse); only the one stored straight into cursePenalty is the health cost
            while (c.TryGotoNext(MoveType.After,
                x => x.MatchLdcR4(2f), x => x.MatchLdloc(loc), x => x.MatchConvR4(),
                x => x.MatchCall(out Mono.Cecil.MethodReference m) && m.Name == "Pow"))
                if (c.Next != null && c.Next.MatchCall(out Mono.Cecil.MethodReference set) && set.Name == "set_cursePenalty") { found = true; break; }
            if (!found) return false;
            if (apply) c.EmitDelegate<Func<float, float>>(v => LunarSettings.Enabled("ShapedGlass")
                ? UnityEngine.Mathf.Pow(1f / LunarSettings.glassHealthKeep, UnityEngine.Mathf.Log(v, 2f)) : v);
            return true;
        }

        /// <summary>Transcendence: health set to 1 -> keep a fraction of health as health.</summary>
        public static bool Transcendence(ILContext il, bool apply)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(MoveType.After, x => x.MatchLdarg(0), x => x.MatchLdcR4(1f),
                x => x.MatchCall(out Mono.Cecil.MethodReference m) && m.Name == "set_maxHealth")) return false;
            // ldarg.0; ldc.r4 1; call set_maxHealth  ->  ldarg.0; ldc.r4 1; <delegate(body, 1) -> kept health>; call set_maxHealth
            c.Index -= 1;
            if (apply)
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, RoR2.CharacterBody, float>>((one, body) =>
                    LunarSettings.Enabled("Transcendence") ? UnityEngine.Mathf.Max(one, body.maxHealth * LunarSettings.transcendenceHealthKeep) : one);
            }
            return true;
        }

        /// <summary>Light Flux: attack speed / (stacks + 1) -> / (1 + stacks * lightFluxPerStack).</summary>
        public static bool LightFlux(ILContext il, bool apply)
        {
            int loc = ItemCountLocal(il, "HalfAttackSpeedHalfCooldowns"); if (loc < 0) return false;
            var c = new ILCursor(il);
            if (!c.TryGotoNext(MoveType.After, x => LdLoc(x, loc), x => x.MatchLdcI4(1), x => x.MatchAdd(), x => x.MatchConvR4())) return false;
            if (!c.Next.MatchDiv()) return false;
            if (apply) c.EmitDelegate<Func<float, float>>(d => LunarSettings.Enabled("LightFlux") ? 1f + (d - 1f) * LunarSettings.lightFluxPerStack : d);
            return true;
        }

        /// <summary>Stone Flux: both movement-slow terms scaled so the total slow per stack is stoneFluxSlowPerStack.</summary>
        public static bool StoneFlux(ILContext il, bool apply)
        {
            int loc = ItemCountLocal(il, "HalfSpeedDoubleHealth"); if (loc < 0) return false;
            var c = new ILCursor(il); int found = 0;
            // term A:  ldloc n; conv.r4; add            term B:  ldloc n; conv.r4; ldc.r4 1; mul; add
            while (c.TryGotoNext(MoveType.After, x => LdLoc(x, loc), x => x.MatchConvR4()))
            {
                bool termA = c.Next.MatchAdd();
                bool termB = c.Next.MatchLdcR4(1f) && c.Next.Next.MatchMul() && c.Next.Next.Next.MatchAdd();
                if (!termA && !termB) continue;
                found++;
                if (apply) c.EmitDelegate<Func<float, float>>(n => LunarSettings.Enabled("StoneFlux") ? n * LunarSettings.stoneFluxSlowPerStack * 0.5f : n);
            }
            return found == 2;
        }

        /// <summary>Purity: luck -1 per stack -> -purityLuckPerStack per stack (fractional luck is rolled as a chance).</summary>
        public static bool Purity(ILContext il, bool apply)
        {
            int loc = ItemCountLocal(il, "LunarBadLuck"); if (loc < 0) return false;
            var c = new ILCursor(il);
            if (!c.TryGotoNext(MoveType.After, x => LdLoc(x, loc), x => x.MatchConvR4())) return false;
            while (!c.Next.MatchSub())
                if (!c.TryGotoNext(MoveType.After, x => LdLoc(x, loc), x => x.MatchConvR4())) return false;
            if (apply) c.EmitDelegate<Func<float, float>>(n => LunarSettings.Enabled("Purity") ? n * LunarSettings.purityLuckPerStack : n);
            return true;
        }

        /// <summary>Neutronium Weight: -30% (x0.9/stack) stats and -35 (-15/stack) armor -> configured values.</summary>
        public static bool Neutronium(ILContext il, bool apply)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(x => x.MatchLdsfld(out Mono.Cecil.FieldReference f) && f.Name == "TransferDebuffOnHit" && f.DeclaringType.Name == "Buffs")) return false;
            int start = c.Index, f07 = 0, f09 = 0, i35 = 0, i15 = 0;
            for (int i = start; i < Math.Min(start + 80, il.Instrs.Count); i++)
            {
                var ins = il.Instrs[i];
                if (ins.MatchLdcR4(0.7f)) f07++;
                else if (ins.MatchLdcR4(0.9f)) f09++;
                else if (ins.MatchLdcI4(35)) i35++;
                else if (ins.MatchLdcI4(15)) i15++;
            }
            if (f07 != 3 || f09 != 3 || i35 != 1 || i15 != 1) return false;
            if (!apply) return true;
            // swap each constant for a call that reads the setting (respecting the per-item toggle)
            c.Index = start;
            for (int k = 0; k < 8; k++)
            {
                if (!c.TryGotoNext(x => x.MatchLdcR4(0.7f) || x.MatchLdcR4(0.9f) || x.MatchLdcI4(35) || x.MatchLdcI4(15))) break;
                if (c.Index > start + 80) break;
                if (c.Next.MatchLdcR4(0.7f)) { c.Index++; c.EmitDelegate<Func<float, float>>(v => LunarSettings.Enabled("Neutronium") ? 1f - LunarSettings.neutroniumStatPenalty : v); }
                else if (c.Next.MatchLdcR4(0.9f)) { c.Index++; c.EmitDelegate<Func<float, float>>(v => LunarSettings.Enabled("Neutronium") ? 1f - LunarSettings.neutroniumStatPenaltyPerStack : v); }
                else if (c.Next.MatchLdcI4(35)) { c.Index++; c.EmitDelegate<Func<int, int>>(v => LunarSettings.Enabled("Neutronium") ? LunarSettings.neutroniumArmor : v); }
                else { c.Index++; c.EmitDelegate<Func<int, int>>(v => LunarSettings.Enabled("Neutronium") ? LunarSettings.neutroniumArmorPerStack : v); }
            }
            return true;
        }

        // ---------------------------------------------------------------- other methods
        /// <summary>Brittle Crown (HealthComponent.TakeDamageProcess): gold lost scales 0.5 + 0.25/stack instead of 1/stack.</summary>
        public static bool BrittleCrown(ILContext il, bool apply)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(MoveType.After, x => x.MatchLdfld(out Mono.Cecil.FieldReference f) && f.Name == "goldOnHit", x => x.MatchConvR4())) return false;
            if (!c.Next.MatchMul()) return false;
            if (apply) c.EmitDelegate<Func<float, float>>(n => LunarSettings.Enabled("BrittleCrown") ? LunarSettings.crownLossBase + LunarSettings.crownLossPerStack * (n - 1f) : n);
            return true;
        }

        /// <summary>Defiant Gouge (GlobalEventManager.OnInteractionBegin): monster credit 40 -> 40 * gougeCreditMult.</summary>
        public static bool DefiantGouge(ILContext il, bool apply)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(MoveType.After, x => x.MatchLdcR4(40f), x => x.MatchCall(out Mono.Cecil.MethodReference m) && m.Name == "get_instance" && m.DeclaringType.Name == "Stage")) return false;
            c.Index--;
            if (apply) c.EmitDelegate<Func<float, float>>(v => LunarSettings.Enabled("DefiantGouge") ? v * LunarSettings.gougeCreditMult : v);
            return true;
        }

        /// <summary>Corpsebloom (RepeatHealComponent): heal cap 10% / stacks -> corpsebloomCap / (1 + 0.5 * (stacks - 1)).</summary>
        public static bool Corpsebloom(ILContext il, bool apply)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(MoveType.After, x => x.MatchLdcR4(0.1f), x => x.MatchLdloc(out _), x => x.MatchConvR4(), x => x.MatchDiv())) return false;
            if (!c.Next.MatchStfld(out Mono.Cecil.FieldReference f) || f.Name != "healthFractionToRestorePerSecond") return false;
            if (apply) c.EmitDelegate<Func<float, float>>(v =>
            {
                if (!LunarSettings.Enabled("Corpsebloom") || v <= 0f) return v;
                float n = 0.1f / v;
                return LunarSettings.corpsebloomCap / (1f + LunarSettings.corpsebloomCapLossPerStack * (n - 1f));
            });
            return true;
        }

        /// <summary>Egocentrism (LunarSunBehavior.FixedUpdate): converts an item every 60s -> every egoInterval s.</summary>
        public static bool Egocentrism(ILContext il, bool apply)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(MoveType.After, x => x.MatchLdsfld(out Mono.Cecil.FieldReference f) && f.Name == "secondsPerTransform")) return false;
            if (apply) c.EmitDelegate<Func<float, float>>(v => LunarSettings.Enabled("Egocentrism") ? LunarSettings.egoInterval : v);
            return true;
        }

        /// <summary>Focused Convergence (FocusConvergenceController.ApplyRadius): radius / (2 * stacks) -> / (1 + convergenceShrinkPerStack * stacks).</summary>
        public static bool FocusedConvergence(ILContext il, bool apply)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld(out Mono.Cecil.FieldReference f) && f.Name == "convergenceRadiusDivisor",
                x => x.MatchLdarg(0), x => x.MatchLdfld(out Mono.Cecil.FieldReference g) && g.Name == "currentFocusConvergenceCount",
                x => x.MatchConvR4(), x => x.MatchMul())) return false;
            if (!c.Next.MatchDiv()) return false;
            if (apply) c.EmitDelegate<Func<float, float>>(v => LunarSettings.Enabled("FocusedConvergence") ? 1f + LunarSettings.convergenceShrinkPerStack * (v / 2f) : v);
            return true;
        }

        /// <summary>Spinel Tonic (EquipmentSlot.FireTonic): 80% safe roll -> 100 - tonicAfflictionChance.</summary>
        public static bool SpinelTonic(ILContext il, bool apply)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(MoveType.After, x => x.MatchLdcR4(80f))) return false;
            if (apply) c.EmitDelegate<Func<float, float>>(v => LunarSettings.Enabled("SpinelTonic") ? 100f - LunarSettings.tonicAfflictionChance : v);
            return true;
        }

        /// <summary>Gesture of the Drowned (EquipmentSlot.MyFixedUpdate): the forced-activation count reads as 0 when the auto-fire should be held.</summary>
        public static bool Gesture(ILContext il, bool apply)
        {
            var c = new ILCursor(il); int found = 0;
            while (c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld(out Mono.Cecil.FieldReference f) && f.Name == "AutoCastEquipment" && f.DeclaringType.Name == "Items",
                x => x.MatchCallOrCallvirt(out Mono.Cecil.MethodReference m) && m.Name == "GetItemCountEffective"))
            {
                found++;
                if (apply) { c.Emit(OpCodes.Ldarg_0); c.EmitDelegate<Func<int, RoR2.EquipmentSlot, int>>(LunarRework.GestureCount); }
            }
            return found == 2;
        }

        /// <summary>Lunar coin drops (PlayerCharacterMasterController, on kill): chance x coinDropMult.</summary>
        public static bool CoinDrop(ILContext il, bool apply)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(MoveType.After, x => x.MatchLdcR4(1f), x => x.MatchLdloc(out _),
                x => x.MatchLdfld(out Mono.Cecil.FieldReference f) && f.Name == "lunarCoinChanceMultiplier", x => x.MatchMul())) return false;
            if (apply) c.EmitDelegate<Func<float, float>>(v => LunarSettings.AcquisitionActive ? v * LunarSettings.coinDropMult : v);
            return true;
        }
    }
}
