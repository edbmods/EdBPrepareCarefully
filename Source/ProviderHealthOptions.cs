using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using static HarmonyLib.Code;

namespace EdB.PrepareCarefully {
    public class ProviderHealthOptions {
        protected Dictionary<Tuple<ThingDef, MutantDef>, OptionsHealth> optionsLookup = new Dictionary<Tuple<ThingDef, MutantDef>, OptionsHealth>();
        protected HashSet<string> excludedOptions = new HashSet<string>() {
            "VatLearning", "VatGrowing", "Pregnant", "PsychicBond", "PsychicBondTorn",
            "ResearchCommand", "Animal_Flu", "Stillborn", "Animal_Plague"
        };
        protected HashSet<string> excludedGivers = new HashSet<string>() {
            "Verse.HediffGiver_Terrain"
        };

        public OptionsHealth GetOptions(CustomizedPawn customizedPawn) {
            var cacheKey = Tuple.Create(customizedPawn.Pawn.def, customizedPawn.Pawn.mutant?.Def);
            if (!optionsLookup.TryGetValue(cacheKey, out var result)) {
                result = InitializeHealthOptions(customizedPawn.Pawn.def, customizedPawn.Pawn.mutant?.Def);
                optionsLookup.Add(cacheKey, result);
            }
            return result;
        }

        public OptionsHealth GetOptions(Pawn pawn) {
            var cacheKey = Tuple.Create(pawn.def, pawn.mutant?.Def);
            if (!optionsLookup.TryGetValue(cacheKey, out var result)) {
                result = InitializeHealthOptions(pawn.def, pawn.mutant?.Def);
                optionsLookup.Add(cacheKey, result);
            }
            return result;
        }
        public OptionsHealth GetOptions(ThingDef def) {
            var cacheKey = Tuple.Create(def, (MutantDef)null);
            if (!optionsLookup.TryGetValue(cacheKey, out var result)) {
                result = InitializeHealthOptions(def, null);
                optionsLookup.Add(cacheKey, result);
            }
            return result;
        }

        protected OptionsHealth InitializeHealthOptions(ThingDef pawnThingDef, MutantDef mutantDef) {
            OptionsHealth result = new OptionsHealth();
            BodyDef bodyDef = pawnThingDef.race.body;
            result.BodyDef = bodyDef;
            
            HashSet<UniqueBodyPart> ancestors = new HashSet<UniqueBodyPart>();
            ProcessBodyPart(result, bodyDef.corePart, 1, ancestors);

            List<ImplantOption> implantOptions = InitializeImplantRecipes(result, pawnThingDef, mutantDef);
            foreach (var implantOption in implantOptions) {
                result.AddImplantOption(implantOption);
            }
            InitializeInjuryOptions(result, pawnThingDef);

            result.Sort();
            return result;
        }
        protected int ProcessBodyPart(OptionsHealth options, BodyPartRecord record, int index, HashSet<UniqueBodyPart> ancestors) {
            int partIndex = options.CountOfMatchingBodyParts(record.def);
            FieldInfo skinCoveredField = typeof(BodyPartDef).GetField("skinCovered", BindingFlags.Instance | BindingFlags.NonPublic);
            bool skinCoveredValue = (bool)skinCoveredField.GetValue(record.def);
            FieldInfo solidField = typeof(BodyPartDef).GetField("solid", BindingFlags.Instance | BindingFlags.NonPublic);
            bool isSolidValue = (bool)solidField.GetValue(record.def);
            UniqueBodyPart part = new UniqueBodyPart() {
                Index = partIndex,
                Record = record,
                SkinCovered = skinCoveredValue,
                Solid = isSolidValue,
                Ancestors = ancestors.ToList()
            };
            options.AddBodyPart(part);
            ancestors.Add(part);
            foreach (var c in record.parts) {
                index = ProcessBodyPart(options, c, index + 1, ancestors);
            }
            ancestors.Remove(part);
            return index;
        }

        protected List<ImplantOption> InitializeImplantRecipes(OptionsHealth options, ThingDef pawnThingDef, MutantDef mutantDef) {
            List<ImplantOption> result = new List<ImplantOption>();
            // Find all recipes that replace a body part.
            List<RecipeDef> recipes = new List<RecipeDef>();
            IEnumerable<RecipeDef> startingRecipes = DefDatabase<RecipeDef>.AllDefs.Where((RecipeDef def) => {
                if (def.addsHediff != null
                        && ((def.appliedOnFixedBodyParts != null && def.appliedOnFixedBodyParts.Count > 0) || (def.appliedOnFixedBodyPartGroups != null && def.appliedOnFixedBodyPartGroups.Count > 0))
                        && (def.recipeUsers.NullOrEmpty() || def.recipeUsers.Contains(pawnThingDef))) {
                    //Logger.Debug("Adding implant recipe: " + def.defName);
                    return true;
                }
                else {
                    //Logger.Debug("Excluding implant recipe: " + def.defName);
                    return false;
                }
            }).Where(r => {
                if (mutantDef != null && r.mutantBlacklist.CountAllowNull() > 0 && r.mutantBlacklist.Contains(mutantDef)) {
                    Logger.Debug("Removing recipe because mutant pawn is not allowed: " + r.LabelCap + ", mutant = " + mutantDef.LabelCap + ", exclusion list = " + string.Join(",", r.mutantBlacklist));
                    return false;
                }
                else if (r.mutantPrerequisite.CountAllowNull() > 0 && !r.mutantPrerequisite.Contains(mutantDef)) {
                    Logger.Debug("Removing recipe because pawn is not an allowed kind of mutant: " + r.LabelCap + ", required = " + string.Join(",", r.mutantPrerequisite));
                    return false;
                }
                else {
                    return true;
                } 
            });
            recipes.AddRange(startingRecipes);
            
            // Remove duplicates: recipes that apply the same hediff on the same body parts.
            HashSet<int> recipeHashes = new HashSet<int>();
            List<RecipeDef> dedupedRecipes = new List<RecipeDef>();
            foreach (var recipe in recipes) {
                int hash = recipe.addsHediff.GetHashCode();
                foreach (var part in recipe.appliedOnFixedBodyParts) {
                    hash = hash * 31 + part.GetHashCode();
                }
                if (!recipeHashes.Contains(hash)) {
                    dedupedRecipes.Add(recipe);
                    recipeHashes.Add(hash);
                }
            }
            recipes = new List<RecipeDef>(dedupedRecipes);

            // Iterate the recipes. Populate a list of all of the body parts that apply to a given recipe.
            foreach (var r in recipes) {
                // Add all of the body parts for that recipe to the list.
                foreach (var bodyPartDef in r.appliedOnFixedBodyParts) {
                    List<UniqueBodyPart> fixedParts = options.FindBodyPartsForDef(bodyPartDef).ToList();
                    if (fixedParts != null && fixedParts.Count > 0) {
                        //Logger.Debug("Adding recipe for " + r.defName + " for fixed parts " + String.Join(", ", fixedParts.ConvertAll(p => p.Record.LabelCap)));
                        options.AddImplantRecipe(r, fixedParts);
                        foreach (var part in fixedParts) {
                            part.Replaceable = true;
                        }
                    }
                }
                foreach (var group in r.appliedOnFixedBodyPartGroups) {
                    List<UniqueBodyPart> partsFromGroup = options.PartsForBodyPartGroup(group.defName);
                    if (partsFromGroup != null && partsFromGroup.Count > 0) {
                        //Logger.Debug("Adding recipe for " + r.defName + " for group " + group.defName + " for parts " + String.Join(", ", partsFromGroup.ConvertAll(p => p.Record.LabelCap)));
                        options.AddImplantRecipe(r, partsFromGroup);
                        foreach (var part in partsFromGroup) {
                            part.Replaceable = true;
                        }
                    }
                }
            }

            // Add options for thing definitions that have an install implant comp
            Dictionary<string, ImplantOption> implantOptionLookup = new Dictionary<string, ImplantOption>();
            foreach (var def in DefDatabase<ThingDef>.AllDefs.Where(d => d.HasComp<CompUsableImplant>())) {
                var useEffectInstallImplant = def.GetCompProperties<CompProperties_UseEffectInstallImplant>();
                var usable = def.GetCompProperties<CompProperties_Usable>();
                if (useEffectInstallImplant != null && usable != null) {
                    string hediffDefName = useEffectInstallImplant.hediffDef?.defName;
                    if (hediffDefName == null) {
                        continue;
                    }
                    HediffDef hediffDef = useEffectInstallImplant.hediffDef;
                    // Exclude psychic amplifier because that's added as an injury and requires special handling
                    // to avoid the default behavior that adds a random ability
                    if (hediffDef == HediffDefOf.PsychicAmplifier) {
                        continue;
                    }
                    if (!implantOptionLookup.TryGetValue(hediffDefName, out ImplantOption option)) {
                        option = new ImplantOption() {
                            HediffDef = hediffDef,
                            ThingDef = def,
                            BodyPartDefs = new HashSet<BodyPartDef>(),
                            Dependency = usable.userMustHaveHediff,
                        };
                        if (typeof(Hediff_Level).IsAssignableFrom(hediffDef.hediffClass)) {
                            option.MinSeverity = hediffDef.minSeverity > 0 ? hediffDef.minSeverity : 1;
                            option.MaxSeverity = hediffDef.maxSeverity;
                            Logger.Debug(string.Format("Adding option {0} with severity {1}-{2} from thingDef {3}", hediffDef.defName, option.MinSeverity, option.MaxSeverity, def.defName));
                        }
                        implantOptionLookup.Add(hediffDefName, option);
                    }
                    option.BodyPartDefs.Add(useEffectInstallImplant.bodyPart);
                }
            }
            foreach (var value in implantOptionLookup.Values) {
                result.Add(value);
            }

            // Add options for mutations
            foreach (var def in DefDatabase<HediffDef>.AllDefs.Where(d => d.organicAddedBodypart && d.defaultInstallPart != null)) {
                ImplantOption option = new ImplantOption() {
                    HediffDef = def,
                    BodyPartDefs = new HashSet<BodyPartDef>() { def.defaultInstallPart },
                };
                result.Add(option);
            }

            return result;
        }

        protected bool InitializeHediffGivenByUseEffect(OptionsHealth options, CompProperties_UseEffectInstallImplant useEffect) {
            InjuryOption option = new InjuryOption();
            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("PsychicAmplifier");
            if (hediffDef == null) {
                return false;
            }
            option.HediffDef = hediffDef;
            option.Label = useEffect.hediffDef.LabelCap;
            if (useEffect.bodyPart == null) {
                //Logger.Debug("Body part was null for hediff use effect: " + hediffDef.defName);
                return false;
            }
            if (useEffect.bodyPart != null) {
                List<BodyPartDef> validParts = new List<BodyPartDef>() { useEffect.bodyPart };
                List<UniqueBodyPart> parts = options.FindBodyPartsForDef(useEffect.bodyPart).ToList();
                if (parts == null || parts.Count == 0) {
                    //Logger.Debug("Found no valid body parts for hediff use effect: " + hediffDef.defName + ", " + useEffect.bodyPart.defName);
                    return false;
                }
                option.ValidParts = validParts;
            }
            //Logger.Debug($"Add hediff option given by use effect. Hediff = {option.HediffDef.defName}, Label = {option.Label}, BodyPart = {string.Join(", ", option.ValidParts)}");
            options.AddInjury(option);
            return true;
        }

        protected bool IsGiverExcluded(HediffGiver giver) {
            if (giver?.GetType() == null) {
                return true;
            }
            return excludedGivers.Contains(giver.GetType().FullName);
        }

        protected void InitializeHediffGiverInjuries(OptionsHealth options, HediffGiver giver) {
            if (giver == null) {
                Logger.Warning("Could not add injury/health conditions from HediffGiver because it was null");
                return;
            }
            if (IsGiverExcluded(giver)) {
                Logger.Debug("Did not add injury/health conditions from excluded HediffGiver " + giver.GetType().FullName);
                return;
            }
            if (giver.hediff == null) {
                Logger.Warning("Could not add injury/health conditions from HediffGiver because the hediff for " + giver.GetType().FullName + " was null");
                return;
            }
            InjuryOption option = new InjuryOption();
            option.HediffDef = giver.hediff;
            option.Label = giver.hediff.LabelCap;
            option.Giver = giver;
            if (giver.partsToAffect == null) {
                option.WholeBody = true;
            }
            if (giver.canAffectAnyLivePart) {
                option.WholeBody = false;
            }
            if (giver.partsToAffect != null && !giver.canAffectAnyLivePart) {
                List<BodyPartDef> validParts = new List<BodyPartDef>();
                foreach (var def in giver.partsToAffect) {
                    List<UniqueBodyPart> parts = options.FindBodyPartsForDef(def).ToList();
                    if (parts != null) {
                        validParts.Add(def);
                    }
                }
                if (validParts.Count == 0) {
                    return;
                }
                else {
                    option.ValidParts = validParts;
                }
            }
            options.AddInjury(option);
        }

        protected InjuryOption CreateMissingPartInjuryOption(OptionsHealth options, HediffDef hd, ThingDef pawnThingDef) {
            InjuryOption option = new InjuryOption();
            option.HediffDef = hd;
            option.Label = hd.LabelCap;

            HashSet<BodyPartDef> uniquenessLookup = new HashSet<BodyPartDef>();
            List<BodyPartDef> validParts = new List<BodyPartDef>();
            foreach (var p in pawnThingDef.race.body.AllParts.Where(p => p.def.canSuggestAmputation)) {
                if (!uniquenessLookup.Contains(p.def)) {
                    validParts.Add(p.def);
                    uniquenessLookup.Add(p.def);
                }
            }
            option.ValidParts = validParts;
            //Logger.Debug("For pawn of {" + pawnThingDef.defName + "} missing parts allowed are {" + String.Join(", ", option.ValidParts) + "}");
            return option;
        }

        protected void InitializeInjuryOptions(OptionsHealth options, ThingDef pawnThingDef) {
            HashSet<HediffDef> addedDefs = new HashSet<HediffDef>();
            // Go through all of the hediff giver sets for the pawn's race and intialize injuries from
            // each giver.
            if (pawnThingDef.race.hediffGiverSets != null) {
                foreach (var giverSetDef in pawnThingDef.race.hediffGiverSets) {
                    foreach (var giver in giverSetDef.hediffGivers) {
                        InitializeHediffGiverInjuries(options, giver);
                    }
                }
            }
            // Go through all hediff stages, looking for hediff givers.
            foreach (var hd in DefDatabase<HediffDef>.AllDefs) {
                if (hd.stages != null) {
                    foreach (var stage in hd.stages) {
                        if (stage.hediffGivers != null) {
                            foreach (var giver in stage.hediffGivers) {
                                InitializeHediffGiverInjuries(options, giver);
                            }
                        }
                    }
                }
            }
            // Go through all of the chemical defs, looking for hediff givers.
            foreach (var chemicalDef in DefDatabase<ChemicalDef>.AllDefs) {
                if (chemicalDef.onGeneratedAddictedEvents != null) {
                    foreach (var giver in chemicalDef.onGeneratedAddictedEvents) {
                        InitializeHediffGiverInjuries(options, giver);
                    }
                }
            }

            // Go through all thing defs with a CompProperties_UseEffectInstallImplant
            foreach (var def in DefDatabase<ThingDef>.AllDefs.Where(d => d.HasComp(typeof(CompUsableImplant)))) {
                CompProperties_UseEffectInstallImplant props = def.GetCompProperties<CompProperties_UseEffectInstallImplant>();
                if (props != null) {
                    if (InitializeHediffGivenByUseEffect(options, props)) {
                        addedDefs.Add(props.hediffDef);
                    }
                }
            }

            // Get all of the hediffs that can be added via the "forced hediff" scenario part and
            // add them to a hash set so that we can quickly look them up.
            ScenPart_ForcedHediff scenPart = new ScenPart_ForcedHediff();
            IEnumerable<HediffDef> scenPartDefs = Reflection.ReflectorScenPart_ForcedHediff.PossibleHediffs(scenPart);
            HashSet<HediffDef> scenPartDefSet = new HashSet<HediffDef>(scenPartDefs);
            
            // Add injury options.
            foreach (var hd in DefDatabase<HediffDef>.AllDefs) {
                //Logger.Debug("{0} ({1}), comps = {2}, givers = {3} tags = {4}",
                //    hd.LabelCap,
                //    hd.defName,
                //    string.Join(", ", hd?.comps?.Select(c => c.compClass.FullName) ?? new string[] { "none" }),
                //    string.Join(", ", hd?.hediffGivers?.Select(g => g.GetType().FullName) ?? new string[] { "none" }),
                //    string.Join(", ", hd?.tags ?? new List<string>(new string[] { "none" }))
                //);
                try {
                    // Exclude hediffs that are added by wearing apparel
                    if (hd.comps != null && hd.comps.Where(c => c is HediffCompProperties_RemoveIfApparelDropped).Any()) {
                        Logger.Debug($"Skipping hediff because it is removed when the pawn drops apparel: {hd.defName}");
                        continue;
                    }

                    if (hd.hediffClass == typeof(Hediff_MissingPart)) {
                        options.AddInjury(CreateMissingPartInjuryOption(options, hd, pawnThingDef));
                        continue;
                    }
                    // Filter out defs that were already added via the hediff giver sets.
                    if (addedDefs.Contains(hd)) {
                        //Logger.Debug($"Skipping hediff because it was already added: {hd.defName}");
                        continue;
                    }
                    // Filter out implants.
                    if (hd.hediffClass != null && typeof(Hediff_Implant).IsAssignableFrom(hd.hediffClass)) {
                        continue;
                    }

                    // If it's an old injury, use the old injury properties to get the label.
                    HediffCompProperties p = hd.CompPropsFor(typeof(HediffComp_GetsPermanent));
                    HediffCompProperties_GetsPermanent getsPermanentProperties = p as HediffCompProperties_GetsPermanent;

                    bool warning = false;
                    if (getsPermanentProperties == null) {
                        if (!hd.scenarioCanAdd) {
                            if (hd.comps != null && hd.comps.Count > 0) {
                                warning = true;
                            }
                        }
                    }

                    String label;
                    if (getsPermanentProperties != null) {
                        if (getsPermanentProperties.permanentLabel != null) {
                            label = getsPermanentProperties.permanentLabel.CapitalizeFirst();
                        }
                        else {
                            Logger.Warning("Could not find label for old injury: " + hd.defName);
                            continue;
                        }
                    }
                    else {
                        label = hd.LabelCap;
                    }

                    // Add the injury option..
                    InjuryOption option = new InjuryOption();
                    option.HediffDef = hd;
                    option.Label = label;
                    option.Warning = warning;
                    if (getsPermanentProperties != null) {
                        option.IsOldInjury = true;
                    }
                    else if (hd.hediffClass == typeof(Hediff_Injury)) {
                        continue;
                    }
                    else {
                        option.ValidParts = new List<BodyPartDef>();
                    }
                    options.AddInjury(option);
                }
                catch (Exception e) {
                    Logger.Warning("There was en error while processing hediff {" + hd.defName + "} when trying to add it to the list of available injury options", e);
                    continue;
                }
            }

            // Mark whether or not the injury can be added to a pawn in the health panel
            foreach (var o in options.InjuryOptions) {
                o.Selectable = IsInjuryOptionSelectable(o.HediffDef.defName);
            }
            
            // Disambiguate duplicate injury labels.
            HashSet<string> labels = new HashSet<string>();
            HashSet<string> duplicateLabels = new HashSet<string>();
            foreach (var option in options.InjuryOptions) {
                if (labels.Contains(option.Label)) {
                    duplicateLabels.Add(option.Label);
                }
                else {
                    labels.Add(option.Label);
                }
            }
            foreach (var option in options.InjuryOptions) {
                HediffCompProperties p = option.HediffDef.CompPropsFor(typeof(HediffComp_GetsPermanent));
                HediffCompProperties_GetsPermanent props = p as HediffCompProperties_GetsPermanent;
                if (props != null) {
                    if (duplicateLabels.Contains(option.Label)) {
                        string label = "EdB.PC.Dialog.Injury.OldInjury.Label".Translate(props.permanentLabel.CapitalizeFirst(), option.HediffDef.LabelCap);
                        option.Label = label;
                    }
                }
            }
            foreach (var option in options.InjuryOptions) {
                List<UniqueBodyPart> uniqueParts = new List<UniqueBodyPart>();
                if (option.ValidParts != null && option.ValidParts.Count > 0) {
                    foreach (var part in option.ValidParts) {
                        HashSet<BodyPartDef> uniquenessLookup = new HashSet<BodyPartDef>();
                        if (!uniquenessLookup.Contains(part)) {
                            uniquenessLookup.Add(part);
                            uniqueParts.AddRange(options.FindBodyPartsForDef(part));
                        }
                    }
                }
                option.UniqueParts = uniqueParts;
            }
        }

        public bool IsInjuryOptionSelectable(string defName) {
            if (excludedOptions.Contains(defName)) {
                return false;
            }
            if (defName.EndsWith("InEyes")) {
                return false;
            }
            if (defName.EndsWith("Command")) {
                return false;
            }
            if (defName.EndsWith("CommandBuff")) {
                return false;
            }
            return true;
        }
    }
}
