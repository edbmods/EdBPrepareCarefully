using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public static class ExtensionsPawn {
        // It would be nice if we could just do this to deep copy a pawn, but there are references to world objects in a saved pawn that can cause
        // problems, i.e. relationship references to other pawns.  So we follow the more explicit technique below to copy the pawn.
        // Leaving this here to remind us to not bother trying to do this again.
        public static Pawn Copy(this Pawn source) {
            try {
                Pawn copy = UtilityCopy.CopyExposable<Pawn>(source, CreateCrossReferenceListForCopying());
                copy.ClearCaches();
                return copy;
            }
            catch (Exception e) {
                Logger.Warning("Failed to copy pawn with preferred method.  Using backup method instead.\n" + e);
                return CopyBackup(source);
            }
        }

        public static Pawn CopyBackup(this Pawn source) {

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

            // Set up any cross-references
            Dictionary<String, IExposable> crossReferences = CreateCrossReferenceListForCopying();

            // Copy trackers.
            object[] constructorArgs = new object[] { result };
            result.ageTracker = UtilityCopy.CopyExposable(source.ageTracker, constructorArgs);
            result.story = UtilityCopy.CopyExposable(source.story, constructorArgs);
            result.skills = UtilityCopy.CopyExposable(source.skills, constructorArgs);
            result.health = UtilityCopy.CopyExposable(source.health, constructorArgs);
            result.apparel = UtilityCopy.CopyExposable(source.apparel, constructorArgs);
            result.style = UtilityCopy.CopyExposable(source.style, constructorArgs);
            result.ideo = UtilityCopy.CopyExposable(source.ideo, constructorArgs, new Dictionary<string, IExposable>(crossReferences));

            // Copy comps
            //List<ThingComp> validComps = new List<ThingComp>();
            //foreach (var c in source.AllComps) {
            //    PawnCompsSaver saver = new PawnCompsSaver(new List<ThingComp>() { c }, null);
            //    string xml = UtilityCopy.SerializeExposableToString(saver);
            //    XmlDocument doc = new XmlDocument();
            //    doc.LoadXml(xml);
            //    if (doc.DocumentElement.HasChildNodes) {
            //        validComps.Add(c);
            //        Logger.Debug(c.GetType().FullName + ": \n  " + xml);
            //    }
            //    else {
            //        Logger.Debug(c.GetType().FullName + " is empty");
            //    }
            //}

            CopyPawnComps(source, result);

            // Clear all of the pawn caches.
            source.ClearCaches();
            result.ClearCaches();

            return result;
        }

        public static Dictionary<string, IExposable> CreateCrossReferenceListForCopying() {
            var result = new Dictionary<string, IExposable>();
            foreach (var i in Find.World.ideoManager.IdeosListForReading) {
                result.Add(i.GetUniqueLoadID(), i);
            }
            foreach (var p in Find.GameInitData.startingAndOptionalPawns) {
                result.Add(p.GetUniqueLoadID(), p);
            }
            foreach (var p in Find.World.worldPawns.AllPawnsAliveOrDead) {
                result.Add(p.GetUniqueLoadID(), p);
            }
            return result;
        }

        public static void CopyPawnComps(Pawn source, Pawn target) {
            PawnCompsSaver saver = new PawnCompsSaver(source, DefaultPawnCompRules.RulesForCopying);
            string xml = UtilityCopy.SerializeExposableToString(saver);
            //Logger.Debug("Serialized comps to string\n" + xml);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            foreach (var fieldPath in DefaultPawnCompRules.RulesForCopying.ExcludedFields) {
                XmlNode node = doc.DocumentElement.SelectSingleNode(fieldPath);
                if (node != null) {
                    //Logger.Debug("Removing " + node.Name + " element");
                    doc.DocumentElement.RemoveChild(node);
                }
            }
            xml = "<saveable Class=\"" + typeof(PawnCompsLoader).FullName + "\">" + doc.DocumentElement.InnerXml + "</saveable>";
            //Logger.Debug("Post node-exclusions\n" + xml);
            UtilityCopy.DeserializeExposable<PawnCompsLoader>(xml, new object[] { target, DefaultPawnCompRules.RulesForCopying });
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
                Logger.Warning("Pawn healthState mismatched: " + savedHealthState + " != " + pawn.health.State + ";  Resetting value to match.");
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
