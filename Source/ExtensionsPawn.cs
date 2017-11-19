using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public static class ExtensionsPawn {
        public static Pawn Copy(this Pawn source) {

            PawnHealthState savedHealthState = source.health.State;

            Pawn result = (Pawn)ThingMaker.MakeThing(source.kindDef.race, null);
            result.kindDef = source.kindDef;
            result.SetFactionDirect(source.Faction);
            PawnComponentsUtility.CreateInitialComponents(result);

            // Copy gender.
            result.gender = source.gender;

            // Copy name;
            NameTriple nameTriple = source.Name as NameTriple;
            NameSingle nameSingle = source.Name as NameSingle;
            if (nameTriple != null) {
                result.Name = new NameTriple(nameTriple.First, nameTriple.Nick, nameTriple.Last);
            }
            else if (nameSingle != null) {
                result.Name = new NameSingle(nameSingle.Name, nameSingle.Numerical);
            }

            // Copy trackers.
            object[] constructorArgs = new object[] { result };
            result.ageTracker = UtilityCopy.CopyExposable(source.ageTracker, constructorArgs);
            result.story = UtilityCopy.CopyExposable(source.story, constructorArgs);
            result.skills = UtilityCopy.CopyExposable(source.skills, constructorArgs);
            result.health = UtilityCopy.CopyExposable(source.health, constructorArgs);
            result.apparel = UtilityCopy.CopyExposable(source.apparel, constructorArgs);

            // Copy properties added to pawns by mods.
            CopyModdedProperties(source, result);

            // Verify the pawn health state.
            if (result.health.State != savedHealthState) {
                Log.Warning("Mismatched health state on copied pawn: " + savedHealthState + " != " + result.health.State + ";  Resetting value to match.");
                result.SetHealthState(savedHealthState);
            }

            // Clear all of the pawn caches.
            source.ClearCaches();
            
            return result;
        }

        // Deep-copies modded properties from a source pawn to a target pawn.  Kept separate from the Copy() method to provide a
        // clear place for modders to add patches/detours for their mods.
        private static void CopyModdedProperties(Pawn source, Pawn target) {
            // Copy fields from Psychology mod.
            object[] constructorArgs = new object[] { target };
            UtilityCopy.CopyExposableViaReflection("psyche", source, target, constructorArgs);
            UtilityCopy.CopyExposableViaReflection("sexuality", source, target, constructorArgs);
        }

        public static void ClearCaches(this Pawn pawn) {
            pawn.ClearCachedHealth();
            pawn.ClearCachedLifeStage();
            pawn.ClearCachedDisabledWorkTypes();
            pawn.ClearCachedDisabledSkillRecords();
        }

        public static void ClearCachedDisabledWorkTypes(this Pawn pawn) {
            if (pawn.story != null) {
                typeof(Pawn_StoryTracker).GetField("cachedDisabledWorkTypes", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(pawn.story, null);
            }
        }

        public static void ClearCachedDisabledSkillRecords(this Pawn pawn) {
            if (pawn.skills != null && pawn.skills.skills != null) {
                FieldInfo field = typeof(SkillRecord).GetField("cachedTotallyDisabled", BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var record in pawn.skills.skills) {
                    field.SetValue(record, BoolUnknown.Unknown);
                }
            }
        }

        public static void ClearCachedHealth(this Pawn pawn) {
            PawnHealthState savedHealthState = pawn.health.State;
            pawn.health.summaryHealth.Notify_HealthChanged();
            pawn.health.capacities.Clear();
            if (pawn.health.State != savedHealthState) {
                Log.Warning("Pawn healthState mismatched: " + savedHealthState + " != " + pawn.health.State + ";  Resetting value to match.");
                pawn.SetHealthState(savedHealthState);
            }
        }

        public static void SetHealthState(this Pawn pawn, PawnHealthState state) {
            typeof(Pawn_HealthTracker).GetField("healthState", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(pawn, state);
        }

        public static void ClearCachedLifeStage(this Pawn pawn) {
            FieldInfo field = typeof(Pawn_AgeTracker).GetField("cachedLifeStageIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(pawn.ageTracker, -1);
        }

        public static void ClearCachedPortraits(this Pawn pawn) {
            pawn.Drawer.renderer.graphics.ResolveAllGraphics();
            PortraitsCache.SetDirty(pawn);
        }

    }
}
