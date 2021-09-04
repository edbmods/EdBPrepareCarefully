using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully {
    public interface PawnCompRules {
        bool IsCompIncluded(string compTypeFullName);
        bool IsCompIncluded(ThingComp comp);
        IEnumerable<string> ExcludedFields { get; }
    }

    public delegate void ModifyPawnCompPostLoad(Pawn pawn, Dictionary<string, ThingComp> thingCompLookup, HashSet<string> loadedComps);
    public delegate void ModifyCustomPawnCompPostLoad(CustomPawn pawn, Dictionary<string, ThingComp> thingCompLookup, HashSet<string> loadedComps);

    public class PawnCompPostLoadModifiers {
        public static List<ModifyPawnCompPostLoad> pawnFunctions = new List<ModifyPawnCompPostLoad>();
        public static List<ModifyCustomPawnCompPostLoad> customPawnFunctions = new List<ModifyCustomPawnCompPostLoad>();

        public PawnCompPostLoadModifiers Add(ModifyPawnCompPostLoad func) {
            pawnFunctions.Add(func);
            return this;
        }

        public PawnCompPostLoadModifiers Add(ModifyCustomPawnCompPostLoad func) {
            customPawnFunctions.Add(func);
            return this;
        }

        public void Apply(CustomPawn pawn, Dictionary<string, ThingComp> thingCompLookup, HashSet<string> loadedComps) {
            foreach (var fn in pawnFunctions) {
                fn(pawn.Pawn, thingCompLookup, loadedComps);
            }
            foreach (var fn in customPawnFunctions) {
                fn(pawn, thingCompLookup, loadedComps);
            }
        }
    }

    public class CustomPawnCompPostLoadModifiers {

    }

    public static class DefaultPawnCompRules {
        public static PawnCompExclusionRules rulesForCopying = null;
        public static PawnCompInclusionRules rulesForSaving = null;
        public static PawnCompPostLoadModifiers postLoadModifiers = null;

        public static PawnCompExclusionRules RulesForCopying {
            get {
                if (rulesForCopying == null) {
                    InitializeRulesForCopying();
                }
                return rulesForCopying;
            }
        }

        public static PawnCompInclusionRules RulesForSaving {
            get {
                if (rulesForSaving == null) {
                    InitializeRulesForSaving();
                }
                return rulesForSaving;
            }
        }

        public static PawnCompPostLoadModifiers PostLoadModifiers {
            get {
                if (postLoadModifiers == null) {
                    InitializePostLoadModifiers();
                }
                return postLoadModifiers;
            }
        }

        // MODMAKERS: If you need to exclude a modded ThingComp from the list of ThingComps that are copied when Prepare Carefully
        // makes a copy of a starting pawn, you can add the FullName for the ThingComp Type to excluded comps.
        // You can do that in one of two ways:
        // 1) add a harmony PostFix for this initialization method and use it to  modify the DefaultPawnCompRules.rulesForCopying field value
        // or
        // 2) get the RulesForCopying property in a static initializer and modify accordingly
        public static void InitializeRulesForCopying() {
            if (rulesForCopying != null) {
                return;
            }
            rulesForCopying = new PawnCompExclusionRules();
        }

        // MODMAKERS: If you need to add a modded ThingComp from the list of ThingComps that are saved when Prepare Carefully saves
        // a pawn preset, add the FullName of the ThingComp Type to the included comps.
        // You can do that in one of two ways:
        // 1) add a harmony PostFix for this initialization method and use it to  modify the DefaultPawnCompRules.rulesForSaving field value
        // or
        // 2) get the RulesForSaving property in a static initializer and modify accordingly
        public static void InitializeRulesForSaving() {
            if (rulesForSaving != null) {
                return;
            }
            rulesForSaving = new PawnCompInclusionRules();
            rulesForSaving.IncludeComp("AlienRace.AlienPartGenerator+AlienComp")
                .IncludeComp("FacialStuff.CompFace")
                .IncludeComp("VanillaHairExpanded.CompBeard")
                .IncludeComp("Psychology.CompPsychology")
                .IncludeComp("GradientHair.CompGradientHair")
                .IncludeCompWithPrefix("ReviaRace.")
                .ExcludeField("pawnFaction");
        }

        public static void InitializePostLoadModifiers() {
            if (postLoadModifiers != null) {
                return;
            }
            postLoadModifiers = new PawnCompPostLoadModifiers();
            postLoadModifiers.Add(RemoveDefaultVanillaHairExtendedBeard);
            postLoadModifiers.Add(RemoveDefaultFacialStuff);
            postLoadModifiers.Add(ValidateAlienCrownType);
        }

        public static void RemoveDefaultVanillaHairExtendedBeard(Pawn pawn, Dictionary<string, ThingComp> compLookup, HashSet<string> savedComps) {
            // If the pawn was saved when vanilla hair expanded was not enabled, but it was enabled when they load the pawn,
            // then they may end up with a beard by default.  This post-load action clears out that default beard.
            if (!savedComps.Contains("VanillaHairExpanded.CompBeard")) {
                if (compLookup.TryGetValue("VanillaHairExpanded.CompBeard", out ThingComp c)) {
                    HairDef beardDef = ReflectionUtil.GetFieldValue<HairDef>(c, "beardDef");
                    if (beardDef != null && !String.Equals(beardDef, "VHE_BeardCleanShaven")) {
                        HairDef defaultBeardDef = DefDatabase<HairDef>.GetNamedSilentFail("VHE_BeardCleanShaven");
                        if (defaultBeardDef != null) {
                            ReflectionUtil.SetPublicField(c, "beardDef", defaultBeardDef);
                        }
                        else {
                            Logger.Warning("Vanilla Hairs Extended added a default beard because none was saved with the pawn preset.  We tried to remove it but failed.");
                        }
                    }
                }
            }
        }

        public static void RemoveDefaultFacialStuff(Pawn pawn, Dictionary<string, ThingComp> compLookup, HashSet<string> savedComps) {
            // If the pawn was saved when facial stuff was not enabled, but it was enabled when they load the pawn,
            // then the pawn will end up with random facial stuff settings, possibly including facial hair.  This post-load action clears
            // out as much of those random settings as possible.
            if (!savedComps.Contains("FacialStuff.CompFace")) {
                if (compLookup.TryGetValue("FacialStuff.CompFace", out ThingComp c)) {
                    ReflectionUtil.InvokeActionMethod(c, "InitializeCompFace");
                    object pawnFace = ReflectionUtil.GetPropertyValue<object>(c, "PawnFace");
                    if (pawnFace == null) {
                        //Logger.Debug("Couldn't get the PawnFace value from the comp with class " + c.GetType().FullName);
                        return;
                    }
                    Type beardDefType = ReflectionUtil.TypeByName("FacialStuff.Defs.BeardDef");
                    if (beardDefType == null) {
                        //Logger.Debug("Didn't find the beard definition type");
                    }
                    else {
                        Def defaultBeardDef = GenDefDatabase.GetDef(beardDefType, "Beard_Shaved", false);
                        if (defaultBeardDef == null) {
                            //Logger.Debug("Didn't find the default beard definition");
                        }
                        else {
                            ReflectionUtil.SetFieldValue(pawnFace, "_beardDef", defaultBeardDef);
                        }
                    }
                    Type stacheDefType = ReflectionUtil.TypeByName("FacialStuff.Defs.MoustacheDef");
                    if (stacheDefType == null) {
                        //Logger.Debug("Didn't find the moustache definition type");
                    }
                    else {
                        Def defaultStacheDef = GenDefDatabase.GetDef(stacheDefType, "Shaved", false);
                        if (defaultStacheDef == null) {
                            //Logger.Debug("Didn't find the default moustache definition");
                        }
                        else {
                            ReflectionUtil.SetFieldValue(pawnFace, "_moustacheDef", defaultStacheDef);
                        }
                    }
                }
            }
        }

        public static void ValidateAlienCrownType(CustomPawn pawn, Dictionary<string, ThingComp> compLookup, HashSet<string> savedComps) {
            // If the pawn was saved when vanilla hair expanded was not enabled, but it was enabled when they load the pawn,
            // then they may end up with a beard by default.  This post-load action clears out that default beard.
            if (!savedComps.Contains("AlienRace.AlienPartGenerator+AlienComp")) {
                if (compLookup.TryGetValue("AlienRace.AlienPartGenerator+AlienComp", out ThingComp c)) {
                    string crownType = ReflectionUtil.GetFieldValue<string>(c, "crownType");
                    if (crownType != null) {
                        CustomHeadType headType = pawn.HeadType;
                        if (headType != null && headType.AlienCrownType != crownType) {
                            ReflectionUtil.SetFieldValue(c, "crownType", null);
                        }
                    }
                }
            }
        }
    }

    // A set of pawn comp rules where you specify exactly which comps to include.
    // You can also define a set of individual fields to exclude where saving a comp.
    // You can also define a list of post-load functions
    public class PawnCompInclusionRules : PawnCompRules {
        public HashSet<string> includedComps = new HashSet<string>();
        public List<string> compPrefixesToInclude = new List<string>();
        public HashSet<string> excludedFields = new HashSet<string>();
        public IEnumerable<string> IncludedComps {
            get {
                return includedComps;
            }
        }
        public IEnumerable<string> ExcludedFields {
            get {
                return excludedFields;
            }
        }
        public PawnCompInclusionRules IncludeComp(Type compType) {
            includedComps.Add(compType.FullName);
            return this;
        }
        public PawnCompInclusionRules IncludeComps(IEnumerable<string> compTypeFullNames) {
            if (compTypeFullNames != null) {
                includedComps.AddRange(compTypeFullNames);
            }
            return this;
        }
        public PawnCompInclusionRules IncludeComp(string compTypeFullName) {
            includedComps.Add(compTypeFullName);
            return this;
        }
        public PawnCompInclusionRules IncludeCompWithPrefix(string prefix) {
            compPrefixesToInclude.Add(prefix);
            return this;
        }
        public PawnCompInclusionRules ExcludeField(string fieldName) {
            excludedFields.Add(fieldName);
            return this;
        }
        public bool IsCompIncluded(string compTypeFullName) {
            if (includedComps.Contains(compTypeFullName)) {
                return true;
            }
            foreach (var prefix in compPrefixesToInclude) {
                if (compTypeFullName.StartsWith(prefix)) {
                    return true;
                }
            }
            return false;
        }
        public bool IsCompIncluded(ThingComp comp) {
            return IsCompIncluded(comp.GetType().FullName);
        }
    }

    public class PawnCompExclusionRules : PawnCompRules {
        public HashSet<string> excludedComps = new HashSet<string>();
        public List<string> compPrefixesToExclude = new List<string>();
        public HashSet<string> excludedFields = new HashSet<string>();
        public IEnumerable<string> ExcludedComps {
            get {
                return excludedComps;
            }
        }
        public IEnumerable<string> ExcludedFields {
            get {
                return excludedFields;
            }
        }
        public PawnCompExclusionRules ExcludeComp(Type compType) {
            excludedComps.Add(compType.FullName);
            return this;
        }
        public PawnCompExclusionRules ExcludeComps(IEnumerable<string> compTypeFullNames) {
            if (compTypeFullNames != null) {
                excludedComps.AddRange(compTypeFullNames);
            }
            return this;
        }
        public PawnCompExclusionRules ExcludeComp(string compTypeFullName) {
            excludedComps.Add(compTypeFullName);
            return this;
        }
        public PawnCompExclusionRules ExcludeCompWithPrefix(string prefix) {
            compPrefixesToExclude.Add(prefix);
            return this;
        }
        public PawnCompExclusionRules ExcludeField(string fieldName) {
            excludedFields.Add(fieldName);
            return this;
        }
        public bool IsCompIncluded(string compTypeFullName) {
            if (excludedComps.Contains(compTypeFullName)) {
                return false;
            }
            foreach (var prefix in compPrefixesToExclude) {
                if (compTypeFullName.StartsWith(prefix)) {
                    return false;
                }
            }
            return true;
        }
        public bool IsCompIncluded(ThingComp comp) {
            return IsCompIncluded(comp.GetType().FullName);
        }
    }
}
