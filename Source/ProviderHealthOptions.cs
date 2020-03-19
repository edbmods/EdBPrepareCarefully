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
    public class ProviderHealthOptions {
        protected Dictionary<ThingDef, OptionsHealth> optionsLookup = new Dictionary<ThingDef, OptionsHealth>();
        public OptionsHealth GetOptions(CustomPawn pawn) {
            OptionsHealth result = null;
            if (!optionsLookup.TryGetValue(pawn.Pawn.def, out result)) {
                result = InitializeHealthOptions(pawn.Pawn.def);
                optionsLookup.Add(pawn.Pawn.def, result);
            }
            return result;
        }
        protected OptionsHealth InitializeHealthOptions(ThingDef pawnThingDef) {
            OptionsHealth result = new OptionsHealth();
            BodyDef bodyDef = pawnThingDef.race.body;
            result.BodyDef = bodyDef;
            
            HashSet<UniqueBodyPart> ancestors = new HashSet<UniqueBodyPart>();
            ProcessBodyPart(result, bodyDef.corePart, 1, ancestors);

            InitializeImplantRecipes(result, pawnThingDef);
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
        protected void InitializeImplantRecipes(OptionsHealth options, ThingDef pawnThingDef) {
            // Find all recipes that replace a body part.
            List<RecipeDef> recipes = new List<RecipeDef>();
            recipes.AddRange(DefDatabase<RecipeDef>.AllDefs.Where((RecipeDef def) => {
                if (def.addsHediff != null && def.appliedOnFixedBodyParts != null && def.appliedOnFixedBodyParts.Count > 0
                        && (def.recipeUsers.NullOrEmpty() || def.recipeUsers.Contains(pawnThingDef))) {
                    return true;
                }
                else {
                    return false;
                }
            }));
            
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
                    List<UniqueBodyPart> validBodyParts = options.FindBodyPartsForDef(bodyPartDef);
                    if (validBodyParts != null && validBodyParts.Count > 0) {
                        options.AddImplantRecipe(r, validBodyParts);
                        foreach (var part in validBodyParts) {
                            part.Replaceable = true;
                        }
                    }
                }
            }
        }
        protected void InitializeHediffGiverInjuries(OptionsHealth options, HediffGiver giver) {
            if (giver == null) {
                Logger.Warning("Could not add injury/health condition because a HediffGiver was null");
                return;
            }
            if (giver.hediff == null) {
                Logger.Warning("Could not add injury/health condition because the hediff for " + giver.GetType().FullName + " was null");
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
                    List<UniqueBodyPart> parts = options.FindBodyPartsForDef(def);
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

            // Get all of the hediffs that can be added via the "forced hediff" scenario part and
            // add them to a hash set so that we can quickly look them up.
            ScenPart_ForcedHediff scenPart = new ScenPart_ForcedHediff();
            IEnumerable<HediffDef> scenPartDefs = Reflection.ScenPart_ForcedHediff.PossibleHediffs(scenPart);
            HashSet<HediffDef> scenPartDefSet = new HashSet<HediffDef>(scenPartDefs);
            
            // Add injury options.
            foreach (var hd in DefDatabase<HediffDef>.AllDefs) {
                try {
                    // TODO: Missing body part seems to be a special case.  The hediff giver doesn't itself remove
                    // limbs, so disable it until we can add special-case handling.
                    if (hd.defName == "MissingBodyPart") {
                        continue;
                    }
                    // Filter out defs that were already added via the hediff giver sets.
                    if (addedDefs.Contains(hd)) {
                        continue;
                    }
                    // Filter out implants.
                    if (hd.hediffClass != null && typeof(Hediff_Implant).IsAssignableFrom(hd.hediffClass)) {
                        continue;
                    }

                    // If it's an old injury, use the old injury properties to get the label.
                    HediffCompProperties p = hd.CompPropsFor(typeof(HediffComp_GetsPermanent));
                    HediffCompProperties_GetsPermanent getsPermanentProperties = p as HediffCompProperties_GetsPermanent;
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
                    if (getsPermanentProperties != null) {
                        option.IsOldInjury = true;
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
        }
    }
}
