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
        // It would be nice if we could just do this to deep copy a pawn, but there are references in a saved pawn that can cause
        // problems, i.e. relationships.  So we follow the more explicit technique below to copy the pawn.
        // Leaving this here to remind us to not bother trying to do this again.
        //public static Pawn IdealCopy(this Pawn source) {
        //    try {
        //        Pawn copy = UtilityCopy.CopyExposable<Pawn>(source);
        //        copy.ClearCaches();
        //        return copy;
        //    }
        //    catch (Exception e) {
        //        Logger.Warning("Failed to copy pawn with preferred method.  Using backup method instead.\n" + e);
        //        return CopyBackup(source);
        //    }
        //}

        public static List<string> CompsExcludedFromCopying = null;

        // MODMAKERS: If you need to exclude a modded ThingComp from the list of ThingComps that are copied when duplicating a pawn,
        // add the FullName for the ThingComp class to the CompsExcludedFromCopying list in a PostFix method.
        public static void CreateExcludedCompList() {
            CompsExcludedFromCopying = new List<string>();
        }

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

            // Copy comps
            CompsCopier copier = new CompsCopier(source);
            source.AllComps.ForEach((c) => {
                copier.AllComps.Add(c);
            });
            CompsCopier compsCopy = UtilityCopy.CopyExposable(copier, new[] { result });
            CopyCompsIntoResult(result, compsCopy);

            // Verify the pawn health state.
            if (result.health.State != savedHealthState) {
                Log.Warning("Mismatched health state on copied pawn: " + savedHealthState + " != " + result.health.State + ";  Resetting value to match.");
                result.SetHealthState(savedHealthState);
            }

            // Clear all of the pawn caches.
            source.ClearCaches();
            result.ClearCaches();

            return result;
        }

        public static void CopyCompsIntoResult(Pawn target, CompsCopier comps) {
            Dictionary<string, ThingComp> lookup = new Dictionary<string, ThingComp>();
            comps.AllComps.ForEach(c => lookup.Add(c.GetType().FullName, c));
            RemoveExcludedComps(lookup);
            int count = target.AllComps.Count;
            for (int i=0; i<count; i++) {
                var type = target.AllComps[i].GetType();
                if (lookup.TryGetValue(type.FullName, out ThingComp c)) {
                    //Logger.Debug("Replaced the " + type.FullName + " comp with the copied version");
                    target.AllComps[i] = c;
                    lookup.Remove(type.FullName);
                }
            }
            foreach (var pair in lookup) {
                //Logger.Debug("Added the copied " + pair.Key + " version of the comp");
                target.AllComps.Add(pair.Value);
            }
        }

        public static void RemoveExcludedComps(Dictionary<string, ThingComp> lookup) {
            if (CompsExcludedFromCopying == null) {
                CreateExcludedCompList();
            }
            List<Type> removals = new List<Type>();
            foreach (var name in CompsExcludedFromCopying) {
                if (lookup.ContainsKey(name)) {
                    //Logger.Debug("Removed excluded " + name + " comp from the list of comps to copy over");
                    lookup.Remove(name);
                }
            }
        }

        // Utility class to copy all of the comps in a pawn.
        public class CompsCopier : IExposable {
            private List<ThingComp> comps = new List<ThingComp>();
            public ThingDef Def { get; set; }
            public ThingWithComps Parent { get; set; }
            // The constructor needs to take the target pawn as an argument--the pawn to which the comps will be copied.
            public CompsCopier(Pawn pawn) {
                Parent = pawn;
                Def = pawn.def;
            }
            public List<ThingComp> AllComps {
                get {
                    return comps;
                }
            }
            // Partially duplicated from ThingWithComps.
            public void ExposeData() {
                if (Scribe.mode == LoadSaveMode.LoadingVars) {
                    this.InitializeComps();
                }
                if (this.comps != null) {
                    for (int i = 0; i < this.comps.Count; i++) {
                        this.comps[i].PostExposeData();
                    }
                }
            }
            // Partially duplicated from ThingWithComps.
            public void InitializeComps() {
                if (Def.comps.Any<CompProperties>()) {
                    this.comps = new List<ThingComp>();
                    for (int i = 0; i < Def.comps.Count; i++) {
                        ThingComp thingComp = null;
                        try {
                            thingComp = (ThingComp)Activator.CreateInstance(Def.comps[i].compClass);
                            // We set the parent to our target pawn.
                            thingComp.parent = Parent;
                            this.comps.Add(thingComp);
                            thingComp.Initialize(Def.comps[i]);
                        }
                        catch (Exception arg) {
                            Log.Error("Could not instantiate or initialize a ThingComp: " + arg, false);
                            this.comps.Remove(thingComp);
                        }
                    }
                }
            }
        }

        public static void ClearCaches(this Pawn pawn) {
            pawn.ClearCachedHealth();
            pawn.ClearCachedLifeStage();
            pawn.ClearCachedDisabledSkillRecords();
        }

        public static void ClearCachedDisabledSkillRecords(this Pawn pawn) {
            if (pawn.skills != null && pawn.skills.skills != null) {
                pawn.skills.Notify_SkillDisablesChanged();
            }
            Reflection.Pawn.ClearCachedDisabledWorkTypes(pawn);
            Reflection.Pawn.ClearCachedDisabledWorkTypesPermanent(pawn);
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

        public static void AssignToFaction(this Pawn pawn, Faction faction) {
            FieldInfo field = typeof(Pawn_AgeTracker).GetField("factionInt", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(pawn, faction);
        }

    }
}
